/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;

namespace YetaWF.Core.Support.Zip {

    /// <summary>
    /// Class describing a ZIP file. A thin layer around the actual Zip file implementation (SharpZipLib)
    /// </summary>
    public class YetaWFZipFile : IDisposable {
        /// <summary>
        /// The file name (without path) of the ZIP archive.
        /// </summary>
        public string FileName { get; set; } = null!;
        /// <summary>
        /// Temporary files referenced by the ZIP archive when creating a ZIP archive. These are automatically removed when the YetaWFZipFile object is disposed.
        /// </summary>
        public List<string> TempFiles { get; set; }

        /// <summary>
        /// Temporary folders referenced by the ZIP archive when creating a ZIP archive. These are automatically removed when the YetaWFZipFile object is disposed.
        /// </summary>
        public List<string> TempFolders { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public YetaWFZipFile() {
            TempFiles = new List<string>();
            TempFolders = new List<string>();
            Entries = new List<YetaWFZipEntry>();
            DisposableTracker.AddObject(this);
        }

        /// <summary>
        /// Performs cleanup of temporary files and folders (TempFiles, TempFolders).
        /// </summary>
        public void Dispose() { Dispose(true); }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                DisposableTracker.RemoveObject(this);
            }
        }
        public async Task CleanupFoldersAsync() {
            foreach (var tempFile in TempFiles) {
                try {
                    await FileSystem.FileSystemProvider.DeleteFileAsync(tempFile);
                } catch (Exception) { }
            }
            TempFiles = new List<string>();

            foreach (var tempFolder in TempFolders) {
                try {
                    await FileSystem.FileSystemProvider.DeleteDirectoryAsync(tempFolder);
                } catch (Exception) { }
            }
            TempFolders = new List<string>();
        }

        public void AddFile(string absFileName, string fileName) {
            Entries.Add(new YetaWFZipEntry {
                AbsoluteFileName = absFileName,
                RelativeName = CleanFileName(fileName),
            });
        }
        public static string CleanFileName(string fileName) {
            fileName = fileName.Replace("\\", "/");
            if (fileName.StartsWith("/"))
                fileName = fileName.Substring(1);
            return fileName;
        }
        public void AddData(string data, string fileName) {
            Entries.Add(new YetaWFZipEntry {
                Data = data,
                RelativeName = fileName,
            });
        }
        public async Task AddFolderAsync(string tempFolder) {
            List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(tempFolder);
            foreach (string file in files)
                AddFile(file, Path.GetFileName(file));
        }
        public async Task SaveAsync(string file) {
            await using (IFileStream fs = await FileSystem.FileSystemProvider.CreateFileStreamAsync(file)) {
                await SaveAsync(fs.GetFileStream());
            }
            await CleanupFoldersAsync();
        }
        public async Task SaveAsync(Stream stream) {
            // add all files
            using (ZipOutputStream zipStream = new ZipOutputStream(stream)) {
                zipStream.IsStreamOwner = false;
                foreach (YetaWFZipEntry entry in this.Entries) {
                    if (entry.Data != null)
                        WriteData(zipStream, entry.Data, entry.RelativeName);
                    else
                        await WriteFileAsync(zipStream, entry.AbsoluteFileName, entry.RelativeName);
                }
            }
            stream.Position = 0;
        }

        private void WriteData(ZipOutputStream zipStream, string data, string relativeName) {
            ZipEntry newEntry = new ZipEntry(relativeName);

            using (MemoryStream ms = new MemoryStream()) {
                // create a memory stream from the string
                StreamWriter writer = new StreamWriter(ms, System.Text.Encoding.ASCII);
                writer.Write(data);
                writer.Flush();
                ms.Position = 0;

                newEntry.Size = ms.Length;
                zipStream.PutNextEntry(newEntry);

                byte[] buffer = new byte[4096];
                StreamUtils.Copy(ms, zipStream, buffer);
            }
        }

        private async Task WriteFileAsync(ZipOutputStream zipStream, string absoluteFileName, string relativeName) {
            await using (IFileStream fs = await FileSystem.FileSystemProvider.OpenFileStreamAsync(absoluteFileName)) {
                DateTime lastWrite = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(absoluteFileName);
                ZipEntry newEntry = new ZipEntry(relativeName);
                newEntry.DateTime = lastWrite;
                newEntry.Size = fs.GetLength();
                zipStream.PutNextEntry(newEntry);

                byte[] buffer = new byte[4096];
                StreamUtils.Copy(fs.GetFileStream(), zipStream, buffer);
            }
        }

        /// <summary>
        /// Zip entries referenced by the ZIP archive when creating a ZIP archive.
        /// </summary>
        public List<YetaWFZipEntry> Entries { get; set; }

        public class YetaWFZipEntry {
            public string RelativeName { get; set; } = null!;
            public string AbsoluteFileName { get; set; } = null!;
            public string? Data { get; set; }
        }
    }
}
