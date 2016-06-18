/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using YetaWF.Core.Addons;
using YetaWF.Core.Image;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Upload;

namespace YetaWF.Core.HttpHandler {
    public class ImageHttpHandler : IHttpHandler, IReadOnlySessionState {

        // IHttpHandler
        // IHttpHandler
        // IHttpHandler

        public bool IsReusable {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context) {

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
                    string filePath = fileUpload.GetTempFilePathFromName(nameVal, locationVal);

                    // if we don't have an image yet, try to get the file from the registered type
                    if (((img == null || bytes == null) && filePath == null) && entry.GetFilePath != null) {
                        string file;
                        if (entry.GetFilePath(nameVal, locationVal, out file)) {
                            filePath = file;
                        }
                    }

                    // if we don't have an image yet, try to get the raw bytes from the registered type
                    if (((img == null || bytes == null) && filePath == null) && entry.GetBytes != null) {
                        if (entry.GetBytes(nameVal, locationVal, out bytes)) {
                            using (MemoryStream ms = new MemoryStream(bytes)) {
                                img = System.Drawing.Image.FromStream(ms);
                            }
                        }
                    }

                    // if there is no image, use a default image
                    if ((img == null || bytes == null) && (filePath == null || !File.Exists(filePath))) {
                        Package package = Package.GetPackageFromType(typeof(YetaWFManager));// get the core package
                        string addonUrl = VersionManager.GetAddOnTemplateUrl(package.Domain, package.Product, "Image");// and the Url of the Image template
                        filePath = YetaWFManager.UrlToPhysical(Path.Combine(addonUrl, "Images", "NoImage.png"));
                        if (!File.Exists(filePath))
                            throw new InternalError("The image {0} is missing", filePath);
                    }

                    if ((img == null || bytes == null) && filePath == null) {
                        context.Response.StatusCode = 404;
                        context.Response.StatusDescription = Logging.AddErrorLog("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal);
                        context.ApplicationInstance.CompleteRequest();
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
                        MimeSection mimeSection = MimeSection.GetMimeSection();
                        MimeEntry me = mimeSection.GetElementFromExtension(Path.GetExtension(filePath));
                        if (me == null)
                            throw new InternalError("File type not suitable as image - {0}", filePath);// shouldn't have been uploaded in the first place
                        contentType = me.Type;
                        DateTime lastMod = File.GetLastWriteTimeUtc(filePath);
                        context.Response.Cache.SetCacheability(HttpCacheability.Public);
                        context.Response.Headers.Add("ETag", GetETag(filePath, lastMod));
                        context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                        context.Response.ContentType = contentType;
                        context.Response.StatusDescription = "OK";
                        if (context.Request.Headers["If-None-Match"] != GetETag(filePath, lastMod)) {
                            context.Response.TransmitFile(filePath);
                            context.Response.StatusCode = 200;
                        } else {
                            context.Response.StatusCode = 304;
                        }
                        context.ApplicationInstance.CompleteRequest();
                        return;
                    } else if (img != null && bytes != null) {
                        if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Gif) contentType = "image/gif";
                        else if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Png) contentType = "image/png";
                        else if (img.RawFormat == System.Drawing.Imaging.ImageFormat.Jpeg) contentType = "image/jpeg";
                        else contentType = "image/jpeg";

                        context.Response.Cache.SetCacheability(HttpCacheability.Public);
                        context.Response.Headers.Add("ETag", GetETag(bytes));
                        context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", DateTime.Now.AddDays(-1)));/*can use local time*/
                        context.Response.ContentType = contentType;
                        context.Response.StatusDescription = "OK";
                        if (context.Request.Headers["If-None-Match"] != GetETag(bytes)) {
                            context.Response.StatusCode = 200;
                            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        } else {
                            context.Response.StatusCode = 304;
                        }
                        context.ApplicationInstance.CompleteRequest();

                        img.Dispose();
                        return;
                    } else {
                        // we got nothing
                        context.Response.StatusCode = 404;
                        context.Response.StatusDescription = Logging.AddErrorLog(string.Format("Not Found - Image file {0} (location {1}) not found", nameVal, locationVal));
                        context.ApplicationInstance.CompleteRequest();
                        return;
                    }
                }
            }
            context.Response.StatusCode = 404;
            context.Response.StatusDescription = Logging.AddErrorLog("Not Found");
            context.ApplicationInstance.CompleteRequest();
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
