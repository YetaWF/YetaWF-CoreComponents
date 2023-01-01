/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.Extensions;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.HttpHandler {

    /// <summary>
    /// Class implementing middleware for the CSS HTTP handler.
    /// </summary>
    public class CssMiddleware {

        private readonly RequestDelegate _next;
        private CssHttpHandler Handler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
        public CssMiddleware(RequestDelegate next) {
            _next = next;
            Handler = new CssHttpHandler();
        }

        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        public async Task InvokeAsync(HttpContext context) {
            await Handler.ProcessRequest(context);
            //await _next(context);
        }
    }

    /// <summary>
    /// Implements the CSS HTTP Handler.
    /// </summary>
    public class CssHttpHandler {
        // IHttpHandler (Async)
        // IHttpHandler (Async)
        // IHttpHandler (Async)

        /// <summary>
        /// Called by the middleware (.NET) to process
        /// a request for a CSS file.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        public async Task ProcessRequest(HttpContext context) {
            await StartupRequest.StartRequestAsync(context, true);
            YetaWFManager manager = YetaWFManager.Manager;

            string fullUrl = context.Request.Path;
            string file;
            if (fullUrl.StartsWith(Globals.VaultPrivateUrl)) {
                // Private Vault files are a special case as they're not within the website.
                // We need to make sure to only allow .css, .less, .scss files otherwise this would expose other files.
                if (!fullUrl.EndsWith(".css", true, CultureInfo.InvariantCulture) && !fullUrl.EndsWith(".less", true, CultureInfo.InvariantCulture) && !fullUrl.EndsWith(".scss", true, CultureInfo.InvariantCulture)) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found");
                    return;
                }
            }
            file = Utility.UrlToPhysical(fullUrl);

            DateTime lastMod = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(file);

            // Cache verification?
            string? ifNoneMatch = context.Request.Headers["If-None-Match"];
            if (ifNoneMatch != null && ifNoneMatch.TruncateStart("W/") == GetETag()) {
                context.Response.ContentType = "text/css";
                context.Response.StatusCode = 304;
                context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                YetaWFManager.SetStaticCacheInfo(context);
                context.Response.Headers.Add("ETag", GetETag());
                return;
            }

            // Send entire file
            byte[]? bytes = null;
            string cacheKey = "CssHttpHandler_" + file + "_";

            if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                GetObjectInfo<byte[]> objInfo;
                await using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                    objInfo = await localCacheDP.GetAsync<byte[]>(cacheKey);
                }
                if (objInfo.Success)
                    bytes = objInfo.Data;
            }
            if (bytes == null) {
                string text = "";
                try {
                    text = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
                } catch (Exception) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found");
                    if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                        await using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                            await localCacheDP.AddAsync<byte[]>(cacheKey, null);
                        }
                    }
                    return;
                }
                bytes = Encoding.UTF8.GetBytes(text);
                if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                    await using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                        await localCacheDP.AddAsync<byte[]>(cacheKey, bytes);
                    }
                }
            }
            context.Response.ContentType = "text/css";
            context.Response.StatusCode = 200;
            context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
            YetaWFManager.SetStaticCacheInfo(context);
            context.Response.Headers.Add("ETag", GetETag());
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
        private static string GetETag() {
            return string.Format(@"""{0}""", YetaWFManager.CacheBuster);
        }
    }
}
