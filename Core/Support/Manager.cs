/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneConverter;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.Extensions;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support.Repository;
using YetaWF.Core.Support.StaticPages;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Support {

    /// <summary>
    /// An instance of this class is associated with each HTTP request.
    /// </summary>
    /// <remarks>
    /// An instance of this class contains information about the current HTTP request. Most of it is obtained as needed (lazy loading).
    /// Important items such as logged on user, global information, etc. is accessible through this instance.
    ///
    /// The instance can be retrieved using the static property YetaWFManager.Manager (YetaWF.Core.Support.YetaWFManager.Manager).
    /// Controllers, modules and components and many others provide an accessor in their base classes.
    ///
    /// I can hear the Dependency Injection crowd moaning that this is not a good pattern. Sometimes getting things done is more
    /// important than technical perfection, which ultimately doesn't make the result any better, or user friendly.
    /// Sometimes things are really just global. And really, who cares.
    ///
    /// Because of the abstraction provided by YetaWF with the YetaWFManager, it is possible to write simple console applications
    /// that use all the services of YetaWF, including data providers. These are not .NET based, they are "plain old" console applications.
    /// </remarks>
    public class YetaWFManager {

        /// <summary>
        /// Defines the run-time mode used by console applications.
        /// </summary>
        public const string BATCHMODE = "__Batch";
        /// <summary>
        /// Defines the run-time mode used by service application (API).
        /// </summary>
        public const string SERVICEMODE = "__Service";

        /// <summary>
        /// Defines the current run-time mode. Currently defined are batch mode and service mode. Is neither is set, this is a web application.
        /// </summary>
        public static string Mode { get; set; } = null!;
        /// <summary>
        /// Returns whether the current application is running in batch mode.
        /// </summary>
        /// <returns>Returns true if the current application is running in batch mode, false otherwise.</returns>
        public static bool IsBatchMode { get { return Mode == BATCHMODE; } }
        /// <summary>
        /// Returns whether the current application is a service application (API).
        /// </summary>
        /// <returns>Returns true if the current application is a service application (API), false otherwise.</returns>
        public static bool IsServiceMode { get { return Mode == SERVICEMODE; } }

        private static readonly string YetaWF_ManagerKey = typeof(YetaWFManager).Module + " sft";

        internal class DummyHttpContextAccessor : IHttpContextAccessor {
            public HttpContext? HttpContext { get { return null; } set { } }
        }
        internal class DummyMemoryCache : IMemoryCache {
            public ICacheEntry CreateEntry(object key) { return null!; }
            public void Dispose() { }
            public void Remove(object key) { }
            public bool TryGetValue(object key, out object value) { value = null!; return false; }
        }

        /// <summary>
        /// Called during application startup to save some environmental information. For internal framework use only.
        /// </summary>
        /// <param name="httpContextAccessor">An IHttpContextAccessor instance.</param>
        /// <param name="memoryCache">An IMemoryCache instance.</param>
        /// <param name="svp">An IServiceProvider instance.</param>
        public static void Init(IHttpContextAccessor? httpContextAccessor = null, IMemoryCache? memoryCache = null, IServiceProvider? svp = null) {
            HttpContextAccessor = httpContextAccessor ?? new DummyHttpContextAccessor();
            MemoryCache = memoryCache ?? new DummyMemoryCache();
            ServiceProvider = svp!;
        }

        /// <summary>
        /// A global instance of Microsoft.AspNetCore.Http.IHttpContextAccessor. For internal framework use only.
        /// </summary>
        public static IHttpContextAccessor HttpContextAccessor { get; private set; } = null!;
        /// <summary>
        /// A global instance of Microsoft.Extensions.Caching.Memory.IMemoryCache. For internal framework use only.
        /// </summary>
        public static IMemoryCache MemoryCache { get; private set; } = null!;
        /// <summary>
        /// A global instance of System.IServiceProvider. For internal framework use only.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        private YetaWFManager(string? host) {
            SiteDomain = host ?? "(default)" ; // save the host name that owns this Manager
        }

        /// <summary>
        /// Used for threads, console applications that don't have an HttpContext instance.
        /// </summary>
        [ThreadStatic]
        private static YetaWFManager? _ManagerThreadInstance = null;

        /// <summary>
        /// Returns the instance of the YetaWFManager class associated with the current HTTP request.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public static YetaWFManager Manager {
            get {
                YetaWFManager? manager = _ManagerThreadInstance;
                if (manager != null)
                    return manager;

                HttpContext? context = HttpContextAccessor.HttpContext;
                if (context != null)
                    manager = context.Items[YetaWF_ManagerKey] as YetaWFManager;

                if (manager == null)
                    throw new Error("We don't have a YetaWFManager object.");
                return manager;
            }
        }

        /// <summary>
        /// Determines whether a YetaWFManager instance is available.
        /// </summary>
        /// <remarks>During application startup or during HTTP request startup, the YetaWFManager instance may not yet be available.</remarks>
        public static bool HaveManager {
            get {
                if (_ManagerThreadInstance != null)
                    return true;
                if (HttpContextAccessor != null && HttpContextAccessor.HttpContext != null && HttpContextAccessor.HttpContext.Items[YetaWF_ManagerKey] != null)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Creates an instance of the YetaWFManager class for a site. For internal framework use only.
        /// This is only used by the framework during request startup as soon as the site URL has been determined.
        /// </summary>
        /// <param name="siteHost">The site name as it would appear in a URL (without scheme).</param>
        /// <param name="httpContext">An instance of Microsoft.AspNetCore.Http.HttpContext.</param>
        public static YetaWFManager MakeInstance(HttpContext httpContext, string? siteHost) {
            if (siteHost == null)
                throw new Error("Site host required to create a YetaWFManager object.");
#if DEBUG
            if (httpContext.Items[YetaWF_ManagerKey] != null)
                throw new Error("We already have a YetaWFManager object.");
#endif
            YetaWFManager manager = new YetaWFManager(siteHost);
            httpContext.Items[YetaWF_ManagerKey] = manager;
            _ManagerThreadInstance = null;
            manager._HttpContext = httpContext;
            return manager;
        }

        /// <summary>
        /// Creates an instance of the YetaWFManager - used for non-site specific threads (e.g., scheduler). For internal framework use only.
        /// Can only be used once MakeInitialThreadInstance has been used.
        /// </summary>
        /// <param name="site">Defines the site associated with the Manager instance. Can be null implying batch mode.</param>
        /// <param name="context">An instance of Microsoft.AspNetCore.Http.HttpContext.</param>
        /// <param name="forceSync">Specify true to force synchronous requests, otherwise async requests are used.</param>
        public static YetaWFManager MakeThreadInstance(SiteDefinition? site, HttpContext? context, bool forceSync = false) {
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
                    _ManagerThreadInstance = null;
                    manager.HostUsed = SiteDefinition.GetDefaultSiteDomainAsync().Result; // sync OK as it's cached - this will not run async as we don't have a Manager
                    manager.HostPortUsed = 80;
                    manager.HostSchemeUsed = "http";
                } else {
                    manager.HostUsed = BATCHMODE;
                }
            }

            _ManagerThreadInstance = manager;

            manager._HttpContext = context;
            if (forceSync)
                manager._syncCount++;

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

        /// <summary>
        /// An instance of this class is assigned to the UserSettingsObject property to provide minimal user info.
        /// This is only used for the Scheduler thread.
        /// </summary>
        internal class SchedulerUserData {
            public Localize.Formatting.DateFormatEnum DateFormat { get; set; }
            public string TimeZone { get; set; } = null!;
            public Localize.Formatting.TimeFormatEnum TimeFormat { get; set; }
            public string LanguageId { get; set; } = null!;
        }

        /// <summary>
        /// Attaches a YetaWFManager instance to the current thread. For internal framework use only.
        /// This is only used by console applications.
        /// </summary>
        /// <param name="site">Defines the site associated with the Manager instance.</param>
        /// <returns>The Manager instance for the current request.</returns>
        public static YetaWFManager MakeInitialThreadInstance(SiteDefinition? site) {
            _ManagerThreadInstance = null;
            return MakeThreadInstance(site, null, true);
        }
        /// <summary>
        /// Attaches a YetaWFManager instance to the current thread. For internal framework use only.
        /// This is only used by console applications.
        /// </summary>
        /// <param name="site">A SiteDefinition object. Is always null as this is not available in console applications.</param>
        /// <param name="context">The HttpContext instance for the current request. If null is specified, local thread storage is used instead of attaching the Manager instance to the HttpRequest.</param>
        /// <param name="forceSync">Specify true to force synchronous requests, otherwise async requests are used.</param>
        /// <returns>The Manager instance for the current request.</returns>
        public static YetaWFManager MakeInitialThreadInstance(SiteDefinition site, HttpContext? context, bool forceSync = false) {
            _ManagerThreadInstance = null;
            return MakeThreadInstance(site, context, forceSync);
        }
        /// <summary>
        /// Removes the YetaWF instance from the current thread. For internal framework use only.
        /// </summary>
        public static void RemoveThreadInstance() {
            _ManagerThreadInstance = null;
        }

        // DOMAIN
        // DOMAIN
        // DOMAIN

        private static void SetRequestedDomain(HttpContext httpContext, string siteDomain) {
            if (siteDomain == null)
                httpContext.Session.Remove(Globals.Link_ForceSite);
            else
                httpContext.Session.SetString(Globals.Link_ForceSite, siteDomain);
        }

        /// <summary>
        /// Used by the framework during HTTP request startup to determine the requested domain. For internal framework use only.
        /// </summary>
        /// <param name="httpContext">An instance of Microsoft.AspNetCore.Http.HttpContext associated with the current request.</param>
        /// <param name="uri">The URI of the current request.</param>
        /// <param name="loopBack">true if the current request is for the loopback address 127.0.0.1/localhost.</param>
        /// <param name="siteDomain">The domain name detected.</param>
        /// <param name="overridden">Returns whether the domain name was explicitly defined using the !Domain query string argument.</param>
        /// <param name="newSwitch">Returns whether this is a request for a new domain (being created).</param>
        /// <returns></returns>
        public static string GetRequestedDomain(HttpContext httpContext, Uri uri, bool loopBack, string siteDomain, out bool overridden, out bool newSwitch) {
            overridden = newSwitch = false;

            if (loopBack) {
                if (!string.IsNullOrWhiteSpace(siteDomain)) {
                    overridden = newSwitch = true;
                    SetRequestedDomain(httpContext, siteDomain);
                }
                ISession? session = null;
                try {
                    session = httpContext.Session;
                } catch (Exception) { }

                if (!overridden && session != null) {
                    siteDomain = httpContext.Session.GetString(Globals.Link_ForceSite);
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

        // FOLDERS
        // FOLDERS
        // FOLDERS

        /// <summary>
        /// The physical location (path) of the website's root folder (wwwroot on .NET - MVC).
        /// </summary>
        public static string RootFolder { get; set; } = null!;

        /// <summary>
        /// The physical location (path) of the Website project (Website.csproj) root folder.
        /// </summary>
        public static string RootFolderWebProject { get; set; } = null!;
        /// <summary>
        /// The physical location (path) of the Solution (*.sln) root folder.
        /// </summary>
        /// <remarks>
        /// Defines the folder where the solution file is located.
        /// </remarks>
        public static string RootFolderSolution {
            get { return Path.Combine(RootFolderWebProject, ".."); }
        }

        /// <summary>
        /// Returns the folder containing all sites' file data.
        /// </summary>
        /// <remarks>
        /// The sites data folder is located at ./Website/Sites/DataFolder.
        ///
        /// This folder is not publicly accessible on .NET - MVC.
        /// </remarks>
        public static string RootSitesFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolderWebProject, Globals.SitesFolder, "DataFolder");
            }
        }

        /// <summary>
        /// Returns the default site name used for this instance of YetaWF.
        /// </summary>
        /// <remarks>The default site is defined in AppSettings.json (Application.P.YetaWF_Core.DEFAULTSITE).</remarks>
        public static string DefaultSiteName {
            get {
                if (_defaultSiteName == null) {
                    _defaultSiteName = WebConfigHelper.GetValue<string>("YetaWF_Core"/*==YetaWF.Core.AreaRegistration.CurrentPackage.AreaName*/, "DEFAULTSITE") ?? throw new InternalError("Default site must be defined in AppSettings.json");
                }
                return _defaultSiteName;
            }
        }
        private static string? _defaultSiteName;

        /// <summary>
        /// The physical location (path) of the Data folder (not site specific).
        /// </summary>
        /// <remarks>
        /// The Data folder is located at ./Website/Data/DataFolder.
        ///
        /// This folder is not publicly accessible on .NET - MVC.
        /// </remarks>
        public static string DataFolder {
            get {
                string rootFolder = YetaWFManager.RootFolderWebProject;
                return Path.Combine(rootFolder, Globals.DataFolder, "DataFolder");
            }
        }

        /// <summary>
        /// The physical location (path) of the product license folder (not site specific). This is used by third-party licensed products only.
        /// </summary>
        /// <remarks>
        /// The License folder is located at ./Website/Data/Licenses.
        ///
        /// This folder is not publicly accessible on .NET - MVC.
        /// </remarks>
        public static string LicenseFolder {
            get {
                string rootFolder = YetaWFManager.RootFolderWebProject;
                return Path.Combine(rootFolder, Globals.DataFolder, "Licenses");
            }
        }

        /// <summary>
        /// The physical location (path) of the Vault private folder (not site specific).
        /// </summary>
        /// <remarks>
        /// The Vault private folder is located at ./Website/VaultPrivate.
        ///
        /// This folder is not publicly accessible on .NET - MVC.
        /// </remarks>
        public static string VaultPrivateFolder {
            get {
                string rootFolder = YetaWFManager.RootFolderWebProject;
                return Path.Combine(rootFolder, Globals.VaultPrivateFolder);
            }
        }

        /// <summary>
        /// The physical location (path) of the Vault folder (not site specific).
        /// </summary>
        /// <remarks>
        /// The Vault folder is located at ./Website/wwwroot/Vault on .NET.
        ///
        /// This folder is publicly accessible on .NET - MVC.
        /// </remarks>
        public static string VaultFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolder, Globals.VaultFolder);
            }
        }

        /// <summary>
        /// The physical location (path) containing the current site's file data.
        /// </summary>
        /// <remarks>
        /// An individual site's file data folder is located at ./Website/Sites/DataFolder/{..siteidentity..}.
        ///
        /// This folder is not publicly accessible on .NET - MVC.
        /// </remarks>
        public string SiteFolder {
            get {
                return Path.Combine(RootSitesFolder, CurrentSite.Identity.ToString());
            }
        }
        /// <summary>
        /// The physical location (path) of the site's custom addons folder.
        /// </summary>
        /// <remarks>
        /// An individual site's file data folder is located at ./Website/wwwroot/AddonsCustom/{..domainname..} on .NET.
        ///
        /// This folder is publicly accessible. It contains JavaScript and CSS files which are served as static files.
        /// </remarks>
        public string AddonsCustomSiteFolder {
            get {
                return Path.Combine(Utility.UrlToPhysical(Globals.AddOnsCustomUrl), SiteDomain);
            }
        }

        // CACHE
        // CACHE
        // CACHE

        /// <summary>
        /// Returns a string that can be used in generated URLs as "cache buster", i.e., to defeat client-side caching.
        /// </summary>
        /// <remarks>The value returned by this property is the same for each call. It changes only when the site is restarted.</remarks>
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
        private static string? _cacheBuster;

        // BUILD
        // BUILD
        // BUILD

        /// <summary>
        /// Defines whether the currently running instance of YetaWF is using additional run-time diagnostics to find issues, typically used during development.
        /// </summary>
        /// <value>true in diagnostic mode, false otherwise.</value>
        public static bool DiagnosticsMode {
            get {
                if (_diagnosticsMode == null)
                    _diagnosticsMode = WebConfigHelper.GetValue<bool>(YetaWF.Core.AreaRegistration.CurrentPackage.AreaName, "Diagnostics");
                return (bool)_diagnosticsMode;
            }
        }
        private static bool? _diagnosticsMode = null;

        /// <summary>
        /// Defines whether the currently running instance of YetaWF is a deployed instance or not.
        /// </summary>
        /// <remarks>
        /// A "deployed" instance is not necessarily a Release build, but behaves as though it is.
        ///
        /// A deployed instance is considered to run as a public website with all development features disabled.
        /// TODO: Need an actual list of development features here.
        ///
        /// AppSettings.json (Application.P.YetaWF_Core.Deployed) is used to define whether the site is a deployed site.
        /// </remarks>
        /// <value>true for a deployed site, false otherwise.</value>
        public static bool Deployed {
            get {
                if (_deployed == null)
                    _deployed = WebConfigHelper.GetValue<bool>(YetaWF.Core.AreaRegistration.CurrentPackage.AreaName, "Deployed");
                return (bool)_deployed;
            }
        }
        private static bool? _deployed = null;

        // SETTINGS
        // SETTINGS
        // SETTINGS

        /// <summary>
        /// Returns whether a CDN can be used for website data.
        /// </summary>
        public static bool CanUseCDN {
            get {
                if (_canUseCDN == null)
                    _canUseCDN = WebConfigHelper.GetValue<bool>(YetaWF.Core.AreaRegistration.CurrentPackage.AreaName, "UseCDN");
                return (bool)_canUseCDN;
            }
        }
        private static bool? _canUseCDN = null;

        public static bool CanUseCDNComponents {
            get {
                if (canUseCDNComponents == null) {
                    canUseCDNComponents = WebConfigHelper.GetValue<bool>(YetaWF.Core.AreaRegistration.CurrentPackage.AreaName, "UseCDNComponents");
                }
                return (bool)canUseCDNComponents;
            }
        }
        private static bool? canUseCDNComponents = null;

        public static bool CanUseStaticDomain {
            get {
                if (canUseStaticDomain == null) {
                    canUseStaticDomain = WebConfigHelper.GetValue<bool>(YetaWF.Core.AreaRegistration.CurrentPackage.AreaName, "UseStaticDomain");
                }
                return (bool)canUseStaticDomain;
            }
        }
        private static bool? canUseStaticDomain = null;

        /// <summary>
        /// Defines whether the current request is for the static site. A static site URL can be defined using Admin > Settings > Site Settings, CDN tab, Static Files Domain.
        /// </summary>
        /// <remarks>Defining a static domain offers the ability to have all static files served from that domain, which is hosted by the same YetaWF instance as the main domain. Static domains don't send cookie information with each response, reducing overhead.
        ///
        /// Support for static domains once enabled is fully automatic.</remarks>
        public bool IsStaticSite { get; set; }

        // DEMO
        // DEMO
        // DEMO

        /// <summary>
        /// Defines whether the current YetaWF instance runs in demo mode.
        /// </summary>
        /// <remarks>Demo mode allows anonymous users to use all features in Superuser mode, without being able to change any data.
        ///
        /// Demo mode is enabled/disabled using AppSettings.json (Application.P.YetaWF_Core.Demo).
        /// </remarks>
        public static bool IsDemo {
            get {
                if (_isDemo == null)
                    _isDemo = WebConfigHelper.GetValue<bool>(YetaWF.Core.AreaRegistration.CurrentPackage.AreaName, "Demo");
                return (bool)_isDemo;
            }
        }
        private static bool? _isDemo = null;

        /// <summary>
        /// Defines whether the current user has the "Demo" role.
        /// </summary>
        /// <remarks>The demo user role can be assigned to any user. Pages and modules can be limited in their access by a demo user (Authorization tab).</remarks>
        public bool IsDemoUser {
            get {
                if (_isDemoUser == null)
                    _isDemoUser = Manager.UserRoles != null && Manager.UserRoles.Contains(Resource.ResourceAccess.GetUserDemoRoleId());
                return (bool)_isDemoUser;
            }
        }
        private bool? _isDemoUser = null;

        // HTTPCONTEXT
        // HTTPCONTEXT
        // HTTPCONTEXT

        /// <summary>
        /// The current site's domain - E.g., softelvdm.com, localhost, etc.
        /// </summary>
        public string SiteDomain { get; set; }

        /// <summary>
        /// The host used to access this website.
        /// </summary>
        public string HostUsed { get; set; } = null!;
        /// <summary>
        /// The port used to access this website.
        /// </summary>
        public int HostPortUsed { get; set; }
        /// <summary>
        /// The scheme used to access this website.
        /// </summary>
        public string HostSchemeUsed { get; set; } = null!;
        /// <summary>
        /// Defines whether localhost/127.0.0.1 was used to access this website.
        /// </summary>
        public bool IsLocalHost { get; set; }
        /// <summary>
        /// Defines whether the test domain was used to access this website.
        /// </summary>
        public bool IsTestSite { get; set; }
        /// <summary>
        /// Defines whether the domain uses http:// only (overrides site/page settings)
        /// </summary>
        public static bool IsHTTPSite {
            get {
                if (_IsHTTPSite == null)
                    _IsHTTPSite = WebConfigHelper.GetValue<bool>(YetaWF.Core.AreaRegistration.CurrentPackage.AreaName, "ForceHttp", false);
                return (bool)_IsHTTPSite;
            }
        }
        private static bool? _IsHTTPSite = null;

        /// <summary>
        /// The current site definition.
        /// The current site is identified based on the URL of the current request.
        /// </summary>
        /// <returns>The current site's definitions.</returns>
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
        private SiteDefinition? _currentSite;

        /// <summary>
        /// Returns whether information about the current site is available.
        /// This may return false during startup or before processing the current HTTP request has started.
        /// </summary>
        public bool HaveCurrentSite {
            get {
                return _currentSite != null;
            }
        }

        /// <summary>
        /// Saved URL where we came from (e.g. used for return handling after Save).
        /// TODO: This needs some rework.
        /// </summary>
        public List<Origin> OriginList { get; set; } = null!;

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
                    qh.Add(Globals.Link_OriginList, Utility.JsonSerialize(originList), Replace: true);
                    url = qh.ToUrl(urlOnly);
                }
                return url;
            }
        }

        // GetUrlArg and TryGetUrlArg is used to retrieve optional Url args (outside of a Controller) added to a page using AddUrlArg, so one module can add args for other modules on the same page

        /// <summary>
        /// Used to retrieve URL query string arguments (outside of a controller).
        /// </summary>
        /// <typeparam name="TYPE">The expected return value type.</typeparam>
        /// <param name="arg">The name of the query string argument.</param>
        /// <returns>Returns the query string argument. If the argument is not available, an exception occurs.</returns>
        /// <remarks>This would not be used in a controller as these have access to all arguments via their parameter list. This is typically only used in a module action that is dynamically added by a module.</remarks>
        public TYPE GetUrlArg<TYPE>(string arg) {
            if (!TryGetUrlArg<TYPE>(arg, out TYPE? val))
                throw new InternalError(this.__ResStr("invUrlArg", "{0} URL argument invalid or missing", arg));
            return val;
        }
        /// <summary>
        /// Used to retrieve URL query string arguments (outside of a controller).
        /// </summary>
        /// <typeparam name="TYPE">The expected return value type.</typeparam>
        /// <param name="arg">The name of the query string argument.</param>
        /// <param name="val">Returns the query string argument. If the argument is not available, the type's default value is returned.</param>
        /// <param name="dflt">An optional value, which is returned if the argument is not available.</param>
        /// <returns>true if the argument was found, false otherwise.</returns>
        /// <remarks>This would not be used in a controller as these have access to all arguments via their parameter list. This is typically only used in a module action that is dynamically added by a module.</remarks>
        public bool TryGetUrlArg<TYPE>(string arg, [NotNullWhen(true)] out TYPE? val, TYPE? dflt = default) {
            val = dflt;
            string? v;
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
                val = (TYPE)(object)(v == "1" || v.ToLower() == "on" || v.ToLower() == "true" || v.ToLower() == "yes");
                return true;
            } else if (typeof(TYPE) == typeof(string)) {
                try {
                    val = (TYPE)(object)v;
                } catch (Exception) {
                    return false;
                }
                return true;
            } else if (typeof(TYPE) == typeof(DateTime) || typeof(TYPE) == typeof(DateTime?)) {
                DateTime dt;
                if (DateTime.TryParse(v, out dt)) {
                    val = (TYPE)(object)dt;
                    return true;
                }
                return false;
            } else {
                // TryGetUrlArg doesn't support this type
                return false;
            }
        }

        /// <summary>
        /// Add a temporary (non-visible) URL query string argument to the current page being rendered.
        /// This is mainly used to propagate selections from one module to another (top-down on page only).
        /// Other modules must use TryGetUrlArg or GetUrlArg to retrieve these arguments as they're not part of the actual query string/URL.
        /// TO BE REMOVED.
        /// </summary>
        public void AddUrlArg(string arg, string? value) {
            ExtraUrlArgs.Add(arg, value);
        }
        private readonly Dictionary<string, string?> ExtraUrlArgs = new Dictionary<string, string?>();

        /// <summary>
        /// Returns whether the page control module is visible.
        /// </summary>
        public bool PageControlShown { get; set; }

        /// <summary>
        /// Returns whether we're in a popup.
        /// </summary>
        public bool IsInPopup { get; set; }

        internal void Verify_NotPostRequest() {
            if (IsPostRequest)
                throw new InternalError("This is not supported for POST requests");
        }
        internal void Verify_PostRequest() {
            if (!IsPostRequest)
                throw new InternalError("This is only supported for POST requests");
        }

        // MANAGERS
        // MANAGERS
        // MANAGERS

        /// <summary>
        /// Returns the instance of MetatagsManager associated with the current HTTP request.
        /// </summary>
        public MetatagsManager MetatagsManager {
            get {
                if (_metatagsManager == null)
                    _metatagsManager = new MetatagsManager(this);
                return _metatagsManager;
            }
        }
        private MetatagsManager? _metatagsManager = null;

        /// <summary>
        /// Used by skin modules to retrieve all meta tags as an HTML string.
        /// </summary>
        public string MetatagsHtml {
            get {
                return MetatagsManager.Render();
            }
        }

        /// <summary>
        /// Returns the instance of LinkAltManager associated with the current HTTP request.
        /// </summary>
        public LinkAltManager LinkAltManager {
            get {
                if (_linkAltManager == null)
                    _linkAltManager = new LinkAltManager();
                return _linkAltManager;
            }
        }
        private LinkAltManager? _linkAltManager = null;

        /// <summary>
        /// Returns the instance of ScriptManager associated with the current HTTP request.
        /// </summary>
        public ScriptManager ScriptManager {
            get {
                if (_scriptManager == null)
                    _scriptManager = new ScriptManager(this);
                return _scriptManager;
            }
        }
        private ScriptManager? _scriptManager = null;

        /// <summary>
        /// Returns the instance of CssManager associated with the current HTTP request.
        /// </summary>
        public CssManager CssManager {
            get {
                if (_cssManager == null)
                    _cssManager = new CssManager(this);
                return _cssManager;
            }
        }
        private CssManager? _cssManager = null;

        /// <summary>
        /// Returns the instance of AddOnManager associated with the current HTTP request.
        /// </summary>
        public AddOnManager AddOnManager {
            get {
                if (_addOnManager == null)
                    _addOnManager = new AddOnManager(this);
                return _addOnManager;
            }
        }
        private AddOnManager? _addOnManager = null;

        /// <summary>
        /// Returns the instance of StaticPageManager associated with the current HTTP request.
        /// </summary>
        /// <remarks>
        /// The Static Page Manager instance is used to manage a site's static pages. It is only allocated if static pages are actually used.
        /// </remarks>
        public StaticPageManager StaticPageManager {
            get {
                if (_staticPageManager == null)
                    _staticPageManager = new StaticPageManager();
                return _staticPageManager;
            }
        }
        private StaticPageManager? _staticPageManager = null;

        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT

        /// <summary>
        /// Returns a unique HTML id.
        /// </summary>
        /// <param name="name">A string prefix prepended to the generated id.</param>
        /// <returns>Returns a unique HTML id.</returns>
        /// <remarks>Every call to the Unique() method returns a new, unique id.
        ///
        /// Whenever an HTML id is needed, this method must be used. This insures that Ajax/Post requests do not
        /// accidentally use ids that were used in prior requests.
        /// </remarks>
        public string UniqueId(string name = "a") {
            ++UniqueIdCounters.UniqueIdCounter;
            return $"{UniqueIdCounters.UniqueIdPrefix}{UniqueIdCounters.UniqueIdPrefixCounter}_{name}{UniqueIdCounters.UniqueIdCounter}";
        }

        public void NextUniqueIdPrefix() {
            UniqueIdCounters.UniqueIdPrefixCounter++;
            UniqueIdCounters.UniqueIdCounter = 0;
        }

        public UniqueIdInfo UniqueIdCounters { get; set; } = new UniqueIdInfo { UniqueIdPrefix = UniqueIdTracked };
        private const string UniqueIdTracked = "u";

        public class UniqueIdInfo {
            public string UniqueIdPrefix { get; set; } = null!;
            public int UniqueIdPrefixCounter { get; set; }
            public int UniqueIdCounter { get; set; }

            [JsonIgnore]
            public bool IsTracked { get { return UniqueIdPrefix == UniqueIdTracked; } }
        }

        // HTTPCONTEXT
        // HTTPCONTEXT
        // HTTPCONTEXT

        /// <summary>
        /// Returns the user's IP address. If none is available, an empty string is returned.
        /// </summary>
        public string UserHostAddress {
            get {
                if (!HaveCurrentRequest) return string.Empty;
                IHttpConnectionFeature connectionFeature = CurrentContext.Features.Get<IHttpConnectionFeature>();
                if (connectionFeature != null && connectionFeature.RemoteIpAddress != null)
                    return connectionFeature.RemoteIpAddress.ToString();
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the current HTTP request's query string.
        /// </summary>
        public QueryHelper RequestQueryString {
            get {
                if (_requestQueryString == null)
                    _requestQueryString = QueryHelper.FromQueryCollection(CurrentRequest.Query);
                return _requestQueryString;
            }
        }
        private QueryHelper? _requestQueryString = null;

        /// <summary>
        /// Returns the current HTTP request's form information.
        /// </summary>
        public FormHelper RequestForm {
            get {
                if (_requestForm == null) {
                    if (!CurrentRequest.HasFormContentType)
                        _requestForm = new FormHelper();
                    else
                        _requestForm = FormHelper.FromFormCollection(CurrentRequest.Form);
                }
                return _requestForm;
            }
        }
        private FormHelper? _requestForm = null;

        private HttpContext? _HttpContext = null;

        public bool HaveCurrentContext {
            get {
                return _HttpContext != null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpContext CurrentContext {
            get {
                if (_HttpContext == null)
                    throw new InternalError("No HttpContext available");
                return _HttpContext;
            }
            set {
                _HttpContext = value;
            }
        }

        public bool HaveCurrentRequest {
            get {
                return HaveCurrentContext && CurrentContext.Request != null;
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpRequest CurrentRequest {
            get {
                HttpRequest request = CurrentContext.Request;
                if (request == null) throw new InternalError("No current Request available");
                return request;
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public HttpResponse CurrentResponse {
            get {
                HttpResponse response = CurrentContext.Response;
                if (response == null) throw new InternalError("No current Response available");
                return response;
            }
        }
        public string ReferrerUrl {
            get {
                return Manager.CurrentRequest.Headers["Referer"].ToString();
            }
        }

        public static void SetStaticCacheInfo(HttpContext context) {
            if (YetaWFManager.Deployed && StaticCacheDuration > 0) {
                context.Response.Headers[HeaderNames.CacheControl] = string.Format("max-age={0}", StaticCacheDuration * 60);
            }
            // add CORS header for static site
#if DEBUG
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
#else
            SiteDefinition? site = SiteDefinition.LoadStaticSiteDefinitionAsync(context.Request.Host.Host).Result;// cached, so ok to use result
            if (site != null)
                context.Response.Headers.Add("Access-Control-Allow-Origin", $"{context.Request.Scheme}://{site.SiteDomain.ToLower()}");
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
                try {
                    return HaveCurrentContext && CurrentContext.Session != null;
                } catch (Exception) {
                    return false;
                }
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

        public string? CurrentSessionId {
            get {
                if (HaveCurrentSession)
                    return CurrentContext.Session.Id;
                return null;
            }
        }

        /// <summary>
        /// Describes the current URL requested. CurrentRequestUrl may not match the full page URL in UPS requests.
        /// </summary>
        public string CurrentRequestUrl {
            get {
                if (_currentRequestUrl == null) {
                    _currentRequestUrl = UriHelper.GetDisplayUrl(Manager.CurrentRequest);
                }
                return _currentRequestUrl;
            }
            set {
                _currentRequestUrl = value;
            }
        }
        private string? _currentRequestUrl;

        /// <summary>
        /// Describes the current URL (path and query string only) requested, as shown by browser, which matches the page URL. CurrentRequestUrl may not match the full page URL in UPS requests.
        /// </summary>
        public string CurrentUrl { get; set; } = null!;

        public void RestartSite(string? url = null) {
            IHostApplicationLifetime applicationLifetime = (IHostApplicationLifetime)ServiceProvider.GetService(typeof(IHostApplicationLifetime)) !;
            applicationLifetime.StopApplication();

#if DEBUG
            if (!string.IsNullOrWhiteSpace(url)) {
                // with Kestrel/IIS Express we shut down so provide some feedback
                try {
                    byte[] btes = System.Text.Encoding.ASCII.GetBytes("<html><head></head><body><strong>The site has stopped - Please close your browser and restart the application.<strong></body></html>");
                    Manager.CurrentResponse.Body.WriteAsync(btes, 0, btes.Length).Wait(); // Wait OK, this is debug only
                    Manager.CurrentResponse.Body.FlushAsync().Wait(); // Wait OK, this is debug only
                } catch (Exception) { }
        }
#endif
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsPostRequest {
            get {
                HttpRequest request = CurrentRequest;
                string overRide = request.Headers["X-HTTP-Method-Override"];
                if (overRide != null)
                    return request.Headers["X-HTTP-Method-Override"] == "POST";
                return (request.Method == "POST");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsGetRequest {
            get {
                HttpRequest request = CurrentRequest;
                string? overRide = request.Headers["X-HTTP-Method-Override"];
                if (overRide != null)
                    return overRide == "GET";
                return (request.Method == "GET" || request.Method == "HEAD" || request.Method == "");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsHeadRequest {
            get {
                HttpRequest request = CurrentRequest;
                string overRide = request.Headers["X-HTTP-Method-Override"];
                if (overRide != null)
                    return overRide == "HEAD";
                return (request.Method == "HEAD");
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
        private SessionSettings? _SessionSettings = null;

        public bool EditMode { get; set; }

        public ModuleDefinition? CurrentModuleEdited { get; set; }// used during module editing to signal which module is being edited
        public string ModeCss { get { return EditMode ? "yEditMode" : "yDisplayMode"; } }// used on body tag when in edit mode

        // PAGES
        // PAGES
        // PAGES

        /// <summary>
        /// The current page.
        /// </summary>
        public PageDefinition CurrentPage { get; set; } = null!;

        /// <summary>
        /// The current page title. Modules can override the page title (we don't use the title in the page definition, except to set the default title).
        /// </summary>
        public MultiString PageTitle { get; set; } = null!;

        public string PageTitleHtml {
            get {
                string title = PageTitle.ToString();
                return string.Format("<title>{0}</title>", Utility.HE(title ?? ""));
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

        public ModuleDefinition? CurrentModule { get; set; } // current module rendered

        // COMPONENTS/VIEWS
        // COMPONENTS/VIEWS
        // COMPONENTS/VIEWS

        public List<Package> ComponentPackagesSeen = new List<Package>();
        public List<string> ComponentsSeen = new List<string>();

        public IDisposable StartNestedComponent(string fieldName) {
            NestedComponents.Add(fieldName);
            return new NestedComponent();
        }
        public string? NestedComponentPrefix {
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
        /// Set when rendering content only (UPS)
        /// </summary>
        public bool RenderContentOnly { get; set; }

        /// <summary>
        /// The current pane being rendered.
        /// </summary>
        public string? PaneRendered { get; set; }

        public bool IsRenderingPane { get { return PaneRendered != null; } }

        public bool ForceModuleActionLinks { get; set; } // force module action links outside of a pane

        // While rendering a module, this is set to reflect whether the module wants the input focus
        public bool WantFocus { get; set; }

        /// <summary>
        /// Contains the page's last date/time updated while rendering a page.
        /// </summary>
        public DateTime LastUpdated { get { return _lastUpdated; } set { if (value > LastUpdated) _lastUpdated = value; } }
        private DateTime _lastUpdated;

        public bool RenderingUniqueModuleAddons { get; set; }
        public bool RenderingUniqueModuleAddonsAjax { get; set; }

        /// <summary>
        /// This property can be used by a component rendering package to save information for the current HTTP request.
        /// It is not used by YetaWF.
        /// </summary>
        public object? ComponentsData { get; set; }

        // FORM PROCESSING
        // FORM PROCESSING
        // FORM PROCESSING

        /// <summary>
        /// True while processing a partial view (usually a partial form/ajax)
        /// </summary>
        public bool InPartialView { get; set; }

        /// <summary>
        /// Cache rendered HTML for anti-forgery token.
        /// </summary>
        /// <remarks>I don't like this.</remarks>
        public string? AntiForgeryTokenHTML { get; set; }

        /// <summary>
        /// Defines whether non-site specific data is also imported when importing packages
        /// </summary>
        /// <remarks>Site specific data is always imported</remarks>
        public bool ImportChunksNonSiteSpecifics { get; set; }

        // SKIN
        // SKIN
        // SKIN

        /// <summary>
        /// Returns skin information for the skin used by the current page.
        /// </summary>
        public SkinCollectionInfo SkinInfo { 
            get {
                if (_skinInfo == null) {
                    SkinAccess skinAccess = new SkinAccess();
                    _skinInfo = skinAccess.GetSkinCollectionInfo();
                }
                return _skinInfo;
            }
        }
        private SkinCollectionInfo? _skinInfo = null;

        /// <summary>
        /// Adds the page's or popup's css classes (the current edit mode, the current page's defined css and other page css).
        /// </summary>
        /// <param name="css">The skin-defined Css class identifying the skin.</param>
        /// <returns>A Css class string.</returns>
        public string PageCss() {
            SkinAccess skinAccess = new SkinAccess();
            PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
            string s = pageSkin.CSS;
            s = CssManager.CombineCss(s, ModeCss);// edit/display mode (doesn't change in same Unified page set)
            s = CssManager.CombineCss(s, HaveUser ? "yUser" : "yAnonymous");// add whether we have an authenticated user (doesn't change in same Unified page set)
            s = CssManager.CombineCss(s, IsInPopup ? "yPopup" : "yPage"); // popup or full page (doesn't change in same Unified page set)
            s = CssManager.CombineCss(s, $"pageTheme{Manager.CurrentSite.Theme}");

            string cssClasses = CurrentPage.GetCssClass(); // get page specific Css (once only, used 2x)

            // add the extra page css class and generated page specific Css via javascript to body tag (used for dynamic content)
            ScriptBuilder sb = new Support.ScriptBuilder();
            sb.Append("document.body.setAttribute('data-pagecss', '{0}');", Utility.JserEncode(cssClasses));
            Manager.ScriptManager.AddLast(sb.ToString());

            return CssManager.CombineCss(s, cssClasses);
        }

        // CURRENT USER
        // CURRENT USER
        // CURRENT USER

        // user info is obtained in global.asax.cs by Authentication provider ResolveUser
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public int UserId { get; set; }
        public List<int>? UserRoles { get; set; }
        public object? UserObject { get; set; }// data saved by Authentication provider
        public object? UserSettingsObject { get; set; } // data saved by usersettings module/data provider IUserSettings
        public string? UserLanguage { get; private set; }
        public string GetUserLanguage() {
            string? userLang = UserSettings.GetProperty<string>("LanguageId");
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
        /// Saves data for a package. The saved data is only available during the current HTTP request.
        /// </summary>
        /// <param name="areaName">The area name for which data is saved.</param>
        /// <param name="o">The data.</param>
        public void SetPackageData(string areaName, object o) {
            if (_packageData == null)
                _packageData = new Dictionary<string, object>();
            _packageData[areaName] = o;
        }
        /// <summary>
        /// Retrieves saved data for a package.
        /// </summary>
        /// <typeparam name="TYPE">The type of the data.</typeparam>
        /// <param name="areaName">The area name for which data was saved.</param>
        /// <returns>Returns the data or null, if not available.</returns>
        public TYPE? GetPackageData<TYPE>(string areaName) {
            if (_packageData == null)
                return default(TYPE);
            if (_packageData.TryGetValue(areaName, out object? data))
                return (TYPE)data;
            return default;
        }
        private Dictionary<string, object>? _packageData = null;

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
        public List<string>? UserAuthorizedUrls { get; set; }
        public List<string>? UserNotAuthorizedUrls { get; set; }

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
                // Timezones don't use the same ids between Windows and other environments (nothing is ever easy)
                // We store Windows Id so we translate on non-windows environments
                string? tz = UserSettings.GetProperty<string>("TimeZone");
                if (!string.IsNullOrWhiteSpace(tz)) {
                    if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                        try {
                            tz = TZConvert.WindowsToIana(tz);
                        } catch (Exception) { }
                    }
                    try {
                        timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                    } catch (Exception) { }
                }
                if (timeZoneInfo == null) {
                    if (!Manager.LocalizationSupportEnabled) // if false, ResolveUserAsync has not been called, so we can't determine the timezone
                        throw new InternalError("Retrieving time zone information without user settings");
                    timeZoneInfo = TimeZoneInfo.Local;
                }
            }
            return timeZoneInfo;
        }
        private TimeZoneInfo? timeZoneInfo = null;

        public CultureInfo GetCultureInfo() {
            if (cultureInfo == null) {
                string? lang = UserSettings.GetProperty<string>("LanguageId");
                cultureInfo = lang != null ? new CultureInfo(lang) : CultureInfo.InvariantCulture;
            }
            return cultureInfo;
        }
        private CultureInfo? cultureInfo = null;

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

        /// <summary>
        /// Returns whether requests must be made synchronously (i.e., no async).
        /// </summary>
        /// <remarks>
        /// At certain times, particularly during rendering, requests cannot be made asynchronously.
        /// The IsSync method can be used to determine whether requests must be made synchronously.
        /// </remarks>
        /// <returns>Returns whether all requests must be made synchronously (i.e., no async).</returns>
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
            private YetaWFManager? Manager;
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
