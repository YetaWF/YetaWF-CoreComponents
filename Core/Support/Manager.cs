﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.Extensions;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Support.Repository;
using YetaWF.Core.Support.StaticPages;
using YetaWF.Core.Support.UrlHistory;
using YetaWF.Core.Skins;
using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using System.Globalization;
using Newtonsoft.Json;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
#else
using System.Web;
using System.Web.Hosting;
#endif

namespace YetaWF.Core.Support {
    public class YetaWFManager {

        private static readonly string YetaWF_ManagerKey = typeof(YetaWFManager).Module + " sft";
        public const string BATCHMODE = "Batch";

#if MVC6
        public class DummyServiceProvider : IServiceProvider {
            public object GetService(Type serviceType) { return null; }
        }
        public class DummyHttpContextAccessor : IHttpContextAccessor {
            public HttpContext HttpContext { get { return null; } set { } }
        }
        public class DummyMemoryCache : IMemoryCache {
            public ICacheEntry CreateEntry(object key) { return null; }
            public void Dispose() { }
            public void Remove(object key) { }
            public bool TryGetValue(object key, out object value) { value = null; return false; }
        }
#else
#endif

#if MVC6
        public static void Init(IServiceProvider serviceProvider = null, IHttpContextAccessor httpContextAccessor = null, IMemoryCache memoryCache = null) {
            ServiceProvider = serviceProvider ?? new DummyServiceProvider();
            HttpContextAccessor = httpContextAccessor ?? new DummyHttpContextAccessor();
            MemoryCache = memoryCache ?? new DummyMemoryCache();
        }
        public static IServiceProvider ServiceProvider = null;
        public static IHttpContextAccessor HttpContextAccessor = null;
        public static IMemoryCache MemoryCache = null;
#else
#endif
        private YetaWFManager(string host) {
            SiteDomain = host; // save the host name that owns this Manager
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public static YetaWFManager Manager {
            get {
#if MVC6
                YetaWFManager manager = null;
                HttpContext context = HttpContextAccessor.HttpContext;

                if (context != null) {
                    manager = context.Items[YetaWF_ManagerKey] as YetaWFManager;
                } else {
                    // not a webrequest - most likely a scheduled task
                    // check if we have thread data
                    LocalDataStoreSlot slot = Thread.GetNamedDataSlot(YetaWF_ManagerKey);
                    manager = (YetaWFManager)Thread.GetData(slot);
                }
                if (manager == null)
                    throw new Error("We don't have a YetaWFManager object.");
                return manager;
#else
                YetaWFManager manager = null;
                if (HttpContext.Current != null) {
                    manager = HttpContext.Current.Items[YetaWF_ManagerKey] as YetaWFManager;
                } else {
                    // not a webrequest - most likely a scheduled task
                    // check if we have thread data
                    LocalDataStoreSlot slot = Thread.GetNamedDataSlot(YetaWF_ManagerKey);
                    manager = (YetaWFManager)Thread.GetData(slot);
                }
                if (manager == null)
                    throw new Error("We don't have a YetaWFManager object.");
                return manager;
#endif
            }
        }

        public static bool HaveManager {
            get {
#if MVC6
                if (HttpContextAccessor.HttpContext != null) {
                    if (HttpContextAccessor.HttpContext.Items[YetaWF_ManagerKey] == null) return false;
#else
                if (HttpContext.Current != null) {
                    if (HttpContext.Current.Items[YetaWF_ManagerKey] == null) return false;
#endif
                } else {
                    LocalDataStoreSlot slot = Thread.GetNamedDataSlot(YetaWF_ManagerKey);
                    if (slot == null) return false;
                    if (Thread.GetData(slot) == null) return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Creates an instance of the YetaWFManager for a site.
        /// Only done in Global.asax as soon as the site URL has been determined.
        /// </summary>
#if MVC6
        public static YetaWFManager MakeInstance(HttpContext httpContext, string siteHost) {
            if (siteHost == null)
                throw new Error("Site host required to create a YetaWFManager object.");
#if DEBUG
            if (httpContext.Items[YetaWF_ManagerKey] != null)
                throw new Error("We already have a YetaWFManager object.");
#endif
            YetaWFManager manager = new YetaWFManager(siteHost);
            httpContext.Items[YetaWF_ManagerKey] = manager;
            return manager;
        }
#else
        public static YetaWFManager MakeInstance(string siteHost) {
            if (siteHost == null)
                throw new Error("Site host required to create a YetaWFManager object.");
#if DEBUG
            if (HttpContext.Current.Items[YetaWF_ManagerKey] != null)
                throw new Error("We already have a YetaWFManager object.");
#endif
            YetaWFManager manager = new YetaWFManager(siteHost);
            HttpContext.Current.Items[YetaWF_ManagerKey] = manager;
            return manager;
        }
#endif

        /// <summary>
        /// Creates an instance of the YetaWFManager - used for non-site specific threads (e.g., scheduler).
        /// Can only be used once MakeInitialThreadInstance has been used
        /// </summary>
        public static YetaWFManager MakeThreadInstance(SiteDefinition site) {
            YetaWFManager manager;
            if (site != null) {
                manager = new YetaWFManager(site.Identity.ToString());
                manager.CurrentSite = site;
                manager.HostUsed = site.SiteDomain;
                manager.HostPortUsed = 80;
                manager.HostSchemeUsed = "http";
            } else {
                manager = new YetaWFManager(null);
                manager.UserLanguage = MultiString.DefaultLanguage;
                if (SiteDefinition.LoadSiteDefinitionAsync != null) {
                    manager.HostUsed = SiteDefinition.GetDefaultSiteDomainAsync().Result; // sync OK as it's cached - this will not run async as we don't have a Manager
                    manager.HostPortUsed = 80;
                    manager.HostSchemeUsed = "http";
                } else {
                    manager.HostUsed = BATCHMODE;
                }
            }
            LocalDataStoreSlot slot = Thread.GetNamedDataSlot(YetaWF_ManagerKey);
            Thread.SetData(slot, manager);
            manager.LocalizationSupportEnabled = false;

            manager.UserName = null;// current user (anonymous)
            manager.UserSettingsObject = new SchedulerUserData {
                DateFormat = Localize.Formatting.DateFormatEnum.MMDDYYYY,
                TimeFormat = Localize.Formatting.TimeFormatEnum.HHMMAM,
                LanguageId = MultiString.DefaultLanguage,
                TimeZone = TimeZoneInfo.Local.Id,
            };
            if (site != null)
                manager.GetUserLanguage();// get user's default language

            return manager;
        }
        public class SchedulerUserData {
            public Localize.Formatting.DateFormatEnum DateFormat { get; set; }
            public string TimeZone { get; set; }
            public Localize.Formatting.TimeFormatEnum TimeFormat { get; set; }
            public string LanguageId { get; set; }
        }

        public static YetaWFManager MakeInitialThreadInstance(SiteDefinition site) {
#if DEBUG
            if (Thread.GetNamedDataSlot(YetaWF_ManagerKey) == null) { // avoid exception spam
#endif
                try {
                    Thread.AllocateNamedDataSlot(YetaWF_ManagerKey);
                } catch (Exception) { }
#if DEBUG
            }
#endif
            return MakeThreadInstance(site);
        }
        public static void RemoveThreadInstance() {
            Thread.FreeNamedDataSlot(YetaWF_ManagerKey);
        }

        public enum AspNetMvcVersion {
            MVC5 = 0, // ASP.NET MVC 5
            MVC6 = 6, // ASP.NET Core MVC
        }
        public static AspNetMvcVersion AspNetMvc {
            get {
#if MVC6
                return AspNetMvcVersion.MVC6;
#else
                return AspNetMvcVersion.MVC5;
#endif
            }
        }
        public static string GetAspNetMvcName(AspNetMvcVersion version) {
            switch (version) {
                case AspNetMvcVersion.MVC5:
                    return "MVC5";
                case AspNetMvcVersion.MVC6:
                    return "ASP.NET Core MVC";
                default:
                    return "(unknown)";
            }
        }
        public static string GetAspNetCss(AspNetMvcVersion version) {
            switch (version) {
                case AspNetMvcVersion.MVC5:
                    return "yASPNET4 yMVC5";
                case AspNetMvcVersion.MVC6:
                    return "yASPNETCore yMVC6";
                default:
                    return null;
            }
        }

        private static void SetRequestedDomain(string siteDomain) {
#if MVC6
            if (siteDomain == null)
                HttpContextAccessor.HttpContext.Session.Remove(Globals.Link_ForceSite);
            else
                HttpContextAccessor.HttpContext.Session.SetString(Globals.Link_ForceSite, siteDomain);
#else
            HttpContext.Current.Session[Globals.Link_ForceSite] = siteDomain;
#endif

        }
        public static string GetRequestedDomain(Uri uri, bool loopBack, string siteDomain, out bool overridden, out bool newSwitch) {
            overridden = newSwitch = false;

            if (loopBack) {
                if (!string.IsNullOrWhiteSpace(siteDomain)) {
                    overridden = newSwitch = true;
                    SetRequestedDomain(siteDomain);
                }
#if MVC6
                if (!overridden && HttpContextAccessor.HttpContext.Session != null) {
                    siteDomain = (string)HttpContextAccessor.HttpContext.Session.GetString(Globals.Link_ForceSite);
#else
                if (!overridden && HttpContext.Current.Session != null) {
                    siteDomain = (string)HttpContext.Current.Session[Globals.Link_ForceSite];
#endif
                    if (!string.IsNullOrWhiteSpace(siteDomain))
                        overridden = true;
                }
            }
            if (!overridden)
                siteDomain = uri.Host;
            // beautify the host name a bit
            if (siteDomain.Length > 1)
                siteDomain = char.ToUpper(siteDomain[0]) + siteDomain.Substring(1).ToLower();
            else
                siteDomain = siteDomain.ToUpper();
            return siteDomain;
        }

        public static string CacheBuster {
            get {
                if (_cacheBuster == null) {
                    if (Manager.CurrentSite.DEBUGMODE || !Manager.CurrentSite.AllowCacheUse)
                        _cacheBuster = (DateTime.Now.Ticks / TimeSpan.TicksPerSecond).ToString();/*local time*/
                    else
                        _cacheBuster = (YetaWF.Core.Support.Startup.MultiInstanceStartTime.Ticks / TimeSpan.TicksPerSecond).ToString();
                }
                return _cacheBuster;
            }
        }
        private static string _cacheBuster;

        /// <summary>
        /// Web site root folder (physical, wwwroot on ASP.NET Core MVC)
        /// </summary>
        public static string RootFolder { get; set; }

        /// <summary>
        /// Web project root folder (physical)
        /// </summary>
        /// <remarks>
        /// With MVC5, this is the same as the web site root folder (RootFolder). MVC6+ this is the root folder of the web project.</remarks>
        public static string RootFolderWebProject {
#if MVC6
            get; set;
#else
            get { return RootFolder; }
#endif
        }
        /// <summary>
        /// Solution root folder (physical)
        /// </summary>
        /// <remarks>
        /// The folder where the solution file is located.</remarks>
        public static string RootFolderSolution {
            get { return Path.Combine(RootFolderWebProject, ".."); }
        }

        /// <summary>
        /// Returns the folder containing all sites' file data.
        /// </summary>
        public static string RootSitesFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolderWebProject, Globals.SitesFolder, "DataFolder");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error")]
        public static string DefaultSiteName {
            get {
                if (defaultSiteName == null)
                    defaultSiteName = WebConfigHelper.GetValue<string>("YetaWF_Core"/*==YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName*/, "DEFAULTSITE");
                if (defaultSiteName == null)
                    throw new InternalError("Default site must be defined in Appsettings.json");
                return defaultSiteName;
            }
        }
        private static string defaultSiteName;

        /// <summary>
        /// Data folder (not site specific).
        /// </summary>
        public static string DataFolder {
            get {
                string rootFolder;
#if MVC6
                rootFolder = YetaWFManager.RootFolderWebProject;
#else
                rootFolder = YetaWFManager.RootFolder;
#endif
                return Path.Combine(rootFolder, Globals.DataFolder, "DataFolder");
            }
        }

        /// <summary>
        /// Data folder (not site specific).
        /// </summary>
        public static string LicenseFolder {
            get {
                string rootFolder;
#if MVC6
                rootFolder = YetaWFManager.RootFolderWebProject;
#else
                rootFolder = YetaWFManager.RootFolder;
#endif
                return Path.Combine(rootFolder, Globals.DataFolder, "Licenses");
            }
        }

        /// <summary>
        /// Vault data folder (not site specific).
        /// </summary>
        /// <remarks>Not publicly accessible on ASP.NET Core MVC. Publicly accessible on MVC5 and must be protected using Web.config files.</remarks>
        public static string VaultPrivateFolder {
            get {
                string rootFolder;
#if MVC6
                rootFolder = YetaWFManager.RootFolderWebProject;
#else
                rootFolder = YetaWFManager.RootFolder;
#endif
                return Path.Combine(rootFolder, Globals.VaultPrivateFolder);
            }
        }

        /// <summary>
        /// Vault data folder (not site specific).
        /// </summary>
        /// <remarks>Publicly accessible.</remarks>
        public static string VaultFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolder, Globals.VaultFolder);
            }
        }
        /// <summary>
        /// Translate a Url to a local file name.
        /// </summary>
        /// <remarks>Doesn't really take special characters (except spaces %20) into account.</remarks>
        public static string UrlToPhysical(string url) {
            if (!url.StartsWith("/")) throw new InternalError("Urls to translate must start with /.");
#if MVC6
            string path;
            if (url.StartsWith(Globals.NodeModulesUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + YetaWFManager.UrlToPhysicalRaw(url);
            } else if (url.StartsWith(Globals.BowerComponentsUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + YetaWFManager.UrlToPhysicalRaw(url);
            } else if (url.StartsWith(Globals.VaultPrivateUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + YetaWFManager.UrlToPhysicalRaw(url);
            } else {
                path = RootFolder + UrlToPhysicalRaw(url);
            }
            return path;
#else
            if (url.StartsWith(Globals.VaultPrivateUrl, StringComparison.OrdinalIgnoreCase))
                url = url.ReplaceFirst(Globals.VaultPrivateUrl, Globals.VaultUrl);
            url = url.Replace("%20", " ");
            return HostingEnvironment.MapPath(url.RemoveStartingAt('?'));
#endif
        }
        private static string UrlToPhysicalRaw(string url) {
            if (!url.StartsWith("/")) throw new InternalError("Urls to translate must start with /.");
            url = url.Replace('/', '\\');
            url = url.Replace("%20", " ");
            return url;
        }
        /// <summary>
        /// Translate a local file name to a Url.
        /// </summary>
        /// <remarks>Doesn't really take special characters (except spaces %20) into account.</remarks>
        public static string PhysicalToUrl(string path) {
            path = ReplaceString(path, RootFolder, String.Empty, StringComparison.OrdinalIgnoreCase);
#if MVC6
            path = ReplaceString(path, RootFolderWebProject, String.Empty, StringComparison.OrdinalIgnoreCase);
#else
#endif
            path = path.Replace(" ", "%20");
            return path.Replace('\\', '/');
        }
        private static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison) {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1) {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));
            return sb.ToString();
        }

        public static string JsonSerialize(object value, bool Indented = false) {
            return JsonConvert.SerializeObject(value, Indented ? _JsonSettingsIndented : _JsonSettings);
        }
        public static object JsonDeserialize(string value) {
            return JsonConvert.DeserializeObject(value, _JsonSettings);
        }
        public static object JsonDeserialize(string value, Type type) {
            return JsonConvert.DeserializeObject(value, type, _JsonSettings);
        }
        public static TYPE JsonDeserialize<TYPE>(string value) {
            return JsonConvert.DeserializeObject<TYPE>(value, _JsonSettings);
        }
        private static JsonSerializerSettings _JsonSettings = new JsonSerializerSettings {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };
        private static JsonSerializerSettings _JsonSettingsIndented = new JsonSerializerSettings {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            ObjectCreationHandling = ObjectCreationHandling.Replace,

            Formatting = Newtonsoft.Json.Formatting.Indented,
        };

        public static string JserEncode(string s) {
#if MVC6
            if (s == null) return "";
            return System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(s);
#else
            return HttpUtility.JavaScriptStringEncode(s);
#endif
        }
        public static string UrlFor(Type type, string actionName, object args = null) {
            if (!type.Name.EndsWith("Controller")) throw new InternalError("Type {0} is not a controller", type.FullName);
            string controller = type.Name.Substring(0, type.Name.Length - "Controller".Length);
            Package package = Package.TryGetPackageFromAssembly(type.Assembly);
            if (package == null)
                throw new InternalError("Type {0} is not part of a package", type.FullName);
            string area = package.AreaName;
            string url = "/" + area + "/" + controller + "/" + actionName;
            QueryHelper query = QueryHelper.FromAnonymousObject(args);
            return query.ToUrl(url);
        }
        public static string HtmlEncode(string s) {
            if (s == null) return "";
#if MVC6
            return System.Net.WebUtility.HtmlEncode(s);
#else
            return HttpUtility.HtmlEncode(s);
#endif
        }
        public static string HtmlDecode(string s) {
            if (s == null) return "";
#if MVC6
            return System.Net.WebUtility.HtmlDecode(s);
#else
            return HttpUtility.HtmlDecode(s);
#endif
        }
        public static string HtmlAttributeEncode(string s) {
            if (s == null) return "";
#if MVC6
            return System.Net.WebUtility.HtmlEncode(s);
#else
            return HttpUtility.HtmlAttributeEncode(s);
#endif
        }
        // used to encode args in url
        public static string UrlEncodeArgs(string s) {
            if (s == null) return "";
            return Uri.EscapeDataString(s);
        }
        // used to decode args in url
        public static string UrlDecodeArgs(string s) {
            if (s == null) return "";
            return Uri.UnescapeDataString(s);
        }
        // used to encode the page path segments (between /xxx/)
        public static string UrlEncodeSegment(string s) {
            if (s == null) return "";
            string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-[]@!$";
            int inv = 0;
            StringBuilder sb = new StringBuilder();
            foreach (char c in s) {
                if (validChars.Contains(c)) {
                    if (inv > 0) {
                        sb.Append("%20");
                        inv = 0;
                    }
                    sb.Append(c);
                } else
                    ++inv;
            }
            return sb.ToString();
        }
        public static string UrlEncodePath(string s) {
            if (string.IsNullOrWhiteSpace(s)) return null;
            StringBuilder sb = new StringBuilder();
            s = SkipSchemeAndDomain(sb, s);
            string validChars = "_./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-[]@!$";
            for (int len = s.Length, ix = 0; ix < len; ++ix) {
                char c = s[ix];
                if (c == '?' || c == '#') {
                    sb.Append(s.Substring(ix));
                    break;
                }
                if (validChars.Contains(c)) {
                    sb.Append(c);
                } else if (c == '%') {
                    if (ix + 1 < len && char.IsNumber(s[ix + 1])) {
                        if (ix + 2 < len && char.IsNumber(s[ix + 2])) {
                            sb.Append(s.Substring(ix, 3));
                            ix += 2;// all good, skip %nn
                            continue;
                        }
                    }
                    sb.Append(string.Format("%{0:X2}", (int)c));
                } else {
                    sb.Append(string.Format("%{0:X2}", (int)c));
                }
            }
            return sb.ToString();
        }
        internal static string UrlDecodePath(string s) {
            if (string.IsNullOrWhiteSpace(s)) return null;
            StringBuilder sb = new StringBuilder();
            s = SkipSchemeAndDomain(sb, s);
            for (int len = s.Length, ix = 0; ix < len; ++ix) {
                char c = s[ix];
                if (c == '?' || c == '#') {
                    sb.Append(s.Substring(ix));
                    break;
                }
                if (c == '%') {
                    if (ix + 1 < len && char.IsNumber(s[ix + 1])) {
                        if (ix + 2 < len && char.IsNumber(s[ix + 2])) {
                            string val = s[ix + 1].ToString() + s[ix + 2].ToString();
                            sb.Append(Convert.ToChar(Convert.ToInt32(val, 16)));
                            ix += 2;// all good, skip %nn
                        } else {
                            sb.Append(c);
                        }
                    } else {
                        sb.Append(c);
                    }
                } else {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string SkipSchemeAndDomain(StringBuilder sb, string s) {
            // handle this: (some parts optional)  see https://en.wikipedia.org/wiki/Uniform_Resource_Identifier
            //   abc://username:password@example.com:123/path/data?key=value#fragid1
            int iScheme = s.IndexOf(':');
            if (iScheme >= 0) {
                sb.Append(s.Substring(0, iScheme + 1));
                s = s.Substring(iScheme + 1);
            }
            if (s.StartsWith("//")) {
                sb.Append("//");
                s = s.Substring(2);
            }
            int iAuth = s.IndexOf('@');
            if (iAuth >= 0) {
                sb.Append(s.Substring(0, iAuth + 1));
                s = s.Substring(iAuth + 1);
            }
            int iPort = s.IndexOf(':');
            if (iPort >= 0) {
                sb.Append(s.Substring(0, iPort + 1));
                s = s.Substring(iPort + 1);
            }
            if (iAuth >= 0 || iPort >= 0 || iPort >= 0) {
                int iPath = s.IndexOf('/');
                if (iPath >= 0) {
                    sb.Append(s.Substring(0, iPath + 1));
                    s = s.Substring(iPath + 1);
                }
            }
            return s;
        }

        private static string SkipDomain(StringBuilder sb, string s) {
            int i = s.IndexOf('/');
            if (i >= 0) {
                sb.Append(s.Substring(0, i));
                s = s.Substring(i);
            }
            return s;
        }

        public static string CombineCss(string css1, string css2) {
            if (string.IsNullOrWhiteSpace(css1)) return css2;
            if (string.IsNullOrWhiteSpace(css2)) return css1;
            return string.Format("{0} {1}", css1.Trim(), css2.Trim());
        }

        // BUILD
        // BUILD
        // BUILD

        /// <summary>
        /// Defines whether the currently running instance of YetaWF is a deployed instance or not.
        /// </summary>
        /// <value>false for a deployed site, true otherwise.</value>
        [Obsolete("Horribly misnamed property - Do not use because it's confusing")]
        public bool DebugBuild {
            get {
                return !GetDeployed();
            }
        }

        /// <summary>
        /// Defines whether the currently running instance of YetaWF is a deployed instance or not.
        /// </summary>
        /// <remarks>
        /// A "deployed" instance is not necessarily a Release build, but behaves as though it is.
        ///
        /// A deployed instance is considered to run as a public website with all development features disabled.
        /// TODO: Need an actual list of development features here.
        /// </remarks>
        /// <value>true for a deployed site, false otherwise.</value>
        public bool Deployed {
            get {
                return GetDeployed();
            }
        }
        private static bool? deployed = null;

        protected static bool GetDeployed() {
            if (deployed == null) {
                deployed = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "Deployed");
            }
            return (bool)deployed;
        }

        public bool CanUseCDN {
            get {
                if (canUseCDN == null) {
                    canUseCDN = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "UseCDN");
                }
                return (bool)canUseCDN;
            }
        }
        private static bool? canUseCDN = null;

        public bool CanUseCDNComponents {
            get {
                if (canUseCDNComponents == null) {
                    canUseCDNComponents = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "UseCDNComponents");
                }
                return (bool)canUseCDNComponents;
            }
        }
        private static bool? canUseCDNComponents = null;

        public bool CanUseStaticDomain {
            get {
                if (canUseStaticDomain == null) {
                    canUseStaticDomain = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "UseStaticDomain");
                }
                return (bool)canUseStaticDomain;
            }
        }
        private static bool? canUseStaticDomain = null;

        public bool IsStaticSite { get; set; }

        /// <summary>
        /// Defines whether the current YetaWF instance runs in demo mode.
        /// </summary>
        /// <remarks>Demo mode allows anonymous users to use all features in Superuser mode, without being able to change any data.
        ///
        /// Demo mode is enabled/disabled using the Appsettings.json setting P:YetaWF_Core:Demo.
        /// </remarks>
        public bool IsDemo {
            get {
                if (isDemo == null)
                    isDemo = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "Demo");
                return (bool)isDemo;
            }
        }
        private static bool? isDemo = null;

