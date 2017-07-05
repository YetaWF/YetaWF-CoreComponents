﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using dotless.Core;
using dotless.Core.configuration;
using dotless.Core.Loggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private const bool _renderImmediately = true;  // delaying style steets causes significant FOUC (flash of unformatted content)

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
                            else throw new InternalError("Invalid keyword {0} in statement '{1}' ({2}/{3})'.", part, info, version.Domain, version.Product);
                        }
                    }
                    if (cdn == true && !Manager.CurrentSite.UseCDNComponents)
                        continue;
                    else if (cdn == false && Manager.CurrentSite.UseCDNComponents)
                        continue;
                    // check if we want to send this file
                    string filePathURL;
                    if (file.IsAbsoluteUrl()) {
                        filePathURL = file;
                        if (bundle == true)
                            throw new InternalError("Can't use bundle with {0} in {1}/{2}", filePathURL, version.Domain, version.Product);
                        bundle = false;
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
                    if (bundle == null) {
                        if (filePathURL.ContainsIgnoreCase("/" + Globals.GlobalJavaScript + "/") || filePathURL.ContainsIgnoreCase(Globals.NugetScriptsUrl) || filePathURL.ContainsIgnoreCase(Globals.NugetContentsUrl)) {
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

            if (fullUrl.IsAbsoluteUrl() ||
                fullUrl.StartsWith(Globals.SiteFilesUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.VaultUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.VaultPrivateUrl, StringComparison.InvariantCultureIgnoreCase) ||
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

            if (!_CssFileKeys.Contains(key)) {
                string file = minify ? CssCompress(fullUrl) : fullUrl;
                if (file == null)
                    return false; // empty file
                _CssFileKeys.Add(key);
                _CssFiles.Add(new Pages.CssManager.CssEntry { Url = file, Bundle = bundle, Last = skinRelated });
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

            // Make sure we don't have multiple threads processing the same file
            StringLocks.DoAction(string.Format("{0}_{1}_{2}", AreaRegistration.CurrentPackage.AreaName, nameof(CssManager), fullCssPath.ToLower()), () => {
                if (File.Exists(fullCssPath)) {
                    if (File.GetLastWriteTimeUtc(fullCssPath) >= File.GetLastWriteTimeUtc(fullScssPath)) {
                        text = File.ReadAllText(fullCssPath);
                        return;
                    }
                }
                try {
                    NSass.SassCompiler nsass = new NSass.SassCompiler();
                    text = nsass.Compile(text);
                    File.WriteAllText(fullCssPath, text);
                } catch (Exception exc) {
                    throw new InternalError(Logging.AddErrorLog("Sass compile error in file {0}: {1}", fullScssPath, exc.Message));
                }
            });
            return text;
        }

        public static string CompileLess(string fullLessPath, string text) {
            // http://stackoverflow.com/questions/4798154/how-can-i-output-errors-when-using-less-programmatically
            // create a compiled .css file if it doesn't exist or is older than .less
            if (!File.Exists(fullLessPath))
                throw new InternalError("File {0} not found - can't be compiled to css", fullLessPath);
            string fullCssPath = Path.ChangeExtension(fullLessPath, Globals.Compiled + ".css");

            // Make sure we don't have multiple threads processing the same file
            StringLocks.DoAction(string.Format("{0}_{1}_{2}", AreaRegistration.CurrentPackage.AreaName, nameof(CssManager), fullCssPath.ToLower()), () => {
                if (File.Exists(fullCssPath)) {
                    if (File.GetLastWriteTimeUtc(fullCssPath) >= File.GetLastWriteTimeUtc(fullLessPath)) {
                        text = File.ReadAllText(fullCssPath);
                        return;
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
            });
            return text;
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
                string url = entry.Url;
                if (Manager.CurrentSite.CanUseCDN)
                    url = Manager.GetCDNUrl(url);
                if (cr == null) {
                    string sep = url.Contains("?") ? "&amp;" : "?";
                    if (!Manager.CurrentSite.UseHttpHandler || url.ContainsIgnoreCase("/" + Globals.GlobalJavaScript + "/") || url.ContainsIgnoreCase(Globals.NugetContentsUrl))
                        tag.Append(string.Format("<link rel='stylesheet' type='text/css' href='{0}{1}{2}'>", YetaWFManager.HtmlAttributeEncode(url), sep, YetaWFManager.CacheBuster));
                    else
                        tag.Append(string.Format("<link rel='stylesheet' type='text/css' href='{0}{1}{2}={3},{4}&amp;__yVrs={5}'>", YetaWFManager.HtmlAttributeEncode(url), sep, Globals.Link_CharInfo, Manager.CharWidthAvg, Manager.CharHeight, YetaWFManager.CacheBuster));
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
                List<string> bundleList = (from s in _CssFiles orderby s.Last where s.Bundle select s.Url).ToList();
                if (bundleList.Count > 1)
                    return bundleList;
            }
            return null;
        }
    }
}
