/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Net;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif

namespace YetaWF.Core.Upload {
    public class FileUpload {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const string TempId = "temp";

        // UPLOAD
        // UPLOAD
        // UPLOAD

        /// <summary>
        /// Returns whether a file name (no path) is a temporary file (based on naming conventions).
        /// </summary>
        /// <param name="file">The file name (no path)</param>
        /// <returns>true if the file is a temporary file.</returns>
        public static bool IsUploadedFile(string file) {
            string tempFile = Path.Combine(Manager.SiteFolder, Globals.TempFiles);
            return file.StartsWith(tempFile, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Saves an uploaded package file as a temporary file.
        /// </summary>
        /// <param name="uploadFile">Package file being uploaded.</param>
        /// <returns>A file name (no path) of the uploaded file in the site's temporary folder.</returns>
#if MVC6
        public string StoreTempPackageFile(IFormFile uploadFile) {
#else
        public string StoreTempPackageFile(HttpPostedFileBase uploadFile) {
#endif
            string name = StoreTempFile(uploadFile, MimeSection.PackageUse);
            return GetTempFilePathFromName(name);
        }

        /// <summary>
        /// Saves an uploaded image file as a temporary file.
        /// </summary>
        /// <param name="uploadFile">Image file being uploaded.</param>
        /// <returns>A file name (no path) of the uploaded file in the site's temporary folder.</returns>
#if MVC6
        public string StoreTempImageFile(IFormFile uploadFile) {
#else
        public string StoreTempImageFile(HttpPostedFileBase uploadFile) {
#endif
            return StoreTempFile(uploadFile, MimeSection.ImageUse);
        }

        /// <summary>
        /// Saves an uploaded file as a temporary file.
        /// </summary>
        /// <param name="uploadFile">File being uploaded.</param>
        /// <param name="canUse"></param>
        /// <returns>A file name (with path) of the uploaded file in the site's temporary folder.</returns>
#if MVC6
        public string StoreTempFile(IFormFile uploadFile, string useType) {
#else
        public string StoreTempFile(HttpPostedFileBase uploadFile, string useType) {
#endif
            return StoreFile(uploadFile, Globals.TempFiles, useType,
                (uf => {
                    string tempExt = Path.GetExtension(uf.FileName);
                    string name = TempId + Guid.NewGuid().ToString();
                    return Path.ChangeExtension(name, tempExt);
                })
            );
        }

        /// <summary>
        /// Saves an uploaded file as a file.
        /// </summary>
        /// <param name="uploadFile">File being uploaded.</param>
        /// <param name="folder"></param>
        /// <param name="canUse"></param>
        /// <param name="getFileName"></param>
        /// <returns>A file name (with path) of the uploaded file in the specified folder</returns>

#if MVC6
        public string StoreFile(IFormFile uploadFile, string folder, string useType, Func<IFormFile, string> getFileName)
#else
        public string StoreFile(HttpPostedFileBase uploadFile, string folder, string useType, Func<HttpPostedFileBase, string> getFileName)
#endif
        {
            long fileLength = 0;
            if (uploadFile != null) {
#if MVC6
                fileLength = uploadFile.Length;
#else
                fileLength = uploadFile.ContentLength;
#endif
            }
            if (fileLength == 0)
                throw new InternalError("Can't upload an empty file");

            MimeSection mimeSection = new MimeSection();
            if (!mimeSection.CanUse(uploadFile.ContentType, useType))
                throw new Error(this.__ResStr("errPkgType", "Upload not allowed - The file type '{0}' is not an allowable file type"), uploadFile.ContentType);

            string name = getFileName(uploadFile);

            folder = Path.Combine(Manager.SiteFolder, folder);
            Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, name);
#if MVC6
            using (FileStream fileStream = File.Create(filePath)) {
                uploadFile.CopyTo(fileStream);
            }
#else
            uploadFile.SaveAs(filePath);
#endif
            return Path.GetFileName(name);
        }

        // DOWNLOAD
        // DOWNLOAD
        // DOWNLOAD

        /// <summary>
        /// Saves a remote package file as a temporary file.
        /// </summary>
        /// <param name="remoteUrl">Package file being downloaded.</param>
        /// <returns>A file name (no path) of the downloaded file in the site's temporary folder.</returns>
        public string StoreTempPackageFile(string remoteUrl) {
            string name = StoreTempFile(remoteUrl, MimeSection.PackageUse);
            return GetTempFilePathFromName(name);
        }
        /// <summary>
        /// Saves a remote file as a temporary file.
        /// </summary>
        /// <param name="remoteUrl">File being downloaded.</param>
        /// <param name="canUse"></param>
        /// <returns>A file name (with path) of the downloaded file in the site's temporary folder.</returns>
        private string StoreTempFile(string remoteUrl, string useType) {
            return StoreFile(remoteUrl, Globals.TempFiles, useType,
                (uf => {
                    string tempExt = Path.GetExtension(remoteUrl);
                    string name = TempId + Guid.NewGuid().ToString();
                    return Path.ChangeExtension(name, tempExt);
                })
            );
        }
        private string StoreFile(string remoteUrl, string folder, string useType, Func<string, string> getFileName) {

            MimeSection mimeSection = new MimeSection();

            const string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            HttpWebRequest req = null;
            HttpWebResponse resp = null;

            try {
                req = (HttpWebRequest)WebRequest.Create(remoteUrl);
                req.UserAgent = UserAgent;
                resp = (HttpWebResponse)req.GetResponse();
            } catch (Exception ex) {
                throw new Error(this.__ResStr("cantDownload", "File {0} cannot be downloaded: {1}", remoteUrl, ex.Message));
            }
            if (!mimeSection.CanUse(resp.ContentType, useType)) {
                string contentType = resp.ContentType;
                resp.Close();
                throw new Error(this.__ResStr("errDownloadPkgType", "Download not allowed - The file type '{0}' is not an allowable file type"), contentType);
            }

            string name = getFileName(remoteUrl);

            folder = Path.Combine(Manager.SiteFolder, folder);
            Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, name);

            using (Stream strm = resp.GetResponseStream()) {
                int totlen = (int)resp.ContentLength;
                byte[] bts = new byte[totlen];
                using (FileStream fs = System.IO.File.Create(filePath, totlen, FileOptions.SequentialScan)) {
                    int remlen = totlen;
                    for (int offset = 0 ; remlen > 0 ;) {
                        int nRead = strm.Read(bts, offset, remlen);
                        if (nRead == 0)
                            break;// shouldn't happen
                        fs.Write(bts, offset, nRead);
                        offset += nRead;
                        remlen -= nRead;
                    }
                    fs.Flush();
                }
            }
            if (resp != null)
                resp.Close();

            return Path.GetFileName(name);
        }

        // HELPERS
        // HELPERS
        // HELPERS

        public void RemoveTempFile(string tempName) {
            string tempFilePath = GetTempFilePathFromName(tempName);
            if (tempFilePath == null) return;
            try {
                File.Delete(tempFilePath);
            } catch (Exception) { }
        }

        public void RemoveFile(string name, string folder) {
            string filePath = Path.Combine(Manager.SiteFolder, folder, name);
            if (filePath == null) return;
            try {
                File.Delete(filePath);
            } catch (Exception) { }
        }

        public string GetTempFilePathFromName(string name, string location = null) {
            if (!string.IsNullOrWhiteSpace(name) && name.StartsWith(FileUpload.TempId)) {
                string tempFilePath;
                if (string.IsNullOrWhiteSpace(location))
                    tempFilePath = Path.Combine(Manager.SiteFolder, Globals.TempFiles, name);
                else
                    tempFilePath = Path.Combine(Manager.SiteFolder, Globals.TempFiles, location, name);
                if (File.Exists(tempFilePath))
                    return tempFilePath;
            }
            return null;
        }
        public bool IsTempName(string name) {
            return (!string.IsNullOrWhiteSpace(name) && name.StartsWith(FileUpload.TempId));
        }
        public System.Drawing.Image GetImageFromTempName(string name) {
            string fileName = GetTempFilePathFromName(name);
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            return System.Drawing.Image.FromFile(fileName);
        }
        public byte[] GetImageBytesFromTempName(string name) {
            byte[] bytes;
            using (System.Drawing.Image image = GetImageFromTempName(name)) {
                if (image == null) return null;
                using (MemoryStream ms = new MemoryStream()) {
                    image.Save(ms, image.RawFormat);
                    bytes = ms.GetBuffer();
                }
            }
            return bytes;
        }
        public void RemoveAllExpiredTempFiles(TimeSpan timeSpan) {
            // remove all temp files on all sites that are older than "timeSpan"
            string[] siteFolders = Directory.GetDirectories(YetaWFManager.RootSitesFolder);
            foreach (var siteFolder in siteFolders) {
                Logging.AddLog("Removing temp files in {0}", siteFolder);
                string tempFolder = Path.Combine(siteFolder, Globals.TempFiles);
                RemoveExpiredTempFiles(tempFolder, timeSpan);
            }
        }
        private void RemoveExpiredTempFiles(string tempFolder, TimeSpan timeSpan) {
            if (File.Exists(tempFolder)) {
                string[] tempFiles = Directory.GetFiles(tempFolder);
                DateTime oldest = DateTime.UtcNow.Subtract(timeSpan);
                foreach (var tempFile in tempFiles) {
                    if (Path.GetFileName(tempFile).StartsWith(TempId)) {
                        DateTime created = File.GetCreationTimeUtc(tempFile);
                        if (created < oldest) {
                            Logging.AddLog("Removing temp file {0}", tempFile);
                            File.Delete(tempFile);
                        }
                    }
                }
            }
        }
    }
}
