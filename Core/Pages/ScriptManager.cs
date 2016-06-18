/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

// Why do we put js at the top of the page?

// https://developer.yahoo.com/performance/rules.html
// YetaWF satisfies most of these suggestions (except *)
// Minimize HTTP Requests
//    YetaWF bundles js and css files (except for large packages like jquery, kendo, etc.)
//    (*) No use of inline images
// Use a Content Delivery Network
//   Built-in CDN support (off by default until you have a CDN provider)
// Add an Expires or a Cache-Control Header
//   YetaWF uses Expires and a Cache-Control Header
// Gzip Components
//   While YetaWF doesn't use Gzip, it compresses html, js and css by eliminating unnecessary comments, spaces, new lines, etc.
//   In IIS dynamic & static compression can be enabled outside of YetaWF (which is fully supported by YetaWF and its CDN support)
// Put Stylesheets at the Top
//   YetaWF places style sheets at the top
// Put Scripts at the Bottom
//   (*) No. We prefer avoiding FOUC (flash of unformatted content)
//          see http://demianlabs.com/lab/post/top-or-bottom-of-the-page-where-should-you-load-your-javascript/
// Avoid CSS Expressions
//   Not used by YetaWF
// Make JavaScript and CSS External
//   Done for everything except small snippets that may be introduced by templates to serve one particular html tag
// Reduce DNS Lookups
//
// Minify JavaScript and CSS
//   Indeed
// Avoid Redirects
//   YetaWF supports page redirects but does not use them unless specifically requested by a page designer
// Remove Duplicate Scripts
//   That's an automatic feature of the ScriptManager class which oversees all script use
// Configure ETags
//   used for images and other resources, css (javascript is static and reported as such without ETag)
// Make Ajax Cacheable
//   not typically done as ajax requests in general expect modified data
// Flush the Buffer Early
//   (*) under consideration (TODO:)
// Use GET for AJAX Requests
//   (*) under consideration (TODO:)
// Post-load Components
//   (*) done for grids, captcha only
// Preload Components
//   (*) This is somewhat in conflict with YetaWF's design philosophy as modules at the end of a page can affect modules at the top of the page, completely changing rendering of all affected html,css,js
// Reduce the Number of DOM Elements
//   Always considered when creating new templates, modules, etc.
// Split Components Across Domains
//   (*) Appears to be a maintenance issue, but could be considered for the future
// Minimize the Number of iframes
//   YetaWF make little use of iframes, except for popups
// No 404s
//   Duh
// Reduce Cookie Size
//   YetaWF makes no use of cookies except for authentication
// Use Cookie-free Domains for Components
//   That's almost identical as Split "Components Across Domains"
// Minimize DOM Access
//   Always considered when creating new templates, modules, etc.
// Develop Smart Event Handlers
//   Always considered when creating new templates, modules, etc.
// Choose <link> over @import
//   YetaWF make no use of @import
// Avoid Filters
//   YetaWF make no use of filters
// Optimize Images
//   Always considered for new images - YetaWF also dynamically re-renders images if requested at other than the natural size
// Optimize CSS Sprites
//   (*) No built-in support
// Don't Scale Images in HTML
//   YetaWF dynamically re-renders images if requested at other than the natural size
// Make favicon.ico Small and Cacheable
//   favicon.ico is user-provided
// Keep Components under 25K
//   (*) pass on this one - not worth the trouble - if more than 25K is needed, there is probably a reason
// Pack Components into a Multipart Document
//   (*) pass on this one - not worth the trouble
// Avoid Empty Image src
//    No empty img src used by YetaWF


namespace YetaWF.Core.Pages {
    public partial class ScriptManager {

        public ScriptManager(YetaWFManager manager) { Manager = manager; }
        protected YetaWFManager Manager { get; private set; }

