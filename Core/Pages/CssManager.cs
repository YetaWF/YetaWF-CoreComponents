/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Controllers;
using YetaWF.Core.Extensions;
using YetaWF.Core.IO;
using YetaWF.Core.Support;

// RESEARCH: evaluate @import and replace inline to avoid multiple http requests
// Currently, we use filelistCSS.txt to include @imported css. If a css file is still @imported we get an exception in CssHttpHandler

namespace YetaWF.Core.Pages {

    public class CssManager {

        public CssManager(YetaWFManager manager) { 
            Manager = manager;
            if (CssLegacy.SupportLegacyBrowser() && CssLegacy.IsLegacyBrowser(manager.CurrentRequest))
                LegacyManager = new CssLegacy();
        }
        protected YetaWFManager Manager { get; private set; }
        protected CssLegacy? LegacyManager { get; private set; }

        public class CssEntry {
            public string Url { get; set; } = null!;
            public bool Bundle { get; set; }
            public bool Last { get; set; } // after all addons
        }

        private readonly List<string> _CssFileKeys = new List<string>(); // already processed script files (not necessarily added to page yet)
        private readonly List<CssEntry> _CssFiles = new List<CssEntry>(); // css files to include (already minified, etc.) using <link...> tags

        /// <summary>
        /// Returns CSS classes which identify the MVC version provided by the <paramref name="version"/> parameter.
        /// </summary>
        /// <param name="version">The MVC version.</param>
        /// <returns>Returns CSS classes.</returns>
        public static string GetAspNetCss(Utility.AspNetMvcVersion version) {
            return version switch {
                Utility.AspNetMvcVersion.MVC5 => "yASPNET4 yMVC5",
                Utility.AspNetMvcVersion.MVC6 => "yASPNETCore yMVC6",
                _ => string.Empty,
            };
        }
        /// <summary>
        /// Combines two strings containing CSS class(es).
        /// </summary>
        /// <param name="css1">A string containing 0, 1 or multiple space separated CSS classes. May be null.</param>
        /// <param name="css2">A string containing 0, 1 or multiple space separated CSS classes. May be null.</param>
        /// <returns>Returns the combined CSS classes.</returns>
        /// <remarks>
        /// This method does not eliminate duplicate CSS classes.
        /// </remarks>
        public static string CombineCss(string? css1, string? css2) {
            if (string.IsNullOrWhiteSpace(css1)) {
                if (string.IsNullOrWhiteSpace(css2))
                    return string.Empty;
                return css2.Trim();
            } else if (string.IsNullOrWhiteSpace(css2)) {
                return css1.Trim();
            } else {
                return $"{css1.Trim()} {css2.Trim()}";
            }
        }
        /// <summary>
        /// Add CSS class to a string containing CSS class(es).
        /// </summary>
        /// <param name="css">A string containing 0, 1 or multiple space separated CSS classes. May be null.</param>
        /// <param name="add">A string containing a CSS class.</param>
        /// <remarks>The CSS class <paramref name="add"/> is only added if the classes <paramref name="css"/> don't already contain the specified class.</remarks>
        public static string AddCss(string? css, string add) {
            if (string.IsNullOrWhiteSpace(css)) return add;
            if (!ContainsCss(css, add))
                return CombineCss(css, add);
            return css;
        }
        /// <summary>
        /// Tests whether a string with blank separated CSS classes contains a CSS class.
        /// </summary>
        /// <param name="css">The string containing blank separated CSS classes.</param>
        /// <param name="search">The CSS class to search for.</param>
        /// <returns>true if the CSS class is found, false otherwise.</returns>
        public static bool ContainsCss(string css, string search) {
            string[] parts = css.Split(new char[] { ' ' });
            return (from p in parts where p == search select p).FirstOrDefault() != null;
        }

        public async Task AddAddOnAsync(VersionManager.AddOnProduct version, params object?[] args) {
            if (Manager.IsPostRequest) return;// we never add css files for Post requests
            await AddFromFileListAsync(version, args);
        }

