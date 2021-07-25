/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Extensions;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    /// <summary>
    /// Creates CSS files for legacy browsers that don't support CSS custom properties (<see href="https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties" />).
    /// </summary>
    /// <remarks>For more information see <see href="https://YetaWF.com/Documentation/YetaWF/Topic/g_dev_cssvariables">CSS Custom Properties.</see>
    /// This is mainly used to support IE11 and older. By default this is not used in YetaWF.
    /// 
    /// When using Bootstrap 4 in a skin, AppSettings.json, Application:P:YetaWF_Core:SupportLegacyBrowser can be set to true to support IE11. 
    /// Otherwise, any request by IE 11 (and lower) will be redirected to a customizable "Unsupported Browser" page (/Maintenance/Unsupported Browser.html).
    /// The default skins in YetaWF use Bootstrap 5. Bootstrap 4 is only available in custom skins.
    /// </remarks>
    public class CssLegacy : IInitializeApplicationStartup {

        internal const string LEGACYPATH = "/_L";

        /// <inheritdoc/>
        public async Task InitializeApplicationStartupAsync() {

            BuiltinCommands.Add("/$css", CoreInfo.Resource_BuiltinCommands, CreateCssAsync); // used to manually rebuild all legacy css

            await FileSystem.FileSystemProvider.DeleteDirectoryAsync(Utility.UrlToPhysical(LEGACYPATH));

            if (SupportLegacyBrowser())
                await CreateCssAsync(new QueryHelper());
        }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CssLegacy() { }

        /// <summary>
        /// Returns whether the browser for the current request is considered a legacy browser.
        /// </summary>
        /// <returns>Returns whether the browser for the current request is considered a legacy browser.</returns>
        public static bool IsLegacyBrowser() {
            HttpRequest request = Manager.CurrentRequest;
            var userAgent = request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(userAgent)) return false;
            if (userAgent.Contains("MSIE ") || userAgent.Contains("Trident/")) // IE 11 or older
                return true;
            return false;
        }
        public static void ExcludeLegacyBrowser() {
            if (IsLegacyBrowser())
                throw new InternalError("This feature requires a modern browser");
        }


        internal static bool SupportLegacyBrowser() {
            if (_supportLegacyBrowser == null)
                _supportLegacyBrowser = WebConfigHelper.GetValue<bool>(AreaRegistration.CurrentPackage.AreaName, "SupportLegacyBrowser", false);
            return (bool)_supportLegacyBrowser;
        }
        private static bool? _supportLegacyBrowser = null;

        internal async Task<string> GetLegacyAddonUrlAsync(string addonUrl) {
            if (!addonUrl.StartsWith(Package.AddOnsUrl, StringComparison.InvariantCultureIgnoreCase)) // only urls in /addons folder support legacy files
                return addonUrl;
            SkinDefinition skin = Manager.CurrentSite.Skin;
            if (skin.Collection == null)
                return addonUrl;
            string[] parts = skin.Collection.Split(new char[] { '/' });
            string name = parts[1];
            string file = $"{LEGACYPATH}/{name}{addonUrl}";
            if (!await FileSystem.FileSystemProvider.FileExistsAsync(Utility.UrlToPhysical(file)))
                return addonUrl;
            return file;
        }

        private async Task CreateCssAsync(QueryHelper arg) {

            await FileSystem.FileSystemProvider.DeleteDirectoryAsync(Utility.UrlToPhysical(LEGACYPATH));

            List<Package.AddOnProduct> addons = Package.GetAvailableAddOns();
            List<Package.AddOnProduct> skins = (from a in addons where a.Type == Package.AddOnType.Skin select a).ToList();

            // process each skin and copy all css files to the legacy css folder
            foreach (Package.AddOnProduct skin in skins) {
                Dictionary<string, string> variables = await GetSkinVariablesAsync(skin);
                await CopyAddonsCSSFilesAsync(skin, addons, variables);
            }
        }

        private async Task CopyAddonsCSSFilesAsync(Package.AddOnProduct skin, List<Package.AddOnProduct> addons, Dictionary<string, string> variables) {
            string url = $"{LEGACYPATH}/{skin.Product}";
            foreach (Package.AddOnProduct addon in addons) {
                await CopyOneAddonCSSFilesAsync(skin, url, addon, variables);
            }
        }

        private async Task CopyOneAddonCSSFilesAsync(Package.AddOnProduct skin, string url, Package.AddOnProduct addon, Dictionary<string, string> variables) {
            // copy css files    
            foreach (string file in addon.CssFiles) {
                if (!string.IsNullOrWhiteSpace(addon.CssPath)) // redirected, so not one of ours, no css variables 
                    continue;
                if (file.StartsWith("http")) // no css variables in external css
                    continue;
                if (file.StartsWith("/")) // no css variables when an absolute url is given
                    continue;
                if (file.Contains("{")) // no css variables when file arguments are used
                    continue;

                string realFile = file;

                if (realFile.Contains(",")) {
                    // we can safely ignore all extra directives like bundle, cdn, etc.
                    string[] parts = realFile.Split(new char[] { ',' });
                    realFile = parts[0].Trim();
                }

                string ext = Path.GetExtension(realFile);
                if (ext == ".scss")
                    realFile = Path.ChangeExtension(realFile, "css");
                else if (ext != ".css") { 
#if DEBUG
                    throw new InternalError($"Non-CSS file found: {realFile}");
#else
                    continue;
#endif
                }

                string fromUrl = $"{addon.Url}/{realFile}";
                string fromPath = Utility.UrlToPhysical(fromUrl);
                string toUrl = $"{LEGACYPATH}/{skin.Product}{addon.Url}/{realFile}";
                string toPath = Utility.UrlToPhysical(toUrl);

                if (!await FileSystem.FileSystemProvider.FileExistsAsync(fromPath)) {
#if DEBUG
                    throw new InternalError($"Found {realFile} but no matching .css file");
#else
                    continue;
#endif
                }

                if (await ReplaceVariablesAsync(variables, addon.Url, fromPath, toPath)) {

                    // check if there is a .min.css file also, in which case minify that too
                    if (!fromPath.EndsWith(".min.css", System.StringComparison.Ordinal)) {
                        fromPath = Path.ChangeExtension(fromPath, ".min.css");
                        toPath = Path.ChangeExtension(toPath, ".min.css");
                        if (await FileSystem.FileSystemProvider.FileExistsAsync(fromPath))
                            await ReplaceVariablesAsync(variables, addon.Url, fromPath, toPath);
                    }
                }
            }
        }

        private async Task<bool> ReplaceVariablesAsync(Dictionary<string, string> variables, string originalUrl, string fromPath, string toPath) {

            bool changed = false;
            string text = await FileSystem.FileSystemProvider.ReadAllTextAsync(fromPath);

            // replace all css custom properties
            foreach (string name in variables.Keys) {
                string key = name.Replace("-", @"\-");// escape special chars
                Regex reVar = new Regex($@"var\({key}\s*(\,[^\)]*\s*|)\)", RegexOptions.Singleline);
                text = reVar.Replace(text, (Match m) => {
                    changed = true;
                    return variables[name]; 
                });
            }

            if (changed) {
                // we're creating a copy of the file with a different path so we need to
                // update any relative url() directives to point to the original URL and clean up leading ../ in URL
                text = _reUrl.Replace(text, (Match m) => {
                    string urlOrig = m.Groups["url"].Value.Trim();
                    string url = urlOrig;
                    url = url.Trim(' ');
                    if (string.IsNullOrWhiteSpace(url) || url.Length < 3 || url.StartsWith("data:", System.StringComparison.Ordinal))
                        return $"url({urlOrig})";
                    if (url[0] == '\'')
                        url = url.Trim('\'');
                    else if (url[0] == '"')
                        url = url.Trim('"');
                    string prefix = Utility.PhysicalToUrl(fromPath).RemoveStartingAtLast('/');
                    while (url.StartsWith("../", System.StringComparison.Ordinal)) {
                        prefix = prefix.RemoveStartingAtLast('/');
                        url = url.Substring(3);
                    }
                    return $"url({prefix}/{url})";
                });
            }

            if (changed) { // only create legacy file if there are changes
#if DEBUG
                // make sure there are no stray var() directives. This will miss files where ALL var() are wrong. This is intentional
                // for now as skins that have not yet been converted will complain about SkinBasics.scss.
                // This error means you either missed some var() definitions in the theme or are using Bootstrap 5 with SupportLegacyBrowser == true.
                // Bootstrap 5 does not support IE11, so that is an invalid combination.
                int varIx = text.IndexOf("var(", StringComparison.Ordinal);
                if (varIx >= 0)
                    throw new InternalError($"{fromPath} still contains var() directives: {text.Substring(varIx).Truncate(100)}");
#endif
                string toFolder = Path.GetDirectoryName(toPath)!;
                await FileSystem.FileSystemProvider.CreateDirectoryAsync(toFolder);

                await FileSystem.FileSystemProvider.WriteAllTextAsync(toPath, text);
            }
            return changed;
        }
        private static Regex _reUrl = new Regex(@"url\((?'url'[^\)]*)\s*\)", RegexOptions.Compiled | RegexOptions.Singleline);

        private async Task<Dictionary<string, string>> GetSkinVariablesAsync(Package.AddOnProduct skin) {

            Dictionary<string, string> collected = new Dictionary<string, string>();

            foreach (string file in skin.CssFiles) {
                if (file.Contains("{") || file.StartsWith("Themes/"))
                    continue;
                string realFile = file;
                string ext = Path.GetExtension(realFile);
                if (ext == ".scss")
                    realFile = Path.ChangeExtension(realFile, "css");
                string fromUrl = $"{skin.Url}/{realFile}";
                string fromPath = Utility.UrlToPhysical(fromUrl);

                string text = await FileSystem.FileSystemProvider.ReadAllTextAsync(fromPath);

                ExtractRootVariables(collected, text);
            }
            return collected;
        }

        private void ExtractRootVariables(Dictionary<string, string> collected, string text) {
            Match m = _reRoot.Match(text);
            while (m.Success) {
                string varText = m.Groups["vars"].Value;
                ExtractVariables(collected, varText);
                m = m.NextMatch();
            }
        }
        private static Regex _reRoot = new Regex(@"\:root\s*\{\s*(?'vars'[^\}]*)\s*\}", RegexOptions.Compiled | RegexOptions.Singleline);

        private void ExtractVariables(Dictionary<string, string> collected, string varText) {
            Match m = _reVar.Match(varText);
            while (m.Success) {
                string name = m.Groups["name"].Value;
                string value = m.Groups["value"].Value;
                collected.Add(name.Trim(), value.Trim());
                m = m.NextMatch();
            }
        }
        private static Regex _reVar = new Regex(@"\s*(?'name'\-\-[^\:]+)\s*\:\s*(?'value'[^\;]+)\s*\;", RegexOptions.Compiled | RegexOptions.Singleline);
    }
}