        private readonly Dictionary<string, string> _SavedFirstNamedScripts = new Dictionary<string, string>(); // included named script snippets before all included files (if added multiple times, added only once
        private readonly Dictionary<string, string> _ScriptFiles = new Dictionary<string, string>(); // already processed script files (not necessarily added to page yet)
        private readonly List<string> _FinalScriptFiles = new List<string>(); // script files to include (already minified, etc.) using <script...> tags
        private readonly List<string> _BundleScriptFiles = new List<string>(); // files in _FinalScriptFiles that must be bundled
        private readonly List<string> _DontBundleScriptFiles = new List<string>(); // files in _FinalScriptFiles that cannot be bundled
        private readonly Dictionary<string, string> _SavedNamedScripts = new Dictionary<string, string>(); // included named script snippets (if added multiple times, added only once
        private readonly List<string> _SavedScripts = new List<string>(); // included unnamed script snippets
        private readonly Dictionary<string, string> _SavedNamedScriptsDocReady = new Dictionary<string, string>(); // included unnamed script snippets wrapped in $document.ready

        // permanent config options to pass to javascript (may be language dependent)
        private readonly Dictionary<string, Dictionary<string, object>> _SavedConfigOptionsGroups = new Dictionary<string, Dictionary<string, object>>();

        // volatile config options to pass to javascript
        private readonly Dictionary<string, Dictionary<string, object>> _SavedVolatileOptionsGroups = new Dictionary<string, Dictionary<string, object>>();

        // localized text to pass to javascript
        private readonly Dictionary<string, Dictionary<string, object>> _SavedLocalizationsGroups = new Dictionary<string, Dictionary<string, object>>();


        // ADDON
        // ADDON
        // ADDON

        internal void AddAddOn(VersionManager.AddOnProduct version, params object[] args) {
            string productUrl = version.GetAddOnUrl();
            AddFromSupportTypes(version);
            AddFromFileList(version, productUrl, args);
        }

        /// <summary>
        /// Add a specific file for an addon product - This is normally only used for addons that are known to exist but don't automatically add all required files.
        /// </summary>
        /// <remarks>This is a bad pattern. Don't use it!</remarks>
        public void AddSpecificJsFile(VersionManager.AddOnProduct version, string file) {
            string productJsUrl = version.GetAddOnJsUrl();
            string url = productJsUrl + file;
            Add(url, true, true);
        }
        public void AddKendoUICoreJsFile(string file) {
            if (Manager.IsAjaxRequest) return;// can't add this while processing an ajax request
            if (VersionManager.KendoAddonType == VersionManager.KendoAddonTypeEnum.Pro) return;// everything is already included
            AddSpecificJsFile(VersionManager.KendoAddon, file);
        }

        // Add localizations and configurations
        private void AddFromSupportTypes(VersionManager.AddOnProduct version) {
            foreach (var type in version.SupportTypes) {
                object o = Activator.CreateInstance(type);
                if (o == null)
                    throw new InternalError("Type {0} can't be created for {1}/{2}", type.Name, version.Domain, version.Product);
                IAddOnSupport addSupport = o as IAddOnSupport;
                if (addSupport == null)
                    throw new InternalError("No IAddOnSupport interface found on type {0} for {1}/{2}", type.Name, version.Domain, version.Product);
                addSupport.AddSupport(Manager);
            }
        }

