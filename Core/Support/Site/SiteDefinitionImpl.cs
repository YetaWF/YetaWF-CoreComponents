using System;
using System.Collections.Generic;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Extensions;
using YetaWF.Core.Image;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Site {

    public partial class SiteDefinition : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        public const string ImageType = "YetaWF_Core_FavIcon";
        public const string LargeImageType = "YetaWF_Core_FavIconLrg";

        public void InitializeApplicationStartup() {
            ImageSupport.AddHandler(ImageType, GetBytes: RetrieveImage);
            ImageSupport.AddHandler(LargeImageType, GetBytes: RetrieveLargeImage);
        }
        private bool RetrieveImage(string name, string location, out byte[] content) {
            content = null;
            if (!string.IsNullOrWhiteSpace(location)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            SiteDefinition site = SiteDefinition.LoadSiteDefinition(name);
            if (site == null) return false;
            if (site.FavIcon_Data == null || site.FavIcon_Data.Length == 0) return false;
            content = site.FavIcon_Data;
            return true;
        }
        private bool RetrieveLargeImage(string name, string location, out byte[] content) {
            content = null;
            if (!string.IsNullOrWhiteSpace(location)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            SiteDefinition site = SiteDefinition.LoadSiteDefinition(name);
            if (site == null) return false;
            if (site.FavIconLrg_Data == null || site.FavIconLrg_Data.Length == 0) return false;
            content = site.FavIconLrg_Data;
            return true;
        }

        // URLS
        // URLS
        // URLS

        /// <summary>
        /// Turns a local Url to be used on the current page into a fully qualified Url.
        /// </summary>
        /// <param name="pathAndQs">Local or remote Url. If nothing is specified, "/" is the default.</param>
        /// <param name="PagePageSecurity">Desired page security. This is used as a suggestion as site and page definition will determine the final page security.</param>
        /// <param name="RealDomain">Optional. Defines the domain name to be used to build the fully qualified Url. This can only be used if pathAndQs defines a local Url (starting with /).
        /// This can be used to build a Url for another domain name than the current domain name.</param>
        /// <param name="ForceDomain">Optional. Defines the domain name to be used as querystring argument (!Domain=) to force access to the specified Url.
        /// This is used while creating a new site and the site can not yet be accessed because it hasn't been defined in IIS or the hosts file.</param>
        /// <returns>A fully qualified Url.</returns>
        /// <remarks>
        /// This method is used to format a fully qualified Url, including http(s)://, domain, port if necessary, and also takes into consideration whether the site is
        /// using IIS Express, in which case Localhost could be used.
        ///
        /// RealDomain and ForceDomain are rarely used and usually only in YetaWF Core code as they are used to redirect to another site hosted by the same YetaWF instance.
        /// ForceDomain is used while creating a new site only and should not otherwise be used.
        /// </remarks>
        public string MakeUrl(string pathAndQs = null, PageDefinition.PageSecurityType PagePageSecurity = PageDefinition.PageSecurityType.Any,
                string RealDomain = null, string ForceDomain = null) {
            return MakeFullUrl(pathAndQs, DetermineSchema(PagePageSecurity), RealDomain: RealDomain, ForceDomain: ForceDomain);
        }
        /// <summary>
        /// Determine schema used based on page and site settings.
        /// </summary>
        /// <param name="PagePageSecurity">The current page's security settings.</param>
        /// <returns>Security settings.</returns>
        public PageDefinition.PageSecurityType DetermineSchema(PageDefinition.PageSecurityType PagePageSecurity = PageDefinition.PageSecurityType.Any) {
            PageDefinition.PageSecurityType securityType = PagePageSecurity;// assume the page decides the security type
            switch (PageSecurity) {
                case PageSecurityType.AsProvided:
                    if (securityType != PageDefinition.PageSecurityType.httpsOnly)
                        securityType = PageDefinition.PageSecurityType.Any;
                    break;
                case PageSecurityType.AsProvidedAnonymous_LoggedOnhttps:
                    if (Manager.HaveUser) {
                        if (securityType == PageDefinition.PageSecurityType.Any)
                            securityType = PageDefinition.PageSecurityType.httpsOnly;
                    }
                    break;
                case PageSecurityType.AsProvidedLoggedOn_Anonymoushttp:
                    if (!Manager.HaveUser) {
                        if (securityType == PageDefinition.PageSecurityType.Any)
                            securityType = PageDefinition.PageSecurityType.httpOnly;
                    }
                    break;
                case PageSecurityType.UsePageModuleSettings:
                    break;
                case PageSecurityType.NoSSLOnly:
                    securityType = PageDefinition.PageSecurityType.httpOnly;
                    break;
                case PageSecurityType.NoSSLOnlyAnonymous_LoggedOnhttps:
                    if (Manager.HaveUser)
                        securityType = PageDefinition.PageSecurityType.httpsOnly;
                    else
                        securityType = PageDefinition.PageSecurityType.httpOnly;
                    break;
                case PageSecurityType.SSLOnly:
                    securityType = PageDefinition.PageSecurityType.httpsOnly;
                    break;
            }
            return securityType;
        }
        /// <summary>
        /// Turns a local Url into a fully qualified Url.
        /// </summary>
        /// <param name="pathAndQs">Local or remote Url. If nothing is specified, "/" is the default.</param>
        /// <param name="Security">Desired page security.</param>
        /// <param name="RealDomain">Optional. Defines the domain name to be used to build the fully qualified Url. This can only be used if pathAndQs defines a local Url (starting with /).
        /// This can be used to build a Url for another domain name than the current domain name.</param>
        /// <param name="ForceDomain">Optional. Defines the domain name to be used as querystring argument (!Domain=) to force access to the specified Url.
        /// This is used while creating a new site and the site can not yet be accessed because it hasn't been defined in IIS or the hosts file.</param>
        /// <returns>A fully qualified Url.</returns>
        /// <remarks>
        /// This method is used to format a fully qualified Url, including http(s)://, domain, port if necessary, and also takes into consideration whether the site is
        /// using IIS Express, in which case Localhost could be used.
        ///
        /// RealDomain and ForceDomain are rarely used and usually only in YetaWF Core code as they are used to redirect to another site hosted by the same YetaWF instance.
        /// ForceDomain is used while creating a new site only and should not otherwise be used.
        /// </remarks>
        public string MakeFullUrl(string pathAndQs = null, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any, string RealDomain = null, string ForceDomain = null) {
            if (!string.IsNullOrWhiteSpace(ForceDomain) && !string.IsNullOrWhiteSpace(RealDomain))
                throw new InternalError("Can't use ForceDomain and RealDomain at the same time");
            if (string.IsNullOrWhiteSpace(pathAndQs))
                pathAndQs = "/";
            if (pathAndQs.IsAbsoluteUrl()) {
                if (ForceDomain != null || RealDomain != null)
                    throw new InternalError("Can't use ForceDomain or RealDomain with full URL");
                return pathAndQs;
            }
            if (!pathAndQs.StartsWith("/"))
                throw new InternalError("All pages must start with /");
            pathAndQs = pathAndQs.Substring(1);

            if (SecurityType == PageDefinition.PageSecurityType.Any)
                SecurityType = PageDefinition.PageSecurityType.httpOnly;

            UriBuilder uri;
            if (!string.IsNullOrWhiteSpace(RealDomain) && Manager.HostUsed != "localhost") {
                // if we're not using localhost, we can simply access the other domain
                uri = new UriBuilder(SecurityType == PageDefinition.PageSecurityType.httpsOnly ? "https" : "http", RealDomain);
            } else {
                string host = Manager.CurrentSite.SiteDomain;
                int port = -1;
                string scheme = "http";
                if (SecurityType == PageDefinition.PageSecurityType.httpsOnly) {
                    scheme = "https";
                    if (Manager.IsLocalHost) {
                        host = Manager.HostUsed;
                        if (Manager.HostPortUsed != 443)
                            port = Manager.HostPortUsed;
                    } else if (Manager.CurrentSite.PortNumberSSLEval != 443)
                        port = Manager.CurrentSite.PortNumberSSLEval;
                } else {
                    if (Manager.IsLocalHost) {
                        host = Manager.HostUsed;
                        if (Manager.HostPortUsed != 80)
                            port = Manager.HostPortUsed;
                    } else if (Manager.CurrentSite.PortNumberEval != 80)
                        port = Manager.CurrentSite.PortNumberEval;
                    else {
                        // the only time we preserve the user provided domain name is when we don't switch http/https and the port number wasn't specified/forced
                        // this is mostly a "just in case" measure to allow access to a site even if its domain name doesn't match
                        host = Manager.HostUsed;
                        if (Manager.HostPortUsed != 80 && Manager.HostSchemeUsed == "http")
                            port = Manager.HostPortUsed;
                    }
                }
                if (port != -1)
                    uri = new UriBuilder(scheme, host, port);
                else
                    uri = new UriBuilder(scheme, host);
                if (!string.IsNullOrWhiteSpace(ForceDomain)) {
                    pathAndQs += (pathAndQs.Contains("?")) ? "&" : "?";
                    pathAndQs += string.Format("{0}={1}", Globals.Link_ForceSite, YetaWFManager.UrlEncodeArgs(ForceDomain));
                } else if (!string.IsNullOrWhiteSpace(RealDomain)) {
                    pathAndQs += (pathAndQs.Contains("?")) ? "&" : "?";
                    pathAndQs += string.Format("{0}={1}", Globals.Link_ForceSite, YetaWFManager.UrlEncodeArgs(RealDomain));
                }
            }
            return uri.ToString() + pathAndQs;
        }
        /// <summary>
        /// Used to retrieve a fully qualified Url for the current domain (without page or querystring component).
        /// </summary>
        /// <param name="Secure">true for https://, false for http://</param>
        /// <returns>
        /// The site's default page security defined using the SiteDefinition.PageSecurity property is honored.
        /// </returns>
        public string MakeRealUrl(bool Secure = false) {
            bool secure = Secure;
            switch (PageSecurity) {
                case PageSecurityType.AsProvided:
                case PageSecurityType.UsePageModuleSettings:
                    break;
                case PageSecurityType.AsProvidedAnonymous_LoggedOnhttps:
                    if (Manager.HaveUser)
                        secure = true;
                    break;
                case PageSecurityType.AsProvidedLoggedOn_Anonymoushttp:
                    if (!Manager.HaveUser)
                        secure = false;
                    break;
                case PageSecurityType.NoSSLOnly:
                    secure = false;
                    break;
                case PageSecurityType.NoSSLOnlyAnonymous_LoggedOnhttps:
                    secure = Manager.HaveUser;
                    break;
                case PageSecurityType.SSLOnly:
                    secure = true;
                    break;
            }
            UriBuilder uri;
            if (secure) {
                if (Manager.CurrentSite.PortNumberSSLEval == 443)
                    uri = new UriBuilder("https", Manager.CurrentSite.SiteDomain);
                else
                    uri = new UriBuilder("https", Manager.CurrentSite.SiteDomain, Manager.CurrentSite.PortNumberSSLEval);
            } else {
                if (Manager.CurrentSite.PortNumberEval == 80)
                    uri = new UriBuilder("http", Manager.CurrentSite.SiteDomain);
                else
                    uri = new UriBuilder("http", Manager.CurrentSite.SiteDomain, Manager.CurrentSite.PortNumberEval);
            }
            return uri.ToString();
        }

        // Helpers
        public string CurrentYear {
            get {
                return Localize.Formatting.FormatDateTimeYear(DateTime.UtcNow);
            }
        }

        // FAVICON
        // FAVICON
        // FAVICON

        public string GetFavIconLinks(byte[] data, string name, byte[] dataLrg, string nameLrg) {
            HtmlBuilder hb = new HtmlBuilder();

            string url;
            // Seriously? All this for a favicon? Who thought this was a good idea?
            if (data != null && data.Length > 0) {
                // std
                url = ImageHelper.FormatUrl(SiteDefinition.ImageType, null, name, 32, 32, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='32x32' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.ImageType, null, name, 16, 16, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='16x16' />", YetaWFManager.HtmlEncode(url));
                // apple-touch
                url = ImageHelper.FormatUrl(SiteDefinition.ImageType, null, name, 57, 57, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='57x57' href='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.ImageType, null, name, 60, 60, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='60x60' href='{0}' />", YetaWFManager.HtmlEncode(url));
            }
            if (dataLrg != null && dataLrg.Length > 0) {
                // std
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 196, 196, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='196x196' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 96, 96, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='96x96' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 128, 128, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='128x128' />", YetaWFManager.HtmlEncode(url));
                // apple-touch
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 114, 114, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='114x114' href='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 72, 72, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='72x72' href='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 144, 144, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='144x144' href='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 120, 120, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='120x120' href='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 76, 76, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='76x76' href='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 152, 152, Stretch: true, ForceHttpHandler: true);
                hb.Append("<link rel='apple-touch-icon' sizes='152x152' href='{0}' />", YetaWFManager.HtmlEncode(url));
                // msbs
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 144, 144, Stretch: true, ForceHttpHandler: true);
                hb.Append("<meta name='msapplication-TileImage' content='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 70, 70, Stretch: true, ForceHttpHandler: true);
                hb.Append("<meta name='msapplication-square70x70logo' content='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 150, 150, Stretch: true, ForceHttpHandler: true);
                hb.Append("<meta name='msapplication-square150x150logo' content='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 310, 150, Stretch: true, ForceHttpHandler: true);
                hb.Append("<meta name='msapplication-wide310x150logo' content='{0}' />", YetaWFManager.HtmlEncode(url));
                url = ImageHelper.FormatUrl(SiteDefinition.LargeImageType, null, nameLrg, 310, 310, Stretch: true, ForceHttpHandler: true);
                hb.Append("<meta name='msapplication-square310x310logo' content='{0}' />", YetaWFManager.HtmlEncode(url));
                hb.Append("<meta name='application-name' content='{0}'/>", SiteDomain);
                hb.Append("<meta name='msapplication-TileColor' content='#FFFFFF' />");
            }
            return hb.ToString();
        }

        // LOAD/SAVE
        // LOAD/SAVE
        // LOAD/SAVE

        // these must be provided during app startup
        public static Func<string, SiteDefinition> LoadSiteDefinition { get; set; }
        public static Func<SiteDefinition, bool> SaveSiteDefinition { get; set; }
        public static Action RemoveSiteDefinition { get; set; }
        public static Func<int, int, List<DataProviderSortInfo>, List<DataProviderFilterInfo>, SitesInfo> GetSites { get; set; }
        public static Func<string, SiteDefinition> LoadStaticSiteDefinition { get; set; }

        public class SitesInfo {
            public List<SiteDefinition> Sites { get; set; }
            public int Total { get; set; }
        }

        public void Save(out bool restart) {
            restart = SiteDefinition.SaveSiteDefinition(this);
        }
        public void AddNew() {
            // Add a new site
            if (SiteDefinition.SaveSiteDefinition(this))
                throw new InternalError("SaveSiteDefinition implementation error - restart required");
            // we also have to create all site specific data - data providers expect the current site to be active so we have to switch temporarily
            SiteDefinition origSite = Manager.CurrentSite;
            Manager.CurrentSite = this;// new site
            // create all site specific data
            Package.AddSiteData();
            // restore original site
            Manager.CurrentSite = origSite;
        }
        public void Remove() {
            SiteDefinition.RemoveSiteDefinition();
        }

        // INITIAL INSTALL
        // INITIAL INSTALL
        // INITIAL INSTALL

        /// <summary>
        /// Defines whether the current application instance started as an initial install.
        /// </summary>
        public static bool INITIAL_INSTALL {
            get {
                if (_initial_install == null) {
                    _initial_install = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "INITIAL-INSTALL");
                }
                return (bool)_initial_install;
            }
        }
        /// <summary>
        /// Call when the initial install process ends.
        /// </summary>
        /// <remarks>
        /// Even once the initial install process has ended, INITIAL_INSTALL still returns true to indicate that a site restart is needed.
        /// Some data providers are still marked as not installed until the site is restarted.
        /// </remarks>
        public static void RemoveInitialInstall() {
            WebConfigHelper.SetValue<string>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "INITIAL-INSTALL", "0");
            WebConfigHelper.Save();
            _initial_install_ended = true;
        }
        private static bool? _initial_install = null;
        private static bool _initial_install_ended = false;

        /// <summary>
        /// Defines whether the current application instance started as an initial install, but the initial install process has ended.
        /// </summary>
        public static bool INITIAL_INSTALL_ENDED {
            get {
                return _initial_install_ended;
            }
        }
    }
}

