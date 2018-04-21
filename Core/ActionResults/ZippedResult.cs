/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Support.Zip;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

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
        public override async Task ExecuteResultAsync(ActionContext context) {
#else
        /// <param name="context">The controller context.</param>
        public override void ExecuteResult(ControllerContext context) {
            YetaWFManager.Syncify(async () => { // sorry, MVC5, no async for you
#endif
                var Response = context.HttpContext.Response;

                Response.ContentType = "application/zip";
#if MVC6
                Response.Headers.Add("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(Zip.FileName) ? "" : "filename=" + Zip.FileName));
                Response.Cookies.Append(Basics.CookieDone, CookieToReturn.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, Path = "/" } );

                using (Zip) {
                    await Zip.SaveAsync(Response.Body);
                    await Zip.CleanupFoldersAsync();
                }
#else
                Response.AddHeader("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(Zip.FileName) ? "" : "filename=" + Zip.FileName));

                HttpCookie cookie = new HttpCookie(Basics.CookieDone, CookieToReturn.ToString());
                Response.Cookies.Remove(Basics.CookieDone);
                Response.SetCookie(cookie);

                using (Zip) {
                    Zip.Zip.Save(Response.OutputStream);
                    Response.End();
                    await Zip.CleanupFoldersAsync();
                }
            });
#endif
        }
    }
}
