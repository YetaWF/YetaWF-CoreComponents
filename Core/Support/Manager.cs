/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
using TimeZoneConverter;
using Newtonsoft.Json;
using YetaWF.Core.Identity;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
#else
using System.Web;
#endif

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
    /// that use all the services of YetaWF, including data providers. These are not ASP.NET Core based, they are "plain old" console applications.
    /// </remarks>
    public class YetaWFManager {

        /// <summary>
        /// Defines the name used by the HostUsed property to identity that the current execution is a console application.
        /// </summary>
        public const string BATCHMODE = "__Batch";
        /// <summary>
        /// Defines the name used by the HostUsed property to identity that the current execution is a service application.
        /// </summary>
        public const string SERVICEMODE = "__Service";

        public static string Mode { get; set; }
        public static bool IsBatchMode { get { return Mode == BATCHMODE; } }
        public static bool IsServiceMode { get { return Mode == SERVICEMODE; } }

        private static readonly string YetaWF_ManagerKey = typeof(YetaWFManager).Module + " sft";

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
        public static void Init(IHttpContextAccessor httpContextAccessor = null, IMemoryCache memoryCache = null, IServiceProvider svp = null) {
            HttpContextAccessor = httpContextAccessor ?? new DummyHttpContextAccessor();
            MemoryCache = memoryCache ?? new DummyMemoryCache();
            ServiceProvider = svp;
        }
        public static IHttpContextAccessor HttpContextAccessor = null;
        public static IMemoryCache MemoryCache = null;
        public static IServiceProvider ServiceProvider = null;
#else
#endif

        private YetaWFManager(string host) {
            SiteDomain = host; // save the host name that owns this Manager
        }

        /// <summary>
        /// Used for threads, console applications that don't have an HttpContext instance.
        /// </summary>
        [ThreadStatic]
        private static YetaWFManager _ManagerThreadInstance = null;

        /// <summary>
        /// Returns the instance of the YetaWFManager class associated with the current HTTP request.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public static YetaWFManager Manager {
            get {
                YetaWFManager manager = _ManagerThreadInstance;
                if (manager != null)
                    return manager;
#if MVC6
                HttpContext context = HttpContextAccessor.HttpContext;
                if (context != null) {
                    manager = context.Items[YetaWF_ManagerKey] as YetaWFManager;
                }
#else
                if (HttpContext.Current != null) {
                    manager = HttpContext.Current.Items[YetaWF_ManagerKey] as YetaWFManager;
                }
#endif
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
#if MVC6
                if (HttpContextAccessor != null && HttpContextAccessor.HttpContext != null) {
                    if (HttpContextAccessor.HttpContext.Items[YetaWF_ManagerKey] != null)
                        return true;
                }
#else
                if (HttpContext.Current != null) {
                    if (HttpContext.Current.Items[YetaWF_ManagerKey] != null)
                        return true;
                }
#endif
                return false;
            }
        }

        /// <summary>
        /// Creates an instance of the YetaWFManager class for a site.
        /// This is only used by the framework during request startup as soon as the site URL has been determined.
        /// </summary>
        /// <param name="siteHost">The site name as it would appear in a URL (without scheme).</param>
        public static YetaWFManager MakeInstance(HttpContext httpContext, string siteHost) {
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
        /// Creates an instance of the YetaWFManager - used for non-site specific threads (e.g., scheduler).
        /// Can only be used once MakeInitialThreadInstance has been used
        /// </summary>
        public static YetaWFManager MakeThreadInstance(SiteDefinition site, HttpContext context, bool forceSync = false) {
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
            public string TimeZone { get; set; }
            public Localize.Formatting.TimeFormatEnum TimeFormat { get; set; }
            public string LanguageId { get; set; }
        }

        /// <summary>
        /// Attaches a YetaWFManager instance to the current thread.
        /// This is only used by console applications.
        /// </summary>
        /// <param name="site">A SiteDefinition object.</param>
        /// <returns>The Manager instance for the current request.</returns>
        public static YetaWFManager MakeInitialThreadInstance(SiteDefinition site) {
            _ManagerThreadInstance = null;
            return MakeThreadInstance(site, null, true);
        }
        /// <summary>
        /// Attaches a YetaWFManager instance to the current thread.
        /// This is only used by console applications.
        /// </summary>
        /// <param name="site">A SiteDefinition object. Is always null as this is not available in console applications.</param>
        /// <param name="context">The HttpContext instance for the current request. If null is specified, local thread storage is used instead of attaching the Manager instance to the HttpRequest.</param>
        /// <param name="forceSync">Specify true to force synchronous requests, otherwise async requests are used.</param>
        /// <returns>The Manager instance for the current request.</returns>
        public static YetaWFManager MakeInitialThreadInstance(SiteDefinition site, HttpContext context, bool forceSync = false) {
            _ManagerThreadInstance = null;
            return MakeThreadInstance(site, context, forceSync);
        }
        /// <summary>
        /// Removes the YetaWF instance from the current thread.
        /// </summary>
        public static void RemoveThreadInstance() {
            _ManagerThreadInstance = null;
        }

        // DOMAIN
        // DOMAIN
        // DOMAIN

        private static void SetRequestedDomain(HttpContext httpContext, string siteDomain) {
#if MVC6
            if (siteDomain == null)
                httpContext.Session.Remove(Globals.Link_ForceSite);
            else
                httpContext.Session.SetString(Globals.Link_ForceSite, siteDomain);
#else
            httpContext.Session[Globals.Link_ForceSite] = siteDomain;
#endif
        }

        /// <summary>
        /// Used by the framework during HTTP request startup to determine the requested domain.
        /// </summary>
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
                ISession session = null;
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

            // check headers, trumps all
            string domain;
            domain = (string)httpContext.Request.Headers["X-Forwarded-Host"] ?? (string)httpContext.Request.Headers["X-Original-Host"];
            if (!string.IsNullOrWhiteSpace(domain))
                siteDomain = domain;

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
        /// The location of the website's root folder (physical, wwwroot on ASP.NET Core MVC).
        /// </summary>
        public static string RootFolder { get; set; }

        /// <summary>
        /// The location of the Website project (Website.csproj) root folder (physical).
        /// </summary>
        /// <remarks>
        /// With MVC5, this is the same as the web site root folder (RootFolder). MVC6+ this is the root folder of the web project.
        /// </remarks>
        public static string RootFolderWebProject {
#if MVC6
            get; set;
#else
            get { return RootFolder; }
#endif
        }
        /// <summary>
        /// The location of the Solution (*.sln) root folder (physical).
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
        /// The sites data folder is located at .\Website\Sites\DataFolder.
        ///
        /// This folder is not publicly accessible on ASP.NET Core MVC.
        /// It is publicly accessible on ASP.NET and must be protected using Web.config files.
        /// </remarks>
        public static string RootSitesFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolderWebProject, Globals.SitesFolder, "DataFolder");
            }
        }

        /// <summary>
        /// Returns the default site name used for this instance of YetaWF.
        /// </summary>
        /// <remarks>The default site is defined in Appsettings.json (Application.P.YetaWF_Core.DEFAULTSITE).</remarks>
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
        /// The location of the Data folder (not site specific).
        /// </summary>
        /// <remarks>
        /// The Data folder is located at .\Website\Data\DataFolder.
        ///
        /// This folder is not publicly accessible on ASP.NET Core MVC.
        /// It is publicly accessible on ASP.NET and must be protected using Web.config files.
        /// </remarks>
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
        /// The location of the product license folder (not site specific). This is used by third-party licensed products only.
        /// </summary>
        /// <remarks>
        /// The License folder is located at .\Website\Data\Licenses.
        ///
        /// This folder is not publicly accessible on ASP.NET Core MVC.
        /// It is publicly accessible on ASP.NET and must be protected using Web.config files.
        /// </remarks>
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
        /// The location of the Vault private folder (not site specific).
        /// </summary>
        /// <remarks>
        /// The Vault private folder is located at .\Website\VaultPrivate.
        ///
        /// This folder is not publicly accessible on ASP.NET Core MVC.
        /// It is publicly accessible on ASP.NET and must be protected using Web.config files.
        /// </remarks>
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
        /// The location of the Vault folder (not site specific).
        /// </summary>
        /// <remarks>
        /// The Vault folder is located at .\Website\wwwroot\Vault on ASP.NET Core and .\Website\Vault on ASP.NET.
        ///
        /// This folder is publicly accessible on ASP.NET Core MVC and ASP.NET.
        /// </remarks>
        public static string VaultFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolder, Globals.VaultFolder);
            }
        }

        /// <summary>
        /// Returns the folder containing the current site's file data.
        /// </summary>
        /// <remarks>
        /// An individual site's file data folder is located at .\Website\Sites\DataFolder\{..siteidentity..}.
        ///
        /// This folder is not publicly accessible on ASP.NET Core MVC.
        /// It is publicly accessible on ASP.NET and must be protected using Web.config files.
        /// </remarks>
        public string SiteFolder {
            get {
                return Path.Combine(RootSitesFolder, CurrentSite.Identity.ToString());
            }
        }
        /// <summary>
        /// Returns the site's custom addons folder.
        /// </summary>
        /// <remarks>
        /// An individual site's file data folder is located at .\Website\wwwroot\AddonsCustom\{..domainname..} on ASP.NET Core and .\Website\AddonsCustom\{..domainname..} on ASP.NET.
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
        private static string _cacheBuster;

        // BUILD
        // BUILD
        // BUILD

        /// <summary>
        /// Defines whether the currently running instance of YetaWF is using additional run-time diagnostics to find issues, typically used during development.
        /// </summary>
        public static bool DiagnosticsMode {
            get {
                if (diagnosticsMode == null) {
                    diagnosticsMode = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "Diagnostics");
                }
                return (bool)diagnosticsMode;
            }
        }
        private static bool? diagnosticsMode = null;

        /// <summary>
        /// Defines whether the currently running instance of YetaWF is a deployed instance or not.
        /// </summary>
        /// <remarks>
        /// A "deployed" instance is not necessarily a Release build, but behaves as though it is.
        ///
        /// A deployed instance is considered to run as a public website with all development features disabled.
        /// TODO: Need an actual list of development features here.
        ///
        /// Appsettings.json (Application.P.YetaWF_Core.Deployed) is used to define whether the site is a deployed site.
        /// </remarks>
        /// <value>true for a deployed site, false otherwise.</value>
        public static bool Deployed {
            get {
                if (deployed == null) {
                    deployed = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "Deployed");
                }
                return (bool)deployed;
            }
        }
        private static bool? deployed = null;

        // SETTINGS
        // SETTINGS
        // SETTINGS

        public static bool CanUseCDN {
            get {
                if (canUseCDN == null) {
                    canUseCDN = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "UseCDN");
                }
                return (bool)canUseCDN;
            }
        }
        private static bool? canUseCDN = null;

        public static bool CanUseCDNComponents {
            get {
                if (canUseCDNComponents == null) {
                    canUseCDNComponents = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "UseCDNComponents");
                }
                return (bool)canUseCDNComponents;
            }
        }
        private static bool? canUseCDNComponents = null;

        public static bool CanUseStaticDomain {
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
        /// Demo mode is enabled/disabled using Appsettings.json (Application.P.YetaWF_Core.Demo).
        /// </remarks>
        public static bool IsDemo {
            get {
                if (isDemo == null)
                    isDemo = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "Demo");
                return (bool)isDemo;
            }
        }
        private static bool? isDemo = null;

        public bool IsDemoUser {
            get {
                if (isDemoUser == null)
                    isDemoUser = Manager.UserRoles != null && Manager.UserRoles.Contains(Resource.ResourceAccess.GetUserDemoRoleId());
                return (bool)isDemoUser;
            }
        }
        private bool? isDemoUser = null;

        // HTTPCONTEXT
        // HTTPCONTEXT
        // HTTPCONTEXT

        /// <summary>
        /// The current site's domain - E.g., softelvdm.com, localhost, etc. or an empty string if it's the default site.
        /// </summary>
        public string SiteDomain { get; set; }

        /// <summary>
        /// The host used to access this website.
        /// </summary>
        public string HostUsed { get; set; }
        /// <summary>
        /// The port used to access this website.
        /// </summary>
        public int HostPortUsed { get; set; }
        /// <summary>
        /// The scheme used to access this website.
        /// </summary>
        public string HostSchemeUsed { get; set; }
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
                    _IsHTTPSite = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "ForceHttp", false);
                return (bool)_IsHTTPSite;
            }
        }
        private static bool? _IsHTTPSite = null;

        /// <summary>
        /// The current site definition.
        /// The current site is identified based on the URL of the current request.
        /// </summary>
        /// <returns>The current site's definitions.</returns>
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
                    qh.Add(Globals.Link_OriginList, Utility.JsonSerialize(originList), Replace: true);
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

        // MANAGERS
        // MANAGERS
        // MANAGERS

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
        /// Returns the Static Page Manager instance.
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
        private StaticPageManager _staticPageManager = null;

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
        /// accidentally use ids that were used in prior request.
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
        public const string UniqueIdTracked = "u";

        public class UniqueIdInfo {
            public string UniqueIdPrefix { get; set; }
            public int UniqueIdPrefixCounter { get; set; }
            public int UniqueIdCounter { get; set; }

            [JsonIgnore]
            public bool IsTracked { get { return UniqueIdPrefix == UniqueIdTracked; } }
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

        private HttpContext _HttpContext = null;

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
#if MVC6
                return Manager.CurrentRequest.Headers["Referer"].ToString();
#else
                return Manager.CurrentRequest.UrlReferrer != null ? Manager.CurrentRequest.UrlReferrer.ToString() : null;
#endif
            }
        }

        public static void SetStaticCacheInfo(HttpContext context) {
            if (YetaWFManager.Deployed && StaticCacheDuration > 0) {
#if MVC6
                context.Response.Headers[HeaderNames.CacheControl] = string.Format("max-age={0}", StaticCacheDuration * 60);
#else
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetMaxAge(new TimeSpan(0, StaticCacheDuration, 0));
#endif
            }
            // add CORS header for static site
#if DEBUG
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
#else
#if MVC6
            SiteDefinition site = SiteDefinition.LoadStaticSiteDefinitionAsync(context.Request.Host.Host).Result;// cached, so ok to use result
            if (site != null)
                context.Response.Headers.Add("Access-Control-Allow-Origin", $"{context.Request.Scheme}://{site.SiteDomain.ToLower()}");
#else
            SiteDefinition site = SiteDefinition.LoadStaticSiteDefinitionAsync(context.Request.Url.Host).Result;// cached, so ok to use result
            if (site != null)
                context.Response.Headers.Add("Access-Control-Allow-Origin", $"{context.Request.Url.Scheme}://{site.SiteDomain.ToLower()}");
#endif
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
                if (_currentRequestUrl == null) {
#if MVC6
                    _currentRequestUrl = UriHelper.GetDisplayUrl(Manager.CurrentRequest);
#else
                    _currentRequestUrl = Manager.CurrentRequest.Url.ToString();
#endif
                }
                return _currentRequestUrl;
            }
            set {
                _currentRequestUrl = value;
            }
        }
        private string _currentRequestUrl = null;

        public void RestartSite(string url = null) {
#if MVC6
            IHostApplicationLifetime applicationLifetime = (IHostApplicationLifetime)ServiceProvider.GetService(typeof(IHostApplicationLifetime));
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
                return string.Format("<title>{0}</title>", Utility.HtmlEncode(title ?? ""));
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
        /// Set when rendering content only (UPS)
        /// </summary>
        public bool RenderContentOnly { get; set; }

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

        /// <summary>
        /// This property can be used by a component rendering package to save information for the current HTTP request.
        /// It is not used by YetaWF.
        /// </summary>
        public object ComponentsData { get; set; }

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
        public string AntiForgeryTokenHTML { get; set; }

        /// <summary>
        /// Defines whether non-site specific data is also imported when importing packages
        /// </summary>
        /// <remarks>Site specific data is always imported</remarks>
        public bool ImportChunksNonSiteSpecifics { get; set; }

        // SKIN
        // SKIN
        // SKIN

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
            ScriptManager.AddVolatileOption("Skin", "MinWidthForCondense", SkinInfo.MinWidthForCondense);

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

        /// <summary>
        /// Contains skin information for the skin used by the current page.
        /// </summary>
        public SkinCollectionInfo SkinInfo { get; private set; }

        /// <summary>
        /// Adds the page's or popup's css classes (the current edit mode, the current page's defined css and other page css).
        /// </summary>
        /// <param name="css">The skin-defined Css class identifying the skin.</param>
        /// <returns>A Css class string.</returns>
        public string PageCss() {
            SkinAccess skinAccess = new SkinAccess();
            PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
            string s = pageSkin.Css;
            s = CssManager.CombineCss(s, ModeCss);// edit/display mode (doesn't change in same Unified page set)
            s = CssManager.CombineCss(s, HaveUser ? "yUser" : "yAnonymous");// add whether we have an authenticated user (doesn't change in same Unified page set)
            s = CssManager.CombineCss(s, IsInPopup ? "yPopup" : "yPage"); // popup or full page (doesn't change in same Unified page set)
            switch (UnifiedMode) { // unified page set mode (if any) (doesn't change in same Unified page set)
                case PageDefinition.UnifiedModeEnum.None:
                    break;
                case PageDefinition.UnifiedModeEnum.HideDivs:
                    s = CssManager.CombineCss(s, "yUnifiedHideDivs");
                    break;
                case PageDefinition.UnifiedModeEnum.ShowDivs:
                    s = CssManager.CombineCss(s, "yUnifiedShowDivs");
                    break;
                case PageDefinition.UnifiedModeEnum.DynamicContent:
                    s = CssManager.CombineCss(s, "yUnifiedDynamicContent");
                    break;
                case PageDefinition.UnifiedModeEnum.SkinDynamicContent:
                    s = CssManager.CombineCss(s, "yUnifiedSkinDynamicContent");
                    break;
            }
            if (Manager.SkinInfo.UsingBootstrap && Manager.SkinInfo.UseDefaultBootstrap) {
                string skin = Manager.CurrentPage.BootstrapSkin;
                if (string.IsNullOrWhiteSpace(skin))
                    skin = Manager.CurrentSite.BootstrapSkin;
                if (!string.IsNullOrWhiteSpace(skin)) {
                    skin = skin.ToLower().Replace(' ', '-');
                    s = CssManager.CombineCss(s, $"ySkin-bs-{skin}");
                }
            }
            string cssClasses = CurrentPage.GetCssClass(); // get page specific Css (once only, used 2x)
            if (UnifiedMode == PageDefinition.UnifiedModeEnum.DynamicContent || UnifiedMode == PageDefinition.UnifiedModeEnum.SkinDynamicContent) {
                // add the extra page css class and generated page specific Css via javascript to body tag (used for dynamic content)
                ScriptBuilder sb = new Support.ScriptBuilder();
                sb.Append("document.body.setAttribute('data-pagecss', '{0}');", Utility.JserEncode(cssClasses));
                Manager.ScriptManager.AddLast(sb.ToString());
            }
            return CssManager.CombineCss(s, cssClasses);
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
        public TYPE GetPackageData<TYPE>(string areaName) {
            if (_packageData == null)
                return default(TYPE);
            return (TYPE) _packageData[areaName];
        }
        private Dictionary<string, object> _packageData = null;

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
                // Timezones don't use the same ids between Windows and other environments (nothing is ever easy)
                // We store Windows Id so we translate on non-windows environments
                string tz = UserSettings.GetProperty<string>("TimeZone");
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

        /// <summary>
        /// Returns whether requests must be made synchronously (i.e., no async).
        /// </summary>
        /// <remarks>
        /// At certain times, particularly during ASP.NET rendering, requests cannot be made asynchronously.
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
