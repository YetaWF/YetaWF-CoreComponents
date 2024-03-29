﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Used to return a ZIP file from a controller.
    /// </summary>
    /// <remarks>
    /// This action result works in conjunction with JavaScript in Basics.ts returning a cookie indicating the file is available.
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
        /// <param name="context">The controller context.</param>
        public override async Task ExecuteResultAsync(ActionContext context) {

            HttpResponse Response = context.HttpContext.Response;

            Response.ContentType = "application/zip";
            Response.Headers.Add("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(Zip.FileName) ? "" : $@"filename=""{Zip.FileName}"""));
            Response.Cookies.Append(Basics.CookieDone, CookieToReturn.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, Path = "/" });

            Utility.AllowSyncIO(context.HttpContext);
            using (Zip) {
                await Zip.SaveAsync(Response.Body);
            }
        }
    }
}