        // HTTPCONTEXT
        // HTTPCONTEXT
        // HTTPCONTEXT

        /// <summary>
        /// Current site's domain - E.g., softelvdm.com, localhost, etc. or empty string if default site
        /// </summary>
        public string SiteDomain { get; set; }

        /// <summary>
        /// The host used to access this web site
        /// </summary>
        public string HostUsed { get; set; }
        public int HostPortUsed { get; set; }
        public string HostSchemeUsed { get; set; }
        public bool IsLocalHost { get; set; }
        public bool IsTestSite { get; set; }

        /// <summary>
        /// Returns the folder containing the current site's file data.
        /// </summary>
        public string SiteFolder {
            get {
                return Path.Combine(RootSitesFolder, CurrentSite.Identity.ToString());
            }
        }
        /// <summary>
        /// Returns the site's custom addons folder
        /// </summary>
        public string AddonsCustomSiteFolder {
            get {
                return Path.Combine(YetaWFManager.UrlToPhysical(Globals.AddOnsCustomUrl), SiteDomain);
            }
        }
        /// <summary>
        /// The current site definition.
        /// The current site is identified based on the URL of the current request.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public SiteDefinition CurrentSite {
            get {
                if (_currentSite == null)
                    throw new InternalError("Site definition for {0} has not yet been loaded", SiteDomain);
                return _currentSite;
            }
            set {
                _currentSite = value;
            }
        }
        private SiteDefinition _currentSite;

