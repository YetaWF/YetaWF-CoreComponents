/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YetaWF.Core.Addons;
using YetaWF.Core.Controllers;
using YetaWF.Core.Extensions;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

// RESEARCH: evaluate @import and replace inline to avoid multiple http requests
// Currently, we use filelistCSS.txt to include @imported css. If a css file is still @imported we get an exception in CssHttpHandler

namespace YetaWF.Core.Pages {

    public class CssManager {

        public CssManager(YetaWFManager manager) { Manager = manager; }
        protected YetaWFManager Manager { get; private set; }

        public class CssEntry {
            public string Url { get; set; }
            public bool Bundle { get; set; }
            public bool Last { get; set; } // after all addons
        }

        private readonly List<string> _CssFileKeys = new List<string>(); // already processed script files (not necessarily added to page yet)
        private readonly List<CssEntry> _CssFiles = new List<CssEntry>(); // css files to include (already minified, etc.) using <link...> tags

        public void AddAddOn(VersionManager.AddOnProduct version, params object[] args) {
            if (Manager.IsPostRequest) return;// we never add css files for Post requests
            AddFromFileList(version, args);
        }

        // Add all css files listed in filelistCSS.txt
        private void AddFromFileList(VersionManager.AddOnProduct version, params object[] args) {
            string productUrl = version.GetAddOnUrl();
            List<string> list = (from i in version.CssFiles select Path.Combine(version.CssPath, i)).ToList(); // make a copy
            foreach (var info in list) {
                bool nominify = false;
                bool? bundle = null;
                bool? cdn = null;
                bool allowCustom = false;
                string[] parts = info.Split(new Char[] { ',' });
                int count = parts.Length;
                string file;
                if (count > 0) {
                    // at least a file name is present
                    file = string.Format(parts[0].Trim(), args);
                    if (count > 1) {
                        // there are some keywords
                        for (int i = 1; i < count; ++i) {
                            var part = parts[i].Trim().ToLower();
                            if (part == "nominify") nominify = true;
                            else if (part == "bundle") bundle = true;
                            else if (part == "nobundle") bundle = false;
                            else if (part == "cdn") cdn = true;
                            else if (part == "nocdn") cdn = false;
                            else if (part == "allowcustom") allowCustom = true;
                            else throw new InternalError("Invalid keyword {0} in statement '{1}' ({2}/{3})'.", part, info, version.Domain, version.Product);
                        }
                    }
                    if (cdn == true && !Manager.CurrentSite.CanUseCDNComponents)
                        continue;
                    else if (cdn == false && Manager.CurrentSite.CanUseCDNComponents)
                        continue;
                    // check if we want to send this file
                    string filePathURL;
                    if (file.IsAbsoluteUrl()) {
                        filePathURL = file;
                        if (bundle == true)
                            throw new InternalError("Can't use bundle with {0} in {1}/{2}", filePathURL, version.Domain, version.Product);
                        if (allowCustom)
                            throw new InternalError("Can't use allowCustom with {0} in {1}/{2}", filePathURL, version.Domain, version.Product);
                        bundle = false;
                    } else if (file.StartsWith("\\")) {
                        string f;
#if MVC6
                        if (file.StartsWith("\\" + Globals.NodeModulesFolder + "\\"))
                            f = Path.Combine(YetaWFManager.RootFolderWebProject, file.Substring(1));
                        else if (file.StartsWith("\\" + Globals.BowerComponentsFolder + "\\"))
                            f = Path.Combine(YetaWFManager.RootFolderWebProject, file.Substring(1));
                        else
#endif
                            f = Path.Combine(YetaWFManager.RootFolder, file.Substring(1));
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
                    if (allowCustom) {
                        string customUrl = VersionManager.GetCustomUrlFromUrl(filePathURL);
                        string f = YetaWFManager.UrlToPhysical(customUrl);
                        if (File.Exists(f))
                            filePathURL = customUrl;
                    }
                    if (bundle == null) {
                        if (filePathURL.ContainsIgnoreCase(Globals.NodeModulesUrl) || filePathURL.ContainsIgnoreCase(Globals.BowerComponentsUrl) || filePathURL.ContainsIgnoreCase("/" + Globals.GlobalJavaScript + "/")) {
                            /* While possible to add these to a bundle, it's inefficient and can cause errors with scripts that load their own scripts */
                            bundle = false;
                        } else {
                            bundle = true;
                        }
                    }
                    if (!AddFile(version.Type == VersionManager.AddOnType.Skin, filePathURL, !nominify, (bool)bundle))
                        version.CssFiles.Remove(info);// remove empty file so we don't use it any more
                }
            }
        }

        public bool AddFile(bool skinRelated, string fullUrl, bool minify = true, bool bundle = true) {

            string key = fullUrl;

            if (fullUrl.IsAbsoluteUrl()) {
                // nothing to do
                bundle = false;
            } else if (fullUrl.StartsWith(Globals.NodeModulesUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.BowerComponentsUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.SiteFilesUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.VaultUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.VaultPrivateUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.AddOnsUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.AddOnsCustomUrl, StringComparison.InvariantCultureIgnoreCase)) {

                if (key.EndsWith(".css", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 4);
                else if (key.EndsWith(".scss", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 5);
                else if (key.EndsWith(".less", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 5);
                if (key.EndsWith(".min", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 4);
                if (key.EndsWith(".pack", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 5);

                if (fullUrl.EndsWith(".min.css") || fullUrl.EndsWith(".pack.css") ||
                        fullUrl.EndsWith(".min.less") || fullUrl.EndsWith(".pack.less") ||
                        fullUrl.EndsWith(".min.scss") || fullUrl.EndsWith(".pack.scss"))
                    minify = false;

                // get the compiled file name
                if (fullUrl.EndsWith(".scss") && !fullUrl.EndsWith(".min.scss"))
                    fullUrl = fullUrl.Substring(0, fullUrl.Length - 5) + ".css";
                else if (fullUrl.EndsWith(".less") && !fullUrl.EndsWith(".min.less"))
                    fullUrl = fullUrl.Substring(0, fullUrl.Length - 5) + ".css";
                if (minify) {
                    if (!fullUrl.EndsWith(".css"))
                        throw new InternalError("Unsupported extension for {0}", fullUrl);
                    if (Manager.Deployed && Manager.CurrentSite.CompressCSSFiles) {
                        fullUrl = fullUrl.Substring(0, fullUrl.Length - 4) + ".min.css";
                    }
                }
            } else {
                throw new InternalError("Css filename '{0}' is invalid.", fullUrl);
            }

            if (!_CssFileKeys.Contains(key)) {
                string file = CssPreProcess(fullUrl);
                if (file == null)
                    return false; // empty file
                _CssFileKeys.Add(key);
                _CssFiles.Add(new Pages.CssManager.CssEntry { Url = file, Bundle = bundle, Last = skinRelated });
            }
            return true;
        }

        private bool CanPreProcess(string fullPathUrl) {
            if (fullPathUrl.IsAbsoluteUrl())
                return false;
            return true;
        }


        private string CssPreProcess(string fullUrl) {
            if (!Manager.CurrentSite.DEBUGMODE) {
                // release mode, compile, compress, minimize
                return ProcessFile(fullUrl, !Manager.CurrentSite.UseHttpHandler);
            } else {
                // debug mode, compile only
                if (Manager.CurrentSite.UseHttpHandler) {
                    if (CanPreProcess(fullUrl)) {
                        string path = YetaWFManager.UrlToPhysical(fullUrl);
                        if (!File.Exists(path))
                            throw new InternalError("File {0} not found", fullUrl);
                    }
                    return fullUrl;
                } else {
                    return ProcessFile(fullUrl, true);
                }
            }
        }

        private string ProcessFile(string fullPathUrl, bool processCharSize) {

            if (!CanPreProcess(fullPathUrl))
                return fullPathUrl;
            if (!fullPathUrl.EndsWith(".css"))
                return fullPathUrl;

            if (!processCharSize || (!fullPathUrl.ContainsIgnoreCase(Globals.AddOnsUrl) && !fullPathUrl.ContainsIgnoreCase(Globals.AddOnsCustomUrl)))
                return fullPathUrl;

            // process css with charsize
            string fullPath = YetaWFManager.UrlToPhysical(fullPathUrl);
            if (!File.Exists(fullPath))
                throw new InternalError("File {0} not found - can't be processed", fullPath);

            string extension = Path.GetExtension(fullPath);
            string minPathUrl = fullPath.Remove(fullPathUrl.Length - extension.Length);
            string minPathUrlWithCharInfo = minPathUrl;

            // add character size to css
            if (extension != ".css") {
                minPathUrl += extension;
                minPathUrlWithCharInfo += extension;
            }
            minPathUrlWithCharInfo += string.Format("._ci_{0}_{1}", Manager.CharWidthAvg, Manager.CharHeight);
            minPathUrlWithCharInfo += ".css";
            minPathUrl += ".css";

            string minPath = YetaWFManager.UrlToPhysical(minPathUrl);
            string minPathWithCharInfo = YetaWFManager.UrlToPhysical(minPathUrlWithCharInfo);

            if (File.Exists(minPathWithCharInfo)) {
                if (File.GetLastWriteTimeUtc(minPathWithCharInfo) >= File.GetLastWriteTimeUtc(fullPath)) {
                    minPathUrl = minPathUrlWithCharInfo;
                    return minPathUrl;
                }
            }
            if (File.Exists(minPath)) {
                if (File.GetLastWriteTimeUtc(minPath) >= File.GetLastWriteTimeUtc(fullPath))
                    return minPathUrl;
            }
            Logging.AddLog("Processing {0}", minPath);

            // Make sure we don't have multiple threads processing the same file
            StringLocks.DoAction("YetaWF##Packer_" + fullPath, () => {
                minPath = ProcessOneFileCharSize(fullPath, minPath, minPathWithCharInfo);
                if (minPath == null) {// empty file
                    Logging.AddLog("Processed and discarded {0}, because it's empty", fullPath);
                    minPathUrl = null;
                } else
                    minPathUrl = YetaWFManager.PhysicalToUrl(minPath);
            });
            return minPathUrl;
        }

        private string ProcessOneFileCharSize(string fullPath, string minPath, string minPathWithCharInfo) {
            string text = File.ReadAllText(minPath);
            if (!string.IsNullOrWhiteSpace(text)) {
                string newText = ProcessCss(text, Manager.CharWidthAvg, Manager.CharHeight);
                if (newText != text) {
                    text = newText;
                    minPath = minPathWithCharInfo;
                }
                File.WriteAllText(minPath, text);
            } else {
                // this was an empty file, discard it
                File.Delete(minPath);
                minPath = null;
            }
            return minPath;
        }

        private static readonly Regex varChRegex = new Regex("(?'num'[0-9\\.]+)\\s*ch(?'delim'(\\s*|;|\\}))", RegexOptions.Compiled | RegexOptions.Multiline);

        public string ProcessCss(string text, int avgCharWidth, int charHeight) {
            // replace all instances of nn ch with pixels
            text = varChRegex.Replace(text, match => ProcessChMatch(match, avgCharWidth));
            return text;
        }
        private string ProcessChMatch(Match match, int avgCharWidth) {
            string num = match.Groups["num"].Value;
            string delim = match.Groups["delim"].Value;
            string pix = match.ToString();
            if (num != ".") {
                try {
                    double f = Convert.ToDouble(num);
                    pix = string.Format("{0}px{1}", Math.Round(f * avgCharWidth, 0), delim);
                } catch (Exception) { }
            }
            return pix;
        }

        // RENDER
        // RENDER
        // RENDER

        private bool WantBundle(PageContentController.PageContentData cr) {
            if (cr != null)
                return Manager.CurrentSite.BundleCSSFilesContent;
            else
                return Manager.CurrentSite.BundleCSSFiles;
        }

        public HtmlBuilder Render(PageContentController.PageContentData cr = null, List<string> KnownCss = null) {
            HtmlBuilder tag = new HtmlBuilder();

            List<CssEntry> externalList;
            if (!Manager.CurrentSite.DEBUGMODE && WantBundle(cr)) {
                List<string> bundleList = (from s in _CssFiles orderby s.Last where s.Bundle select s.Url).ToList();
                if (KnownCss != null)
                    bundleList = bundleList.Except(KnownCss).ToList();
                if (bundleList.Count > 1) {
                    externalList = (from s in _CssFiles orderby s.Last where !s.Bundle select s).ToList();
                    string bundleUrl = FileBundles.MakeBundle(bundleList, FileBundles.BundleTypeEnum.CSS);
                    if (!string.IsNullOrWhiteSpace(bundleUrl))
                        externalList.Add(new Pages.CssManager.CssEntry {
                            Url = bundleUrl,
                            Bundle = false,
                            Last = true,
                        });
                    if (cr != null)
                        cr.CssBundleFiles.AddRange(bundleList);
                } else
                    externalList = (from s in _CssFiles orderby s.Last select s).ToList();
            } else {
                externalList = (from s in _CssFiles orderby s.Last select s).ToList();
            }

            foreach (CssEntry entry in externalList) {
                string url = MakeCssUrl(entry.Url);
                if (cr == null) {
                    tag.Append(string.Format("<link rel='stylesheet' type='text/css' href='{0}'>", YetaWFManager.HtmlAttributeEncode(url)));
                } else {
                    if (KnownCss == null || !KnownCss.Contains(url))
                        cr.CssFiles.Add(url);
                }
            }
            return tag;
        }
        /// <summary>
        /// Returns the list of css files in the current bundle (if any).
        /// </summary>
        /// <returns></returns>
        internal List<string> GetBundleFiles() {
            if (!Manager.CurrentSite.DEBUGMODE && WantBundle(null)) {
                List<string> bundleList = (from s in _CssFiles orderby s.Last where s.Bundle select MakeCssUrl(s.Url)).ToList();
                if (bundleList.Count > 1)
                    return bundleList;
            }
            return null;
        }

        private string MakeCssUrl(string url) {
            url = url.AddUrlCharInfo();
            url = Manager.GetCDNUrl(url);
            return url;
        }
    }
}
