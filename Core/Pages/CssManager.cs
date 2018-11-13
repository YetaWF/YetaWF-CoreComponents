/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public async Task AddAddOnAsync(VersionManager.AddOnProduct version, params object[] args) {
            if (Manager.IsPostRequest) return;// we never add css files for Post requests
            await AddFromFileListAsync(version, args);
        }

        // Add all css files listed in filelistCSS.txt
        private async Task AddFromFileListAsync(VersionManager.AddOnProduct version, params object[] args) {
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
                    file = parts[0].Trim();
                    if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.BootstrapSkin))
                        file = file.Replace("{BootstrapSkin}", Manager.CurrentSite.BootstrapSkin);
                    file = string.Format(file, args);
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
                        if (YetaWFManager.DiagnosticsMode) {
                            if (!await FileSystem.FileSystemProvider.FileExistsAsync(f))
                                throw new InternalError("File list has physical file {0} which doesn't exist at {1}", file, f);
                        }
                        filePathURL = YetaWFManager.PhysicalToUrl(f);
                    } else {
                        file = file.Replace("\\", "/");// convert to Url in case this is file spec
                        filePathURL = string.Format("{0}{1}", productUrl, file);
                        string fullPath = YetaWFManager.UrlToPhysical(filePathURL);
                        if (YetaWFManager.DiagnosticsMode) {
                            if (!await FileSystem.FileSystemProvider.FileExistsAsync(fullPath))
                                throw new InternalError("File list has relative url {0} which doesn't exist in {1}/{2}", filePathURL, version.Domain, version.Product);
                        }
                    }
                    if (allowCustom) {
                        string customUrl = VersionManager.GetCustomUrlFromUrl(filePathURL);
                        string f = YetaWFManager.UrlToPhysical(customUrl);
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(f))
                            filePathURL = customUrl;
                    }
                    if (bundle == null) {
                        if (filePathURL.ContainsIgnoreCase(Globals.NodeModulesUrl) || filePathURL.ContainsIgnoreCase(Globals.BowerComponentsUrl)) {
                            /* While possible to add these to a bundle, it's inefficient and can cause errors with scripts that load their own scripts */
                            bundle = false;
                        } else {
                            bundle = true;
                        }
                    }
                    if (!await AddFileAsync(version.Type == VersionManager.AddOnType.Skin, filePathURL, !nominify, (bool)bundle))
                        version.CssFiles.Remove(info);// remove empty file so we don't use it any more
                }
            }
        }

        public async Task<bool> AddFileAsync(bool skinRelated, string fullUrl, bool minify = true, bool bundle = true) {

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
                if (YetaWFManager.DiagnosticsMode) {
                    if (!await UrlHasContentAsync(fullUrl))
                        throw new InternalError($"File {fullUrl} is empty and should be removed");
                }
                _CssFileKeys.Add(key);
                _CssFiles.Add(new Pages.CssManager.CssEntry { Url = fullUrl, Bundle = bundle, Last = skinRelated });
            }
            return true;
        }

        private async Task<bool> UrlHasContentAsync(string fullUrl) {
            if (fullUrl.IsAbsoluteUrl())
                return true;
            if (!fullUrl.EndsWith(".css"))
                return true;
            if (!fullUrl.ContainsIgnoreCase($"{Globals.AddOnsUrl}/") && !fullUrl.ContainsIgnoreCase($"{Globals.AddOnsCustomUrl}/"))
                return true;
            string fullPath = YetaWFManager.UrlToPhysical(fullUrl);
            if (YetaWFManager.DiagnosticsMode) {
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(fullPath))
                    throw new InternalError("File {0} not found - can't be processed", fullPath);
            }
            string text = await FileSystem.FileSystemProvider.ReadAllTextAsync(fullPath);
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return true;
        }

        // RENDER
        // RENDER
        // RENDER

        private bool WantBundle(PageContentController.PageContentData cr) {
            if (cr != null)
                return false;
            else
                return Manager.CurrentSite.BundleCSSFiles;
        }

        public async Task<HtmlBuilder> RenderAsync(PageContentController.PageContentData cr = null, List<string> KnownCss = null) {
            HtmlBuilder tag = new HtmlBuilder();

            List<CssEntry> externalList;
            if (!Manager.CurrentSite.DEBUGMODE && WantBundle(cr)) {
                List<string> bundleList = (from s in _CssFiles orderby s.Last where s.Bundle select s.Url).ToList();
                if (KnownCss != null)
                    bundleList = bundleList.Except(KnownCss).ToList();
                if (bundleList.Count > 1) {
                    externalList = (from s in _CssFiles orderby s.Last where !s.Bundle select s).ToList();
                    string bundleUrl = await FileBundles.MakeBundleAsync(bundleList, FileBundles.BundleTypeEnum.CSS);
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
                string url = Manager.GetCDNUrl(entry.Url);
                if (cr == null) {
                    tag.Append(string.Format("<link rel='stylesheet' type='text/css' data-name='{0}' href='{1}'>", YetaWFManager.HtmlAttributeEncode(entry.Url), YetaWFManager.HtmlAttributeEncode(url)));
                } else {
                    if (KnownCss == null || !KnownCss.Contains(entry.Url)) {
                        cr.CssFiles.Add(new Controllers.PageContentController.UrlEntry {
                            Name = entry.Url,
                            Url = entry.Url,
                        });
                        if (entry.Bundle) {
                            string file = YetaWFManager.UrlToPhysical(entry.Url);
                            string contents = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
                            contents = FileBundles.ProcessIncludedFiles(contents, entry.Url);
                            cr.CssFilesPayload.Add(new PageContentController.Payload {
                                Name = entry.Url,
                                Text = contents,
                            });
                        }
                    }
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
