/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider;
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

        public Task InitializeApplicationStartupAsync() {
            // Delete all temp images
            string physFolder = Path.Combine(YetaWFManager.RootFolder, Globals.LibFolder, Globals.TempImagesFolder);
            YetaWF.Core.IO.DirectoryIO.DeleteFolder(physFolder);
            // Create folder for temp images
            YetaWF.Core.IO.DirectoryIO.CreateFolder(physFolder);
            return Task.CompletedTask;
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

        public class GetImageAsFileInfo {
            public bool Success { get; set; }
            public string File { get; set; }
        }
        public class GetImageInBytesInfo {
            public bool Success { get; set; }
            public byte[] Content { get; set; }
        }

        public delegate Task<GetImageInBytesInfo> GetImageInBytesAsync(string name, string location);
        public delegate Task<GetImageAsFileInfo> GetImageAsFileAsync(string name, string location);

        public class ImageHandlerEntry {
            public string Type { get; set; }
            public GetImageInBytesAsync GetImageInBytesAsync { get; set; }
            public GetImageAsFileAsync GetImageAsFileAsync { get; set; }
        }

        public ImageSupport() {
            DisposableTracker.AddObject(this);
        }

        public static void AddHandler(string type, GetImageInBytesAsync GetBytesAsync = null, GetImageAsFileAsync GetAsFileAsync = null) {
            HandlerEntries.Add(new ImageHandlerEntry { Type = type, GetImageInBytesAsync = GetBytesAsync, GetImageAsFileAsync = GetAsFileAsync });
        }

        public static List<ImageHandlerEntry> HandlerEntries = new List<ImageHandlerEntry>();

        protected virtual void Dispose(bool disposing) { if (disposing) DisposableTracker.RemoveObject(this); }
        public void Dispose() { Dispose(true); }

        // IINSTALLABLEMODEL (used so we can install the scheduler events)
        // IINSTALLABLEMODEL
        // IINSTALLABLEMODEL

        public Task<bool> IsInstalledAsync() {
            return Task.FromResult(true);
        }
        public Task<bool> InstallModelAsync(List<string> errorList) {
            return Task.FromResult(true);
        }
        public Task<bool> UninstallModelAsync(List<string> errorList) {
            return Task.FromResult(true);
        }
        public Task AddSiteDataAsync() { return Task.CompletedTask;  }
        public Task RemoveSiteDataAsync() { return Task.CompletedTask; }
        public Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList) { return Task.FromResult(new DataProviderExportChunk()); }
        public Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj) { return Task.CompletedTask; }

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

        static Regex _imgCDNRe = new Regex(@"(?'kwd'\s+(src|href)=)(?'quot'(""|'))(?'url'/FileHndlr\.image\?Type=[^\'\""]*)", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Replace all ..href="/FileHndlr.image..." and ..src="/FileHndlr.image..." with CDN Url
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
    }
}