        // Add all css files listed in filelistCSS.txt
        private async Task AddFromFileListAsync(VersionManager.AddOnProduct version, params object?[] args) {
            foreach (VersionManager.AddOnProduct.UsesInfo uses in version.CssUses) {
                await Manager.AddOnManager.AddAddOnNamedCssAsync(uses.PackageName, uses.AddonName);
            }
            List<string> list = version.CssFiles.ToList(); // make a copy in case we remove an empty file
            foreach (var info in list) {
                bool nominify = false;
                bool? bundle = null;
                bool? cdn = null;
                bool? deployed = null;
                bool allowCustom = false;
                string[] parts = info.Split(new Char[] { ',' });
                int count = parts.Length;
                string file;
                if (count > 0) {
                    // at least a file name is present
                    file = parts[0].Trim();
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
                            else if (part == "deployed") deployed = true;
                            else if (part == "notdeployed") deployed = false;
                            else if (part == "allowcustom") allowCustom = true;
                            else throw new InternalError("Invalid keyword {0} in statement '{1}' ({2}/{3})'.", part, info, version.Domain, version.Product);
                        }
                    }
                    if (cdn == true && !Manager.CurrentSite.CanUseCDNComponents)
                        continue;
                    else if (cdn == false && Manager.CurrentSite.CanUseCDNComponents)
                        continue;
                    else if (deployed == true && !YetaWFManager.Deployed)
                        continue;
                    else if (deployed == false && YetaWFManager.Deployed)
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
                    } else {
                        if (!string.IsNullOrWhiteSpace(file)) {
                            if (!string.IsNullOrWhiteSpace(version.CssPath))
                                file = Path.Combine(version.CssPath, file);
                        }
                        if (file.StartsWith("/")) {
                            string f;
                            if (file.StartsWith("/" + Globals.NodeModulesFolder + "/"))
                                f = Path.Combine(YetaWFManager.RootFolderWebProject, Utility.FileToPhysical(file.Substring(1)));
                            else if (file.StartsWith("/" + Globals.BowerComponentsFolder + "/"))
                                f = Path.Combine(YetaWFManager.RootFolderWebProject, Utility.FileToPhysical(file.Substring(1)));
                            else
                                f = Path.Combine(YetaWFManager.RootFolder, Utility.FileToPhysical(file.Substring(1)));
                            if (YetaWFManager.DiagnosticsMode) {
                                if (!await FileSystem.FileSystemProvider.FileExistsAsync(f))
                                    throw new InternalError($"File list has physical file {file} which doesn't exist at {f}");
                            }
                            filePathURL = Utility.PhysicalToUrl(f);
                        } else {
                            file = file.Replace("\\", "/");// convert to Url in case this is file spec (probably no longer used)

                            string addonUrl = version.GetAddOnUrl();
                            filePathURL = $"{addonUrl}{file}";
                            if (YetaWFManager.DiagnosticsMode) {
                                string fullPath = Utility.UrlToPhysical(filePathURL);
                                if (!await FileSystem.FileSystemProvider.FileExistsAsync(fullPath))
                                    throw new InternalError($"File list has relative URL {filePathURL} which doesn't exist in {version.Domain}/{version.Product}");
                            }
                        }
                    }
                    if (allowCustom) {
                        string customUrl = VersionManager.GetCustomUrlFromUrl(filePathURL);
                        string f = Utility.UrlToPhysical(customUrl);
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
                    if (!await AddFileAsync(false, filePathURL, !nominify, (bool)bundle))
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

                if (fullUrl.EndsWith(".min.css", StringComparison.InvariantCultureIgnoreCase) || fullUrl.EndsWith(".pack.css", StringComparison.InvariantCultureIgnoreCase) ||
                        fullUrl.EndsWith(".min.less", StringComparison.InvariantCultureIgnoreCase) || fullUrl.EndsWith(".pack.less", StringComparison.InvariantCultureIgnoreCase) ||
                        fullUrl.EndsWith(".min.scss", StringComparison.InvariantCultureIgnoreCase) || fullUrl.EndsWith(".pack.scss", StringComparison.InvariantCultureIgnoreCase))
                    minify = false;

                // get the compiled file name
                if (fullUrl.EndsWith(".scss", StringComparison.InvariantCultureIgnoreCase) && !fullUrl.EndsWith(".min.scss", StringComparison.InvariantCultureIgnoreCase))
                    fullUrl = fullUrl.Substring(0, fullUrl.Length - 5) + ".css";
                else if (fullUrl.EndsWith(".less", StringComparison.InvariantCultureIgnoreCase) && !fullUrl.EndsWith(".min.less", StringComparison.InvariantCultureIgnoreCase))
                    fullUrl = fullUrl.Substring(0, fullUrl.Length - 5) + ".css";
                if (minify) {
                    if (!fullUrl.EndsWith(".css"))
                        throw new InternalError("Unsupported extension for {0}", fullUrl);
                    if (YetaWFManager.Deployed && Manager.CurrentSite.CompressCSSFiles) {
                        fullUrl = fullUrl.Substring(0, fullUrl.Length - 4) + ".min.css";
                    }
                }
                if (LegacyManager != null)
                    fullUrl = await LegacyManager.GetLegacyAddonUrlAsync(fullUrl);
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
            string fullPath = Utility.UrlToPhysical(fullUrl);
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

        private bool WantBundle(PageContentController.PageContentData? cr) {
            if (cr != null)
                return false;
            else
                return Manager.CurrentSite.BundleCSSFiles;
        }

        internal async Task<string> RenderAsync(PageContentController.PageContentData? cr = null, List<string>? KnownCss = null) {
            HtmlBuilder tag = new HtmlBuilder();

            List<CssEntry> externalList;
            if (!Manager.CurrentSite.DEBUGMODE && WantBundle(cr)) {
                List<string> bundleList = (from s in _CssFiles orderby s.Last where s.Bundle select s.Url).ToList();
                if (KnownCss != null)
                    bundleList = bundleList.Except(KnownCss).ToList();
                if (bundleList.Count > 1) {
                    externalList = (from s in _CssFiles orderby s.Last where !s.Bundle select s).ToList();
                    string? bundleUrl = await FileBundles.MakeBundleAsync(bundleList, FileBundles.BundleTypeEnum.CSS);
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
                    tag.Append(string.Format("<link rel='stylesheet' type='text/css' data-name='{0}' href='{1}'>", Utility.HAE(entry.Url), Utility.HAE(url)));
                } else {
                    if (KnownCss == null || !KnownCss.Contains(entry.Url)) {
                        cr.CssFiles.Add(new Controllers.PageContentController.UrlEntry {
                            Name = entry.Url,
                            Url = entry.Url,
                        });
                        if (entry.Bundle) {
                            string file = Utility.UrlToPhysical(entry.Url);
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
            return tag.ToString();
        }
        /// <summary>
        /// Returns the list of css files in the current bundle (if any).
        /// </summary>
        /// <returns></returns>
        internal List<string>? GetBundleFiles() {
            if (!Manager.CurrentSite.DEBUGMODE && WantBundle(null)) {
                List<string> bundleList = (from s in _CssFiles orderby s.Last where s.Bundle select s.Url).ToList();
                if (bundleList.Count > 1)
                    return bundleList;
            }
            return null;
        }
    }
}
