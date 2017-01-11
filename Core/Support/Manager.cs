/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using YetaWF.Core.Addons;
using YetaWF.Core.Extensions;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Support.Repository;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Support {
    public class YetaWFManager {

        private static readonly string YetaWF_ManagerKey = typeof(YetaWFManager).Module + " sft";
        public const string BATCHMODE = "Batch";

        private YetaWFManager(string host) {
            SiteDomain = host; // save the host name that owns this Manager
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification="This is a catastrophic error so we must abort")]
        public static YetaWFManager Manager
        {
            get {
                YetaWFManager manager = null;
                if (HttpContext.Current != null) {
                    manager = HttpContext.Current.Items[YetaWF_ManagerKey] as YetaWFManager;
                } else {
                    // not a webrequest - most likely a scheduled task
                    // check if we have thread data
                    LocalDataStoreSlot slot = Thread.GetNamedDataSlot(YetaWF_ManagerKey);
                    manager = (YetaWFManager) Thread.GetData(slot);
                }
                if (manager == null)
                    throw new Error("We don't have a YetaWFManager object.");
                return manager;
            }
        }

        public static bool HaveManager {
            get {
                if (HttpContext.Current != null) {
                    if (HttpContext.Current.Items[YetaWF_ManagerKey] == null) return false;
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
        public static YetaWFManager MakeInstance(string siteHost) {
            if (siteHost == null)
                throw new Error("Site host required to create a YetaWFManager object.");
            if (HttpContext.Current.Items[YetaWF_ManagerKey] != null)
                throw new Error("We already have a YetaWFManager object.");
            YetaWFManager manager = new YetaWFManager(siteHost);
            HttpContext.Current.Items[YetaWF_ManagerKey] = manager;
            return manager;
        }

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
                if (SiteDefinition.LoadSiteDefinition != null) {
                    manager.HostUsed = SiteDefinition.GetDefaultSiteDomain();
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
                DateFormat = Formatting.DateFormatEnum.MMDDYYYY,
                TimeFormat = Formatting.TimeFormatEnum.HHMMAM,
                LanguageId = MultiString.DefaultLanguage,
                TimeZone = TimeZoneInfo.Local.Id,
            };
            if (site != null)
                manager.GetUserLanguage();// get user's default language

            return manager;
        }
        public class SchedulerUserData {
            public Formatting.DateFormatEnum DateFormat { get; set; }
            public string TimeZone { get; set; }
            public Formatting.TimeFormatEnum TimeFormat { get; set; }
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

        public static void SetRequestedDomain(string siteDomain) {
            HttpContext.Current.Session[Globals.Link_ForceSite] = siteDomain;
        }
        public static string GetRequestedDomain(Uri uri, NameValueCollection queryString, out bool overridden, out bool newSwitch) {
            string siteDomain = null;
            overridden = newSwitch = false;

            siteDomain = queryString[Globals.Link_ForceSite];
            if (!string.IsNullOrWhiteSpace(siteDomain)) {
                overridden = newSwitch = true;
                YetaWFManager.SetRequestedDomain(siteDomain);
            }
            if (!overridden && HttpContext.Current.Session != null) {
                siteDomain = (string) HttpContext.Current.Session[Globals.Link_ForceSite];
                if (!string.IsNullOrWhiteSpace(siteDomain))
                    overridden = true;
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

        // The date the site was started
        public static DateTime SiteStart = DateTime.UtcNow;

        public static string CacheBuster {
            get {
                if (_cacheBuster == null) {
                    if (Manager.CurrentSite.DEBUGMODE || !Manager.CurrentSite.AllowCacheUse)
                        _cacheBuster = (DateTime.Now.Ticks / TimeSpan.TicksPerSecond).ToString();/*local time*/
                    else
                        _cacheBuster = (YetaWFManager.SiteStart.Ticks / TimeSpan.TicksPerSecond).ToString();
                }
                return _cacheBuster;
            }
        }
        private static string _cacheBuster;

        /// <summary>
        /// Root folder (physical)
        /// </summary>
        public static string RootFolder { get; set; }

        /// <summary>
        /// Returns the folder containing all sites' file data.
        /// </summary>
        public static string RootSitesFolder {
            get {
                return Path.Combine(RootFolder, Globals.SitesFolder, "DataFolder");
            }
        }
        /// <summary>
        /// Returns the folder containing all sites' file data - No longer used.
        /// </summary>
        public static string RootSitesFolderOld {
            get {
                return Path.Combine(RootFolder, Globals.SitesFolder, DefaultSiteName);
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification= "This is a catastrophic error")]
        public static string DefaultSiteName {
            get {
                if (defaultSiteName == null)
                    defaultSiteName = WebConfigHelper.GetValue<string>("YetaWF_Core"/*==YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName*/, "DEFAULTSITE");
                if (defaultSiteName == null)
                    throw new InternalError("Default site must be defined in web.config");
                return defaultSiteName;
            }
        }
        private static string defaultSiteName;

        /// <summary>
        /// Data folder (not site specific).
        /// </summary>
        public static string DataFolder {
            get {
                return Path.Combine(RootFolder, Globals.DataFolder, "DataFolder");
            }
        }
        /// <summary>
        /// Data folder (default site specific) - No longer used.
        /// </summary>
        public static string DataFolderOld {
            get {
                return Path.Combine(RootFolder, Globals.DataFolder, DefaultSiteName);
            }
        }

        public static string UrlToPhysical(string url) {
            return HostingEnvironment.MapPath(url);
        }

        public static string PhysicalToUrl(string path) {
            return ReplaceString(path, RootFolder, String.Empty, StringComparison.OrdinalIgnoreCase).Replace('\\', '/');
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

        public static JavaScriptSerializer Jser {
            get {
                if (_Jser == null)
                    _Jser = new JavaScriptSerializer();
                return _Jser;
            }
        }
        private static JavaScriptSerializer _Jser;

        public static string JserEncode(string s) {
            return HttpUtility.JavaScriptStringEncode(s);
        }

        public static string GetQueryStringFromAnonymousObject(object args) {
            if (args == null) return null;
            string qs = "";
            RouteValueDictionary rvd = AnonymousObjectToRVD(args);
            foreach (var entry in rvd)
                qs += string.Format("&{0}={1}", entry.Key, entry.Value != null ? YetaWFManager.UrlEncodeArgs(entry.Value.ToString()) : "");
            if (string.IsNullOrWhiteSpace(qs)) return null;
            return qs.Substring(1);
        }
        public static RouteValueDictionary AnonymousObjectToRVD(object obj) {
            RouteValueDictionary result = new RouteValueDictionary();
            if (obj != null) {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj))
                    result.Add(property.Name, property.GetValue(obj));
            }
            return result;
        }
        public static string UrlFor(Type type, string actionName, object args = null) {
            if (!type.Name.EndsWith("Controller")) throw new InternalError("Type {0} is not a controller", type.FullName);
            string controller = type.Name.Substring(0, type.Name.Length - "Controller".Length);
            Package package = Package.TryGetPackageFromAssembly(type.Assembly);
            if (package == null)
                throw new InternalError("Type {0} is not part of a package", type.FullName);
            string area = package.AreaName;
            string url = "/" + area + "/" + controller + "/" + actionName;
            if (args != null) {
                string qs = GetQueryStringFromAnonymousObject(args);
                if (qs != null)
                    url = url + "?" + qs.ToString();
            }
            return url;
        }

        public static string HtmlEncode(string s) {
            if (s == null) return "";
            return HttpUtility.HtmlEncode(s);
        }
        public static string HtmlDecode(string s) {
            if (s == null) return "";
            return HttpUtility.HtmlDecode(s);
        }
        public static string HtmlAttributeEncode(string s) {
            if (s == null) return "";
            return HttpUtility.HtmlAttributeEncode(s);
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
            for (int len = s.Length, ix = 0 ; ix < len ; ++ix) {
                char c = s[ix];
                if (c == '?') {
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
            for (int len = s.Length, ix = 0 ; ix < len ; ++ix) {
                char c = s[ix];
                if (c == '?') {
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
                sb.Append(s.Substring(0, iScheme+1));
                s = s.Substring(iScheme+1);
            }
            if (s.StartsWith("//")) {
                sb.Append("//");
                s = s.Substring(2);
            }
            int iAuth = s.IndexOf('@');
            if (iAuth >= 0) {
                sb.Append(s.Substring(0, iAuth+1));
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
#if RELEASE
                deployed = true;
#else
                deployed = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "Deployed");
#endif
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

        /// <summary>
        /// Defines whether the current YetaWF instance runs in demo mode.
        /// </summary>
        /// <remarks>Demo mode allows anonymous users to use all features in Superuser mode, without being able to change any data.
        ///
        /// Demo mode is enabled/disabled using the Web.config setting P:YetaWF_Core:Demo.
        /// </remarks>
        public bool IsDemo {
            get {
                if (isDemo == null) {
                    isDemo = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "Demo");
                }
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
        public bool IsLocalHost { get { return string.Compare(HostUsed, "localhost", true) == 0; } }

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
        /// Returns the last entry of the OriginList
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
        /// Returns the Url to return to, including origin list and other querystring parms
        /// </summary>
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
                    url += url.Contains("?") ? "&" : "?";
                    url += Globals.Link_OriginList + "=" + YetaWFManager.UrlEncodeArgs(Jser.Serialize(originList));
                }
                return url;
            }
        }

        /// <summary>
        /// Normalize a url for the current site
        /// </summary>
        /// <param name="url"></param>
        public string NormalizeUrl(string url) {
            // add page control module visible
            NameValueCollection qs;
            int index = url.IndexOf('?');
            if (index >= 0 && index <= url.Length-1) {
                qs = System.Web.HttpUtility.ParseQueryString(url.Substring(index+1));
                url = url.Substring(0, index);
            } else
                qs = System.Web.HttpUtility.ParseQueryString(string.Empty);

            qs.Remove(Globals.Link_ShowPageControlKey);
            if (Manager.PageControlShown)
                qs[Globals.Link_ShowPageControlKey] = Globals.Link_ShowPageControlValue;

            string qsString = qs.ToString();
            if (qsString.Length > 0)
                url += "?" + qsString;

            return url;
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
                val = (TYPE)(object)Convert.ToInt32(v);
                return true;
            } else if (typeof(TYPE) == typeof(bool) || typeof(TYPE) == typeof(bool?)) {
                val = (TYPE)(object)((v == "1" || v.ToLower() == "on" || v.ToLower() == "true" || v.ToLower() == "yes") ? true : false);
                return true;
            } else if (typeof(TYPE) == typeof(string)) {
                val = (TYPE) (object) v;
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

        public void Verify_NotAjaxRequest() {
            if (IsAjaxRequest)
                throw new InternalError("This is not supported for Ajax requests");
        }
        public void Verify_AjaxRequest() {
            if (!IsAjaxRequest)
                throw new InternalError("This is only supported for Ajax requests");
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

        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT

        // Return a unique id - ids are only unique for 1 request. For Ajax/Post requests we have to
        // insure that we get Unique ids that don't duplicate ids obtained during the original request
        public string UniqueId(string name = "a") {
            ++_uniqueIdCounter;
            if (string.IsNullOrEmpty(UniqueIdPrefix)) {
                if (string.IsNullOrWhiteSpace(name))
                    throw new InternalError("UniqueId must spcify a name prefix");
                return name + _uniqueIdCounter;
            } else
                return UniqueIdPrefix + "_" + name + _uniqueIdCounter;
        }
        private int _uniqueIdCounter = 0;

        public string UniqueIdPrefix { get; set; }

        public void NextUniqueIdPrefix() { UniqueIdPrefix = string.Format("u{0}", ++_uniqueIdPrefixCounter); }
        private int _uniqueIdPrefixCounter = 0;

        // HTTPCONTEXT
        // HTTPCONTEXT
        // HTTPCONTEXT

        public string UserHostAddress {
            get {
                return CurrentRequest.UserHostAddress;
            }
        }
        public NameValueCollection RequestQueryString {
            get { return CurrentRequest.QueryString; }
        }
        public NameValueCollection RequestForm {
            get { return CurrentRequest.Form; }
        }
        public NameValueCollection RequestParams {
            get { return CurrentRequest.Params; }
        }
        public bool HaveCurrentContext { get { return HttpContext.Current != null; } }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpContext CurrentContext {
            get {
                HttpContext context = HttpContext.Current;
                if (context == null) throw new InternalError("No HttpContext.Current available");
                return context;
            }
        }
        public bool HaveCurrentRequest { get { return HaveCurrentContext && HttpContext.Current.Request != null; } }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpRequest CurrentRequest {
            get {
                HttpRequest request = HttpContext.Current.Request;
                if (request == null) throw new InternalError("No HttpContext.Current.Request available");
                return request;
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpResponse CurrentResponse {
            get {
                HttpResponse response = HttpContext.Current.Response;
                if (response == null) throw new InternalError("No HttpContext.Current.Response available");
                return response;
            }
        }
        public bool HaveCurrentSession { get { return HaveCurrentContext && HttpContext.Current.Session != null; } }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpSessionState CurrentSession {
            get {
                HttpSessionState session = HttpContext.Current.Session;
                if (session == null) throw new InternalError("No HttpContext.Current.Session available");
                return session;
            }
        }
        public void RestartSite() {
            HttpRuntime.UnloadAppDomain();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsAjaxRequest {
            get {
                HttpRequest request = CurrentRequest;
                return (request["X-Requested-With"] == "XMLHttpRequest") || ((request.Headers != null) && (request.Headers["X-Requested-With"] == "XMLHttpRequest"));
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsPostRequest {
            get {
                HttpRequest request = CurrentRequest;
                return (request.RequestType == "POST");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsGetRequest {
            get {
                HttpRequest request = CurrentRequest;
                return (request.RequestType == "GET");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsHeadRequest {
            get {
                HttpRequest request = CurrentRequest;
                return (request.RequestType == "HEAD");
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

        public bool EditMode {
            get {
                if (_forcedEditMode != null) return (bool)_forcedEditMode;
                if (_editMode == null)
                    _editMode = SessionSettings.SiteSettings.GetValue<bool>("EditMode");
                return (bool) _editMode;
            }
            set {
                if (_editMode != value) {
                    _editMode = value;
                    _forcedEditMode = null;
                    SessionSettings.SiteSettings.SetValue<bool>("EditMode", (bool)_editMode);
                    SessionSettings.SiteSettings.Save();
                }
            }
        }
        public bool ForcedEditMode {// forced mode (display, edit or not specified) just for the page about to be displayed
            get {
                if (_forcedEditMode == null)
                    _forcedEditMode = EditMode;
                return (bool)_forcedEditMode;
            }
            set {
                if (_forcedEditMode != value)
                    _forcedEditMode = value;
            }
        }
        public bool IsForcedDisplayMode { get { return _forcedEditMode == false; } }
        public bool IsForcedEditMode { get { return _forcedEditMode == true; } }
        private bool? _editMode = null;
        private bool? _forcedEditMode = null;

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
        /// The current page title. Modules can override the page title (we don't use the title in the page definition, except to set the default title).
        /// </summary>
        public MultiString PageTitle { get; set; }

        public string PageTitleHtml {
            get {
                string title = PageTitle.ToString();
                return string.Format("<title>{0}</title>", HtmlEncode(title ?? ""));
            }
        }

        // MODULES
        // MODULES
        // MODULES

        public ModuleDefinition CurrentModule { get; set; } // current module rendered

        // RENDERING
        // RENDERING
        // RENDERING

        /// <summary>
        /// The current pane being rendered.
        /// </summary>
        public string PaneRendered { get; set; }

        public bool IsRenderingPane { get { return PaneRendered != null; } }
        /// <summary>
        /// Defines whether grid data is being rendered.
        /// </summary>
        /// <remarks>Some templates adjust when they are used in a grid.
        ///
        /// This is mainly used to prevent use of complex templates in grids, where javascript cannot be
        /// executed for templates. For example, the dropdownlist template needs javascript for initialization,
        /// so we fall back to the default browser dropdownlist in grids.</remarks>
        public bool IsRenderingGrid { get { return RenderingGridCount > 0; } }
        /// <summary>
        /// Count of grids being rendered (grids within grids).
        /// </summary>
        public int RenderingGridCount { get; set; }

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
            CharWidthAvg = width;
            CharHeight = height;
            PushCharSize();
        }

        public bool RenderingUniqueModuleAddons { get; set; }
        public bool RenderingUniqueModuleAddonsAjax { get; set; }

        // FORM PROCESSING
        // FORM PROCESSING
        // FORM PROCESSING

        /// <summary>
        /// True while processing a partial view (usually a partial form/ajax)
        /// </summary>
        public bool InPartialView { get; set; }

        // MODEL STACK
        // MODEL STACK
        // MODEL STACK

        // preserve models so templates can access the enclosing object
        public void PopModel() {
            if (ModelStack.Count > 0)
                ModelStack.RemoveAt(ModelStack.Count - 1);
        }
        public void PushModel(object model) {
            ModelStack.Add(model);
        }
        private List<object> ModelStack {
            get {
                if (_modelStack == null)
                    _modelStack = new List<object>();
                return _modelStack;
            }
        }
        private List<object> _modelStack = null;

        public object TryGetParentModel(int Skip = 0) {
            if (ModelStack.Count <= 1 + Skip) return null;
            return ModelStack[ModelStack.Count - 2 - Skip];
        }
        public object GetParentModel(int Skip = 0) {
            object o = TryGetParentModel(Skip);
            if (o == null) throw new InternalError("No parent model");
            return o;
        }
        public object GetCurrentModel() {
            if (ModelStack.Count <= 0) throw new InternalError("No model");
            return ModelStack[ModelStack.Count - 1];
        }

        /// <summary>
        /// Defines whether non-site specific data is also imported when importing packages
        /// </summary>
        /// <remarks>Site specific data is always imported</remarks>
        public bool ImportChunksNonSiteSpecifics { get; set; }

        // CONTROLINFOOVERRIDE
        // CONTROLINFOOVERRIDE
        // CONTROLINFOOVERRIDE

        /// <summary>
        /// AdditionalValues meta data overrides for current control being processed
        /// </summary>
        public Dictionary<string, object> ControlInfoOverrides { get; set; }


        // UTILITY
        // UTILITY
        // UTILITY

        /// <summary>
        /// Define options for the current page skin.
        /// </summary>
        /// <param name="JQuerySkin">The default jQuery-UI skin. Specify null for the default jQuery-UI skin.</param>
        /// <param name="KendoSkin">The default Kendo skin. Specify null for the default Kendo skin.</param>
        /// <param name="UsingBootstrap">Set to true if the current skin is a Bootstrap skin, otherwise false.</param>
        /// <param name="UseDefaultBootstrap">Set to true to use the global Addon Bootstrap (without any customizations), false if the optionally customized bootstrap version is provided as part of the skin package.
        /// This parameter is ignored if UsingBootstrap is false.</param>
        /// <param name="UsingBootstrapButtons">Set to true if the current skin uses Bootstrap buttons, otherwise false for jQuery-UI buttons.
        /// This parameter is ignored if UsingBootstrap is false.</param>
        /// <param name="MinWidthForPopups">The minimum page width for which popups are allowed.
        /// If the page is not wide enough, the page is opened as a full page instead of in a popup.
        /// This parameter is ignored if UsingBootstrap is false.</param>
        public void SetSkinOptions(string JQuerySkin = null, string KendoSkin = null,
                bool UsingBootstrap = false, bool UseDefaultBootstrap = true, bool UsingBootstrapButtons = false, int MinWidthForPopups = 0) {
            if (IsInPopup) throw new InternalError("This form of the SetSkinOptions method can only be used in page skins (not popup skins)");
            this.UsingBootstrap = UsingBootstrap;
            if (UsingBootstrap) {
                ScriptManager.AddVolatileOption("Skin", "Bootstrap", true);
                ScriptManager.AddVolatileOption("Skin", "BootstrapButtons", UsingBootstrapButtons);
                ScriptManager.AddVolatileOption("Skin", "MinWidthForPopups", MinWidthForPopups);
                if (UseDefaultBootstrap)
                    AddOnManager.AddAddOnGlobal("getbootstrap.com", "bootstrap-less");
                this.UsingBootstrapButtons = UsingBootstrapButtons;
            } else
                ScriptManager.AddVolatileOption("Skin", "MinWidthForPopups", 0);
            SetSkins(JQuerySkin, KendoSkin);
        }
        /// <summary>
        /// Define options for the current popup skin.
        /// </summary>
        /// <param name="popupWidth">The width of the popup window (in pixels).</param>
        /// <param name="popupHeight">The height of the popup window (in pixels).</param>
        /// <param name="popupMaximize">Set to true if the popup can be maximized, false otherwise.</param>
        /// <param name="JQuerySkin">The default jQuery-UI skin. Specify null for the default jQuery-UI skin.</param>
        /// <param name="KendoSkin">The default Kendo skin. Specify null for the default Kendo skin.</param>
        /// <param name="UsingBootstrap">Set to true if the current skin is a Bootstrap skin, otherwise false.</param>
        /// <param name="UseDefaultBootstrap">Set to true to use the global Addon Bootstrap (without any customizations), false if the optionally customized bootstrap version is provided as part of the skin package.
        /// This parameter is ignored if UsingBootstrap is false.</param>
        /// <param name="UsingBootstrapButtons">Set to true if the current skin uses Bootstrap buttons, otherwise false for jQuery-UI buttons.
        /// This parameter is ignored if UsingBootstrap is false.</param>
        public void SetSkinOptions(int popupWidth, int popupHeight, bool popupMaximize = false, string JQuerySkin = null, string KendoSkin = null,
                bool UsingBootstrap = false, bool UseDefaultBootstrap = true, bool UsingBootstrapButtons = false) {
            if (!IsInPopup) throw new InternalError("This form of the SetSkinOptions method can only be used in popup skins (not page skins)");
            this.UsingBootstrap = UsingBootstrap;
            if (UsingBootstrap) {
                ScriptManager.AddVolatileOption("Skin", "Bootstrap", true);
                ScriptManager.AddVolatileOption("Skin", "BootstrapButtons", UsingBootstrapButtons);
                if (UseDefaultBootstrap)
                    AddOnManager.AddAddOnGlobal("getbootstrap.com", "bootstrap-less");
                this.UsingBootstrapButtons = UsingBootstrapButtons;
            }
            ScriptManager.AddVolatileOption("Skin", "MinWidthForPopups", 0);
            ScriptManager.AddVolatileOption("Skin", "PopupWidth", popupWidth);// Skin size in a popup window
            ScriptManager.AddVolatileOption("Skin", "PopupHeight", popupHeight);
            ScriptManager.AddVolatileOption("Skin", "PopupMaximize", popupMaximize);
            SetSkins(JQuerySkin, KendoSkin);
        }

        private void SetSkins(string JQuerySkin, string KendoSkin) {
            if (!string.IsNullOrWhiteSpace(JQuerySkin) && string.IsNullOrWhiteSpace(CurrentPage.jQueryUISkin))
                CurrentPage.jQueryUISkin = JQuerySkin;
            if (!string.IsNullOrWhiteSpace(KendoSkin) && string.IsNullOrWhiteSpace(CurrentPage.KendoUISkin))
                CurrentPage.KendoUISkin = KendoSkin;
            AddOnManager.AddSkinBasedAddOns();
        }

        public bool UsingBootstrap { get; set; }
        public bool UsingBootstrapButtons { get; set; }

        /// <summary>
        /// Adds the specified css, the current edit mode, the current page's defined css and returns all classes as a string
        /// </summary>
        public MvcHtmlString PageCss(string css) {
            string s = CombineCss(css, ModeCss);
            s = CombineCss(s, HaveUser ? "yUser" : "yAnonymous");
            s = CombineCss(s, IsInPopup ? "yPopup" : "yPage");
            // add a class whether page can be seen by anonymous users and users
            bool showOwnership = UserSettings.GetProperty<bool>("ShowPageOwnership") && Resource.ResourceAccess.IsResourceAuthorized(CoreInfo.Resource_ViewOwnership);
            if (showOwnership) {
                PageDefinition page = Manager.CurrentPage;
                bool anon = page.IsAuthorized_View_Anonymous();
                bool user = page.IsAuthorized_View_AnyUser();
                if (!anon && !user)
                    s = CombineCss(s, "ypagerole_noUserAnon");
                else if (!anon)
                    s = CombineCss(s, "ypagerole_noAnon");
                else if (!user)
                    s = CombineCss(s, "ypagerole_noUser");
            }
            return MvcHtmlString.Create(CombineCss(s, CurrentPage.CssClass));
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
        public bool IsAnonymousUser { get { return string.IsNullOrWhiteSpace(UserName); } }
        public object UserSettingsObject { get; set; } // data saved by usersettings module/data provider
        public string UserLanguage { get; private set; }
        public string GetUserLanguage() {
            UserLanguage = UserSettings.GetProperty<string>("LanguageId");
            return UserLanguage;
        }
        public void SetUserLanguage(string language) {
            UserLanguage = language;
            UserSettings.SetProperty<string>("LanguageId", language);
        }
        public bool HaveUser { get { return UserId != 0; } }
        public void NeedUser() { if (!HaveUser) throw new Error(this.__ResStr("noUser", "You must be logged in to perform this action")); }

        public bool HasSuperUserRole {
            get {
                if (!HaveCurrentSession) return false;
                string superuser = (string) CurrentSession[Globals.Session_Superuser];
                return !string.IsNullOrWhiteSpace(superuser);
            }
        }
        public void SetSuperUserRole(bool isSuperUser) {
            if (CurrentSession != null) {
                CurrentSession[Globals.Session_Superuser] = null;
                if (isSuperUser)
                    CurrentSession[Globals.Session_Superuser] = "I am/was a superuser";// this is set once we see a superuser. Even if logged off, the session value remains
            }
        }

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
                return (DeviceSelected)(CurrentSession[Globals.Session_ActiveDevice] ?? DeviceSelected.Undecided);
            }
            set {
                if (value != DeviceSelected.Desktop && value != DeviceSelected.Mobile) throw new InternalError("Invalid device selection {0}", value);
                _ActiveDevice = value;
                if (CurrentSession == null) return;
                CurrentSession[Globals.Session_ActiveDevice] = value;
            }
        }
        private DeviceSelected _ActiveDevice = DeviceSelected.Undecided;

        // LOCALIZATION
        // LOCALIZATION
        // LOCALIZATION

        // Localization resource loading is not enabled immediately when the http request starts.
        // It is explicitly enabled in global.asax.cs once important information is available so resource loading can actually work.
        public bool LocalizationSupportEnabled { get; set; }

        // CDN
        // CDN
        // CDN

        public string GetCDNUrl(string url) {
            if (url.StartsWith("/")) {
                bool useCDN = Manager.CurrentSite.CanUseCDN;
                if (useCDN) {
                    if (url.StartsWith(Globals.NugetScriptsUrl))
                        useCDN = CurrentSite.CDNScripts;
                    else if (url.StartsWith(Globals.NugetContentsUrl))
                        useCDN = CurrentSite.CDNContent;
                    else if (url.StartsWith(Globals.SiteFilesUrl))
                        useCDN = CurrentSite.CDNSiteFiles;
                    else if (url.StartsWith(Globals.VaultUrl))
                        useCDN = CurrentSite.CDNVault;
                    else if (url.StartsWith(Globals.AddOnsUrl))
                        useCDN = CurrentSite.CDNAddons;
                    else if (url.StartsWith(Globals.AddOnsCustomUrl))
                        useCDN = CurrentSite.CDNAddonsCustom;
                    else if (url.StartsWith(Globals.AddonsBundlesUrl))
                        useCDN = CurrentSite.CDNAddonsBundles;
                    else if (url.StartsWith("/FileHndlr.image") || url.StartsWith("/File.image"))
                        useCDN = CurrentSite.CDNFileImage;
                    else
                        useCDN = false;
                }
                if (useCDN) {
                    if (Manager.CurrentPage != null) {
                        switch (Manager.CurrentPage.PageSecurity) {
                            case PageDefinition.PageSecurityType.httpOnly:
                                url = CurrentSite.CDNUrl + url;
                                url = url.TruncateStart("http:");
                                break;
                            case PageDefinition.PageSecurityType.Any: // Using Any is really discouraged, but supported
                                if (CurrentSite.PageSecurity == PageSecurityType.NoSSLOnly) {
                                    url = CurrentSite.CDNUrl + url;
                                    url = url.TruncateStart("http:");
                                } else {
                                    if (!string.IsNullOrWhiteSpace(CurrentSite.CDNUrlSecure))
                                        // we err on the side of using https when page security is Any
                                        url = CurrentSite.CDNUrlSecure + url;
                                    else
                                        url = CurrentSite.CDNUrl + url;
                                }
                                break;
                            case PageDefinition.PageSecurityType.httpsOnly:
                                if (!string.IsNullOrWhiteSpace(CurrentSite.CDNUrlSecure)) {
                                    url = CurrentSite.CDNUrlSecure + url;
                                    url = url.TruncateStart("https:");
                                } else {
                                    url = CurrentSite.CDNUrl + url;
                                    url = url.TruncateStart("http:");
                                }
                                break;
                        }
                    } else {
                        url = CurrentSite.CDNUrl + url;
                    }
                }
            }
            return url;
        }

        // SITE TEMPLATES
        // SITE TEMPLATES
        // SITE TEMPLATES

        public bool SiteCreationTemplateActive { get; set; }
    }
}
