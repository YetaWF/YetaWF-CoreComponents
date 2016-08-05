/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using dotless.Core;
using dotless.Core.configuration;
using dotless.Core.Loggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

// RESEARCH: evaluate @import and replace inline to avoid multiple http requests
// Currently, we use filelistCSS.txt to include @imported css. If a css file is still @imported we get an exception in CssHttpHandler

namespace YetaWF.Core.Pages {

    public class CssManager {

        public CssManager(YetaWFManager manager) { Manager = manager; }
        protected YetaWFManager Manager { get; private set; }

        private const bool _renderImmediately = true;  // delaying style steets causes significant FOUC (flash of unformatted content)

        private readonly Dictionary<string, string> _CssFileUrls = new Dictionary<string, string>(); // already processed css files (not necessarily added to page yet)
        private readonly List<string> _CssFiles = new List<string>(); // css files to include (already minified, etc.) using <link...> tags
        private readonly List<string> _LastCssFiles = new List<string>(); // css files to include (already minified, etc.) using <link...> tags at the end of all css files

        public void AddAddOn(VersionManager.AddOnProduct version, params object[] args) {
            if (Manager.IsAjaxRequest) return;// we never add css files for Ajax requests
            AddFromFileList(version, args);
        }

        // Add all css files listed in filelistCSS.txt
        private void AddFromFileList(VersionManager.AddOnProduct version, params object[] args) {
            string productUrl = version.GetAddOnUrl();
            List<string> list = (from i in version.CssFiles select Path.Combine(version.CssPath, i)).ToList(); // make a copy
            foreach (var info in list) {
                string file = string.Format(info, args);
                string filePathURL;
                if (file.StartsWith("http://") || file.StartsWith("https://") || file.StartsWith("//")) {
                    filePathURL = file;
                } else if (file.StartsWith("\\")) {
                    string f = Path.Combine(YetaWFManager.RootFolder, file.Substring(1));
                    if (!File.Exists(f))
                        throw new InternalError("File list has physical file {0} which doesn't exist at {1}", file, f);
                    filePathURL = YetaWFManager.PhysicalToUrl(f);
                } else {
                    file = file.Replace("\\", "/");// convert to Url in case this is file spec
                    filePathURL = string.Format("{0}{1}", productUrl, file);
                    string fullPath = YetaWFManager.UrlToPhysical(filePathURL);
                    if (!File.Exists(fullPath))
                        throw new InternalError("File list has relative url {0} which doesn't exist in {1}/{2}", filePathURL, version.Domain, version.Product);
                }
                if (!AddFile(version.Type == VersionManager.AddOnType.Skin, filePathURL))
                    version.CssFiles.Remove(info);// remove empty file so we don't use it any more
            }
        }