        public bool HaveCurrentSite {
            get {
                return _currentSite != null;
            }
        }

        /// <summary>
        /// Saved URL where we came from (e.g. used for return handling after Save)
        /// </summary>
        public List<Origin> OriginList { get; set; }

        /// <summary>
        /// Returns the last entry of the OriginList without removing it.
        /// </summary>
        public Origin QueryReturnToUrl {
            get {
                if (OriginList == null || OriginList.Count == 0)
                    return new Origin {
                        Url = CurrentSite.HomePageUrl,
                        EditMode = false,
                        InPopup = false
                    };
                return OriginList.Last();
            }
        }

        public bool HaveReturnToUrl {
            get {
                return OriginList != null && OriginList.Count > 0;
            }
        }

        /// <summary>
        /// Returns the Url to return to, including origin list and other querystring parms.
        /// </summary>
        /// <remarks>The Return To Url also contains the remaining Origin List as a parameter.
        ///
        /// The Return To Url is removed from the saved Origin List. To preserve the Url, use QueryReturnToUrl instead.</remarks>
        public string ReturnToUrl {
            get {
                if (OriginList == null) return CurrentSite.HomePageUrl;
                List<Origin> originList = (from Origin l in OriginList select l).ToList<Origin>();// copy list
                Origin entry;
                if (originList.Count > 0) {
                    entry = originList.Last();
                } else {
                    entry = new Origin {
                        Url = CurrentSite.HomePageUrl,
                        EditMode = false,
                        InPopup = false
                    };
                    originList = new List<Origin>() { entry };
                }
                originList.RemoveAt(originList.Count - 1); // remove last entry
                string url = entry.Url;
                if (originList.Count > 0) {
                    string urlOnly;
                    QueryHelper qh = QueryHelper.FromUrl(url, out urlOnly);
                    qh.Remove(Globals.Link_OriginList);
                    qh.Add(Globals.Link_OriginList, YetaWFManager.JsonSerialize(originList));
                    url = qh.ToUrl(urlOnly);
                }
                return url;
            }
        }

