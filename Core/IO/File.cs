/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Support.Serializers;

#if MVC6
using Microsoft.Extensions.Caching.Memory;
#else
#endif


namespace YetaWF.Core.IO {

    /// <summary>
    /// Implements file I/O - can only be used for folder retrieval
    /// Supports caching.
    /// </summary>
    public class FileData {
        public FileData() { }
        public string BaseFolder { get; set; } // The full path of the folder where the file(s) is/are stored
        public string CacheKey { // Cache key used to cache the file
            get { return string.Format("folder__{0}", BaseFolder); }
        }
        public string LockKey { // I/O lock used for the file
            get { return "YetaWF##" + CacheKey; }
        }

        private static string ValidChars = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789~`!@#$^&()_-+={}[],.";
        public const string FileExtension = ".dat";

        public static string MakeValidFileName(string name) {
            return MakeValidFileSystemFileName(name) + FileExtension;
        }

        public static string MakeValidFileSystemFileName(string name) {
            StringBuilder sb = new StringBuilder();
            foreach (var c in name) {
                if (ValidChars.Contains(c))
                    sb.Append(c);
                else if (c == '%')
                    sb.Append("%%");
                else
                    sb.Append(string.Format("%{0:x2}", (int)c));
            }
            return sb.ToString();
        }

        public static string ExtractNameFromFileName(string name) {
            StringBuilder sb = new StringBuilder();
            int total = name.Length;
            if (name.EndsWith(FileExtension)) total -= FileExtension.Length;
            for (int i = 0 ; i < total ; ++i) {
                char c = name[i];
                if (c == '%') {
                    if (i + 1 < total && name[i + 1] == '%') {
                        sb.Append('%');
                        i += 1;
                    } else if (i + 2 < total) {
                        string hex = name.Substring(i + 1, 2);
                        int value = (int) '*';
                        try {
                            value = Convert.ToInt32(hex, 16);
                        } catch (Exception) { }
                        sb.Append((char) value);
                        i += 2;
                    }
                } else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        // Retrieves a list of all file names in the basefolder
        public async Task<List<string>> GetNamesAsync() {
            List<string> files = new List<string>();
#if DEBUG
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(BaseFolder)) {// avoid debug spam
#endif
                try {
                    files = await FileSystem.FileSystemProvider.GetFilesAsync(BaseFolder);
                } catch { }
#if DEBUG
            }
#endif
            List<string> names = new List<string>();
            foreach (var file in files) {
                string name = ExtractNameFromFileName(Path.GetFileName(file));
                names.Add(name);
            }
            return names;
        }

        /// <summary>
        /// Removes all the files in the folder.
        /// Ignores any errors.
        /// </summary>
        public async Task TryRemoveAllAsync() {
            await StringLocks.DoActionAsync(LockKey, async () => { //$$$$$
                Debug.Assert(!string.IsNullOrEmpty(BaseFolder));
                if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(BaseFolder))
                    await FileSystem.FileSystemProvider.DeleteDirectoryAsync(BaseFolder);
            });
        }
    }

    /// <summary>
    /// Implements file I/O for an object of type TObj.
    /// Supports caching.
    /// </summary>
    /// <typeparam name="TObj"></typeparam>
    public class FileData<TObj> : CachedObject { //$$$Remove cachedObject

        public FileData() {
#if DEBUG
            Format = GeneralFormatter.Style.Simple; // the preferred format - change for debugging if desired
#else
            Format = GeneralFormatter.Style.Simple;
#endif
        }

        public string BaseFolder { get; set; } // The full path of the folder where the file(s) is/are stored
        public string FileName { get; set; }
        public DateTime? Date { get; set; } // file save/load date
        public GeneralFormatter.Style Format { get; set; }
        public string CacheKey { // Cache key used to cache the file
            get { return string.Format("file__{0}_{1}", BaseFolder, FileName); }
        }
        public string LockKey { // I/O lock used for the file
            get { return "YetaWF##" + CacheKey; }
        }

        /// <summary>
        /// Load a file, returns a new instance of the object.
        /// </summary>
        /// <returns></returns>
        public async Task<TObj> LoadAsync(bool SpecificType = false) {
            object data = null;
            if (!GetObjectFromCache(CacheKey, out data)) {
                FileIO<TObj> io = new FileIO<TObj> {
                    BaseFolder = BaseFolder,
                    FileName = FileName,
                    Data = data,
                    Format = Format,
                };
                await StringLocks.DoActionAsync(LockKey, async () => {
                    data = await io.LoadAsync();//$$
                    if (data != null)
                        AddObjectToCache(CacheKey, data);
                });
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
                await StringLocks.DoActionAsync(LockKey, async () => {
                    if (await ioNew.ExistsAsync()) {
                        status = UpdateStatusEnum.NewKeyExists;
                        return;
                    }
                    if (!await io.ExistsAsync()) {
                        status = UpdateStatusEnum.RecordDeleted;
                        return;
                    }
                    // delete the old file (incl. cache etc.)
                    await RemoveAsync();
                    // save the new file
                    await ioNew.SaveAsync();
                    FileName = newKey;
                    AddObjectToCache(CacheKey, data);
                    status = UpdateStatusEnum.OK;
                });
            } else {
                // Simple Save
                await StringLocks.DoActionAsync(LockKey, async () => {
                    if (!await io.ExistsAsync()) {
                        status = UpdateStatusEnum.RecordDeleted;
                        return;
                    }
                    await io.SaveAsync();
                    AddObjectToCache(CacheKey, data);
                    status = UpdateStatusEnum.OK;
                });
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
            await StringLocks.DoActionAsync(LockKey, async () => {
                success = await io.SaveAsync(replace: false);
                if (success)
                    AddObjectToCache(CacheKey, data);
            });
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
            await StringLocks.DoActionAsync(LockKey, async () => {
                await io.RemoveAsync();
                RemoveFromCache(CacheKey);
            });
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
            bool success = false;
            await StringLocks.DoActionAsync(LockKey, async () => {
                await io.TryRemoveAsync();
                RemoveFromCache(CacheKey);
                success = true;
            });
            return success;
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
            bool success = false;
            await StringLocks.DoActionAsync(LockKey, async () => {//$$$$
                success = await io.ExistsAsync();
            });
            return success;
        }
    }
}