        // Add all javascript files listed in filelistJS.txt
        private void AddFromFileList(VersionManager.AddOnProduct version, string productUrl, params object[] args) {
            List<string> list = (from i in version.JsFiles select Path.Combine(version.JsPath, i)).ToList(); // make a copy
            foreach (var info in list) {
                //bool page = true; // default is to use in page and popups, but not with ajax
                //bool popup = true;
                //bool ajax = false;
                bool nominify = false;
                bool? bundle = null;
                bool? editonly = null;
                string[] parts = info.Split(new Char[] { ',' });
                int count = parts.Length;
                string file;
                if (count > 0) {
                    // at least a file name is present
                    file = string.Format(parts[0].Trim(), args);
                    if (count > 1) {
                        // there are some keywords
                        //page = true;
                        //popup = true;
                        //ajax = nominify = false;
                        for (int i = 1 ; i < count ; ++i) {
                            var part = parts[i].Trim().ToLower();
                            //if (part == "page") page = true;
                            //else if (part == "popup") popup = true;
                            //else if (part == "ajax") ajax = true;
                            //else
                            if (part == "nominify") nominify = true;
                            else if (part == "bundle") bundle = true;
                            else if (part == "editonly") editonly = true;
                            else throw new InternalError("Invalid keyword {0} in statement '{1}' ({2}/{3})'.", part, info, version.Domain, version.Product);
                        }
                    }
                    if (editonly == true && !Manager.EditMode)
                        continue;
                    // check if we want to send this file
                    //if (!ajax && Manager.IsAjaxRequest) // We don't want this file in ajax responses
                    //    continue;
                    //if (!page && !Manager.IsAjaxRequest) // We don't want this file in a main page request
                    //    continue;
                    string filePathURL;
                    if (file.StartsWith("http://") || file.StartsWith("https://") || file.StartsWith("//")) {
                        filePathURL = file;
                    } else if (file.StartsWith("\\")) {
                        string f = Path.Combine(YetaWFManager.RootFolder, file.Substring(1));
                        if (!File.Exists(f))
                            throw new InternalError("File list has physical file {0} which doesn't exist at {1}", file, f);
                        filePathURL = YetaWFManager.PhysicalToUrl(f);
                    } else {
                        filePathURL = string.Format("{0}{1}", productUrl, file);
                        if (!File.Exists(YetaWFManager.UrlToPhysical(filePathURL)))
                            throw new InternalError("File list has relative url {0} which doesn't exist in {1}/{2}", filePathURL, version.Domain, version.Product);
                    }
                    if (!Add(filePathURL, !nominify, bundle))
                        version.JsFiles.Remove(info);// remove empty file
                }
            }
        }

        /// <summary>
        /// Add a javascript file explicitly. This is rarely used because javascript files are automatically added for modules, templates, etc.
        /// </summary>
        public void AddScript(string domainName, string productName, string relativePath, int dummy = 0, bool Minify = true, bool Bundle = true) {
            VersionManager.AddOnProduct addon = VersionManager.FindModuleVersion(domainName, productName);
            Add(addon.GetAddOnJsUrl() + relativePath, Minify, Bundle);
        }

        private bool Add(string fullUrl, bool minify, bool? bundle)
        {
            string key = fullUrl.ToLower();

            if (fullUrl.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith("//", StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.AddOnsUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.NugetScriptsUrl, StringComparison.InvariantCultureIgnoreCase)) {

                if (key.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 3);
                if (key.EndsWith(".min", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 4);
                if (key.EndsWith(".pack", StringComparison.InvariantCultureIgnoreCase)) key = key.Substring(0, key.Length - 5);

            } else {
                throw new InternalError("Script name '{0}' is invalid.", fullUrl);
            }

            if (!_ScriptFiles.ContainsKey(key)) {
                string file = minify ? JsCompress(fullUrl) : fullUrl;
                if (file == null)
                    return false; // empty file
                _ScriptFiles.Add(key, fullUrl);
                _FinalScriptFiles.Add(file);
                if (bundle == true)
                    _BundleScriptFiles.Add(file);
                else if (bundle == false)
                    _DontBundleScriptFiles.Add(file);
            }
            return true;
        }

        private string JsCompress(string fullUrl) {
            Packer packer = new Packer();
            if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.CompressJSFiles) {
                return packer.ProcessFile(fullUrl, Packer.PackMode.JS, true, false);
            } else {
                if (packer.CanCompress(fullUrl, Packer.PackMode.JS)) {
                    string path = YetaWFManager.UrlToPhysical(fullUrl);
                    if (!File.Exists(path))
                        throw new InternalError("File {0} not found - can't be minimized", fullUrl);
                }
                return packer.ProcessFile(fullUrl, Packer.PackMode.JS, false, false);
            }
        }

        // ADD SUPPORT
        // ADD SUPPORT
        // ADD SUPPORT

        /// <summary>
        /// Add a named javascript code section BEFORE all other included FILES. If the named section has already been
        /// added, it is not added again. This is useful for code sections that are added
        /// by controls/modules which may be used multiple times on a page.
        /// You must insure that the code sections with the same name are always identical.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="javascript"></param>
        /// <returns></returns>
        public bool AddFirst(string name, string javascript) {
            if (_SavedFirstNamedScripts.ContainsKey(name)) {
                if (_SavedFirstNamedScripts[name] != javascript)
                    throw new InternalError("Named javascript section on this page is different than a previously added section by the same name");
                return false; // already added
            }
            _SavedFirstNamedScripts.Add(name, javascript);
            return true;
        }
        public bool AddFirst(string name, ScriptBuilder tag) {
            return AddFirst(name, tag.ToString());
        }

