/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Modules;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.IO {

    /// <summary>
    /// Implements data file I/O - can only be used for retrieval of folders with YetaWF data files.
    /// </summary>
    /// <remarks>This data provider always accesses the permanent file system using the file system provided by YetaWF.Core.IO.FileSystem.FileSystemProvider.
    ///
    /// This is intended for framework use to manage data files.
    /// </remarks>
    public static class DataFilesProvider {

        /// <summary>
        /// Returns a collection of files (full path) found in the specified folder <paramref name="baseFolder"/>.
        /// </summary>
        /// <param name="baseFolder">The folder.</param>
        /// <returns>Returns a collection of files (full path) found in the specified folder <paramref name="baseFolder"/>.</returns>
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
        /// Removes all the files in the specified folder <paramref name="baseFolder"/>.
        /// Ignores any errors.
        /// </summary>
        /// <param name="baseFolder">The folder.</param>
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
    /// Implements YetaWF data file I/O for an object of type <typeparamref name="TObj"/> and uses shared caching.
    /// </summary>
    /// <typeparam name="TObj">The object type.</typeparam>
    /// <remarks>
    /// This is intended for framework use to manage data files.
    ///
    /// Data is serialized when saved and deserialized when loaded.
    /// </remarks>
    public class FileData<TObj> {

        /// <summary>
        /// Constructor.
        /// </summary>
        public FileData() {
            Format = GeneralFormatter.Style.Simple; // the preferred format
        }

        /// <summary>
        /// The full path of the folder where the file(s) is/are stored.
        /// </summary>
        public string BaseFolder { get; set; } = null!;
        /// <summary>
        /// The file name.
        /// </summary>
        public string FileName { get; set; } = null!;
        /// <summary>
        /// The date/timestamp.
        /// </summary>
        public DateTime? Date { get; set; } // file save/load date
        /// <summary>
        /// The format used to serialize the data.
        /// </summary>
        public GeneralFormatter.Style Format { get; set; }
        /// <summary>
        /// Defines whether the data should be cached.
        /// </summary>
        public bool Cacheable { get; set; }
        /// <summary>
        /// The cache key used.
        /// </summary>
        public string CacheKey { // Cache key used to cache the file
            get { return $"folder__{BaseFolder}__{FileName}"; }
        }

        /// <summary>
        /// Loads a file, returns a new instance of the object.
        /// </summary>
        /// <returns>Returns the data.</returns>
        public async Task<TObj?> LoadAsync() {
            object? data = null;
            GetObjectInfo<TObj>? info = null;
            if (Cacheable) {
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
                if (Cacheable) {
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
                    data = await io.LoadAsync();
                }
                Date = (data != null) ? io.Date : null;
            } else {
                data = info.Data;
            }
            if (data != null) {
                if (data.GetType() == typeof(TObj) || typeof(TObj) == typeof(ModuleDefinition)) { // type must match exactly or be for generic module
                    try {
                        return (TObj)data;
                    } catch (Exception) { }
                }
            }
            return default(TObj);
        }
        /// <summary>
        /// Updates the object in an existing file.
        /// The file may be renamed at the same time
        /// </summary>
        /// <param name="data">The data to save.</param>
        /// <param name="newKey">The new file name.</param>
        /// <returns>Returns a status indicator.</returns>
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
        /// Adds an new file.
        /// </summary>
        /// <param name="data">The data to save.</param>
        /// <returns>Returns true if the file was added, false if the file already exists.</returns>
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
        /// Removes the file (with locking). Fails if the file doesn't exist.
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
        /// Removes the file (without locking). Fails if the file doesn't exist.
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
        /// Removes the file (with locking).
        /// </summary>
        /// <returns>Returns true if the file was removed, false otherwise.</returns>
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
        /// <returns>Returns true if the file exists, false otherwise.</returns>
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