        // GetUrlArg and TryGetUrlArg is used to retrieve optional Url args (outside of a Controller) added to a page using AddUrlArg, so one module can add args for other modules on the same page

        public TYPE GetUrlArg<TYPE>(string arg) {
            TYPE val;
            if (!TryGetUrlArg<TYPE>(arg, out val))
                throw new InternalError(this.__ResStr("invUrlArg", "{0} URL argument invalid or missing", arg));
            return val;
        }
        public bool TryGetUrlArg<TYPE>(string arg, out TYPE val, TYPE dflt = default(TYPE)) {
            val = dflt;
            string v;
            try {
                v = RequestQueryString[arg];
                if (v == null)
                    ExtraUrlArgs.TryGetValue(arg, out v);
                if (v == null)
                    return false;
            } catch (Exception) {
                return false;
            }
            if (typeof(TYPE) == typeof(int) || typeof(TYPE) == typeof(int?)) {
                try {
                    val = (TYPE)(object)Convert.ToInt32(v);
                } catch (Exception) {
                    return false;
                }
                return true;
            } else if (typeof(TYPE) == typeof(bool) || typeof(TYPE) == typeof(bool?)) {
                val = (TYPE)(object)((v == "1" || v.ToLower() == "on" || v.ToLower() == "true" || v.ToLower() == "yes") ? true : false);
                return true;
            } else if (typeof(TYPE) == typeof(string)) {
                try {
                    val = (TYPE)(object)v;
                } catch (Exception) {
                    return false;
                }
                return true;
            } else {
                // TryGetUrlArg doesn't support this type
                return false;
            }
        }

        /// <summary>
        /// Add a temporary (non-visible) Url argument to the current page being rendered.
        /// This is mainly used to propagate selections from one module to another (top-down on page only).
        ///  Other modules must use TryGetUrlArg or GetUrlArg to retrieve these args as they're not part of the querystring/url.
        /// </summary>
        public void AddUrlArg(string arg, string value) {
            ExtraUrlArgs.Add(arg, value);
        }
        private Dictionary<string, string> ExtraUrlArgs = new Dictionary<string, string>();

        /// <summary>
        /// Returns whether the page control module is visible
        /// </summary>
        public bool PageControlShown { get; set; }

        /// <summary>
        /// Returns whether we're in a popup
        /// </summary>
        public bool IsInPopup { get; set; }

        public void Verify_NotPostRequest() {
            if (IsPostRequest)
                throw new InternalError("This is not supported for POST requests");
        }
        public void Verify_PostRequest() {
            if (!IsPostRequest)
                throw new InternalError("This is only supported for POST requests");
        }

        public MetatagsManager MetatagsManager {
            get {
                if (_metatagsManager == null)
                    _metatagsManager = new MetatagsManager(this);
                return _metatagsManager;
            }
        }
        private MetatagsManager _metatagsManager = null;

        public string MetatagsHtml {
            get {
                return MetatagsManager.Render();
            }
        }

        public LinkAltManager LinkAltManager {
            get {
                if (_linkAltManager == null)
                    _linkAltManager = new LinkAltManager();
                return _linkAltManager;
            }
        }
        private LinkAltManager _linkAltManager = null;

