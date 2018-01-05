/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YetaWF.Core.Addons;
using YetaWF.Core.Controllers;
using YetaWF.Core.Extensions;
using YetaWF.Core.Support;

// https://developer.yahoo.com/performance/rules.html
// YetaWF satisfies most of these suggestions (except *)
// Minimize HTTP Requests
//    YetaWF bundles js and css files (except for large packages like jquery, kendo, etc.)
//    (*) No use of inline images
//    Unified Page Sets use 1 Ajax request to render a new page within the unified page set
// Use a Content Delivery Network
//   Built-in CDN support (off by default until you have a CDN provider)
//   Optional CDN support for all major Javascript addons (jQuery, jQueryUI, KendoUI, CKEditor, etc.)
// Add an Expires or a Cache-Control Header
//   YetaWF uses Expires and a Cache-Control Header
// Gzip Components
//   YetaWF uses Gzip for
//      - Ajax responses for Unified Page Sets (navigating between pages in a Single Page Site)
//      - Ajax responses for grid data when rendering/paging a grid
//   In IIS dynamic & static compression can be enabled outside of YetaWF (which is fully supported by YetaWF and its CDN support)
//   YetaWF (non-Gzip) compresses html, js and css by eliminating unnecessary comments, spaces, new lines, etc.
// Put Stylesheets at the Top
//   YetaWF places style sheets at the top - It is possible to place them at the bottom (Site Settings)
// Put Scripts at the Bottom
//   This is available as a configurable option (Admin > Site Settings, Page tab, JavaScript Location field)
//   However, YetaWF prefers avoiding FOUC (flash of unformatted content)
//     see this opinion: http://demianlabs.com/lab/post/top-or-bottom-of-the-page-where-should-you-load-your-javascript/
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
//   not typically done as Ajax requests in general expect current data
// Flush the Buffer Early
//   (*) under consideration (TODO:)
// Use GET for AJAX Requests
//   In theory that sounds good, but any data (json,xml) is passed as query string. This means the maximum Url size is quickly
//   reached (HTTP 400 - Bad Request (Request Header too long)) which can only be extended by registry changes (not web.config).
//   So, No on this one in general.  https://yetawf.com/BlogEntry/Title/AJAX%20GET%20and%20Query%20String%20Length/?BlogEntry=1030
//   This could still be used for small requests.
// Post-load Components
//   (*) done for grids, captcha only
//   Unified Page Sets (dynamic content) progressively adds page content as user navigates between pages of a set, including css, javascript, etc.
// Preload Components
//   (*) This is somewhat in conflict with YetaWF's design philosophy as modules at the end of a page can affect modules at the top of the page, completely changing rendering of all affected html,css,js
// Reduce the Number of DOM Elements
//   Always considered when creating new templates, modules, etc.
// Split Components Across Domains
//   YetaWF supports an alternate domain/url for all static content
// Minimize the Number of iframes
//   YetaWF makes little use of iframes, except for popups.
//   When using Unified Page Sets (Single Page Site) and popups are enabled for the page set, no iframes are used.
// No 404s
//   Duh
// Reduce Cookie Size
//   YetaWF makes no use of cookies except for authentication
// Use Cookie-free Domains for Components
//   YetaWF supports an alternate domain/url for all static content which is cookie-free
// Minimize DOM Access
//   Always considered when creating new templates, modules, etc.
// Develop Smart Event Handlers
//   Always considered when creating new templates, modules, etc.
// Choose <link> over @import
//   YetaWF makes no use of client-side @import
// Avoid Filters
//   YetaWF makes no use of filters
// Optimize Images
//   Always considered for new images - YetaWF also dynamically re-renders images if requested at other than the natural size
// Optimize CSS Sprites
//   (*) No built-in support
// Don't Scale Images in HTML
//   YetaWF dynamically re-renders images server-side if requested at other than the natural size
// Make favicon.ico Small and Cacheable
//   favicon.ico is user-provided and always cacheable
// Keep Components under 25K
//   (*) pass on this one - not worth the trouble - if more than 25K is needed, there is probably a reason
// Pack Components into a Multipart Document
//   (*) pass on this one - not worth the trouble
// Avoid Empty Image src
//    No empty img src used by YetaWF

