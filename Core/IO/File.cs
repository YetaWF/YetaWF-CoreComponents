/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using YetaWF.Core.DataProvider;

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
        private static string FileExtension = ".dat";

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
        public List<string> GetNames() {
            List<string> files = new List<string>();
            try {
                files = Directory.GetFiles(BaseFolder).ToList<string>();
            } catch { }

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
        public void TryRemoveAll() {
            StringLocks.DoAction(LockKey, () => {
                Debug.Assert(!string.IsNullOrEmpty(BaseFolder));
                if (Directory.Exists(BaseFolder))
                    Directory.Delete(BaseFolder, true);
            });
        }
    }

    /// <summary>
    /// Implements file I/O for an object of type TObj.
    /// Supports caching.
    /// </summary>
    /// <typeparam name="TObj"></typeparam>
    public class FileData<TObj> : CachedObject {

        public FileData() {  }

        public string BaseFolder { get; set; } // The full path of the folder where the file(s) is/are stored
        public string FileName { get; set; }
        public DateTime? Date { get; set; } // file save/load date
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
        public TObj Load() {
            object data = null;
            if (!GetObjectFromCache(CacheKey, out data)) {
                FileIO<TObj> io = new FileIO<TObj> {
                    BaseFolder = BaseFolder,
                    FileName = FileName,
                    Data = data,
                };
                StringLocks.DoAction(LockKey, () =>
                {
                    data = io.Load();
                    if (data != null)
                        AddObjectToCache(CacheKey, data);
                });
                Date = (data!=null) ? io.Date : null;
            }
            return (TObj) (object) data;
        }
        /// <summary>
        /// Update the object in an existing file.
        /// The file may be renamed at the same time
        /// </summary>
        /// <param name="data"></param>
        /// <param name="newKey"></param>
        public UpdateStatusEnum UpdateFile(string newKey, TObj data) {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Data = data,
                Date = Date ?? DateTime.UtcNow,
            };
            UpdateStatusEnum status = UpdateStatusEnum.RecordDeleted;
            if (FileName != newKey) {
                // Rename
                FileIO<TObj> ioNew = new FileIO<TObj> {
                    BaseFolder = BaseFolder,
                    FileName = newKey,
                    Data = data,
                    Date = Date ?? DateTime.UtcNow,
                };
                StringLocks.DoAction(LockKey, () => {
                    if (ioNew.Exists()) {
                        status = UpdateStatusEnum.NewKeyExists;
                        return;
                    }
                    if (!io.Exists()) {
                        status = UpdateStatusEnum.RecordDeleted;
                        return;
                    }
                    // delete the old file (incl. cache etc.)
                    Remove();
                    // save the new file
                    ioNew.Save();
                    FileName = newKey;
                    AddObjectToCache(CacheKey, data);
                    status = UpdateStatusEnum.OK;
                });
            } else {
                // Simple Save
                StringLocks.DoAction(LockKey, () => {
                    if (!io.Exists()) {
                        status = UpdateStatusEnum.RecordDeleted;
                        return;
                    }
                    io.Save();
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
        public bool Add(TObj data) {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName,
                Data = data,
                Date = Date ?? DateTime.UtcNow,
            };
            bool success = true;
            StringLocks.DoAction(LockKey, () => {
                success = io.Save(replace: false);
                if (success)
                    AddObjectToCache(CacheKey, data);
            });
            return success;
        }
        /// <summary>
        /// Remove the file. Fails if the file doesn't exist.
        /// </summary>
        public void Remove() {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName
            };
            StringLocks.DoAction(LockKey, () => {
                io.Remove();
                RemoveFromCache(CacheKey);
            });
        }
        /// <summary>
        /// Remove the file.
        /// </summary>
        public bool TryRemove() {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName
            };
            bool success = false;
            StringLocks.DoAction(LockKey, () => {
                io.TryRemove();
                RemoveFromCache(CacheKey);
                success = true;
            });
            return success;
        }

        /// <summary>
        /// Check if the file exists.
        /// </summary>
        public bool Exists() {
            FileIO<TObj> io = new FileIO<TObj> {
                BaseFolder = BaseFolder,
                FileName = FileName
            };
            bool success = false;
            StringLocks.DoAction(LockKey, () => {
                success = io.Exists();
            });
            return success;
        }
    }
}
