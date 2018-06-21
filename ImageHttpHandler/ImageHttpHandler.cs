/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Extensions;
using YetaWF.Core.Image;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Upload;
using System.Threading.Tasks;
using YetaWF.Core.IO;
#if MVC6
using Microsoft.AspNetCore.Http;
#else
using System.Web;
using System.Web.SessionState;
#endif

namespace YetaWF.Core.HttpHandler {

#if MVC6

    public class ImageMiddleware {

        private readonly RequestDelegate _next;
        private ImageHttpHandler Handler;

        public ImageMiddleware(RequestDelegate next) {
            _next = next;
            Handler = new ImageHttpHandler();
        }

        public async Task InvokeAsync(HttpContext context) {
            await Handler.ProcessRequest(context);
            //await _next(context);
        }
    }

    public class ImageHttpHandler
#else
    public class ImageHttpHandler : HttpTaskAsyncHandler, IReadOnlySessionState
#endif
    {
        // IHttpHandler (Async)
        // IHttpHandler (Async)
        // IHttpHandler (Async)

#if MVC6
        public async Task ProcessRequest(HttpContext context) {
            await StartupRequest.StartRequestAsync(context, true);
#else
        public override bool IsReusable {
            get { return true; }
        }

        public override async Task ProcessRequestAsync(HttpContext context) {
#endif
            YetaWFManager manager = YetaWFManager.Manager;

            string typeVal = manager.RequestQueryString["type"];
            string locationVal = manager.RequestQueryString["location"];
            string nameVal = manager.RequestQueryString["name"];
            if (typeVal != null) {
                ImageSupport.ImageHandlerEntry entry = (from h in ImageSupport.HandlerEntries where h.Type == typeVal select h).FirstOrDefault();
                if (entry != null) {

                    if (!string.IsNullOrWhiteSpace(nameVal)) {
                        string[] parts = nameVal.Split(new char[] { ImageSupport.ImageSeparator });
                        if (parts.Length > 1)
                            nameVal = parts[0];
                    }

                    System.Drawing.Image img = null;
                    byte[] bytes = null;

                    // check if this is a temporary (uploaded image)
                    FileUpload fileUpload = new FileUpload();
                    string filePath = await fileUpload.GetTempFilePathFromNameAsync(nameVal, locationVal);

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
                        string addonUrl = VersionManager.GetAddOnNamedUrl(package.Domain, package.Product, "Image");// and the Url of the Image template
                        filePath = YetaWFManager.UrlToPhysical(Path.Combine(addonUrl, "NoImage.png"));
                        if (!await FileSystem.FileSystemProvider.FileExistsAsync(filePath))
                            throw new InternalError("The image {0} is missing", filePath);
                    }

                    if ((img == null || bytes == null) && filePath == null) {
                        context.Response.StatusCode = 404;
#if MVC6
                        Logging.AddErrorLog("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal);
#else
                        context.Response.StatusDescription = Logging.AddErrorLog("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal);
                        context.ApplicationInstance.CompleteRequest();
#endif
                        return;
                    }

                    string percentString = manager.RequestQueryString["Percent"];
                    if (!string.IsNullOrWhiteSpace(percentString)) {
                        string stretchString = manager.RequestQueryString["Stretch"];
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
                        string widthString = manager.RequestQueryString["Width"];
                        string heightString = manager.RequestQueryString["Height"];
                        string stretchString = manager.RequestQueryString["Stretch"];
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

                    string contentType;
                    if (filePath != null) {
                        MimeSection mimeSection = new MimeSection();
                        contentType = mimeSection.GetContentTypeFromExtension(Path.GetExtension(filePath));
                        if (string.IsNullOrWhiteSpace(contentType))
                            throw new InternalError("File type not suitable as image - {0}", filePath);// shouldn't have been uploaded in the first place
                        DateTime lastMod = await FileSystem.FileSystemProvider.GetLastWriteTimeUtcAsync(filePath);
                        context.Response.Headers.Add("ETag", GetETag(filePath, lastMod));
                        context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                        YetaWFManager.SetStaticCacheInfo(context.Response);
                        context.Response.ContentType = contentType;
                        string ifNoneMatch = context.Request.Headers["If-None-Match"];
                        if (ifNoneMatch.TruncateStart("W/") != GetETag(filePath, lastMod)) {
                            context.Response.StatusCode = 200;
#if MVC6
                            await context.Response.SendFileAsync(filePath);
#else
                            context.Response.TransmitFile(filePath);
#endif
                        } else {
                            context.Response.StatusCode = 304;
                        }
#if MVC6
#else
                        context.Response.StatusDescription = "OK";
                        context.ApplicationInstance.CompleteRequest();
#endif
                        return;
                    } else if (img != null && bytes != null) {
                        if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Gif) contentType = "image/gif";
                        else if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Png) contentType = "image/png";
                        else if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Jpeg) contentType = "image/jpeg";
                        else contentType = "image/jpeg";

                        YetaWFManager.SetStaticCacheInfo(context.Response);
                        context.Response.Headers.Add("ETag", GetETag(bytes));
                        context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", DateTime.Now.AddDays(-1)));/*can use local time*/
                        context.Response.ContentType = contentType;
#if MVC6
#else
                        context.Response.StatusDescription = "OK";
#endif
                        string ifNoneMatch = context.Request.Headers["If-None-Match"];
                        if (ifNoneMatch.TruncateStart("W/") != GetETag(bytes)) {
                            context.Response.StatusCode = 200;
#if MVC6
                            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
#else
                            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
#endif
                        } else {
                            context.Response.StatusCode = 304;
                        }
#if MVC6
#else
                        context.ApplicationInstance.CompleteRequest();
#endif
                        img.Dispose();
                        return;
                    } else {
                        // we got nothing
                        context.Response.StatusCode = 404;
#if MVC6
                        Logging.AddErrorLog(string.Format("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal));
#else
                        context.Response.StatusDescription = Logging.AddErrorLog(string.Format("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal));
                        context.ApplicationInstance.CompleteRequest();
#endif
                        return;
                    }
                }
            }
            context.Response.StatusCode = 404;
#if MVC6
            Logging.AddErrorLog("Not Found");
#else
            context.Response.StatusDescription = Logging.AddErrorLog("Not Found");
            context.ApplicationInstance.CompleteRequest();
#endif
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
