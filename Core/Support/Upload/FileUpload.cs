/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.Upload {

    public class FileUpload {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const string TempId = "temp";

        public static string TempUploadFolder { get { return Path.Combine(FileSystem.TempFileSystemProvider.RequiredRootFolder, "Uploads"); } }
        public static string TempSiteUploadFolder { get { return Path.Combine(FileSystem.TempFileSystemProvider.RequiredRootFolder, "Uploads", Manager.CurrentSite.Identity.ToString()); } }

        private static readonly HttpClientHandler Handler = new HttpClientHandler {
            AllowAutoRedirect = true,
            UseCookies = false,
        };
        private static readonly HttpClient Client = new HttpClient(Handler, true) {
            Timeout = new TimeSpan(0, 1, 0),
        };

        // UPLOAD
        // UPLOAD
        // UPLOAD

        /// <summary>
        /// Returns whether a file name (with path) is a temporary file (based on naming conventions).
        /// </summary>
        /// <param name="file">The file name (with path).</param>
        /// <returns>Returns true if the file is a temporary file.</returns>
        public static bool IsUploadedFile(string file) {
            return file.StartsWith(TempSiteUploadFolder, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Saves an uploaded package file as a temporary file.
        /// </summary>
        /// <param name="uploadFile">Package file being uploaded.</param>
        /// <returns>Returns a file name (with path) of the uploaded file in the site's temporary folder.</returns>
        public async Task<string> StoreTempPackageFileAsync(IFormFile uploadFile) {
            string name = await StoreFileAsync(uploadFile, null, MimeSection.PackageUse, TempFile: true);
            return await GetTempFilePathFromNameAsync(name) ?? throw new InternalError($"{nameof(StoreTempPackageFileAsync)} expected temp file name");
        }
        /// <summary>
        /// Saves a remote package file as a temporary file.
        /// </summary>
        /// <param name="remoteUrl">Remote URL of a package file being uploaded.</param>
        /// <returns>Returns a file name (with path) of the uploaded file in the site's temporary folder.</returns>
        public async Task<string> StoreTempPackageFileAsync(string remoteUrl) {
            string name = await StoreTempFileAsync(remoteUrl, MimeSection.PackageUse);
            return await GetTempFilePathFromNameAsync(name) ?? throw new InternalError($"{nameof(StoreTempPackageFileAsync)} expected temp file name");
        }

        /// <summary>
        /// Saves an uploaded image file as a temporary file.
        /// </summary>
        /// <param name="uploadFile">Image file being uploaded.</param>
        /// <returns>A file name (no path) of the uploaded file.</returns>
        public async Task<string> StoreTempImageFileAsync(IFormFile uploadFile) {
            return await StoreFileAsync(uploadFile, null, MimeSection.ImageUse, TempFile: true);
        }

        /// <summary>
        /// Saves an uploaded file as a file in the permanent or temporary file system.
        /// </summary>
        /// <param name="uploadFile">File being uploaded.</param>
        /// <param name="folder">The folder (absolute path) where the file is saved. Must be null for temporary files.</param>
        /// <param name="canUse"></param>
        /// <returns>Returns a file name (without path) of the uploaded file in the specified folder.</returns>
        public async Task<string> StoreFileAsync(IFormFile uploadFile, string? folder, string useType, bool TempFile = false) {
            if (TempFile && folder != null)
                throw new InternalError("Can't provide folder for temporary files");
            if (!TempFile && folder == null)
                throw new InternalError("Must provide folder for permanent files");

            long fileLength = 0;
            if (uploadFile != null) {
                fileLength = uploadFile.Length;
            }
            if (fileLength == 0)
                throw new InternalError("Can't upload an empty file");

            MimeSection mimeSection = new MimeSection();
            if (!mimeSection.CanUse(uploadFile!.ContentType, useType))
                throw new Error(this.__ResStr("errPkgType", "Upload not allowed - The file type '{0}' is not an allowable file type"), uploadFile.ContentType);

            string fileName = Path.GetFileName(uploadFile.FileName);

            if (TempFile) {

                string tempExt = Path.GetExtension(fileName);
                fileName = TempId + Guid.NewGuid().ToString();
                fileName = Path.ChangeExtension(fileName, tempExt);

                await FileSystem.TempFileSystemProvider.CreateDirectoryAsync(TempSiteUploadFolder);
                string filePath = Path.Combine(TempSiteUploadFolder, fileName);

                using (IFileStream fileStream = await FileSystem.FileSystemProvider.CreateFileStreamAsync(filePath)) {
                    if (YetaWFManager.IsSync()) {
                        uploadFile.CopyTo(fileStream.GetFileStream());
                    } else {
                        await uploadFile.CopyToAsync(fileStream.GetFileStream());
                    }
                }
            } else {

                await FileSystem.FileSystemProvider.CreateDirectoryAsync(folder!);
                string filePath = Path.Combine(folder!, fileName);
                using (IFileStream fileStream = await FileSystem.FileSystemProvider.CreateFileStreamAsync(filePath)) {
                    if (YetaWFManager.IsSync()) {
                        uploadFile.CopyTo(fileStream.GetFileStream());
                    } else {
                        await uploadFile.CopyToAsync(fileStream.GetFileStream());
                    }
                }
            }
            return fileName;
        }

        /// <summary>
        /// Downloads a remote file and saves it as an uploaded file in the temporary file system.
        /// </summary>
        /// <param name="remoteUrl"></param>
        /// <param name="folder"></param>
        /// <param name="useType"></param>
        /// <returns>Returns the file name (without path).</returns>
        private async Task<string> StoreTempFileAsync(string remoteUrl, string useType) {

            MimeSection mimeSection = new MimeSection();

            const string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            HttpResponseMessage? resp = null;

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, remoteUrl)) {
                    request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                    if (YetaWFManager.IsSync()) {
                        resp = Client.Send(request);
                    } else {
                        resp = await Client.SendAsync(request);
                    }
                }
            } catch (Exception ex) {
                throw new Error(this.__ResStr("cantDownload", "File {0} cannot be downloaded: {1}", remoteUrl, ErrorHandling.FormatExceptionMessage(ex)));
            }

            string? contentType = resp.Content.Headers.ContentType?.MediaType;
            if (!mimeSection.CanUse(contentType, useType)) {
                resp.Dispose();
                throw new Error(this.__ResStr("errDownloadPkgType", "Download not allowed - The file type '{0}' is not an allowable file type"), contentType);
            }

            string tempExt = Path.GetExtension(remoteUrl);
            string name = TempId + Guid.NewGuid().ToString();
            name = Path.ChangeExtension(name, tempExt);

            await FileSystem.TempFileSystemProvider.CreateDirectoryAsync(TempSiteUploadFolder);
            string filePath = Path.Combine(TempSiteUploadFolder, name);

            Stream strm;
            if (YetaWFManager.IsSync()) {
                strm = resp.Content.ReadAsStream();
            } else {
                strm = await resp.Content.ReadAsStreamAsync();
            }
            using (strm) {
                int totlen = (int)strm.Length;
                byte[] bts = new byte[totlen];
                using (IFileStream fs = await FileSystem.TempFileSystemProvider.CreateFileStreamAsync(filePath)) {
                    int remlen = totlen;
                    for (int offset = 0; remlen > 0;) {
                        int nRead;
                        if (YetaWFManager.IsSync())
                            nRead = strm.Read(bts, offset, remlen);
                        else
                            nRead = await strm.ReadAsync(bts, offset, remlen);
                        if (nRead == 0)
                            break;// shouldn't happen
                        await fs.WriteAsync(bts, offset, nRead);
                        offset += nRead;
                        remlen -= nRead;
                    }
                    await fs.FlushAsync();
                }
            }

            resp.Dispose();

            return Path.GetFileName(name);
        }

        // HELPERS
        // HELPERS
        // HELPERS

        public async Task RemoveTempFileAsync(string tempName) {
            string? tempFilePath = await GetTempFilePathFromNameAsync(tempName);
            if (tempFilePath == null) return;
            try {
                await FileSystem.TempFileSystemProvider.DeleteFileAsync(tempFilePath);
            } catch (Exception) { }
        }

        public async Task RemoveFileAsync(string name, string folder) {
            string filePath = Path.Combine(Manager.SiteFolder, folder, name);
            if (filePath == null) return;
            try {
                await FileSystem.FileSystemProvider.DeleteFileAsync(filePath);
            } catch (Exception) { }
        }

        public async Task<string?> GetTempFilePathFromNameAsync(string? name, string? location = null) {
            if (!string.IsNullOrWhiteSpace(name) && name.StartsWith(FileUpload.TempId)) {
                string tempFilePath;
                if (string.IsNullOrWhiteSpace(location))
                    tempFilePath = Path.Combine(TempSiteUploadFolder, name);
                else
                    tempFilePath = Path.Combine(TempSiteUploadFolder, location, name);
                if (await FileSystem.TempFileSystemProvider.FileExistsAsync(tempFilePath))
                    return tempFilePath;
            }
            return null;
        }
        public bool IsTempName(string name) {
            return (!string.IsNullOrWhiteSpace(name) && name.StartsWith(FileUpload.TempId));
        }
        public async Task<System.Drawing.Image?> GetImageFromTempNameAsync(string name) {
            string? fileName = await GetTempFilePathFromNameAsync(name);
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            return System.Drawing.Image.FromFile(fileName);
        }
        public async Task<byte[]?> GetImageBytesFromTempNameAsync(string name) {
            byte[] bytes;
            using (System.Drawing.Image? image = await GetImageFromTempNameAsync(name)) {
                if (image == null) return null;
                using (MemoryStream ms = new MemoryStream()) {
                    image.Save(ms, image.RawFormat);
                    bytes = ms.GetBuffer();
                }
            }
            return bytes;
        }
        public async Task RemoveAllExpiredTempFilesAsync(TimeSpan timeSpan) {
            // remove all temp files on all sites that are older than "timeSpan"
            if (!await FileSystem.TempFileSystemProvider.DirectoryExistsAsync(TempUploadFolder)) return;
            List<string> siteFolders = await FileSystem.TempFileSystemProvider.GetDirectoriesAsync(TempUploadFolder);
            foreach (var siteFolder in siteFolders) {
                Logging.AddLog("Removing temp files in {0}", siteFolder);
                await RemoveExpiredTempFilesAsync(siteFolder, timeSpan);
            }
        }
        private async Task RemoveExpiredTempFilesAsync(string tempFolder, TimeSpan timeSpan) {
            if (await FileSystem.TempFileSystemProvider.DirectoryExistsAsync(tempFolder)) {
                List<string> tempFiles = await FileSystem.TempFileSystemProvider.GetFilesAsync(tempFolder);
                DateTime oldest = DateTime.UtcNow.Subtract(timeSpan);
                foreach (var tempFile in tempFiles) {
                    if (Path.GetFileName(tempFile).StartsWith(TempId)) {
                        DateTime created = await FileSystem.TempFileSystemProvider.GetCreationTimeUtcAsync(tempFile);
                        if (created < oldest) {
                            Logging.AddLog("Removing temp file {0}", tempFile);
                            await FileSystem.TempFileSystemProvider.DeleteFileAsync(tempFile);
                        }
                    }
                }
            }
        }
    }
}