        public ScriptManager ScriptManager {
            get {
                if (_scriptManager == null)
                    _scriptManager = new ScriptManager(this);
                return _scriptManager;
            }
        }
        private ScriptManager _scriptManager = null;

        public CssManager CssManager {
            get {
                if (_cssManager == null)
                    _cssManager = new CssManager(this);
                return _cssManager;
            }
        }
        private CssManager _cssManager = null;

        public AddOnManager AddOnManager {
            get {
                if (_addOnManager == null)
                    _addOnManager = new AddOnManager(this);
                return _addOnManager;
            }
        }
        private AddOnManager _addOnManager = null;

        /// <summary>
        /// Static page manager.
        /// </summary>
        /// <remarks>
        /// User to manager a site's static pages. Only allocated if static pages are used.
        /// </remarks>
        public StaticPageManager StaticPageManager {
            get {
                if (_staticPageManager == null)
                    _staticPageManager = new StaticPageManager();
                return _staticPageManager;
            }
        }
        private StaticPageManager _staticPageManager = null;

        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT

        // Return a unique id - ids are only unique for 1 request. For Ajax/Post requests we have to
        // insure that we get Unique ids that don't duplicate ids obtained during the original request
        public string UniqueId(string name = "a") {
            ++_uniqueIdCounter;
            return UniqueIdPrefix + "_" + name + _uniqueIdCounter;
        }
        private int _uniqueIdCounter = 0;
        private int _uniqueIdPrefixCounter = 0;

        public string UniqueIdPrefix { get; set; } = "u0";

        public void NextUniqueIdPrefix() {
            UniqueIdPrefix = string.Format("u{0}", ++UniqueIdPrefixCounter);
        }
        public int UniqueIdPrefixCounter {
            get { return _uniqueIdPrefixCounter; }
            set { _uniqueIdPrefixCounter = value; UniqueIdPrefix = string.Format("u{0}", UniqueIdPrefixCounter); }
        }

        // HTTPCONTEXT
        // HTTPCONTEXT
        // HTTPCONTEXT

