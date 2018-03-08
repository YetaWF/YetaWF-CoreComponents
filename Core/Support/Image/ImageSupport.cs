/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Scheduler;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Upload;

namespace YetaWF.Core.Image {

    public class ImageSupportInit : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        // IInitializeApplicationStartup
        // IInitializeApplicationStartup

        public void InitializeApplicationStartup() {
            // Delete all temp images
            string physFolder = Path.Combine(YetaWFManager.RootFolder, Globals.LibFolder, Globals.TempImagesFolder);
            YetaWF.Core.IO.DirectoryIO.DeleteFolder(physFolder);
            // Create folder for temp images
            YetaWF.Core.IO.DirectoryIO.CreateFolder(physFolder);
        }

    }
    public class ImageSupportScheduling : IScheduling {

        // IScheduling
        // IScheduling
        // IScheduling

        public const string EventRemoveTempFiles = "YetaWF.ImageSupport: Remove Temp Files";

        public SchedulerItemBase[] GetItems() {
            return new SchedulerItemBase[] {
                new SchedulerItemBase {
                    Name=this.__ResStr("eventName", "Remove Temporary Files (uploaded files)"),
                    Description = this.__ResStr("eventDesc", "Removes temporary files that are too old (files that were created on or before the last time this event ran, based on Frequency definition)"),
                    EventName = EventRemoveTempFiles,
                    Enabled = true,
                    EnableOnStartup = true,
                    RunOnce=false,
                    Startup = false,
                    SiteSpecific = false,
                    Frequency = new SchedulerFrequency { TimeUnits = SchedulerFrequency.TimeUnitEnum.Days, Value=2 },
                },
            };
        }

        public Task RunItemAsync(SchedulerItemBase evnt) {
            if (evnt.EventName != EventRemoveTempFiles)
                throw new Error(this.__ResStr("eventNameErr", "Unknown scheduler event {0}."), evnt.EventName);
            FileUpload fileUpload = new FileUpload();
            fileUpload.RemoveAllExpiredTempFiles(evnt.Frequency.TimeSpan);
            return Task.CompletedTask;
        }
    }

    public class ImageSupport : IInstallableModel, IDisposable {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public const char ImageSeparator = '#'; // anything after this is ignored (and usually used to defeat client side caching)
        // note that the separator is used in the Url's name= argument and is encoded so it's NOT mistaken as the # anchor
        // so don't use # for Url cache busting as that will clearly not work - use __yVrs={YetaWFManager.CacheBuster} in a Url
        // # was mistakenly used for a while in urls which caused a rewrite for css,js,etc. - stupid
        // # is only used to encode a cache buster WITHIN the image name (not used by YetaWF itself)

        public delegate bool GetImageInBytes(string name, string location, out byte[] content);
        public delegate bool GetImageAsFile(string name, string location, out string file);

        public class ImageHandlerEntry {
            public string Type { get; set; }
            public GetImageInBytes GetBytes { get; set; }
            public GetImageAsFile GetFilePath { get; set; }
        }

        public ImageSupport() {
            DisposableTracker.AddObject(this);
        }

        public static void AddHandler(string type, GetImageInBytes GetBytes = null, GetImageAsFile GetAsFile = null) {
            HandlerEntries.Add(new ImageHandlerEntry { Type = type, GetBytes = GetBytes, GetFilePath = GetAsFile });
        }

        public static List<ImageHandlerEntry> HandlerEntries = new List<ImageHandlerEntry>();

        protected virtual void Dispose(bool disposing) { if (disposing) DisposableTracker.RemoveObject(this); }
        public void Dispose() { Dispose(true); }

        // IINSTALLABLEMODEL (used so we can install the scheduler events)
        // IINSTALLABLEMODEL
        // IINSTALLABLEMODEL

        public bool IsInstalled() {
            return true;
        }
        public bool InstallModel(List<string> errorList) {
            return true;
        }
        public bool UninstallModel(List<string> errorList) {
            return true;
        }
        public void AddSiteData() { }
        public void RemoveSiteData() { }
        public bool ExportChunk(int chunk, SerializableList<SerializableFile> fileList, out object obj) { obj = null; return false; }
        public void ImportChunk(int chunk, SerializableList<SerializableFile> fileList, object obj) { }

        // IMAGE SUPPORT
        // IMAGE SUPPORT
        // IMAGE SUPPORT

        public static System.Drawing.Image NewImageSize(string filePath, int width, int height, bool stretch, out byte[] bytes) {
            System.Drawing.Image imgOrig = System.Drawing.Image.FromFile(filePath);
            return NewImageSize(imgOrig, width, height, stretch, out bytes);
        }
        public static System.Drawing.Image NewImageSize(string filePath, int percent, bool stretch, out byte[] bytes) {
            System.Drawing.Image imgOrig = System.Drawing.Image.FromFile(filePath);
            return NewImageSize(imgOrig, percent, stretch, out bytes);
        }
        public static System.Drawing.Image NewImageSize(System.Drawing.Image imgOrig, int percent, bool stretch, out byte[] bytes) {
            return NewImageSize(imgOrig, (imgOrig.Size.Width * percent) / 100, (imgOrig.Size.Height * percent) / 100, stretch, out bytes);
        }

        public static System.Drawing.Image NewImageSize(System.Drawing.Image imgOrig, int width, int height, bool stretch, out byte[] bytes) {
            bytes = null;
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            System.Drawing.Size newSize = CalcProportionalSize(new System.Drawing.Size(width, height), stretch, imgOrig.Size);
            System.Drawing.Bitmap newImage = new System.Drawing.Bitmap(width, height);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newImage)) {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                System.Drawing.Brush b = new System.Drawing.SolidBrush(System.Drawing.Color.White);
                g.FillRectangle(b, new System.Drawing.Rectangle(0, 0, width, height));
                g.DrawImage(imgOrig,
                    new System.Drawing.Rectangle(new System.Drawing.Point(0 + (width - newSize.Width) / 2, 0 + (height - newSize.Height) / 2), newSize),
                    new System.Drawing.Rectangle(System.Drawing.Point.Empty, imgOrig.Size),
                    System.Drawing.GraphicsUnit.Pixel);
            }
            imgOrig.Dispose();
            using (MemoryStream ms = new MemoryStream()) {
                newImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                bytes = ms.GetBuffer();
            }
            return newImage;
        }
        private static System.Drawing.Size CalcProportionalSize(System.Drawing.Size sizeMax, bool stretch, System.Drawing.Size sizeCurr) {
            int w, h;

            if (sizeMax.Width <= 0 || sizeMax.Height <= 0 || sizeCurr.Width <= 0 || sizeCurr.Height <= 0)
                return System.Drawing.Size.Empty;

            double maxRatio = sizeMax.Width / (double)sizeMax.Height;
            double actRatio = sizeCurr.Width / (double)sizeCurr.Height;
            if (stretch) {
                if (maxRatio < actRatio) {
                    w = sizeMax.Width;
                    h = (int)Math.Round(w / actRatio);
                } else {
                    h = sizeMax.Height;
                    w = (int)Math.Round(h * actRatio);
                }
            } else {
                if (maxRatio < actRatio) {
                    w = Math.Min(sizeMax.Width, sizeCurr.Width);
                    h = (int)Math.Round(w / actRatio);
                } else {
                    h = Math.Min(sizeMax.Height, sizeCurr.Height);
                    w = (int)Math.Round(h * actRatio);
                }
            }
            return new System.Drawing.Size(w, h);
        }

        /// <summary>
        /// Get the image size (temp files only)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static System.Drawing.Size GetImageSize(string name) {
            System.Drawing.Size size = new System.Drawing.Size();
            FileUpload fileUpload = new FileUpload();
            string filePath = fileUpload.GetTempFilePathFromName(name);
            if (filePath == null)
                return size;
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(filePath)) {
                return img.Size;
            }
        }
        /// <summary>
        /// Get the image size
        /// </summary>
        /// <param name="name"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static System.Drawing.Size GetImageSize(string name, string folder) {
            string filePath = Path.Combine(Manager.SiteFolder, folder, name);
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(filePath)) {
                return img.Size;
            }
        }
        /// <summary>
        /// Get the image size
        /// </summary>
        public static System.Drawing.Size GetImageSize(byte[] data) {
            System.Drawing.Size size = new System.Drawing.Size();
            if (data == null) return size;
            if (data.Length == 0) return size;
            using (MemoryStream ms = new MemoryStream(data)) {
                using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms)) {
                    size = img.Size;
                }
            }
            return size;
        }
        /// <summary>
        /// Replace all ..href="/File.image..." and ..src="/File.image..." with real (temp) files so we don't need to use the httphandler
        /// </summary>
        /// <param name="viewHtml">Html to edit</param>
        /// <remarks>
        /// The img tag, particularly the src= portion must follow the exact format parsed here. If args are in a different order, it won't work.
        /// </remarks>
        internal static string ProcessImagesAsStatic(string viewHtml) {
            viewHtml = _imgStatReWH.Replace(viewHtml, new MatchEvaluator(SubstImgWH));
            viewHtml = _imgStatRePercent.Replace(viewHtml, new MatchEvaluator(SubstImgPercent));
            viewHtml = _imgStatRe.Replace(viewHtml, new MatchEvaluator(SubstImg));
            return viewHtml;
        }
        static Regex _imgStatRe = new Regex(@"(?'kwd'\s+(src|href)=)""?/File.image\?Type=(?'type'[^&""]+?)&(amp;)?Location=(?'location'[^&""]*?)&(amp;)?Name(=)?(?'name'[^&""]*?)(?'rem'(""|&))", RegexOptions.Compiled | RegexOptions.Singleline);
        static Regex _imgStatReWH = new Regex(@"(?'kwd'\s+(src|href)=)""?/File.image\?Type=(?'type'[^&""]+?)&(amp;)?Location=(?'location'[^&""]*?)&(amp;)?Name=(?'name'[^&""]*?)&(amp;)?Width=(?'width'[^&""]+?)&(amp;)?Height=(?'height'[^&""]+?)(?'rem'(""|&))", RegexOptions.Compiled | RegexOptions.Singleline);
        static Regex _imgStatRePercent = new Regex(@"(?'kwd'\s+(src|href)=)""?/File.image\?Type=(?'type'[^&""]+?)&(amp;)?Location=(?'location'[^&""]*?)&(amp;)?Name=(?'name'[^&""]*?)&(amp;)?Percent=(?'percent'[^&""]+?)(?'rem'(""|&))", RegexOptions.Compiled | RegexOptions.Singleline);

        private static string SubstImg(Match m) {
            string retString = m.Value;
            try {
                string type = YetaWFManager.UrlDecodeArgs(m.Groups["type"].Value);
                string loc = YetaWFManager.UrlDecodeArgs(m.Groups["location"].Value);
                if (string.IsNullOrWhiteSpace(loc)) loc = "loc";
                string name = YetaWFManager.UrlDecodeArgs(m.Groups["name"].Value);
                string rem = m.Groups["rem"].Value;
                if (string.IsNullOrWhiteSpace(name)) name = "NoImage.png";
                string physFile = Path.Combine(YetaWFManager.RootFolder, Globals.LibFolder, Globals.TempImagesFolder, Manager.CurrentSite.Identity.ToString(), string.Format("{0}_{1}_{2}",
                    type, loc.Replace(",", "_"), FileData.MakeValidFileSystemFileName(name)));
                if (!File.Exists(physFile))
                    physFile = GetImageFromArgs(physFile, type, loc, name);
                return string.Format(@"{0}""{1}{2}", m.Groups["kwd"].Value, YetaWFManager.PhysicalToUrl(physFile), rem);
            } catch { }
            return retString;
        }
        private static string SubstImgWH(Match m) {
            string retString = m.Value;
            try {
                string type = YetaWFManager.UrlDecodeArgs(m.Groups["type"].Value);
                string loc = YetaWFManager.UrlDecodeArgs(m.Groups["location"].Value);
                if (string.IsNullOrWhiteSpace(loc)) loc = "loc";
                string name = YetaWFManager.UrlDecodeArgs(m.Groups["name"].Value);
                if (string.IsNullOrWhiteSpace(name)) name = "NoImage.png";
                int width = Convert.ToInt32(m.Groups["width"].Value);
                int height = Convert.ToInt32(m.Groups["height"].Value);
                string rem = m.Groups["rem"].Value;
                string physFile = Path.Combine(YetaWFManager.RootFolder, Globals.LibFolder, Globals.TempImagesFolder, Manager.CurrentSite.Identity.ToString(), string.Format("{0}_{1}_{2}_{3}_{4}",
                    type, loc.Replace(",", "_"), width, height, FileData.MakeValidFileSystemFileName(name)));
                if (!File.Exists(physFile))
                    physFile = GetImageFromArgs(physFile, type, loc, name, width, height);
                return string.Format(@"{0}""{1}{2}", m.Groups["kwd"].Value, YetaWFManager.PhysicalToUrl(physFile), rem);
            } catch { }
            return retString;
        }
        private static string SubstImgPercent(Match m) {
            string retString = m.Value;
            try {
                string type = YetaWFManager.UrlDecodeArgs(m.Groups["type"].Value);
                string loc = YetaWFManager.UrlDecodeArgs(m.Groups["location"].Value);
                if (string.IsNullOrWhiteSpace(loc)) loc = "loc";
                string name = YetaWFManager.UrlDecodeArgs(m.Groups["name"].Value);
                if (string.IsNullOrWhiteSpace(name)) name = "NoImage.png";
                int percent = Convert.ToInt32(m.Groups["percent"].Value);
                string rem = m.Groups["rem"].Value;
                string physFile = Path.Combine(YetaWFManager.RootFolder, Globals.LibFolder, Globals.TempImagesFolder, Manager.CurrentSite.Identity.ToString(), string.Format("{0}_{1}_{2}p_{3}",
                    type, loc.Replace(",", "_"), percent, FileData.MakeValidFileSystemFileName(name)));
                if (!File.Exists(physFile))
                    physFile = GetImageFromArgs(physFile, type, loc, name, percent: percent);
                return string.Format(@"{0}""{1}{2}", m.Groups["kwd"].Value, YetaWFManager.PhysicalToUrl(physFile), rem);
            } catch { }
            return retString;
        }

        static Regex _imgCDNRe = new Regex(@"(?'kwd'\s+(src|href)=)(?'quot'(""|'))(?'url'/File(|Hndlr)\.image\?Type=[^\'\""]*)", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Replace all ..href="/File.image..." and ..src="/File.image..." with CDN Url
        /// </summary>
        /// <param name="viewHtml">Html to edit</param>
        /// <remarks>
        /// The img tag, particularly the src= portion must follow the exact format parsed here. If args are in a different order, it won't work.
        /// </remarks>
        internal static string ProcessImagesAsCDN(string viewHtml) {
            viewHtml = _imgCDNRe.Replace(viewHtml, new MatchEvaluator(SubstCDNImg));
            return viewHtml;
        }
        private static string SubstCDNImg(Match m) {
            string retString = m.Value;
            try {
                string kwd = m.Groups["kwd"].Value;
                string quot = m.Groups["quot"].Value;
                string url = m.Groups["url"].Value;
                return string.Format(@"{0}{1}{2}", kwd, quot, Manager.GetCDNUrl(url));
            } catch { }
            return retString;
        }
        private static string GetImageFromArgs(string physFile, string typeVal, string locationVal, string nameVal, int width = -1, int height = -1, bool stretch = false, int percent = -1) {

            if (!string.IsNullOrWhiteSpace(typeVal)) {
                ImageHandlerEntry entry = (from h in HandlerEntries where h.Type == typeVal select h).FirstOrDefault();
                if (entry != null) {
                    if (!string.IsNullOrWhiteSpace(nameVal)) {
                        string[] parts = nameVal.Split(new char[] { ImageSeparator });
                        if (parts.Length > 1)
                            nameVal = parts[0];
                    }

                    // check if this is a temporary (uploaded image)
                    FileUpload fileUpload = new FileUpload();
                    string filePath = fileUpload.GetTempFilePathFromName(nameVal, locationVal);

                    // if we don't have an image yet, try to get the file from the registered type
                    if (string.IsNullOrWhiteSpace(filePath) && entry.GetFilePath != null) {
                        string file;
                        if (entry.GetFilePath(nameVal, locationVal, out file))
                            if (File.Exists(file))
                                filePath = file;
                    }
                    // if we don't have an image yet, try to get the raw bytes from the registered type
                    System.Drawing.Image img = null;
                    byte[] bytes = null;
                    if (string.IsNullOrWhiteSpace(filePath) && entry.GetBytes != null) {
                        if (entry.GetBytes(nameVal, locationVal, out bytes)) {
                            using (MemoryStream ms = new MemoryStream(bytes)) {
                                img = System.Drawing.Image.FromStream(ms);
                            }
                        }
                    }
                    // if there is no image, use a default image
                    if (img == null && string.IsNullOrWhiteSpace(filePath)) {
                        Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                        string addonUrl = VersionManager.GetAddOnTemplateUrl(package.Domain, package.Product, "Image");
                        filePath = YetaWFManager.UrlToPhysical(Path.Combine(addonUrl, "Images", "NoImage.png"));
                        if (!File.Exists(filePath))
                            throw new InternalError("The image {0} is missing", filePath);
                    }

                    // resize the image if necessary
                    if (percent > 0) {
                        // resize to fit
                        if (img != null && bytes != null) {
                            img = NewImageSize(img, percent, stretch, out bytes);
                        } else if (filePath != null) {
                            img = NewImageSize(filePath, percent, stretch, out bytes);
                            filePath = null;
                        }
                    } else {
                        if (width > 0 && height > 0) {
                            // resize to fit
                            if (img != null && bytes != null) {
                                img = NewImageSize(img, width, height, stretch, out bytes);
                            } else if (filePath != null) {
                                img = NewImageSize(filePath, width, height, stretch, out bytes);
                                filePath = null;
                            }
                        }
                    }
                    if (img != null) {
                        YetaWF.Core.IO.DirectoryIO.CreateFolder(Path.GetDirectoryName(physFile));
                        img.Save(physFile);
                        img.Dispose();
                        filePath = physFile;
                    }
                    return filePath;
                }
            }
            return physFile;
        }
    }
}
