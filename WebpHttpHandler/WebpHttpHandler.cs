/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
using YetaWF.Core.IO;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Linq;
using System.Collections.Generic;
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
    /// <summary>
    /// Class implementing middleware for the WEBP HTTP handler.
    /// </summary>
    public class WebpMiddleware {

        private readonly RequestDelegate _next;
        private WebpHttpHandler Handler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
        public WebpMiddleware(RequestDelegate next) {
            _next = next;
            Handler = new WebpHttpHandler();
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
    /// Implements the WEBP HTTP Handler. Responds to image requests for PNG, JPG files by returning a WEBP image (if available).
    /// </summary>
    public class WebpHttpHandler
#else
    /// <summary>
    /// Implements the WEBP HTTP Handler. Responds to image requests for PNG, JPG files by returning a WEBP image (if available).
    /// </summary>
    public class WebpHttpHandler : HttpTaskAsyncHandler, IReadOnlySessionState
#endif
    {
        /// <summary>
        /// Tests whether the specified extension is handled by this HTTP handler.
        /// </summary>
        /// <param name="extension">The extension to test. This may be a full path name. Only the extension portion is inspected.</param>
        /// <returns>Returns true if the HTTP handler processes this file extension, false otherwise.</returns>
        public static bool IsValidExtension(string extension) {
            return extension.EndsWith(".png") || extension.EndsWith(".jpg") || extension.EndsWith(".jpeg");
        }

        // A bit of a simplification - we're just looking for image/webp, don't care about quality and don't look for image/*
        private bool UseWEBP(string acceptHeader) {
            List<MediaTypeHeaderValue> mediaTypes = acceptHeader?.Split(',').Select(MediaTypeHeaderValue.Parse).ToList();
            return (from m in mediaTypes where m.MediaType == "image/webp" select m).FirstOrDefault() != null;
        }

        // IHttpHandler (Async)
        // IHttpHandler (Async)
        // IHttpHandler (Async)
#if MVC6
        /// <summary>
        /// Called by the IIS pipeline (ASP.NET) or middleware (ASP.NET Core) to process
        /// a request for a CSS file.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
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
        /// a request for a PNG, JPG file.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        public override async Task ProcessRequestAsync(HttpContext context) {
#endif
            YetaWFManager manager = YetaWFManager.Manager;

            string fullUrl = context.Request.Path;
            string file = Utility.UrlToPhysical(fullUrl);

            if (!IsValidExtension(file)) {
                context.Response.StatusCode = 404;
                Logging.AddErrorLog("Not Found");
#if MVC6
#else
                context.Response.StatusDescription = "Not Found";
                context.ApplicationInstance.CompleteRequest();
#endif
                return;
            }

            bool useWebp = UseWEBP(context.Request.Headers["Accept"]);

            string contentType = null;

            // Check if there is a generated file
            if (useWebp) {
                string webpImage = System.IO.Path.ChangeExtension(file, ".webp-gen");
                if (await FileSystem.FileSystemProvider.FileExistsAsync(webpImage)) {
                    // use generated image
                    file = webpImage;
                    contentType = "image/webp";
                } else
                    useWebp = false;
            }

            if (!useWebp) {
                // check if there is a regular image
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file)) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found");
#if MVC6
#else
                    context.Response.StatusDescription = "Not Found";
                    context.ApplicationInstance.CompleteRequest();
#endif
                    return;
                }
                MimeSection mimeSection = new MimeSection();
                contentType = mimeSection.GetContentTypeFromExtension(System.IO.Path.GetExtension(file));
            }

            DateTime lastMod;
            lastMod = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(file);

            // Cache verification?
            string ifNoneMatch = context.Request.Headers["If-None-Match"];
            if (ifNoneMatch.TruncateStart("W/") == GetETag()) {
                context.Response.ContentType = contentType;
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
            byte[] btes = null;
            string cacheKey = "WebpHttpHandler_" + file + "_";

            if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                GetObjectInfo<byte[]> objInfo;
                using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                    objInfo = await localCacheDP.GetAsync<byte[]>(cacheKey);
                }
                if (objInfo.Success)
                    btes = objInfo.Data;
            }
            if (btes == null) {
                try {
                    btes = await FileSystem.FileSystemProvider.ReadAllBytesAsync(file);
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
                if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                    using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                        await localCacheDP.AddAsync<byte[]>(cacheKey, btes);
                    }
                }
            }
            context.Response.ContentType = contentType;
            context.Response.StatusCode = 200;
#if MVC6
#else
            context.Response.StatusDescription = "OK";
#endif
            context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
            YetaWFManager.SetStaticCacheInfo(context);
            context.Response.Headers.Add("ETag", GetETag());
#if MVC6
            await context.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
            await context.Response.OutputStream.WriteAsync(btes, 0, btes.Length);
            context.ApplicationInstance.CompleteRequest();
#endif
        }
        private static string GetETag() {
            return string.Format(@"""{0}""", YetaWFManager.CacheBuster);
        }
    }
}
