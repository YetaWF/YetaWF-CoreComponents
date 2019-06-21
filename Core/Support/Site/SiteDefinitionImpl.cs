/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Extensions;
using YetaWF.Core.Image;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Site {

    public partial class SiteDefinition : IInitializeApplicationStartup {

        // IInitializeApplicationStartup
        public const string ImageType = "YetaWF_Core_FavIcon";
        public const string LargeImageType = "YetaWF_Core_FavIconLrg";

        public Task InitializeApplicationStartupAsync() {
            ImageSupport.AddHandler(ImageType, GetBytesAsync: RetrieveImageAsync);
            ImageSupport.AddHandler(LargeImageType, GetBytesAsync: RetrieveLargeImageAsync);
            return Task.CompletedTask;
        }
        private async Task<ImageSupport.GetImageInBytesInfo> RetrieveImageAsync(string name, string location) {
            if (!string.IsNullOrWhiteSpace(location)) return new ImageSupport.GetImageInBytesInfo();
            if (string.IsNullOrWhiteSpace(name)) return new ImageSupport.GetImageInBytesInfo();
            SiteDefinition site = await SiteDefinition.LoadSiteDefinitionAsync(name);
            if (site == null) return new ImageSupport.GetImageInBytesInfo();
            if (site.FavIcon_Data == null || site.FavIcon_Data.Length == 0) return new ImageSupport.GetImageInBytesInfo();
            return new ImageSupport.GetImageInBytesInfo {
                Content = site.FavIcon_Data,
                Success = true,
            };
        }
        private async Task<ImageSupport.GetImageInBytesInfo> RetrieveLargeImageAsync(string name, string location) {
            if (!string.IsNullOrWhiteSpace(location)) return new ImageSupport.GetImageInBytesInfo();
            if (string.IsNullOrWhiteSpace(name)) return new ImageSupport.GetImageInBytesInfo();
            SiteDefinition site = await SiteDefinition.LoadSiteDefinitionAsync(name);
            if (site == null) return new ImageSupport.GetImageInBytesInfo();
            if (site.FavIconLrg_Data == null || site.FavIconLrg_Data.Length == 0) return new ImageSupport.GetImageInBytesInfo();
            return new ImageSupport.GetImageInBytesInfo {
                Content = site.FavIconLrg_Data,
                Success = true,
            };
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
        /// <returns>A fully qualified Url.</returns>
        /// <remarks>
        /// This method is used to format a fully qualified Url, including http(s)://, domain, port if necessary, and also takes into consideration whether the site is
        /// using IIS Express, in which case Localhost could be used.
        ///
        /// RealDomain and ForceDomain are rarely used and usually only in YetaWF Core code as they are used to redirect to another site hosted by the same YetaWF instance.
        /// ForceDomain is used while creating a new site only and should not otherwise be used.
        /// </remarks>
        public string MakeUrl(string pathAndQs = null, PageDefinition.PageSecurityType PagePageSecurity = PageDefinition.PageSecurityType.Any,
                string RealDomain = null) {
            return MakeFullUrl(pathAndQs, DetermineSchema(PagePageSecurity), RealDomain: RealDomain);
        }
        /// <summary>
        /// Determine schema used based on page and site settings.
        /// </summary>
        /// <param name="PagePageSecurity">The current page's security settings.</param>
        /// <returns>Security settings.</returns>
        public PageDefinition.PageSecurityType DetermineSchema(PageDefinition.PageSecurityType PagePageSecurity = PageDefinition.PageSecurityType.Any) {
            PageDefinition.PageSecurityType securityType = PagePageSecurity;// assume the page decides the security type
            if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite) {
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
            } else {
                if (YetaWFManager.IsHTTPSite)
                    securityType = PageDefinition.PageSecurityType.httpOnly;
                else
                    securityType = PageDefinition.PageSecurityType.Any;
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
        /// <returns>A fully qualified Url.</returns>
        /// <remarks>
        /// This method is used to format a fully qualified Url, including http(s)://, domain, port if necessary, and also takes into consideration whether the site is
        /// using IIS Express, in which case Localhost could be used.
        ///
        /// RealDomain and ForceDomain are rarely used and usually only in YetaWF Core code as they are used to redirect to another site hosted by the same YetaWF instance.
        /// ForceDomain is used while creating a new site only and should not otherwise be used.
        /// </remarks>
        public string MakeFullUrl(string pathAndQs = null, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any, string RealDomain = null) {
            if (string.IsNullOrWhiteSpace(pathAndQs))
                pathAndQs = "/";
            if (pathAndQs.IsAbsoluteUrl()) {
                if (RealDomain != null)
                    throw new InternalError("Can't use RealDomain with full URL");
                return pathAndQs;
            }
            if (!pathAndQs.StartsWith("/"))
                throw new InternalError("All pages must start with /");
            pathAndQs = pathAndQs.Substring(1);

            UriBuilder uri;
            if (!string.IsNullOrWhiteSpace(RealDomain) && !Manager.IsLocalHost) {
                // if we're not using localhost, we can simply access the other domain
                uri = new UriBuilder(SecurityType == PageDefinition.PageSecurityType.httpsOnly ? "https" : "http", RealDomain);
            } else {
                SiteDefinition currentSite = Manager.CurrentSite;
                string host = Manager.HostUsed;
                int port = -1;
                string scheme = Manager.HostSchemeUsed;
                if (SecurityType == PageDefinition.PageSecurityType.httpsOnly) {
                    scheme = "https";
                    if (Manager.IsLocalHost) {
                        if (Manager.HostPortUsed != 443)
                            port = Manager.HostPortUsed;
                    } else {
                        if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite && currentSite.EnforceSitePort) {
                            if (currentSite.EnforceSiteUrl)
                                host = currentSite.SiteDomain;
                            if (currentSite.PortNumberSSLEval != 443)
                                port = currentSite.PortNumberSSLEval;
                        } else
                            port = Manager.HostPortUsed;
                    }
                } else if (SecurityType == PageDefinition.PageSecurityType.httpOnly) {
                    scheme = "http";
                    if (Manager.IsLocalHost) {
                        if (Manager.HostPortUsed != 80)
                            port = Manager.HostPortUsed;
                    } else {
                        if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite && currentSite.EnforceSitePort) {
                            if (currentSite.EnforceSiteUrl)
                                host = currentSite.SiteDomain;
                            if (currentSite.PortNumberSSLEval != 80)
                                port = currentSite.PortNumberSSLEval;
                        } else
                            port = Manager.HostPortUsed;
                    }
                } else {
                    if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite && currentSite.EnforceSitePort) {
                        if (currentSite.EnforceSiteUrl)
                            host = currentSite.SiteDomain;
                        if (scheme == "https") {
                            if (currentSite.PortNumberEval != 443)
                                port = currentSite.PortNumberEval;
                        } else {
                            if (currentSite.PortNumberEval != 80)
                                port = currentSite.PortNumberEval;
                        }
                    } else
                        port = Manager.HostPortUsed;
                }
                if (port != -1)
                    uri = new UriBuilder(scheme, host, port);
                else
                    uri = new UriBuilder(scheme, host);
                if (Manager.IsLocalHost && !string.IsNullOrWhiteSpace(RealDomain)) {
                    pathAndQs += (pathAndQs.Contains("?")) ? "&" : "?";
                    pathAndQs += string.Format("{0}={1}", Globals.Link_ForceSite, Utility.UrlEncodeArgs(RealDomain));
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
            if (YetaWFManager.IsHTTPSite)
                secure = false;
            if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite) {
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
            }
            UriBuilder uri;
            if (Manager.IsLocalHost) {
                uri = new UriBuilder(secure ? "https" : "http", Manager.HostUsed, Manager.HostPortUsed);
            } else {
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

        //$$$ resolve favicon caching
        public string GetFavIconLinks(string imageType, byte[] data, string name, string imageTypeLarge, byte[] dataLrg, string nameLrg) {
            HtmlBuilder hb = new HtmlBuilder();

            string url;
            // Seriously? All this for a favicon? Who thought this was a good idea?
            if (data != null && data.Length > 0) {
                // std
                url = ImageHTML.FormatUrl(imageType, null, name, 32, 32, Stretch: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='32x32' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageType, null, name, 16, 16, Stretch: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='16x16' />", Utility.HtmlEncode(url));
                // apple-touch
                url = ImageHTML.FormatUrl(imageType, null, name, 57, 57, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='57x57' href='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageType, null, name, 60, 60, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='60x60' href='{0}' />", Utility.HtmlEncode(url));
            }
            if (!string.IsNullOrWhiteSpace(imageTypeLarge) && dataLrg != null && dataLrg.Length > 0) {
                // std
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 196, 196, Stretch: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='196x196' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 96, 96, Stretch: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='96x96' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 128, 128, Stretch: true);
                hb.Append("<link rel='icon' type='image/png' href='{0}' sizes='128x128' />", Utility.HtmlEncode(url));
                // apple-touch
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 114, 114, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='114x114' href='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 72, 72, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='72x72' href='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 144, 144, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='144x144' href='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 120, 120, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='120x120' href='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 76, 76, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='76x76' href='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 152, 152, Stretch: true);
                hb.Append("<link rel='apple-touch-icon' sizes='152x152' href='{0}' />", Utility.HtmlEncode(url));
                // msbs
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 144, 144, Stretch: true);
                hb.Append("<meta name='msapplication-TileImage' content='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 70, 70, Stretch: true);
                hb.Append("<meta name='msapplication-square70x70logo' content='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 150, 150, Stretch: true);
                hb.Append("<meta name='msapplication-square150x150logo' content='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 310, 150, Stretch: true);
                hb.Append("<meta name='msapplication-wide310x150logo' content='{0}' />", Utility.HtmlEncode(url));
                url = ImageHTML.FormatUrl(imageTypeLarge, null, nameLrg, 310, 310, Stretch: true);
                hb.Append("<meta name='msapplication-square310x310logo' content='{0}' />", Utility.HtmlEncode(url));
                hb.Append("<meta name='application-name' content='{0}'/>", SiteDomain);
                hb.Append("<meta name='msapplication-TileColor' content='#FFFFFF' />");
            }
            return hb.ToString();
        }

        // LOAD/SAVE
        // LOAD/SAVE
        // LOAD/SAVE

        // these must be provided during app startup
        public static Func<string, Task<SiteDefinition>> LoadSiteDefinitionAsync { get; set; }
        public static Func<SiteDefinition, Task> SaveSiteDefinitionAsync { get; set; }
        public static Func<Task> RemoveSiteDefinitionAsync { get; set; }
        public static Func<int, int, List<DataProviderSortInfo>, List<DataProviderFilterInfo>, Task<DataProviderGetRecords<SiteDefinition>>> GetSitesAsync { get; set; }
        public static Func<string, Task<SiteDefinition>> LoadStaticSiteDefinitionAsync { get; set; }
        public static Func<string, Task<SiteDefinition>> LoadTestSiteDefinitionAsync { get; set; }

        public async Task SaveAsync() {
            await SiteDefinition.SaveSiteDefinitionAsync(this);
        }
        public async Task AddNewAsync() {
            // Add a new site
            await SiteDefinition.SaveSiteDefinitionAsync(this);
            // we also have to create all site specific data - data providers expect the current site to be active so we have to switch temporarily
            SiteDefinition origSite = Manager.CurrentSite;
            Manager.CurrentSite = this;// new site
            // create all site specific data
            await Package.AddSiteDataAsync();
            // restore original site
            Manager.CurrentSite = origSite;
        }
        public async Task RemoveAsync() {
            await SiteDefinition.RemoveSiteDefinitionAsync();
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
        /// Even once the initial install process has ended, INITIAL_INSTALL still returns true to indicate that a site restart (including all instances) is needed.
        /// Some data providers are still marked as not installed until the site is restarted.
        /// </remarks>
        public static async Task RemoveInitialInstallAsync() {
            WebConfigHelper.SetValue<string>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "INITIAL-INSTALL", "0");
            await WebConfigHelper.SaveAsync();
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