        /// <summary>
        /// Add a named javascript code section at the end of the page. If the named section has already been
        /// added, it is not added again. This is useful for code sections that are added
        /// by controls/modules which may be used multiple times on a page.
        /// You must insure that the code sections with the same name are always identical.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="javascript"></param>
        /// <returns></returns>
        public bool AddLast(string name, string javascript) {
            if (_SavedNamedScripts.ContainsKey(name)) {
                if (_SavedNamedScripts[name] != javascript)
                    throw new InternalError("Named javascript section on this page is different than a previously added section by the same name");
                return false; // already added
            }
            _SavedNamedScripts.Add(name, javascript);
            return true;
        }
        public bool AddLast(string name, ScriptBuilder tag) {
            return AddLast(name, tag.ToString());
        }

        /// <summary>
        /// Add an unnamed javascript code section.
        /// </summary>
        public void Add(string javascript) {
            _SavedScripts.Add(javascript);
        }

        /// Add javascript code right now (inline)
        public HtmlBuilder AddNow(string javascriptCode) {
            HtmlBuilder tag = new HtmlBuilder();
            if (string.IsNullOrEmpty(javascriptCode)) return tag;
            tag.Append("<script type=\"text/javascript\">");
            //tag.Append("\n<script type=\"text/javascript\">\n//<![CDATA[\n");
            tag.Append(ScriptManager.TrimScript(Manager, javascriptCode));
            //tag.Append("\n//]]>\n</script>\n");
            tag.Append("</script>");
            return tag;
        }

        /// <summary>
        /// Add javascript code (complete functions, etc.) at end of page and run on page load (document.ready).
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="javascriptCode"></param>
        public void AddLastDocumentReady(string javascriptCode) {
            if (string.IsNullOrWhiteSpace(javascriptCode)) return;
            _SavedNamedScriptsDocReady.Add(_SavedNamedScriptsDocReady.Count().ToString(), javascriptCode);
        }
        public void AddLastDocumentReady(ScriptBuilder javascriptCode) {
            AddLastDocumentReady(javascriptCode.ToString());
        }

        /// <summary>
        /// Add named javascript code at end of page and run on page load (document.ready).
        /// </summary>
        /// <param name="name"></param>
        /// <param name="javascript"></param>
        public void AddLastDocumentReady(string name, string javascript) {
            if (_SavedNamedScriptsDocReady.ContainsKey(name)) {
                if (_SavedNamedScriptsDocReady[name] != javascript)
                    throw new InternalError("Named javascript section on this page is different than a previously added section by the same name");
                return;
            }
            _SavedNamedScriptsDocReady.Add(name, javascript);
        }
        public void AddLastDocumentReady(string name, ScriptBuilder javascript) {
            AddLastDocumentReady(name, javascript.ToString());
        }

        // CONFIG OPTIONS (User Specific)
        // CONFIG OPTIONS (User Specific)
        // CONFIG OPTIONS (User Specific)

        public void AddConfigOption(string group, string name, object value) {
            Dictionary<string, object> configOptions = null;
            if (!_SavedConfigOptionsGroups.TryGetValue(group, out configOptions)) {
                configOptions = new Dictionary<string, object>();
                _SavedConfigOptionsGroups.Add(group, configOptions);
            }
            configOptions.Add(name, value);
        }

        public void AddVolatileOption(string group, string name, object value) {
            Dictionary<string, object> volatileOptions = null;
            if (!_SavedVolatileOptionsGroups.TryGetValue(group, out volatileOptions)) {
                volatileOptions = new Dictionary<string, object>();
                _SavedVolatileOptionsGroups.Add(group, volatileOptions);
            }
            volatileOptions.Add(name, value);
        }

        public void AddLocalization(string group, string name, object value) {
            Dictionary<string, object> locOptions = null;
            if (!_SavedLocalizationsGroups.TryGetValue(group, out locOptions)) {
                locOptions = new Dictionary<string, object>();
                _SavedLocalizationsGroups.Add(group, locOptions);
            }
            locOptions.Add(name, value);
        }

        // RENDER
        // RENDER
        // RENDER

