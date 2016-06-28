/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using YetaWF.Core.Addons;

namespace YetaWF.Core.Support {

    public class YetaWFZipFile : IDisposable {
        public ZipFile Zip { get; set; }
        public string FileName { get; set; }
        public List<string> TempFiles { get; set; }
        public List<string> TempFolders { get; set; }
        public bool HasSource { get; set; }

        public YetaWFZipFile() {
            TempFiles = new List<string>();
            TempFolders = new List<string>();
        }

        public void Dispose() { Dispose(true); }
        protected virtual void Dispose(bool disposing) {
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
        ~YetaWFZipFile() { Dispose(false); }
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

        public ZippedFileResult(YetaWFZipFile zip, long cookieToReturn) {
            this.Zip = zip;
            this.CookieToReturn = cookieToReturn;
        }

        public override void ExecuteResult(ControllerContext context) {
            var Response = context.HttpContext.Response;

            Response.ContentType = "application/zip";
            Response.AddHeader("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(Zip.FileName) ? "" : "filename=" + Zip.FileName));

            HttpCookie cookie = new HttpCookie(Basics.CookieDone, CookieToReturn.ToString());
            Response.Cookies.Remove(Basics.CookieDone);
            Response.SetCookie(cookie);

            using (Zip) {
                Zip.Zip.Save(Response.OutputStream);
                Response.End();
            }
        }
    }
}
