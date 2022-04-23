/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.IO {

    /// <summary>
    /// Implements file I/O for an object of type TObj.
    /// </summary>
    public class FileIO<TObj> {

        public FileIO() {
#if DEBUG
            Format = GeneralFormatter.Style.Simple; // the preferred format - change for debugging if desired
#else
            Format = GeneralFormatter.Style.Simple;
#endif
        }

        public string BaseFolder { get; set; } = null!; // The full path of the folder where the file(s) is/are stored
        public string FileName { get; set; } = null!;
        public DateTime? Date { get; set; } // file save/load date
        public object? Data { get; set; } // the data saved/loaded

        public GeneralFormatter.Style Format { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected string FullPath {
            get {
                if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
                // sanitize the file name and translate invalid characters
                string file = FileSystem.FileSystemProvider.MakeValidDataFileName(FileName);
                return string.IsNullOrEmpty(FileName) ? BaseFolder : Path.Combine(BaseFolder, file);
            }
        }
        protected string LockName {
            get {
                return "YetaWF##Lock##" + FullPath.ToLower();
            }
        }

        /// <summary>
        /// Loads an object from a file.
        /// </summary>
        public async Task<TObj?> LoadAsync() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            object? data = null;
            try {
                Date = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(FullPath);
            } catch (Exception) { }
            if (typeof(TObj) == typeof(string)) {
                try {
                    data = await FileSystem.FileSystemProvider.ReadAllTextAsync(FullPath);
                } catch (Exception) { }
            } else {
                IFileStream fs;
                if (YetaWFManager.DiagnosticsMode) {
                    if (!await FileSystem.FileSystemProvider.FileExistsAsync(FullPath))
                        return default(TObj);
                }
                try {
                    fs = await FileSystem.FileSystemProvider.OpenFileStreamAsync(FullPath);
                } catch (Exception exc) {
                    if (exc is System.IO.IOException) return default(TObj); // this can happen if we're trying to load properties while serializing the same properties (file in use)
                    if (!(exc is FileNotFoundException || exc is DirectoryNotFoundException)) throw;
                    return default(TObj);
                }
                byte[] btes = new byte[fs.GetLength()];
                await fs.ReadAsync(btes, 0, (int)fs.GetLength());
                await fs.CloseAsync();
                try {
                    data = new GeneralFormatter(Format).Deserialize<TObj>(btes);
                } catch (Exception) {
                    data = null;
                }

            }
            if (data != null) {
                try {
                    Data = (TObj)data;
                    return (TObj)data;
                } catch (Exception) { }
            }
            Data = null;
            return default(TObj);
        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        public async Task<bool> SaveAsync(bool replace = true) {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            if (Data == null) throw new InternalError("no data");

            await FileSystem.FileSystemProvider.CreateDirectoryAsync(BaseFolder);

            if (!replace && await FileSystem.FileSystemProvider.FileExistsAsync(FullPath))
                return false;

            if (typeof(TObj) == typeof(string)) {
                await FileSystem.FileSystemProvider.WriteAllTextAsync(FullPath, (string)Data);
            } else {
                using (IFileStream fs = await FileSystem.FileSystemProvider.CreateFileStreamAsync(FullPath)) {
                    new GeneralFormatter(Format).Serialize(fs.GetFileStream(), Data);
                    await fs.CloseAsync();
                }
            }
            if (Date != null)
                await FileSystem.FileSystemProvider.SetLastWriteTimeLocalAsync(FullPath, ((DateTime)Date).ToLocalTime());
            return true;
        }

        /// <summary>
        /// Removes the file.
        /// Throws an error if the file does not exist.
        /// </summary>
        public async Task RemoveAsync() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            await FileSystem.FileSystemProvider.DeleteFileAsync(FullPath);
        }
        /// <summary>
        /// Removes the file.
        /// Ignores any errors.
        /// </summary>
        public async Task<bool> TryRemoveAsync() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            bool success = false;
            if (await FileSystem.FileSystemProvider.FileExistsAsync(FullPath)) {
                await FileSystem.FileSystemProvider.DeleteFileAsync(FullPath);
                success = true;
            }
            return success;
        }

        /// <summary>
        /// Test whether the file exists.
        /// </summary>
        public async Task<bool> ExistsAsync() {
            if (string.IsNullOrEmpty(BaseFolder)) throw new InternalError("BaseFolder is empty");
            return await FileSystem.FileSystemProvider.FileExistsAsync(FullPath);
        }
    }
}
