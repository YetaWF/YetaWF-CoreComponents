/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using YetaWF.Core.Extensions;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.HttpHandler {

    /// <summary>
    /// Class implementing middleware for the WEBP HTTP handler.
    /// </summary>
    public class WebpMiddleware {

        //private readonly RequestDelegate _next;
        private readonly WebpHttpHandler Handler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
        public WebpMiddleware(RequestDelegate next) {
            //_next = next;
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
    public class WebpHttpHandler {
        /// <summary>
        /// Tests whether the specified extension is handled by this HTTP handler.
        /// </summary>
        /// <param name="extension">The extension to test. This may be a full path name. Only the extension portion is inspected.</param>
        /// <returns>Returns true if the HTTP handler processes this file extension, false otherwise.</returns>
        public static bool IsValidExtension(string extension) {
            return extension.EndsWith(".png") || extension.EndsWith(".jpg") || extension.EndsWith(".jpeg");
        }

        // A bit of a simplification - we're just looking for image/webp, don't care about quality and don't look for image/*
        private bool UseWEBP(string? acceptHeader) {
            if (string.IsNullOrEmpty(acceptHeader)) return false;
            List<MediaTypeHeaderValue> mediaTypes = acceptHeader.Split(',').Select(MediaTypeHeaderValue.Parse).ToList();
            return (from m in mediaTypes where m.MediaType == "image/webp" select m).FirstOrDefault() != null;
        }

        // IHttpHandler (Async)
        // IHttpHandler (Async)
        // IHttpHandler (Async)
        /// <summary>
        /// Called by the middleware (.NET) to process
        /// a request for an image.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        public async Task ProcessRequest(HttpContext context) {
            await StartupRequest.StartRequestAsync(context, true);
            YetaWFManager manager = YetaWFManager.Manager;

            string fullUrl = context.Request.Path;
            string file = Utility.UrlToPhysical(fullUrl);

            if (!IsValidExtension(file)) {
                context.Response.StatusCode = 404;
                Logging.AddErrorLog("Not Found");
                return;
            }

            bool useWebp = UseWEBP(context.Request.Headers["Accept"]);

            string contentType = "image/webp";

            // Check if there is a generated file
            if (useWebp) {
                string webpImage = System.IO.Path.ChangeExtension(file, ".webp-gen");
                if (await FileSystem.FileSystemProvider.FileExistsAsync(webpImage)) {
                    // use generated image
                    file = webpImage;
                } else
                    useWebp = false;
            }

            if (!useWebp) {
                // check if there is a regular image
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file)) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found");
                    return;
                }
                MimeSection mimeSection = new MimeSection();
                string? ct = mimeSection.GetContentTypeFromExtension(System.IO.Path.GetExtension(file));
                if (ct == null) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found - no MIME type");
                    return;
                }
                contentType = ct;
            }

            DateTime lastMod;
            lastMod = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(file);

            // Cache verification?
            string? ifNoneMatch = context.Request.Headers["If-None-Match"];
            if (ifNoneMatch != null && ifNoneMatch.TruncateStart("W/") == GetETag()) {
                context.Response.ContentType = contentType;
                context.Response.StatusCode = 304;
                context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                YetaWFManager.SetStaticCacheInfo(context);
                context.Response.Headers.Add("ETag", GetETag());
                return;
            }

            // Send entire file
            byte[]? btes = null;
            string cacheKey = "WebpHttpHandler_" + file + "_";

            if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                GetObjectInfo<byte[]> objInfo;
                await using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
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
                    if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                        await using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                            await localCacheDP.AddAsync<byte[]>(cacheKey, null);
                        }
                    }
                    return;
                }
                if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                    await using (ICacheDataProvider localCacheDP = YetaWF.Core.IO.Caching.GetLocalCacheProvider()) {
                        await localCacheDP.AddAsync<byte[]>(cacheKey, btes);
                    }
                }
            }
            context.Response.ContentType = contentType;
            context.Response.StatusCode = 200;
            context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
            YetaWFManager.SetStaticCacheInfo(context);
            context.Response.Headers.Add("ETag", GetETag());
            await context.Response.Body.WriteAsync(btes, 0, btes.Length);
        }
        private static string GetETag() {
            return string.Format(@"""{0}""", YetaWFManager.CacheBuster);
        }
    }
}