        public HtmlBuilder Render() {

            Manager.Verify_NotAjaxRequest();

            HtmlBuilder tag = new HtmlBuilder();

            ScriptBuilder sbA = RenderScriptsPartA();
            if (sbA.Length > 0) {
                //tag.Append("\n<script type=\"text/javascript\">\n//<![CDATA[\n");
                tag.Append("<script type=\"text/javascript\">");
                tag.Append(sbA);
                tag.Append("</script>");
                //tag.Append("\n//]]>\n</script>\n");
            }
            HtmlBuilder hb = RenderScriptsFiles();
            tag.Append(hb);

            ScriptBuilder sbB = RenderScriptsPartB();
            if (sbB.Length > 0) {
                //tag.Append("\n<script type=\"text/javascript\">\n//<![CDATA[\n");
                tag.Append("<script type=\"text/javascript\">");
                tag.Append(sbB);
                tag.Append("</script>");
                //tag.Append("\n//]]>\n</script>\n");
            }
            return tag;
        }

        public HtmlBuilder RenderAjax() {

            Manager.Verify_AjaxRequest();

            HtmlBuilder tag = new HtmlBuilder();

            HtmlBuilder hb = RenderScriptsFiles();
            if (hb.Length > 0) throw new InternalError("Somehow script file links were added in an Ajax request - this is not supported");
            tag.Append(hb);

            tag.Append(RenderEndofPageScripts());

            ScriptBuilder sbB = RenderScriptsPartB();
            if (sbB.Length > 0) {
                //tag.Append("\n<script type=\"text/javascript\">\n//<![CDATA[\n");
                tag.Append("<script type=\"text/javascript\">");
                tag.Append(sbB);
                tag.Append("</script>");
                //tag.Append("\n//]]>\n</script>\n");
            }
            return tag;
        }

        private ScriptBuilder RenderScriptsPartA() {

            ScriptBuilder sb = new ScriptBuilder();

            if (_SavedVolatileOptionsGroups.Count > 0) {

                JavaScriptSerializer jser = YetaWFManager.Jser;
                sb.Append("var YVolatile={");

                foreach (var groupEntry in _SavedVolatileOptionsGroups) {

                    string groupName = groupEntry.Key;
                    Dictionary<string, object> confEntries = groupEntry.Value;

                    sb.Append("{0}:{{", groupName);

                    foreach (var confEntry in confEntries)
                        sb.Append("'{0}':{1},", confEntry.Key, jser.Serialize(confEntry.Value));
                    sb.RemoveLast(); // remove last ,
                    sb.Append("},");
                }
                sb.RemoveLast(); // remove last ,
                sb.Append("};");
            }

            if (Manager.CurrentSite.DEBUGMODE || !Manager.CurrentSite.BundleJSFiles) {
                sb.Append("\n");
                GenerateNonVolatileJSVariables(sb);
            }

            foreach (var script in _SavedFirstNamedScripts) {
                sb.Append(TrimScript(Manager, script.Value));
            }
            return sb;
        }

        private void GenerateNonVolatileJSVariables(ScriptBuilder sb) {

            if (_SavedConfigOptionsGroups.Count > 0) {

                // non-volatile data must be in the same order every time so bundles can be built correctly
                JavaScriptSerializer jser = YetaWFManager.Jser;
                sb.Append("var YConfigs={");

                foreach (var groupEntry in _SavedConfigOptionsGroups.OrderBy(kvp => kvp.Key)) {

                    string groupName = groupEntry.Key;
                    Dictionary<string, object> confEntries = groupEntry.Value;

                    sb.Append("{0}:{{", groupName);

                    foreach (var confEntry in confEntries.OrderBy(kvp => kvp.Key))
                        sb.Append("'{0}':{1},", confEntry.Key, jser.Serialize(confEntry.Value));
                    sb.RemoveLast(); // remove last ,
                    sb.Append("},");
                }
                sb.RemoveLast(); // remove last ,
                sb.Append("};\n");
            }

            if (_SavedLocalizationsGroups.Count > 0) {

                sb.Append("var YLocs={");

                foreach (var groupEntry in _SavedLocalizationsGroups.OrderBy(kvp => kvp.Key)) {

                    string groupName = groupEntry.Key;
                    Dictionary<string, object> locEntries = groupEntry.Value;

                    sb.Append("{0}:{{", groupName);

                    foreach (var locEntry in locEntries.OrderBy(kvp => kvp.Key)) {
                        var loc = locEntry;
                        string val = YetaWFManager.Jser.Serialize(loc.Value);
                        sb.Append("'{0}':{1},", loc.Key, val);
                    }
                    sb.RemoveLast(); // remove last ,
                    sb.Append("},");
                }
                sb.RemoveLast(); // remove last ,
                sb.Append("};\n");
            }
        }