// https://developers.google.com/speed/pagespeed/
// YetaWF sites will generally have a desktop speed rating of around 90. The only penalties are due to "Leverage browser caching" when external
// JavaScript is used, like addthis.com, google-analytics.com (oh the irony) and some CDNs which may have an expiration interval of less than 7 days.
// Google is looking for more than 7 days.
// The other penalty is due to "Eliminate render-blocking JavaScript and CSS in above-the-fold content". Even with JavaScript/Css located at the bottom
// of the page, a certain percentage of the above-the-fold content cannot be rendered without waiting for the resources to load.
// We can't provide a general solution to this as this is content dependent. On the other hand, pretty much any menu (JavaScript dependent) and
// layout (Css dependent) will cause this penalty. Unless you prefer a "Flash Of Unformatted Content" to avoid the "penalty" there is probably not
// all that much that can be done about that.
// The mobile speed rating is generally lower due to the same penalties. There are no additional, mobile specific penalties.

// https://tools.pingdom.com
// YetaWF sites rate A in all categories except one. There is a slight penalty for "Leverage browser caching", all related to Urls linking to other sites,
// like addthis.com, google-analytics.com, etc.
// For an A rating, a static domain must be defined in YetaWF so static files can be served from this static, cookie-less domain.
// The one category where YetaWF will not achieve an A rating is "Remove query strings from static resources". Their explanation:
// Resources with a "?" in the URL are not cached by some proxy caching servers. Remove the query string and encode the parameters into the URL.
// The reasoning behind this recommendation seems antiquated and no longer valid:
// https://webmasters.stackexchange.com/questions/86274/tradeoffs-around-using-a-query-string-vs-embedding-version-number-in-the-css-js/86277#86277
// https://webmasters.stackexchange.com/questions/109042/resources-with-a-in-the-url-are-not-cached-by-some-proxy-caching-servers
// So for now YetaWF will not address this as it seems unnecessary.

namespace YetaWF.Core.Pages {
    public partial class ScriptManager {

        public ScriptManager(YetaWFManager manager) { Manager = manager; }
        protected YetaWFManager Manager { get; private set; }

        public class ScriptEntry {
            public string Url { get; set; }
            public bool Bundle { get; set; }
            public bool Last { get; set; } // after all addons
            public bool Async { get; set; }
            public bool Defer { get; set; }
        }
        private readonly List<string> _ScriptFiles = new List<string>(); // already processed script files (not necessarily added to page yet)
        private readonly List<ScriptEntry> _Scripts = new List<ScriptEntry>(); // all scripts to be added to page

