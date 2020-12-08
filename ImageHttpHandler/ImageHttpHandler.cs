/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Extensions;
using YetaWF.Core.Image;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Upload;

namespace YetaWF.Core.HttpHandler {

    /// <summary>
    /// Class implementing middleware for the Image HTTP handler.
    /// </summary>
    public class ImageMiddleware {

        //private readonly RequestDelegate _next;
        private readonly ImageHttpHandler Handler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
        public ImageMiddleware(RequestDelegate next) {
            //_next = next;
            Handler = new ImageHttpHandler();
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
    /// Implements the Image HTTP Handler.
    /// </summary>
    public class ImageHttpHandler {

        // A bit of a simplification - we're just looking for image/webp, don't care about quality and don't look for image/*
        private bool UseWEBP(string acceptHeader) {
            List<MediaTypeHeaderValue> mediaTypes = acceptHeader.Split(',').Select(MediaTypeHeaderValue.Parse).ToList();
            if (mediaTypes == null)
                return false;
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

            string? typeVal = manager.RequestQueryString["type"];
            string? locationVal = manager.RequestQueryString["location"];
            string? nameVal = manager.RequestQueryString["name"];
            if (typeVal != null) {
                ImageSupport.ImageHandlerEntry? entry = (from h in ImageSupport.HandlerEntries where h.Type == typeVal select h).FirstOrDefault();
                if (entry != null) {

                    if (!string.IsNullOrWhiteSpace(nameVal)) {
                        string[] parts = nameVal.Split(new char[] { ImageSupport.ImageSeparator });
                        if (parts.Length > 1)
                            nameVal = parts[0];
                    }

                    System.Drawing.Image? img = null;
                    byte[]? bytes = null;

                    // check if this is a temporary (uploaded image)
                    FileUpload fileUpload = new FileUpload();
                    string? filePath = await fileUpload.GetTempFilePathFromNameAsync(nameVal, locationVal);

                    // if we don't have an image yet, try to get the file from the registered type
                    if (((img == null || bytes == null) && filePath == null) && entry.GetImageAsFileAsync != null) {
                        ImageSupport.GetImageAsFileInfo info = await entry.GetImageAsFileAsync(nameVal, locationVal);
                        if (info.Success) {
                            filePath = info.File;
                        }
                    }

                    // if we don't have an image yet, try to get the raw bytes from the registered type
                    if (((img == null || bytes == null) && filePath == null) && entry.GetImageInBytesAsync != null) {
                        ImageSupport.GetImageInBytesInfo info = await entry.GetImageInBytesAsync(nameVal, locationVal);
                        if (info.Success) {
                            bytes = info.Content;
                            using (MemoryStream ms = new MemoryStream(info.Content)) {
                                img = System.Drawing.Image.FromStream(ms);
                            }
                        }
                    }

                    // if there is no image, use a default image
                    if ((img == null || bytes == null) && (filePath == null || !await FileSystem.FileSystemProvider.FileExistsAsync(filePath))) {
                        Package package = Package.GetPackageFromType(typeof(YetaWFManager));// get the core package
                        string addonUrl = VersionManager.GetAddOnNamedUrl(package.AreaName, "Image");// and the Url of the Image template
                        filePath = Utility.UrlToPhysical(Path.Combine(addonUrl, "NoImage.png"));
                        if (!await FileSystem.FileSystemProvider.FileExistsAsync(filePath))
                            throw new InternalError("The image {0} is missing", filePath);
                    }

                    if ((img == null || bytes == null) && filePath == null) {
                        context.Response.StatusCode = 404;
                        Logging.AddErrorLog("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal);
                        return;
                    }

                    string? percentString = manager.RequestQueryString["Percent"];
                    if (!string.IsNullOrWhiteSpace(percentString)) {
                        string? stretchString = manager.RequestQueryString["Stretch"];
                        bool stretch = false;
                        if (!string.IsNullOrWhiteSpace(stretchString) && (stretchString == "1" || string.Compare(stretchString, "true", true) == 0))
                            stretch = true;
                        int percent = Convert.ToInt32(percentString);
                        if (percent > 0) {
                            // resize to fit
                            if (img != null && bytes != null) {
                                img = ImageSupport.NewImageSize(img, percent, stretch, out bytes);
                            } else if (filePath != null) {
                                img = ImageSupport.NewImageSize(filePath, percent, stretch, out bytes);
                                filePath = null;
                            }
                        }
                    } else {
                        string? widthString = manager.RequestQueryString["Width"];
                        string? heightString = manager.RequestQueryString["Height"];
                        string? stretchString = manager.RequestQueryString["Stretch"];
                        if (!string.IsNullOrWhiteSpace(widthString) && !string.IsNullOrWhiteSpace(heightString)) {
                            int width = Convert.ToInt32(widthString);
                            int height = Convert.ToInt32(heightString);
                            bool stretch = false;
                            if (!string.IsNullOrWhiteSpace(stretchString) && (stretchString == "1" || string.Compare(stretchString, "true", true) == 0))
                                stretch = true;
                            if (width > 0 && height > 0) {
                                // resize to fit
                                if (img != null && bytes != null) {
                                    img = ImageSupport.NewImageSize(img, width, height, stretch, out bytes);
                                } else if (filePath != null) {
                                    img = ImageSupport.NewImageSize(filePath, width, height, stretch, out bytes);
                                    filePath = null;
                                }
                            }
                        }
                    }

                    string? contentType;
                    if (filePath != null) {
                        MimeSection mimeSection = new MimeSection();

                        bool useWebp = UseWEBP(context.Request.Headers["Accept"]);
                        if (useWebp) {
                            string webpImage = System.IO.Path.ChangeExtension(filePath, ".webp-gen");
                            if (await FileSystem.FileSystemProvider.FileExistsAsync(webpImage)) {
                                // use generated image
                                filePath = webpImage;
                            } else
                                useWebp = false;
                        }

                        contentType = mimeSection.GetContentTypeFromExtension(Path.GetExtension(filePath));
                        if (string.IsNullOrWhiteSpace(contentType))
                            throw new InternalError("File type not suitable as image - {0}", filePath);// shouldn't have been uploaded in the first place
                        DateTime lastMod = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(filePath);
                        context.Response.Headers.Add("ETag", GetETag(filePath, lastMod));
                        context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                        YetaWFManager.SetStaticCacheInfo(context);
                        context.Response.ContentType = contentType;
                        string ifNoneMatch = context.Request.Headers["If-None-Match"];
                        if (ifNoneMatch.TruncateStart("W/") != GetETag(filePath, lastMod)) {
                            context.Response.StatusCode = 200;
                            await context.Response.SendFileAsync(filePath);
                        } else {
                            context.Response.StatusCode = 304;
                        }
                        return;
                    } else if (img != null && bytes != null) {
                        if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Gif) contentType = "image/gif";
                        else if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Png) contentType = "image/png";
                        else if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Jpeg) contentType = "image/jpeg";
                        else contentType = "image/jpeg";

                        YetaWFManager.SetStaticCacheInfo(context);
                        context.Response.Headers.Add("ETag", GetETag(bytes));
                        context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", DateTime.Now.AddDays(-1)));/*can use local time*/
                        context.Response.ContentType = contentType;
                        string ifNoneMatch = context.Request.Headers["If-None-Match"];
                        if (ifNoneMatch.TruncateStart("W/") != GetETag(bytes)) {
                            context.Response.StatusCode = 200;
                            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                        } else {
                            context.Response.StatusCode = 304;
                        }
                        img.Dispose();
                        return;
                    } else {
                        // we got nothing
                        context.Response.StatusCode = 404;
                        Logging.AddErrorLog(string.Format("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal));
                        return;
                    }
                }
            }
            context.Response.StatusCode = 404;
            Logging.AddErrorLog("Not Found");
            return;
        }

        private string GetETag(byte[] bytes) {
            int length = bytes.Length;
            string etag = "";
            int max = 20;// 20 digits;
            for (int i = 0, incr = 100; i < max; ++i, incr += 100) {
                if (incr >= length) break;
                byte b = bytes[incr];
                etag += string.Format("{0:x}", Convert.ToInt16(bytes[i]));
            }
            return string.Format(@"""{0}/{1}""", length, etag);
        }

        private string GetETag(string filePath, DateTime lastMod) {
            return string.Format(@"""{0}""", lastMod.Ticks.ToString());
        }
    }
}
