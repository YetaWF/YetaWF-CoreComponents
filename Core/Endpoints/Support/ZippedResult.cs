/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Zip;

namespace YetaWF.Core.Endpoints;

/// <summary>
/// Used to return a ZIP file from an endpoint.
/// </summary>
/// <remarks>
/// This action result works in conjunction with JavaScript in Basics.ts returning a cookie indicating the file is available.
/// </remarks>
public class ZippedFileResult {

    /// <summary>
    /// Returns a zip file as an IResult.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    /// <param name="zip">The zip file.</param>
    /// <param name="cookieToReturn">The cookie to return, indicating that the ZIP file has been downloaded. Works in conjunction with client-side code in basics.js.</param>
    public static async Task<IResult> ZipFileAsync(HttpContext context, YetaWFZipFile zip, long cookieToReturn) {
        context.Response.Headers.Add("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(zip.FileName) ? "" : $@"filename=""{zip.FileName}"""));
        context.Response.Cookies.Append(Basics.CookieDone, cookieToReturn.ToString(), new CookieOptions { HttpOnly = false, Path = "/" });
        Utility.AllowSyncIO(context);
        using (zip) {
            await zip.SaveAsync(context.Response.Body);
        }
        return Results.Ok();
    }
}