        public bool AddFile(bool skinRelated, string fullUrl) {

            string key = fullUrl;

            if (fullUrl.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.SiteFilesUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.VaultUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.AddOnsUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.AddOnsCustomUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.NugetContentsUrl, StringComparison.InvariantCultureIgnoreCase)) {

                if (key.EndsWith(".css", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 4);
                if (key.EndsWith(".scss", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 5);
                if (key.EndsWith(".less", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 5);
                if (key.EndsWith(".min", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 4);
                if (key.EndsWith(".pack", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 5);

            } else {
                throw new InternalError("Css filename '{0}' is invalid.", fullUrl);
            }

            if (!_CssFileUrls.ContainsKey(key)) {
                string file = CssCompress(fullUrl);
                if (file == null)
                    return false; // empty file
                _CssFileUrls.Add(key, fullUrl);
                if (skinRelated)
                    _LastCssFiles.Add(file);
                else
                    _CssFiles.Add(file);
            }
            return true;
        }

        private string CssCompress(string fullUrl) {
            Packer packer = new Packer();
            if (!Manager.CurrentSite.DEBUGMODE) {
                // release mode, compile, compress, minimize
                return packer.ProcessFile(fullUrl, Packer.PackMode.CSS, Manager.CurrentSite.CompressCSSFiles, !Manager.CurrentSite.UseHttpHandler);
            } else {
                // debug mode, compile only
                if (Manager.CurrentSite.UseHttpHandler) {
                    if (packer.CanCompress(fullUrl, Packer.PackMode.CSS)) {
                        string path = YetaWFManager.UrlToPhysical(fullUrl);
                        if (!File.Exists(path))
                            throw new InternalError("File {0} not found - can't be minimized", fullUrl);
                    }
                    return fullUrl;
                } else {
                    return packer.ProcessFile(fullUrl, Packer.PackMode.CSS, false, true);
                }
            }
        }
        public static string CompileNSass(string fullScssPath, string text) {

            // create a compiled .css file if it doesn't exist or is older than .scss
            if (!File.Exists(fullScssPath))
                throw new InternalError("File {0} not found - can't be compiled to css", fullScssPath);
            string fullCssPath = Path.ChangeExtension(fullScssPath, Globals.Compiled + ".css");

            if (File.Exists(fullCssPath)) {
                if (File.GetLastWriteTimeUtc(fullCssPath) >= File.GetLastWriteTimeUtc(fullScssPath)) {
                    return File.ReadAllText(fullCssPath);
                }
            }
            try {
                NSass.SassCompiler nsass = new NSass.SassCompiler();
                text = nsass.Compile(text);
                File.WriteAllText(fullCssPath, text);
            } catch (Exception exc) {
                throw new InternalError(Logging.AddErrorLog("Sass compile error in file {0}: {1}", fullScssPath, exc.Message));
            }
            return text;
        }
        public static string CompileLess(string fullLessPath, string text) {
            // http://stackoverflow.com/questions/4798154/how-can-i-output-errors-when-using-less-programmatically
            // create a compiled .css file if it doesn't exist or is older than .less
            if (!File.Exists(fullLessPath))
                throw new InternalError("File {0} not found - can't be compiled to css", fullLessPath);
            string fullCssPath = Path.ChangeExtension(fullLessPath, Globals.Compiled + ".css");

            if (File.Exists(fullCssPath)) {
                if (File.GetLastWriteTimeUtc(fullCssPath) >= File.GetLastWriteTimeUtc(fullLessPath)) {
                    return File.ReadAllText(fullCssPath);
                }
            }
            try {
                ILessEngine lessEngine = new EngineFactory(new DotlessConfiguration {
                    CacheEnabled = false,
                    DisableParameters = true,
                    LogLevel = LogLevel.Error,
                    MinifyOutput = true
                }).GetEngine();

                lessEngine.CurrentDirectory = Path.GetDirectoryName(fullCssPath);
                text = lessEngine.TransformToCss(text, null);
                File.WriteAllText(fullCssPath, text);
            } catch (Exception exc) {
                throw new InternalError(Logging.AddErrorLog("Less compile error in file {0}: {1}", fullLessPath, exc.Message));
            }
            return text;
        }

        // RENDER
        // RENDER
        // RENDER

        public HtmlBuilder Render() {
            HtmlBuilder tag = new HtmlBuilder();

            List<string> list = new List<string>();
            list.AddRange(_CssFiles);
            list.AddRange(_LastCssFiles);
            if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.BundleCSSFiles)
                list = FileBundles.MakeBundle(list, FileBundles.BundleTypeEnum.CSS);

            foreach (var src in list) {
                string sep = src.Contains("?") ? "&amp;" : "?";
                string url = src;
                if (Manager.CurrentSite.CanUseCDN)
                    url = Manager.GetCDNUrl(url);
                if (!Manager.CurrentSite.UseHttpHandler || url.Contains("/" + Globals.GlobalJavaScript + "/") || url.Contains(Globals.NugetContentsUrl))
                    tag.Append(string.Format("<link rel='stylesheet' type='text/css' href='{0}{1}{2}'>", YetaWFManager.HtmlAttributeEncode(url), sep, YetaWFManager.CacheBuster));
                else
                    tag.Append(string.Format("<link rel='stylesheet' type='text/css' href='{0}{1}{2}={3},{4}&amp;__yVrs={5}'>", YetaWFManager.HtmlAttributeEncode(url), sep, Globals.Link_CharInfo, Manager.CharWidthAvg, Manager.CharHeight, YetaWFManager.CacheBuster));
            }
            return tag;
        }
    }
}
