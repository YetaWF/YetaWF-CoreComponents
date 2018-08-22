/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.IO {

    /// <summary>
    /// Implements data file I/O - can only be used for retrieval of folder with data files.
    /// </summary>
    public static class DataFilesProvider {

        // Retrieves a list of all file names in the base folder
        public static async Task<List<string>> GetDataFileNamesAsync(string baseFolder) {
            List<string> files = new List<string>();
#if DEBUG
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(baseFolder)) {// avoid debug spam
#endif
                try {
                    files = await FileSystem.FileSystemProvider.GetFilesAsync(baseFolder);
                } catch { }
#if DEBUG
            }
#endif
            List<string> names = new List<string>();
            foreach (var file in files) {
                string name = FileSystem.FileSystemProvider.ExtractNameFromDataFileName(Path.GetFileName(file));
                names.Add(name);
            }
            return names;
        }

        /// <summary>
        /// Removes all the files in the folder.
        /// Ignores any errors.
        /// </summary>
        public static async Task RemoveAllDataFilesAsync(string baseFolder) {
            Debug.Assert(!string.IsNullOrEmpty(baseFolder));
            using (ILockObject lockObject = await FileSystem.FileSystemProvider.LockResourceAsync(baseFolder)) {
                if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(baseFolder))
                    await FileSystem.FileSystemProvider.DeleteDirectoryAsync(baseFolder);
                await lockObject.UnlockAsync();
            }
        }
    }

    /// <summary>
    /// Implements data file I/O for an object of type TObj.
    /// Supports shared caching.
    /// </summary>
    /// <typeparam name="TObj"></typeparam>
    public class FileData<TObj> {

        public FileData() {
            Format = GeneralFormatter.Style.Simple; // the preferred format
        }

        public string BaseFolder { get; set; } // The full path of the folder where the file(s) is/are stored
        public string FileName { get; set; }
        public DateTime? Date { get; set; } // file save/load date
        public GeneralFormatter.Style Format { get; set; }
        public bool Cacheable { get; set; }
        public string CacheKey { // Cache key used to cache the file
            get { return $"folder__{BaseFolder}__{FileName}"; }
        }

        /// <summary>
        /// Load a file, returns a new instance of the object.
        /// </summary>
        /// <returns></returns>
        public async Task<TObj> LoadAsync(bool SpecificTypeOnly = false) {
            object data = null;
            GetObjectInfo<TObj> info = null;
            if (Cacheable && !SpecificTypeOnly) {
                using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                    info = await sharedCacheDP.GetAsync<TObj>(CacheKey);
                }
            }
            if (info == null || !info.Success) {
                FileIO<TObj> io = new FileIO<TObj> {
                    BaseFolder = BaseFolder,
                    FileName = FileName,
                    Data = data,
                    Format = Format,
                };
                if (Cacheable && !SpecificTypeOnly) {
                    using (ILockObject lockObject = await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                        data = await io.LoadAsync();
                        if (Cacheable) {
                            using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                                await sharedCacheDP.AddAsync(CacheKey, data);
                            }
                        }
                        await lockObject.UnlockAsync();
                    }
                } else {
                    data = await io.LoadAsync(SpecificTypeOnly: SpecificTypeOnly);
                }
                Date = (data != null) ? io.Date : null;
            } else {
                data = info.Data;
            }
            if (SpecificTypeOnly) {
                if (data != null && typeof(TObj) == data.GetType())
                    return (TObj)data;
                else
                    return default(TObj);
            }
            return (TObj)data;
        }
        /// <summary>
        /// Update the object in an existing file.
        /// The file may be renamed at the same time
        /// </summary>
        /// <param name="data"></param>
        /// <param name="newKey"></param>
        public async Task<UpdateStatusEnum> UpdateFileAsync(string newKey, TObj data) {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Data = data,
                Date = Date ?? DateTime.UtcNow,
                Format = Format,
            };
            UpdateStatusEnum status = UpdateStatusEnum.RecordDeleted;
            if (FileName != newKey) {
                // Rename
                FileIO<TObj> ioNew = new FileIO<TObj> {
                    BaseFolder = BaseFolder,
                    FileName = newKey,
                    Data = data,
                    Date = Date ?? DateTime.UtcNow,
                    Format = Format,
                };
                using (ILockObject lockObject = await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    if (await ioNew.ExistsAsync())
                        return UpdateStatusEnum.NewKeyExists;
                    if (!await io.ExistsAsync())
                        return UpdateStatusEnum.RecordDeleted;
                    // delete the old file (incl. cache etc.)
                    await RemoveNoLockAsync();
                    // save the new file
                    await ioNew.SaveAsync();
                    FileName = newKey;
                    if (Cacheable) {
                        using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                            await sharedCacheDP.AddAsync(CacheKey, data);
                        }
                    }
                    status = UpdateStatusEnum.OK;
                    await lockObject.UnlockAsync();
                }
            } else {
                // Simple Save
                if (Cacheable) {
                    using (ILockObject lockObject = await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                        if (!await io.ExistsAsync()) {
                            status = UpdateStatusEnum.RecordDeleted;
                        } else {
                            await io.SaveAsync();
                            using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                                await sharedCacheDP.AddAsync(CacheKey, data);
                            }
                            status = UpdateStatusEnum.OK;
                        }
                        await lockObject.UnlockAsync();
                    }
                } else {
                    if (!await io.ExistsAsync())
                        return UpdateStatusEnum.RecordDeleted;
                    await io.SaveAsync();
                    return UpdateStatusEnum.OK;
                }
            }
            return status;
        }
        /// <summary>
        /// Add an new file object.
        /// </summary>
        /// <param name="data"></param>
        public async Task<bool> AddAsync(TObj data) {

            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Data = data,
                Date = Date ?? DateTime.UtcNow,
                Format = Format,
            };
            bool success = true;
            if (Cacheable) {
                using (ILockObject lockObject = await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    success = await io.SaveAsync(replace: false);
                    if (success) {
                        using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                            await sharedCacheDP.AddAsync(CacheKey, data); // save locally cached version
                        }
                    }
                    await lockObject.UnlockAsync();
                }
            } else {
                success = await io.SaveAsync(replace: false);
            }
            return success;
        }
        /// <summary>
        /// Remove the file. Fails if the file doesn't exist.
        /// </summary>
        public async Task RemoveAsync() {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Format = Format,
            };
            if (Cacheable) {
                using (ILockObject lockObject = await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    await io.RemoveAsync();
                    using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                        await sharedCacheDP.RemoveAsync<TObj>(CacheKey);
                    }
                    await lockObject.UnlockAsync();
                }
            } else {
                await io.RemoveAsync();
            }
        }
        /// <summary>
        /// Remove the file. Fails if the file doesn't exist.
        /// </summary>
        public async Task RemoveNoLockAsync() {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Format = Format,
            };
            if (Cacheable) {
                await io.RemoveAsync();
                using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                    await sharedCacheDP.RemoveAsync<TObj>(CacheKey);
                }
            } else {
                await io.RemoveAsync();
            }
        }
        /// <summary>
        /// Remove the file.
        /// </summary>
        public async Task<bool> TryRemoveAsync() {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Format = Format,
            };
            if (Cacheable) {
                using (ILockObject lockObject = await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    await io.TryRemoveAsync();
                    using (ICacheDataProvider sharedCacheDP = YetaWF.Core.IO.Caching.GetSharedCacheProvider()) {
                        await sharedCacheDP.RemoveAsync<TObj>(CacheKey);
                    }
                    await lockObject.UnlockAsync();
                }
            } else {
                await io.TryRemoveAsync();
            }
            return true;
        }

        /// <summary>
        /// Check if the file exists.
        /// </summary>
        public async Task<bool> ExistsAsync() {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Format = Format,
            };
            return await io.ExistsAsync();
        }
    }
}