        private HtmlBuilder RenderScriptsFiles() {
            HtmlBuilder hb = new HtmlBuilder();
            List<string> list = _FinalScriptFiles;

            ScriptBuilder sbStart = new ScriptBuilder();

            if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.BundleJSFiles) {
#if DEBUG
                sbStart.Append("/**** Non-Volatile ****/\n");
#endif
                GenerateNonVolatileJSVariables(sbStart);
                list = FileBundles.MakeBundle(list, FileBundles.BundleTypeEnum.JS, sbStart, ForceIncludeInBundle: _BundleScriptFiles, ForceExcludeFromBundle: _DontBundleScriptFiles);
            }

            bool canUseCDN = Manager.CurrentSite.CanUseCDN;
            foreach (var src in list) {
                string url = src;
                if (canUseCDN)
                    url = Manager.GetCDNUrl(url);
                hb.Append(string.Format("<script type='text/javascript' src='{0}?__yVrs={1}'></script>", YetaWFManager.HtmlAttributeEncode(url), YetaWFManager.CacheBuster));
            }
            return hb;
        }

        private ScriptBuilder RenderScriptsPartB() {

            ScriptBuilder sb = new ScriptBuilder();

            foreach (var script in _SavedScripts) {
                sb.Append(TrimScript(Manager, script));
            }
            return sb;
        }

        public HtmlBuilder RenderEndofPageScripts() {
            HtmlBuilder hb = new HtmlBuilder();
            if (_SavedNamedScripts.Count > 0 || _SavedNamedScriptsDocReady.Count > 0) {
                //hb.Append("\n<script type=\"text/javascript\">\n//<![CDATA[\n");
                hb.Append("<script type=\"text/javascript\">");
                if (_SavedNamedScripts.Count > 0) {
                    foreach (var script in _SavedNamedScripts) {
                        hb.Append(TrimScript(Manager, script.Value));
                    }
                }
                if (_SavedNamedScriptsDocReady.Count > 0) {
                    hb.Append("$(document).ready(function() {\n");
                    foreach (var script in _SavedNamedScriptsDocReady) {
                        hb.Append("(function(){\n");
                        hb.Append(TrimScript(Manager, script.Value));
                        hb.Append("\n})();\n");
                    }
                    hb.Append("});\n");
                }
                hb.Append("</script>");
                //hb.Append("\n//]]>\n</script>\n");
            }
            return hb;
        }

        // TRIM JAVASCRIPT CODE
        // TRIM JAVASCRIPT CODE
        // TRIM JAVASCRIPT CODE

        private static readonly Regex _comment1Re = new Regex("/\\*.*?\\*/", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _comment2Re = new Regex("^([^'\"]*)//.*$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _comment3Re = new Regex("//[^'\"]*$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string TrimScript(YetaWFManager manager, string script) {
            if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.Compression) {

                script = _comment1Re.Replace(script, " "); // remove all /* */
                string[] scriptLines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder str = new StringBuilder();
                foreach (var line in scriptLines) {
                    string s = line.Trim();
                    if (s.EndsWith("<!--")) {
                        str.Append(s);
                        str.Append(Environment.NewLine);
                    } else if (s.StartsWith("//")) {
                        ; // nothing
                    } else {
                        s = _comment2Re.Replace(s, "$1").Trim();
                        // remove all // comments (that don't contain ' or " as they could really be part of a string)
                        s = _comment3Re.Replace(s, "");
                        str.Append(s.Trim());
                        if (s.Contains("//"))
                            str.Append(Environment.NewLine);
                        else
                            str.Append(' ');
                    }
                }
                return str.ToString();
            } else
                return script;
        }
    }
}
