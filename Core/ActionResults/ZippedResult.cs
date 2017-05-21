/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.Addons;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    /// <summary>
    /// Class describing a ZIP file.
    /// </summary>
    public class YetaWFZipFile : IDisposable {
        /// <summary>
        /// The ZIP archive, provided by Ionic.Zip.
        /// </summary>
        public ZipFile Zip { get; set; }
        /// <summary>
        /// The file name (without path) of the ZIP archive.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Temporary files referenced by the ZIP archive when creating a ZIP archive. These are automatically removed when the YetaWFZipFile object is disposed.
        /// </summary>
        public List<string> TempFiles { get; set; }
        /// <summary>
        /// Temporary files referenced by the ZIP archive when creating a ZIP archive. These are automatically removed when the YetaWFZipFile object is disposed.
        /// </summary>
        public List<string> TempFolders { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public YetaWFZipFile() {
            TempFiles = new List<string>();
            TempFolders = new List<string>();
            DisposableTracker.AddObject(this);
        }

        /// <summary>
        /// Performs cleanup of temporary files and folders (TempFiles, TempFolders).
        /// </summary>
        public void Dispose() { Dispose(true); }

        protected virtual void Dispose(bool disposing) {
            if (disposing) { DisposableTracker.RemoveObject(this); }
            if (TempFiles != null) {
                foreach (var tempFile in TempFiles) {
                    try {
                        File.Delete(tempFile);
                    } catch (Exception) { }
                }
                TempFiles = null;
            }
            if (TempFolders != null) {
                foreach (var tempFolder in TempFolders) {
                    try {
                        Directory.Delete(tempFolder, true);
                    } catch (Exception) { }
                }
                TempFolders = null;
            }
        }
    }

    /// <summary>
    /// Used to return a ZIP file from a controller.
    /// </summary>
    /// <remarks>
    /// This action result works in conjunction with javascript in basics.js returning a cookie indicating the file is available.
    /// </remarks>
    public class ZippedFileResult : ActionResult {

        private YetaWFZipFile Zip { get; set; }
        private long CookieToReturn { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="zip">The ZIP file to return.</param>
        /// <param name="cookieToReturn">The cookie to return, indicating that the ZIP file has been downloaded. Works in conjunction with client-side code in basics.js.</param>
        public ZippedFileResult(YetaWFZipFile zip, long cookieToReturn) {
            this.Zip = zip;
            this.CookieToReturn = cookieToReturn;
        }

        /// <summary>
        /// Processes the result of an action method.
        /// </summary>
#if MVC6
        /// <param name="context">The action context.</param>
        public override void ExecuteResult(ActionContext context) {
#else
        /// <param name="context">The controller context.</param>
        public override void ExecuteResult(ControllerContext context) {
#endif
            var Response = context.HttpContext.Response;

            Response.ContentType = "application/zip";
#if MVC6
            Response.Headers.Add("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(Zip.FileName) ? "" : "filename=" + Zip.FileName));
            Response.Cookies.Append(Basics.CookieDone, CookieToReturn.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, Path = "/" } );

            using (Zip) {
                Zip.Zip.Save(Response.Body);
            }
#else
            Response.AddHeader("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(Zip.FileName) ? "" : "filename=" + Zip.FileName));

            HttpCookie cookie = new HttpCookie(Basics.CookieDone, CookieToReturn.ToString());
            Response.Cookies.Remove(Basics.CookieDone);
            Response.SetCookie(cookie);

            using (Zip) {
                Zip.Zip.Save(Response.OutputStream);
                Response.End();
            }
#endif
        }
    }
}
