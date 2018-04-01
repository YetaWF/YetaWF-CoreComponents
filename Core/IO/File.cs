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
            using (FileSystem.FileSystemProvider.LockResourceAsync(baseFolder)) {
                if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(baseFolder))
                    await FileSystem.FileSystemProvider.DeleteDirectoryAsync(baseFolder);
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
            get { return string.Format("folder__{0}", BaseFolder); }
        }

        /// <summary>
        /// Load a file, returns a new instance of the object.
        /// </summary>
        /// <returns></returns>
        public async Task<TObj> LoadAsync(bool SpecificType = false) {
            object data = null;
            GetObjectInfo<TObj> info = null;
            if (Cacheable)
                info = await YetaWF.Core.IO.Caching.SharedCacheProvider.GetAsync<TObj>(CacheKey);
            if (!info.Success) {
                FileIO<TObj> io = new FileIO<TObj> {
                    BaseFolder = BaseFolder,
                    FileName = FileName,
                    Data = data,
                    Format = Format,
                };
                if (Cacheable) {
                    using (await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                        data = await io.LoadAsync();
                        if (Cacheable) await YetaWF.Core.IO.Caching.SharedCacheProvider.AddAsync(CacheKey, data);
                    }
                } else {
                    data = await io.LoadAsync();
                }
                Date = (data != null) ? io.Date : null;
            }
            if (SpecificType) {
                if (typeof(TObj) == data.GetType())
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
                using (await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    if (await ioNew.ExistsAsync())
                        return UpdateStatusEnum.NewKeyExists;
                    if (!await io.ExistsAsync())
                        return UpdateStatusEnum.RecordDeleted;
                    // delete the old file (incl. cache etc.)
                    await RemoveAsync();
                    // save the new file
                    await ioNew.SaveAsync();
                    FileName = newKey;
                    if (Cacheable) await YetaWF.Core.IO.Caching.SharedCacheProvider.AddAsync(CacheKey, data);
                    status = UpdateStatusEnum.OK;
                }
            } else {
                // Simple Save
                if (Cacheable) {
                    using (await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                        if (!await io.ExistsAsync()) {
                            status = UpdateStatusEnum.RecordDeleted;
                        } else {
                            await io.SaveAsync();
                            await YetaWF.Core.IO.Caching.SharedCacheProvider.AddAsync(CacheKey, data);
                            status = UpdateStatusEnum.OK;
                        }
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
                using (await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    success = await io.SaveAsync(replace: false);
                    if (success)
                        await YetaWF.Core.IO.Caching.SharedCacheProvider.AddAsync(CacheKey, data); // save locally cached version
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
                using (await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    await io.RemoveAsync();
                    await YetaWF.Core.IO.Caching.SharedCacheProvider.RemoveAsync<TObj>(CacheKey);
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
                using (await FileSystem.FileSystemProvider.LockResourceAsync(Path.Combine(BaseFolder, FileName))) {
                    await io.TryRemoveAsync();
                    await YetaWF.Core.IO.Caching.SharedCacheProvider.RemoveAsync<TObj>(CacheKey);
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
