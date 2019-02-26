/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Text;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
using YetaWF.Core.IO;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
#else
using System.Web;
using System.Web.SessionState;
#endif

namespace YetaWF.Core.HttpHandler {

#if MVC6
    public class CssMiddleware {

        private readonly RequestDelegate _next;
        private CssHttpHandler Handler;

        public CssMiddleware(RequestDelegate next) {
            _next = next;
            Handler = new CssHttpHandler();
        }

        public async Task InvokeAsync(HttpContext context) {
            await Handler.ProcessRequest(context);
            //await _next(context);
        }
    }

    public class CssHttpHandler
#else
    /// <summary>
    /// Implements the CSS HTTP Handler.
    /// </summary>
    public class CssHttpHandler : HttpTaskAsyncHandler, IReadOnlySessionState
#endif
    {
        // IHttpHandler (Async)
        // IHttpHandler (Async)
        // IHttpHandler (Async)

#if MVC6
        public async Task ProcessRequest(HttpContext context) {
            await StartupRequest.StartRequestAsync(context, true);
#else
        /// <summary>
        /// Returns true indicating that the task handler class instance can be reused for another asynchronous task.
        /// </summary>
        public override bool IsReusable {
            get { return true; }
        }

        /// <summary>
        /// Called by the IIS pipeline (ASP.NET) or middleware (ASP.NET Core) to process
        /// a request for a CSS file.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        public override async Task ProcessRequestAsync(HttpContext context) {
#endif
            YetaWFManager manager = YetaWFManager.Manager;

            string fullUrl = context.Request.Path;
            string file;
#if MVC6
            if (fullUrl.StartsWith(Globals.VaultPrivateUrl)) {
                // Private Vault files are a special case as they're not within the website.
                // We need to make sure to only allow .css, .less, .scss files otherwise this would expose other files.
                if (!fullUrl.EndsWith(".css", true, CultureInfo.InvariantCulture) && !fullUrl.EndsWith(".less", true, CultureInfo.InvariantCulture) && !fullUrl.EndsWith(".scss", true, CultureInfo.InvariantCulture)) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found");
                    return;
                }
            }
#else
#endif
            file = YetaWFManager.UrlToPhysical(fullUrl);

            DateTime lastMod = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(file);

            // Cache verification?
            string ifNoneMatch = context.Request.Headers["If-None-Match"];
            if (ifNoneMatch.TruncateStart("W/") == GetETag()) {
                context.Response.ContentType = "text/css";
                context.Response.StatusCode = 304;
#if MVC6
#else
                context.Response.StatusDescription = "OK";
#endif
                context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                YetaWFManager.SetStaticCacheInfo(context);
                context.Response.Headers.Add("ETag", GetETag());
#if MVC6
#else
                context.ApplicationInstance.CompleteRequest();
#endif
                return;
            }

            // Send entire file
            byte[] bytes = null;
            string cacheKey = "CssHttpHandler_" + file + "_";

            if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                GetObjectInfo<byte[]> objInfo;
                using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
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
#if MVC6
#else
                    context.Response.StatusDescription = "Not Found";
                    context.ApplicationInstance.CompleteRequest();
#endif
                    if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                        using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                            await localCacheDP.AddAsync<byte[]>(cacheKey, null);
                        }
                    }
                    return;
                }
                bytes = Encoding.ASCII.GetBytes(text);
                if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                    using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                        await localCacheDP.AddAsync<byte[]>(cacheKey, bytes);
                    }
                }
            }
            context.Response.ContentType = "text/css";
            context.Response.StatusCode = 200;
#if MVC6
#else
            context.Response.StatusDescription = "OK";
#endif
            context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
            YetaWFManager.SetStaticCacheInfo(context);
            context.Response.Headers.Add("ETag", GetETag());
#if MVC6
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
#else
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.ApplicationInstance.CompleteRequest();
#endif
        }
        private static string GetETag() {
            return string.Format(@"""{0}""", YetaWFManager.CacheBuster);
        }
    }
}