        private readonly Dictionary<string, string> _SavedFirstNamedScripts = new Dictionary<string, string>(); // included named script snippets before all included files (if added multiple times, added only once
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
            Add(url, true, true, false, false, false);
        }
        public void AddKendoUICoreJsFile(string file) {
            if (Manager.IsPostRequest) return;// can't add this while processing a post request
            if (VersionManager.KendoAddonType == VersionManager.KendoAddonTypeEnum.Pro) return;// everything is already included
            if (Manager.CurrentSite.CanUseCDNComponents) return;// already included
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
                bool? cdn = null;
                bool editonly = false;
                bool last = false;
                bool async = false, defer = false;
                bool allowCustom = false;
                bool kendoUICore = false;
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
                        //nominify = false;
                        for (int i = 1 ; i < count ; ++i) {
                            var part = parts[i].Trim().ToLower();
                            //if (part == "page") page = true;
                            //else if (part == "popup") popup = true;
                            //else
                            if (part == "nominify") nominify = true;
                            else if (part == "bundle") bundle = true;
                            else if (part == "nobundle") bundle = false;
                            else if (part == "last") last = true;
                            else if (part == "editonly") editonly = true;
                            else if (part == "async") async = true;
                            else if (part == "defer") defer = true;
                            else if (part == "cdn") cdn = true;
                            else if (part == "nocdn") cdn = false;
                            else if (part == "allowcustom") allowCustom = true;
                            else if (part == "kendouicore") kendoUICore = true;
                            else throw new InternalError("Invalid keyword {0} in statement '{1}' ({2}/{3})'.", part, info, version.Domain, version.Product);
                        }
                    }
                    if (editonly && !Manager.EditMode)
                        continue;
                    if (cdn == true && !Manager.CurrentSite.CanUseCDNComponents)
                        continue;
                    else if (cdn == false && Manager.CurrentSite.CanUseCDNComponents)
                        continue;
                    // check if we want to send this file
                    if (kendoUICore) {
                        if (nominify || bundle != null || cdn != null || editonly || last || async || defer || allowCustom)
                            throw new InternalError("Can't use keywords on statement '{0}' ({1}/{2})'.", info, version.Domain, version.Product);
                        AddKendoUICoreJsFile(file);
                    } else {
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
                            filePathURL = string.Format("{0}{1}", productUrl, file);
                            if (!File.Exists(YetaWFManager.UrlToPhysical(filePathURL)))
                                throw new InternalError("File list has relative url {0} which doesn't exist in {1}/{2}", filePathURL, version.Domain, version.Product);
                        }
                        if (allowCustom) {
                            string customUrl = VersionManager.GetCustomUrlFromUrl(filePathURL);
                            string f = YetaWFManager.UrlToPhysical(customUrl);
                            if (File.Exists(f))
                                filePathURL = customUrl;
                        }
                        if (bundle == true || last) {
                            if (async || defer)
                                throw new InternalError("Can't use async/defer with bundle/last for {0} in {1}/{2}", filePathURL, version.Domain, version.Product);
                        }
                        if (bundle == null) {
                            if (filePathURL.ContainsIgnoreCase(Globals.NodeModulesUrl) || filePathURL.ContainsIgnoreCase(Globals.BowerComponentsUrl) || filePathURL.ContainsIgnoreCase("/" + Globals.GlobalJavaScript + "/")) {
                                /* While possible to add these to a bundle, it's inefficient and can cause errors with scripts that load their own scripts */
                                bundle = false;
                            } else {
                                bundle = true;
                            }
                        }
                        if (!Add(filePathURL, !nominify, (bool)bundle, last, async, defer))
                            version.JsFiles.Remove(info);// remove empty file
                    }
                }
            }
        }

        /// <summary>
        /// Add a Javascript file explicitly. This is rarely used because Javascript files are automatically added for modules, templates, etc.
        /// </summary>
        public void AddScript(string domainName, string productName, string relativePath, int dummy = 0, bool Minify = true, bool Bundle = true, bool Async = false, bool Defer = false) {
            VersionManager.AddOnProduct addon = VersionManager.FindPackageVersion(domainName, productName);
            Add(addon.GetAddOnJsUrl() + relativePath, Minify, Bundle, false, false, false);
        }
        /// <summary>
        /// Add a Javascript file explicitly. This is rarely used because Javascript files are automatically added for modules, templates, etc.
        /// </summary>
        /// <param name="fullUrl">The Url of the script file (starting with /).</param>
        /// <param name="minify">Defines whether the file needs to be minified.</param>
        /// <param name="bundle">Defines whether the file will be bundled (if bundling is enabled).</param>
        /// <param name="last">Defines whether the file will be added at the end of the current file list.</param>
        /// <param name="async">Defines whether async is added to the &lt;script&gt; tag.</param>
        /// <param name="defer">Defines whether defer is added to the &lt;script&gt; tag.</param>
        /// <returns></returns>
        public bool Add(string fullUrl, bool minify = true, bool bundle = true, bool last = false, bool async = false, bool defer = false) {
            string key = fullUrl.ToLower();

            if (fullUrl.IsAbsoluteUrl()) {
                // nothing to do
                bundle = false;
            } else if (fullUrl.StartsWith(Globals.NodeModulesUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(Globals.BowerComponentsUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.AddOnsUrl, StringComparison.InvariantCultureIgnoreCase) ||
                fullUrl.StartsWith(VersionManager.AddOnsCustomUrl, StringComparison.InvariantCultureIgnoreCase)) {

                if (key.EndsWith(".js")) key = key.Substring(0, key.Length - 3);
                if (key.EndsWith(".min")) key = key.Substring(0, key.Length - 4);
                if (key.EndsWith(".pack")) key = key.Substring(0, key.Length - 5);

                // handle file types that are transpiled
                if (fullUrl.EndsWith(".ts")) fullUrl = fullUrl.Substring(0, key.Length - 3) + ".js";
                else if (fullUrl.EndsWith(".tsx")) fullUrl = fullUrl.Substring(0, key.Length - 4) + ".js";

                if (fullUrl.EndsWith(".min.js") || fullUrl.EndsWith(".pack.js"))
                    minify = false;

                // get the compiled file name
                if (fullUrl.EndsWith(".js") && !fullUrl.EndsWith(".min.js"))
                    fullUrl = fullUrl.Substring(0, fullUrl.Length - 3) + ".js";
                if (minify) {
                    if (!fullUrl.EndsWith(".js"))
                        throw new InternalError("Unsupported extension for {0}", fullUrl);
                    if (Manager.Deployed && Manager.CurrentSite.CompressJSFiles) {
                        fullUrl = fullUrl.Substring(0, fullUrl.Length - 3) + ".min.js";
                    }
                }
            } else {
                throw new InternalError("Script name '{0}' is invalid.", fullUrl);
            }

            if (!_ScriptFiles.Contains(key)) {
                if (!fullUrl.IsAbsoluteUrl()) {
                    string path = YetaWFManager.UrlToPhysical(fullUrl);
                    if (!File.Exists(path))
                        throw new InternalError("File {0} not found", fullUrl);
                }
                _ScriptFiles.Add(key);
                _Scripts.Add(new Pages.ScriptManager.ScriptEntry { Url = fullUrl, Bundle = bundle, Last = last, Async = async, Defer = defer });
            }
            return true;
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
        /// Add an unnamed javascript code section at end of page.
        /// </summary>
        public void AddLast(string javascript) {
            _SavedScripts.Add(javascript);
        }

        /// <summary>
        /// Add javascript code right now (inline)
        /// </summary>
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
            if (!configOptions.ContainsKey(name))
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

        public HtmlBuilder Render(PageContentController.PageContentData cr = null, List<string> KnownScripts = null) {

            if (cr == null)
                Manager.Verify_NotPostRequest();

            HtmlBuilder tag = new HtmlBuilder();

            ScriptBuilder sbA = RenderScriptsPartA(cr);
            if (sbA.Length > 0) {
                //tag.Append("\n<script type=\"text/javascript\">\n//<![CDATA[\n");
                if (cr == null)
                    tag.Append("<script type=\"text/javascript\">");
                tag.Append(sbA.ToString());
                if (cr == null)
                    tag.Append("</script>");
                //tag.Append("\n//]]>\n</script>\n");
                if (cr != null) {
                    cr.Scripts = tag.ToString();
                }
            }
            if (cr != null) {
                RenderScriptsFiles(cr, KnownScripts);
                return new HtmlBuilder();
            } else {
                HtmlBuilder hb = RenderScriptsFiles();
                tag.Append(hb.ToHtmlString());
                return tag;
            }
        }

        public HtmlBuilder RenderAjax() {

            Manager.Verify_PostRequest();

            HtmlBuilder tag = new HtmlBuilder();

            HtmlBuilder hb = RenderScriptsFiles();
            if (hb.Length > 0) throw new InternalError("Somehow script file links were added in an Ajax request - this is not supported");
            tag.Append(hb.ToHtmlString());

            tag.Append(RenderEndofPageScripts());
            return tag;
        }

        private ScriptBuilder RenderScriptsPartA(PageContentController.PageContentData cr = null) {

            ScriptBuilder sb = new ScriptBuilder();

            if (_SavedVolatileOptionsGroups.Count > 0) {

                if (cr == null) {
                    sb.Append("var YVolatile={");

                    foreach (var groupEntry in _SavedVolatileOptionsGroups) {

                        string groupName = groupEntry.Key;
                        Dictionary<string, object> confEntries = groupEntry.Value;

                        sb.Append("{0}:{{", groupName);

                        foreach (var confEntry in confEntries)
                            sb.Append("'{0}':{1},", confEntry.Key, YetaWFManager.JsonSerialize(confEntry.Value));
                        sb.RemoveLast(); // remove last ,
                        sb.Append("},");
                    }
                    sb.RemoveLast(); // remove last ,
                    sb.Append("};");
                } else {
                    foreach (var groupEntry in _SavedVolatileOptionsGroups) {

                        string groupName = groupEntry.Key;
                        sb.Append("YVolatile.{0}=YVolatile.{0}||{{}};", groupName);
                        Dictionary <string, object> confEntries = groupEntry.Value;
                        foreach (var confEntry in confEntries)
                            sb.Append("YVolatile.{0}.{1}={2};", groupName, confEntry.Key, YetaWFManager.JsonSerialize(confEntry.Value));
                    }
                }
            }

            sb.Append("\n");
            GenerateNonVolatileJSVariables(sb, cr);

            foreach (var script in _SavedFirstNamedScripts) {
                sb.Append(TrimScript(Manager, script.Value));
            }
            return sb;
        }

        private void GenerateNonVolatileJSVariables(ScriptBuilder sb, PageContentController.PageContentData cr = null) {

            if (_SavedConfigOptionsGroups.Count > 0) {

                if (cr == null) {
                    // non-volatile data must be in the same order every time so bundles can be built correctly
                    sb.Append("var YConfigs={");

                    foreach (var groupEntry in _SavedConfigOptionsGroups.OrderBy(kvp => kvp.Key)) {

                        string groupName = groupEntry.Key;
                        Dictionary<string, object> confEntries = groupEntry.Value;

                        sb.Append("{0}:{{", groupName);

                        foreach (var confEntry in confEntries.OrderBy(kvp => kvp.Key))
                            sb.Append("'{0}':{1},", confEntry.Key, YetaWFManager.JsonSerialize(confEntry.Value));
                        sb.RemoveLast(); // remove last ,
                        sb.Append("},");
                    }
                    sb.RemoveLast(); // remove last ,
                    sb.Append("};\n");
                } else {
                    foreach (var groupEntry in _SavedConfigOptionsGroups.OrderBy(kvp => kvp.Key)) {
                        string groupName = groupEntry.Key;
                        sb.Append("YConfigs.{0}=YConfigs.{0}||{{}};", groupName);
                        Dictionary<string, object> confEntries = groupEntry.Value;
                        foreach (var confEntry in confEntries.OrderBy(kvp => kvp.Key))
                            sb.Append("YConfigs.{0}.{1}={2};", groupName, confEntry.Key, YetaWFManager.JsonSerialize(confEntry.Value));
                    }
                }
            }

            if (_SavedLocalizationsGroups.Count > 0) {
                if (cr == null) {
                    sb.Append("var YLocs={");

                    foreach (var groupEntry in _SavedLocalizationsGroups.OrderBy(kvp => kvp.Key)) {

                        string groupName = groupEntry.Key;
                        Dictionary<string, object> locEntries = groupEntry.Value;

                        sb.Append("{0}:{{", groupName);

                        foreach (var locEntry in locEntries.OrderBy(kvp => kvp.Key)) {
                            var loc = locEntry;
                            string val = YetaWFManager.JsonSerialize(loc.Value);
                            sb.Append("'{0}':{1},", loc.Key, val);
                        }
                        sb.RemoveLast(); // remove last ,
                        sb.Append("},");
                    }
                    sb.RemoveLast(); // remove last ,
                    sb.Append("};\n");
                } else {
                    foreach (var groupEntry in _SavedLocalizationsGroups.OrderBy(kvp => kvp.Key)) {
                        string groupName = groupEntry.Key;
                        sb.Append("YLocs.{0}=YLocs.{0}||{{}};", groupName);
                        Dictionary<string, object> locEntries = groupEntry.Value;
                        foreach (var locEntry in locEntries.OrderBy(kvp => kvp.Key)) {
                            var loc = locEntry;
                            string val = YetaWFManager.JsonSerialize(loc.Value);
                            sb.Append("YLocs.{0}.{1}={2};", groupName, loc.Key, val);
                        }
                    }
                }
            }
        }

        private bool WantBundle(PageContentController.PageContentData cr) {
            if (cr != null)
                return false;
            else
                return Manager.CurrentSite.BundleJSFiles;
        }

        private HtmlBuilder RenderScriptsFiles(PageContentController.PageContentData cr = null, List<string> KnownScripts = null) {
            HtmlBuilder hb = new HtmlBuilder();

            ScriptBuilder sbStart = new ScriptBuilder();

            List<ScriptEntry> externalList;
            if (!Manager.CurrentSite.DEBUGMODE && WantBundle(cr)) {
                List<string> bundleList = (from s in _Scripts orderby s.Last where s.Bundle select s.Url).ToList();
                if (KnownScripts != null)
                    bundleList = bundleList.Except(KnownScripts).ToList();
                externalList = (from s in _Scripts orderby s.Last where !s.Bundle select s).ToList();
                if (bundleList.Count > 1) {
                    string bundleUrl = FileBundles.MakeBundle(bundleList, FileBundles.BundleTypeEnum.JS, sbStart);
                    if (!string.IsNullOrWhiteSpace(bundleUrl))
                        externalList.Add(new ScriptEntry {
                            Url = bundleUrl,
                            Bundle = false,
                            Last = true,
                        });
                    if (cr != null)
                        cr.ScriptBundleFiles.AddRange(bundleList);
                } else {
                    externalList = (from s in _Scripts orderby s.Last select s).ToList();
                }
            } else {
                externalList = (from s in _Scripts orderby s.Last select s).ToList();
            }

            foreach (ScriptEntry entry in externalList) {
                string url = entry.Url;
                url = Manager.GetCDNUrl(url);
                if (cr == null) {
                    string opts = "";
                    opts += entry.Async ? " async" : "";
                    opts += entry.Defer ? " defer" : "";
                    hb.Append(string.Format("<script type='text/javascript' data-name='{0}' src='{1}'{2}></script>",
                        YetaWFManager.UrlEncodePath(entry.Url), YetaWFManager.UrlEncodePath(url), opts));
                } else {
                    if (KnownScripts == null || !KnownScripts.Contains(entry.Url)) {
                        cr.ScriptFiles.Add(new Controllers.PageContentController.UrlEntry {
                            Name = entry.Url,
                            Url = url,
                        });
                        if (entry.Bundle) {
                            string file = YetaWFManager.UrlToPhysical(entry.Url);
                            string contents = File.ReadAllText(file);
                            cr.ScriptFilesPayload.Add(new PageContentController.Payload {
                                Name = entry.Url,
                                Text = contents,
                            });
                        }
                    }
                }
            }
            return hb;
        }
        internal List<string> GetBundleFiles() {
            if (!Manager.CurrentSite.DEBUGMODE && WantBundle(null)) {
                List<string> bundleList = (from s in _Scripts orderby s.Last where s.Bundle select s.Url).ToList();
                if (bundleList.Count > 1)
                    return bundleList;
            }
            return null;
        }

        /// <summary>
        /// End of page script snippets.
        /// </summary>
        /// <returns>The minimized javascript.</returns>
        private ScriptBuilder RenderScriptsPartB() {
            ScriptBuilder sb = new ScriptBuilder();
            foreach (var script in _SavedScripts) {
                sb.Append(TrimScript(Manager, script));
            }
            return sb;
        }

        public string RenderEndofPageScripts(PageContentController.PageContentData cr = null) {
            HtmlBuilder hb = new HtmlBuilder();

            ScriptBuilder sbB = RenderScriptsPartB();
            if (sbB.Length > 0 || _SavedNamedScripts.Count > 0 || _SavedNamedScriptsDocReady.Count > 0) {
                //hb.Append("\n<script type=\"text/javascript\">\n//<![CDATA[\n");
                if (cr == null)
                    hb.Append("<script type=\"text/javascript\">");
                if (_SavedNamedScripts.Count > 0) {
                    foreach (var script in _SavedNamedScripts) {
                        hb.Append(TrimScript(Manager, script.Value));
                    }
                }
                if (_SavedNamedScriptsDocReady.Count > 0) {
                    hb.Append("YetaWF_Basics.whenReadyOnce.push({ callback: function($tag) {\n");
                    foreach (var script in _SavedNamedScriptsDocReady) {
                        hb.Append(TrimScript(Manager, script.Value));
                    }
                    hb.Append("}});\n");
                }
                if (sbB.Length > 0)
                    hb.Append(sbB.ToString());
                if (cr == null)
                    hb.Append("</script>");
                //hb.Append("\n//]]>\n</script>\n");
            }
            if (cr != null) {
                cr.EndOfPageScripts = hb.ToString();
                return null;
            } else
                 return hb.ToString();
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