        public string UserHostAddress {
            get {
                if (!HaveCurrentRequest) return "";
                string ip = CurrentRequest.Headers["X-Forwarded-For"];
                // extract just IP address in case there is a port #
                if (!string.IsNullOrWhiteSpace(ip)) {
                    string[] s = ip.Split(new char[] { ':', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    ip = s[0].Truncate(Globals.MaxIP);
                }
                if (!string.IsNullOrWhiteSpace(ip)) return ip;
#if MVC6
                IHttpConnectionFeature connectionFeature = CurrentContext.Features.Get<IHttpConnectionFeature>();
                if (connectionFeature != null)
                    return connectionFeature.RemoteIpAddress.ToString();
                return "";
#else
                return CurrentRequest.UserHostAddress ?? "";
#endif
            }
        }
        public QueryHelper RequestQueryString {
            get {
                if (_requestQueryString == null) {
#if MVC6
                    _requestQueryString = QueryHelper.FromQueryCollection(CurrentRequest.Query);
#else
                    _requestQueryString = QueryHelper.FromNameValueCollection(CurrentRequest.QueryString);
#endif
                }
                return _requestQueryString;
            }
        }
        private QueryHelper _requestQueryString = null;

        public FormHelper RequestForm {
            get {
                if (_requestForm == null) {
#if MVC6
                    if (!CurrentRequest.HasFormContentType)
                        _requestForm = new FormHelper();
                    else
                        _requestForm = FormHelper.FromFormCollection(CurrentRequest.Form);
#else
                    _requestForm = FormHelper.FromNameValueCollection(CurrentRequest.Form);
#endif
                }
                return _requestForm;
            }
        }
        private FormHelper _requestForm = null;

        public bool HaveCurrentContext {
            get {
#if MVC6
                return HttpContextAccessor.HttpContext != null;
#else
                return HttpContext.Current != null;
#endif
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpContext CurrentContext {
            get {
#if MVC6
                HttpContext context = HttpContextAccessor.HttpContext;
#else
                HttpContext context = HttpContext.Current;
#endif
                if (context == null) throw new InternalError("No HttpContext available");
                return context;
            }
            set {
#if MVC6
                HttpContextAccessor.HttpContext = value;
#else
                throw new InternalError("Can't set current context");
#endif
            }
        }

        public bool HaveCurrentRequest {
            get {
#if MVC6
                return HaveCurrentContext && CurrentContext.Request != null;
#else
                return HaveCurrentContext && HttpContext.Current.Request != null;
#endif
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpRequest CurrentRequest {
            get {
#if MVC6
                HttpRequest request = CurrentContext.Request;
#else
                HttpRequest request = HttpContext.Current.Request;
#endif
                if (request == null) throw new InternalError("No current Request available");
                return request;
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpResponse CurrentResponse {
            get {
#if MVC6
                HttpResponse response = CurrentContext.Response;
#else
                HttpResponse response = HttpContext.Current.Response;
#endif
                if (response == null) throw new InternalError("No current Response available");
                return response;
            }
        }
        public string ReferrerUrl {
            get {
#if MVC6
                return Manager.CurrentRequest.Headers["Referer"].ToString();
#else
                return Manager.CurrentRequest.UrlReferrer != null ? Manager.CurrentRequest.UrlReferrer.ToString() : null;
#endif
            }
        }

        public static void SetStaticCacheInfo(HttpContext context) {
            if (GetDeployed() && StaticCacheDuration > 0) {
#if MVC6
                context.Response.Headers.Add("Cache-Control", string.Format("max-age={0}", StaticCacheDuration * 60));
#else
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetMaxAge(new TimeSpan(0, StaticCacheDuration, 0));
#endif
            }
            // add CORS header for static site
#if MVC6
            SiteDefinition site = SiteDefinition.LoadStaticSiteDefinitionAsync(context.Request.Host.Host).Result;// cached, so ok to use result
            if (site != null)
                context.Response.Headers.Add("Access-Control-Allow-Origin", $"{context.Request.Scheme}://{site.SiteDomain.ToLower()}");
#else
            SiteDefinition site = SiteDefinition.LoadStaticSiteDefinitionAsync(context.Request.Url.Host).Result;// cached, so ok to use result
            if (site != null)
                context.Response.Headers.Add("Access-Control-Allow-Origin", $"{context.Request.Url.Scheme}://{site.SiteDomain.ToLower()}");
#endif
        }
        public static int StaticCacheDuration {
            get {
                if (staticCacheDuration == null) {
                    staticCacheDuration = WebConfigHelper.GetValue<int>("StaticFiles", "Duration", 0);
                }
                return (int)staticCacheDuration;
            }
        }
        private static int? staticCacheDuration = null;

        public bool HaveCurrentSession {
            get {
#if MVC6
                return HaveCurrentContext && CurrentContext.Session != null;
#else
                return HaveCurrentContext && HttpContext.Current.Session != null;
#endif
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public SessionState CurrentSession {
            get {
                SessionState session = new SessionState(CurrentContext);
                if (session == null) throw new InternalError("No Session available");
                return session;
            }
        }

        public string CurrentSessionId {
            get {
#if MVC6
                if (HaveCurrentSession)
                    return CurrentContext.Session.Id;
#else
                if (HaveCurrentSession)
                    return CurrentContext.Session.SessionID;
#endif
                return null;
            }
        }

        public string CurrentRequestUrl {
            get {
#if MVC6
                return UriHelper.GetDisplayUrl(Manager.CurrentRequest);
#else
                return Manager.CurrentRequest.Url.ToString();
#endif
            }
        }
        public void RestartSite(string url = null) {
#if MVC6
            IApplicationLifetime applicationLifetime = (IApplicationLifetime)ServiceProvider.GetService(typeof(IApplicationLifetime));
            applicationLifetime.StopApplication();

            if (!string.IsNullOrWhiteSpace(url)) {
#if DEBUG
                // with Kestrel/IIS Express we shut down so provide some feedback
                try {
                    byte[] btes = System.Text.Encoding.ASCII.GetBytes("<html><head></head><body><strong>The site has stopped - Please close your browser and restart the application.<strong></body></html>");
                    Manager.CurrentResponse.Body.WriteAsync(btes, 0, btes.Length).Wait(); // Wait OK, this is debug only
                    Manager.CurrentResponse.Body.FlushAsync().Wait(); // Wait OK, this is debug only
                } catch (Exception) { }
#else
                CurrentResponse.Redirect(url);
#endif
            }
#else
            HttpRuntime.UnloadAppDomain();
            if (!string.IsNullOrWhiteSpace(url))
                CurrentResponse.Redirect(url);
#endif
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsPostRequest {
            get {
                HttpRequest request = CurrentRequest;
                string overRide = request.Headers["X-HTTP-Method-Override"];
                if (overRide != null)
                    return request.Headers["X-HTTP-Method-Override"] == "POST";
#if MVC6
                return (request.Method == "POST");
#else
                return (request.RequestType == "POST");
#endif
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsGetRequest {
            get {
                HttpRequest request = CurrentRequest;
                string overRide = request.Headers["X-HTTP-Method-Override"];
                if (overRide != null)
                    return overRide == "GET";
#if MVC6
                return (request.Method == "GET" || request.Method == "HEAD" || request.Method == "");
#else
                return (request.RequestType == "GET" || request.RequestType == "HEAD");
#endif
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsHeadRequest {
            get {
                HttpRequest request = CurrentRequest;
                string overRide = request.Headers["X-HTTP-Method-Override"];
                if (overRide != null)
                    return overRide == "HEAD";
#if MVC6
                return (request.Method == "HEAD");
#else
                return (request.RequestType == "HEAD");
#endif
            }
        }

        // SESSION
        // SESSION
        // SESSION

        public SessionSettings SessionSettings {
            get {
                if (_SessionSettings == null)
                    _SessionSettings = new SessionSettings();
                return _SessionSettings;
            }
        }
        private SessionSettings _SessionSettings = null;

        public bool EditMode { get; set; }

        public ModuleDefinition CurrentModuleEdited { get; set; }// used during module editing to signal which module is being edited
        public string ModeCss { get { return EditMode ? "yEditMode" : "yDisplayMode"; } }// used on body tag when in edit mode

        // PAGES
        // PAGES
        // PAGES

        /// <summary>
        /// The current page.
        /// </summary>
        public PageDefinition CurrentPage { get; set; }
        /// <summary>
        /// The set of pages the current page belongs to, if the current page is part of a set of unified pages.
        /// </summary>
        public List<PageDefinition> UnifiedPages { get; set; }
        /// <summary>
        /// The page mode used for unified pages.
        /// </summary>
        public PageDefinition.UnifiedModeEnum UnifiedMode { get; set; }

        /// <summary>
        /// The current page title. Modules can override the page title (we don't use the title in the page definition, except to set the default title).
        /// </summary>
        public MultiString PageTitle { get; set; }

        public string PageTitleHtml {
            get {
                string title = PageTitle.ToString();
                return string.Format("<title>{0}</title>", HtmlEncode(title ?? ""));
            }
        }
        /// <summary>
        /// Indicates whether the current page being rendered is actually rendered as a static page.
        /// </summary>
        /// <remarks>A page may be marked as a static page, but will only be rendered as a static page if there is no logged on user and if site settings permit static pages.
        ///
        /// Static pages are only used with deployed sites.</remarks>
        public bool RenderStaticPage { get; set; }

        // MODULES
        // MODULES
        // MODULES

        public ModuleDefinition CurrentModule { get; set; } // current module rendered

        // COMPONENTS/VIEWS
        // COMPONENTS/VIEWS
        // COMPONENTS/VIEWS

        public List<Package> ComponentPackagesSeen = new List<Package>();

        public IDisposable StartNestedComponent(string fieldName) {
            NestedComponents.Add(fieldName);
            return new NestedComponent();
        }
        public string NestedComponentPrefix {
            get {
                if (NestedComponents.Count == 0) return null;
                return NestedComponents.Last();
            }
        }
        private List<string> NestedComponents = new List<string>();

        private class NestedComponent : IDisposable {
            public NestedComponent() {
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                YetaWFManager manager = YetaWFManager.Manager;
                manager.NestedComponents.RemoveAt(manager.NestedComponents.Count-1);
                if (disposing) DisposableTracker.RemoveObject(this);
            }
        }

        // RENDERING
        // RENDERING
        // RENDERING

        /// <summary>
        /// The current pane being rendered.
        /// </summary>
        public string PaneRendered { get; set; }

        public bool IsRenderingPane { get { return PaneRendered != null; } }

        public bool ForceModuleActionLinks { get; set; } // force module action links outside of a pane

        // While rendering a module, this is set to reflect whether the module wants the input focus
        public bool WantFocus { get; set; }

        // While rendering a page or module, we set some average char width/height values - These are
        // defined by the active skin and are APPROXIMATE only and can be used to size "things". It's typically used
        // to convert em's or ch's to pixels
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public int CharHeight {
            get {
                if (_charSize.Height == 0)
                    throw new InternalError("We don't have a char height");
                return _charSize.Height;
            }
            private set { _charSize.Height = value; }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public int CharWidthAvg {
            get {
                if (_charSize.Width == 0)
                    throw new InternalError("We don't have an average char width");
                return _charSize.Width;
            }
            private set { _charSize.Width = value; }
        }
        public bool HaveCharSize { get { return !_charSize.IsEmpty; } }

        private Size _charSize = new Size();

        private List<Size> CharSizeStack {
            get {
                if (_charSizeStack == null)
                    _charSizeStack = new List<Size>();
                return _charSizeStack;
            }
        }
        private List<Size> _charSizeStack = null;

        public void PopCharSize() {
            if (CharSizeStack.Count > 0)
                CharSizeStack.RemoveAt(CharSizeStack.Count - 1);
        }
        private void PushCharSize() {
            CharSizeStack.Add(_charSize);
        }
        public void NewCharSize(int width, int height) {
            PushCharSize();
            CharWidthAvg = width;
            CharHeight = height;
        }
        /// <summary>
        /// Contains the last date/time updated while rendering a page.
        /// </summary>
        public DateTime LastUpdated { get { return _lastUpdated; } set { if (value > LastUpdated) _lastUpdated = value; } }
        private DateTime _lastUpdated;

        public bool RenderingUniqueModuleAddons { get; set; }
        public bool RenderingUniqueModuleAddonsAjax { get; set; }

        // FORM PROCESSING
        // FORM PROCESSING
        // FORM PROCESSING

        /// <summary>
        /// True while processing a partial view (usually a partial form/ajax)
        /// </summary>
        public bool InPartialView { get; set; }

        /// <summary>
        /// Defines whether non-site specific data is also imported when importing packages
        /// </summary>
        /// <remarks>Site specific data is always imported</remarks>
        public bool ImportChunksNonSiteSpecifics { get; set; }

        // UTILITY
        // UTILITY
        // UTILITY

        /// <summary>
        /// Define options for the current page or popup skin.
        /// </summary>
        internal async Task SetSkinOptions() {
            SkinAccess skinAccess = new SkinAccess();
            SkinCollectionInfo info = skinAccess.GetSkinCollectionInfo();
            SkinInfo = info;

            if (SkinInfo.UsingBootstrap) {
                ScriptManager.AddVolatileOption("Skin", "Bootstrap", true);
                ScriptManager.AddVolatileOption("Skin", "BootstrapButtons", SkinInfo.UsingBootstrapButtons);
                if (SkinInfo.UseDefaultBootstrap) {
                    // Find the bootstrap theme
                    string skin = Manager.CurrentPage.BootstrapSkin;
                    if (string.IsNullOrWhiteSpace(skin))
                        skin = Manager.CurrentSite.BootstrapSkin;
                    string themeFolder = await skinAccess.FindBootstrapSkinAsync(skin);
                    if (string.IsNullOrWhiteSpace(themeFolder))
                        await Manager.AddOnManager.AddAddOnNamedAsync(AreaRegistration.CurrentPackage.AreaName, "getbootstrap.com.bootstrap-less");
                    else
                        await Manager.AddOnManager.AddAddOnNamedAsync(AreaRegistration.CurrentPackage.AreaName, "getbootstrap.com.bootswatch", themeFolder);
                }
            }
            ScriptManager.AddVolatileOption("Skin", "MinWidthForPopups", SkinInfo.MinWidthForPopups);

            if (!string.IsNullOrWhiteSpace(SkinInfo.JQuerySkin) && string.IsNullOrWhiteSpace(CurrentPage.jQueryUISkin))
                CurrentPage.jQueryUISkin = SkinInfo.JQuerySkin;
            if (!string.IsNullOrWhiteSpace(SkinInfo.KendoSkin) && string.IsNullOrWhiteSpace(CurrentPage.KendoUISkin))
                CurrentPage.KendoUISkin = SkinInfo.KendoSkin;
        }

        /// <summary>
        /// Define options for the current page or popup skin (UPS).
        /// </summary>
        internal void SetSkinOptionsContent() {
            SkinAccess skinAccess = new SkinAccess();
            SkinCollectionInfo info = skinAccess.GetSkinCollectionInfo();
            SkinInfo = info;

            if (!string.IsNullOrWhiteSpace(SkinInfo.JQuerySkin) && string.IsNullOrWhiteSpace(CurrentPage.jQueryUISkin))
                CurrentPage.jQueryUISkin = SkinInfo.JQuerySkin;
            if (!string.IsNullOrWhiteSpace(SkinInfo.KendoSkin) && string.IsNullOrWhiteSpace(CurrentPage.KendoUISkin))
                CurrentPage.KendoUISkin = SkinInfo.KendoSkin;
        }

        public SkinCollectionInfo SkinInfo { get; set; }

        /// <summary>
        /// Adds the page's or popup's css classes (the current edit mode, the current page's defined css and other page css).
        /// </summary>
        /// <param name="css">The skin-defined Css class identifying the skin.</param>
        /// <returns>A Css class string.</returns>
        public HtmlString PageCss() {
            SkinAccess skinAccess = new SkinAccess();
            PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
            string s = pageSkin.Css;
            s = CombineCss(s, ModeCss);// edit/display mode (doesn't change in same Unified page set)
            s = CombineCss(s, HaveUser ? "yUser" : "yAnonymous");// add whether we have an authenticated user (doesn't change in same Unified page set)
            s = CombineCss(s, IsInPopup ? "yPopup" : "yPage"); // popup or full page (doesn't change in same Unified page set)
            s = CombineCss(s, GetAspNetCss(AspNetMvc)); // asp/net version used (doesn't change in same Unified page set)
            switch (UnifiedMode) { // unified page set mode (if any) (doesn't change in same Unified page set)
                case PageDefinition.UnifiedModeEnum.None:
                    break;
                case PageDefinition.UnifiedModeEnum.HideDivs:
                    s = CombineCss(s, "yUnifiedHideDivs");
                    break;
                case PageDefinition.UnifiedModeEnum.ShowDivs:
                    s = CombineCss(s, "yUnifiedShowDivs");
                    break;
                case PageDefinition.UnifiedModeEnum.DynamicContent:
                    s = CombineCss(s, "yUnifiedDynamicContent");
                    break;
                case PageDefinition.UnifiedModeEnum.SkinDynamicContent:
                    s = CombineCss(s, "yUnifiedSkinDynamicContent");
                    break;
            }
            if (Manager.SkinInfo.UsingBootstrap) {
                string skin = Manager.CurrentPage.BootstrapSkin;
                if (string.IsNullOrWhiteSpace(skin))
                    skin = Manager.CurrentSite.BootstrapSkin;
                if (!string.IsNullOrWhiteSpace(skin)) {
                    skin = skin.ToLower().Replace(' ', '-');
                    s = CombineCss(s, $"ySkin-bs-{skin}");
                }
            }
            string cssClasses = CurrentPage.GetCssClass(); // get page specific Css (once only, used 2x)
            if (UnifiedMode == PageDefinition.UnifiedModeEnum.DynamicContent || UnifiedMode == PageDefinition.UnifiedModeEnum.SkinDynamicContent) {
                // add the extra page css class and generated page specific Css via javascript to body tag (used for dynamic content)
                ScriptBuilder sb = new Support.ScriptBuilder();
                sb.Append("document.body.setAttribute('data-pagecss', '{0}');", YetaWFManager.JserEncode(cssClasses));
                Manager.ScriptManager.AddLast(sb.ToString());
            }
            return new HtmlString(CombineCss(s, cssClasses));
        }

        // CURRENT USER
        // CURRENT USER
        // CURRENT USER

        // user info is obtained in global.asax.cs by Authentication provider ResolveUser
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public int UserId { get; set; }
        public List<int> UserRoles { get; set; }
        public object UserObject { get; set; }// data saved by Authentication provider
        public object UserSettingsObject { get; set; } // data saved by usersettings module/data provider IUserSettings
        public string UserLanguage { get; private set; }
        public string GetUserLanguage() {
            string userLang = UserSettings.GetProperty<string>("LanguageId");
            UserLanguage = MultiString.NormalizeLanguageId(userLang);
            return UserLanguage;
        }
        public async Task SetUserLanguageAsync(string language) {
            language = MultiString.NormalizeLanguageId(language);
            await UserSettings.SetPropertyAsync<string>("LanguageId", language);
            UserLanguage = language;
        }
        public bool HaveUser { get { return UserId != 0; } }
        public void NeedUser() { if (!HaveUser) throw new Error(this.__ResStr("noUser", "You must be logged in to perform this action")); }

        public bool HasSuperUserRole {
            get {
                if (!HaveCurrentSession) return false;
                string superuser = CurrentSession.GetString(Globals.Session_Superuser);
                return !string.IsNullOrWhiteSpace(superuser);
            }
        }
        public void SetSuperUserRole(bool isSuperUser) {
            bool hasRole = HasSuperUserRole;
            if (hasRole != isSuperUser) {
                if (!HaveCurrentSession) return;
                CurrentSession.Remove(Globals.Session_Superuser);
                if (isSuperUser)
                    CurrentSession.SetString(Globals.Session_Superuser, "I am/was a superuser");// this is set once we see a superuser. Even if logged off, the session value remains
                MenuList.ClearCachedMenus();
            }
        }
        /// <summary>
        /// Currently logged on user is authenticated but needs to set up two-step authentication.
        /// </summary>
        public bool Need2FA {
            get {
                bool? need2FAState = Need2FAState;
                if (need2FAState == null) return false;
                return (bool)need2FAState;
            }
        }
        public bool? Need2FAState {
            get {
                if (!HaveCurrentSession) return null;
                string need2FA = CurrentSession.GetString(Globals.Session_Need2FA);
                if (string.IsNullOrWhiteSpace(need2FA)) return null;
                return need2FA == "Yes";
            }
            set {
                bool? hasNeeds2FA = Need2FAState;
                if (hasNeeds2FA != value) {
                    if (!HaveCurrentSession) return;
                    if (value != null) {
                        CurrentSession.SetString(Globals.Session_Need2FA, (bool)value ? "Yes" : "No");
                        Need2FARedirect = (bool)value;
                    } else {
                        CurrentSession.Remove(Globals.Session_Need2FA);
                        Need2FARedirect = false;
                    }
                }
            }
        }
        public bool Need2FARedirect {
            get {
                return !string.IsNullOrWhiteSpace(CurrentSession.GetString(Globals.Session_Need2FARedirect));
            }
            set {
                if (value) {
                    CurrentSession.SetString(Globals.Session_Need2FARedirect, "Yes");
                } else {
                    CurrentSession.Remove(Globals.Session_Need2FARedirect);
                }
            }
        }

        /// <summary>
        /// Currently logged on user is authenticated but needs to define a new password.
        /// </summary>
        public bool NeedNewPassword { get; set; }

        // repetitive authorization
        // add authorized page urls to this list (new for each http request) so we can avoid repetitive authorizations, particularly in grids
        public List<string> UserAuthorizedUrls { get; set; }
        public List<string> UserNotAuthorizedUrls { get; set; }

        // CURRENT DEVICE
        // CURRENT DEVICE
        // CURRENT DEVICE

        /// <summary>
        /// The selected rendering mode for this site
        /// </summary>
        public enum DeviceSelected {
            Undecided = 0,
            Desktop = 1,
            Mobile = 2,
        }
        public DeviceSelected ActiveDevice {
            get {
                if (CurrentSession == null) return _ActiveDevice;
                return (DeviceSelected)(CurrentSession.GetInt(Globals.Session_ActiveDevice, (int)DeviceSelected.Undecided));
            }
            set {
                if (value != DeviceSelected.Desktop && value != DeviceSelected.Mobile) throw new InternalError("Invalid device selection {0}", value);
                _ActiveDevice = value;
                if (CurrentSession == null) return;
                CurrentSession.SetInt(Globals.Session_ActiveDevice, (int)value);
            }
        }
        private DeviceSelected _ActiveDevice = DeviceSelected.Undecided;

        // LOCALIZATION
        // LOCALIZATION
        // LOCALIZATION

        public TimeZoneInfo GetTimeZoneInfo() {
            if (timeZoneInfo == null) {
                string tz = UserSettings.GetProperty<string>("TimeZone");
                if (!string.IsNullOrWhiteSpace(tz))
                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                if (timeZoneInfo == null)
                    timeZoneInfo = TimeZoneInfo.Local;
            }
            return timeZoneInfo;
        }
        private TimeZoneInfo timeZoneInfo = null;

        public CultureInfo GetCultureInfo() {
            if (cultureInfo == null) {
                string lang = UserSettings.GetProperty<string>("LanguageId");
                cultureInfo = new CultureInfo(lang);
            }
            return cultureInfo;
        }
        private CultureInfo cultureInfo = null;

        // Localization resource loading is not enabled immediately when the http request starts.
        // It is explicitly enabled in global.asax.cs once important information is available so resource loading can actually work.
        public bool LocalizationSupportEnabled { get; set; }

        // CDN
        // CDN
        // CDN

        public string GetCDNUrl(string url, bool WithCacheBuster = true) {
            if (!url.IsAbsoluteUrl()) {
                if (WithCacheBuster)
                    url += url.AddUrlCacheBuster(CacheBuster);
                bool useCDN = Manager.CurrentSite.CanUseCDN;
                bool useAlt = Manager.CurrentSite.CanUseStaticDomain;
                if (useCDN || useAlt) {
                    if (url.StartsWith(Globals.NodeModulesUrl) ||
                            url.StartsWith(Globals.BowerComponentsUrl) ||
                            url.StartsWith(Globals.SiteFilesUrl) ||
                            url.StartsWith(Globals.VaultUrl) ||
                            url.StartsWith(Globals.VaultPrivateUrl) ||
                            url.StartsWith(Globals.AddOnsUrl) ||
                            url.StartsWith(Globals.AddOnsCustomUrl) ||
                            url.StartsWith(Globals.AddonsBundlesUrl) ||
                            url.StartsWith("/FileHndlr.image")) {
                        // leave useAlt as is
                    } else
                        useAlt = false;
                }
                if (useCDN || useAlt) {
                    PageDefinition.PageSecurityType pageSecurity;
                    if (Manager.CurrentPage != null)
                        pageSecurity = Manager.CurrentSite.DetermineSchema(Manager.CurrentPage.PageSecurity);
                    else
                        pageSecurity = Manager.CurrentSite.DetermineSchema();
                    switch (pageSecurity) {
                        default:
                        case PageDefinition.PageSecurityType.Any:
                            if (useCDN) {
                                if (CurrentSite.HaveCDNUrlSecure)
                                    url = CurrentSite.CDNUrlSecure + url;
                                else
                                    url = CurrentSite.CDNUrl + url;
                            } else if (useAlt) {
                                if (Manager.CurrentSite.PortNumberSSLEval == 443)
                                    url = $"https://{CurrentSite.StaticDomain}{url}";
                                else
                                    url = $"https://{CurrentSite.StaticDomain}:{Manager.CurrentSite.PortNumberSSLEval}{url}";
                            }
                            url = url.TruncateStart("http:");
                            url = url.TruncateStart("https:");
                            break;
                        case PageDefinition.PageSecurityType.httpOnly:
                            if (useCDN)
                                url = CurrentSite.CDNUrl + url;
                            else if (useAlt) {
                                if (Manager.CurrentSite.PortNumberEval == 80)
                                    url = $"http://{CurrentSite.StaticDomain}{url}";
                                else
                                    url = $"http://{CurrentSite.StaticDomain}:{Manager.CurrentSite.PortNumberEval}{url}";
                            }
                            url = url.TruncateStart("http:");
                            break;
                        case PageDefinition.PageSecurityType.httpsOnly:
                            if (useCDN && CurrentSite.HaveCDNUrlSecure)
                                url = CurrentSite.CDNUrlSecure + url;
                            else if (useAlt) {
                                if (Manager.CurrentSite.PortNumberSSLEval == 443)
                                    url = $"https://{CurrentSite.StaticDomain}{url}";
                                else
                                    url = $"https://{CurrentSite.StaticDomain}:{Manager.CurrentSite.PortNumberSSLEval}{url}";
                            }
                            url = url.TruncateStart("https:");
                            break;
                    }
                }
            }
            return url;
        }

        // SITE TEMPLATES
        // SITE TEMPLATES
        // SITE TEMPLATES

        public bool SiteCreationTemplateActive { get; set; }

        // ASYNC
        // ASYNC
        // ASYNC

        public static bool IsSync() {
            if (YetaWFManager.HaveManager) return YetaWFManager.Manager._syncCount > 0;
            return true;// if there is no manager, we can't async (and it's probably no advantage to be async in this case)
        }
        private int _syncCount = 0;

        /// <summary>
        /// Used to mark all methods within its scope as synchronous.
        /// Only synchronous data providers are used.
        /// Async code will run synchronously on all platforms.
        /// </summary>
        private class NeedSync : IDisposable {
            private YetaWFManager Manager;
            public NeedSync() {
                if (YetaWFManager.HaveManager) { // if no manager is available, code is synchronous by definition
                    Manager = YetaWFManager.Manager;
                    Manager._syncCount++;
                }
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                if (Manager != null && --Manager._syncCount < 0) Manager._syncCount = 0;
                if (disposing) DisposableTracker.RemoveObject(this);
            }
            //~NeedSync() { Dispose(false); }
        }

        /// <summary>
        /// Runs synchronously and returns the return value of the async body within its scope.
        /// </summary>
        public static TYPE Syncify<TYPE>(Func<Task<TYPE>> func) {
            using (new NeedSync()) {
                return func().Result; // sync OK as we requested sync mode
            }
        }
        /// <summary>
        /// Waits for the async body within its scope and runs synchronously.
        /// </summary>
        /// <param name="func"></param>
        public static void Syncify(Func<Task> func) {
            using (new NeedSync()) {
                func().Wait(); // Sync wait because we're in sync mode
            }
        }
    }
}
