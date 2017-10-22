﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Language;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.SendEmail;
using YetaWF.Core.Serializers;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Site {

    public enum TabStyleEnum {
        [EnumDescription("JQuery", "JQuery-UI Tab Controls")]
        JQuery = 0,
        [EnumDescription("Kendo", "Kendo UI Core Tab Controls")]
        Kendo = 1,
    }
    public enum PageSecurityType {
        [EnumDescription("As Provided in Url", "As Provided in Url - This can be overridden by pages using an explicit mode")]
        AsProvided = 0,
        [EnumDescription("As Provided (Anonymous) - https:// (Logged on)", "As Provided in Url for anonymous users - Logged on users default to https:// - This can be overridden by pages using an explicit mode")]
        AsProvidedAnonymous_LoggedOnhttps = 1,
        [EnumDescription("As Provided (Logged on) - http:// (Anonymous)", "As Provided in Url for logged on users - Anonymous users default to http:// - This can be overridden by pages using an explicit mode")]
        AsProvidedLoggedOn_Anonymoushttp = 2,
        [EnumDescription("Page/Module Settings", "Use Page or Module Property Settings")]
        UsePageModuleSettings = 10,
        [EnumDescription("https:// Only", "Use https:// Only")]
        SSLOnly = 11,
        [EnumDescription("http:// Only", "Use http:// Only")]
        NoSSLOnly = 12,
        [EnumDescription("http:// (Anonymous) - https:// (Logged on)", "http:// for anonymous users - https:// for logged on users")]
        NoSSLOnlyAnonymous_LoggedOnhttps = 14,
    }
    public enum JSLocationEnum {
        [EnumDescription("Top of Page", "All JavaScript files are included at the top of the page (in the <HEAD> section) - This reduces the \"Flash of Unformatted Content\" effect - This is the preferred setting")]
        Top = 0,
        [EnumDescription("Bottom of Page", "All JavaScript files are included at the bottom of the page (right in front of the </BODY> tag) - This may increase the \"Flash of Unformatted Content\" effect")]
        Bottom = 1,
    }
    public enum CssLocationEnum {
        [EnumDescription("Top of Page", "All Css files are included at the top of the page (in the <HEAD> section)")]
        Top = 0,
        [EnumDescription("Bottom of Page", "All Css files are included at the bottom of the page (right in front of the </BODY> tag)")]
        Bottom = 1,
    }

    [Trim]
    public partial class SiteDefinition {

        public static readonly int SiteIdentitySeed = 1000; // the id of the first site

        public const int MaxCopyright = 100;
        public const int MaxSiteName = 40;
        public const int MaxSiteDomain = 80;
        public const int MaxGoogleVerification = 1000;
        public const int MaxAnalytics = 1000;
        public const int MaxMeta = 1000;
        public const int MaxCountry = 50;
        public const int MaxHead = 1000;
        public const int MaxBodyTop = 1000;
        public const int MaxBodyBottom = 1000;

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public SiteDefinition() {
            SiteDomain = "(yourdomain.com)";
            OriginalSiteDomain = null;
            SiteName = "(Your Site Name)";
            PortNumber = PortNumberSSL = -1;
            Localization = true;
            DefaultLanguageId = MultiString.DefaultLanguage;
            AllowAnonymousUsers = true;
            Locked = false;
            LockedExternal = false;
            LockedForIP = null;
            LockedExternalForIP = null;
            AllowPopups = true;
            CssNoTooltips = "linkpreview-show peelback";
            FavIcon_Data = new byte[0];
            FavIconLrg_Data = new byte[0];
            Country = Globals.DefaultCountry;
            Currency = CurrencyISO4217Helper.Currency.DefaultId;
            CurrencyFormat = Globals.DefaultCurrencyFormat;
            CurrencyDecimals = CurrencyISO4217Helper.Currency.DefaultMinorUnit;

            AllowCacheUse = true;
            Compression = true;
            CompressCSSFiles = true;
            BundleCSSFiles = true;
            BundleCSSFilesContent = false;
            CompressJSFiles = true;
            BundleJSFiles = true;
            BundleJSFilesContent = false;
            JSLocation = JSLocationEnum.Top;
            CssLocation = CssLocationEnum.Top;

            Copyright = "YetaWF.com - © Copyright <<Year>> Softel vdm, Inc.";

            EmailDebug = false;
            AdminEmail = "(your@email.com)";
            SMTP = new SMTPServer() { Server = "(yourdomain.com)", };

            HomePageUrl = "/";
            EnforceSiteUrl = false;
            PageSecurity = PageSecurityType.AsProvided;
            UnsupportedBrowserUrl = "/Maintenance/Unsupported Browser.html";
            LoginUrl = "/";
            ExternalAccountSetupUrl = "/";

            SiteMapPriority = PageDefinition.SiteMapPrioritySiteEnum.Medium;
            DefaultChangeFrequency = PageDefinition.ChangeFrequencySiteEnum.Weekly;

            ReferencedModules = new SerializableList<ModuleDefinition.ReferencedModule>();

            SelectedSkin = new SkinDefinition {
                Collection = SkinAccess.FallbackSkinCollectionName,
                FileName = SkinAccess.FallbackPageFileName,
            };
            SelectedPopupSkin = new SkinDefinition {
                Collection = SkinAccess.FallbackPopupSkinCollectionName,
                FileName = SkinAccess.FallbackPopupFileName,
            };
            jQueryUISkin = null;
            KendoUISkin = null;
            TabStyle = TabStyleEnum.JQuery;

            OriginalUseCDN = UseCDN = false;
            OriginalCDNUrl = CDNUrl = null;
            OriginalCDNSiteFiles = CDNSiteFiles = false;
            OriginalCDNVault = CDNVault = false;
            OriginalCDNScriptsContent = CDNScriptsContent = false;
            OriginalCDNAddons = CDNAddons = false;
            OriginalCDNAddonsCustom = CDNAddonsCustom = false;
            OriginalCDNAddonsBundles = CDNAddonsBundles = false;
            OriginalCDNFileImage = CDNFileImage = false;
        }

        [Data_Identity]
        [Category("Variables"), Caption("Site Id"), Description("The id associated with your site, generated by YetaWF when the site is created")]
        [UIHint("IntValue"), ReadOnly]
        [Copy]
        public int Identity { get; set; }

        public virtual List<string> CategoryOrder { get { return new List<string> { "Site", "Pages", "CDN", "Email", "Urls", "References", "Encryption", "Skin", "Addons", "Meta", "Variables" }; } }

        [Data_PrimaryKey]
        [Category("Site"), Caption("Site Domain"), Description("The domain name of your site (e.g., yourcompany.com, yetawf.com)")]
        [UIHint("Text80"), StringLength(MaxSiteDomain), Required, Trim]
        public string SiteDomain { get; set; }

        [Category("Variables"), Caption("Default Site Domain"), Description("The domain name of the default site for this instance of YetaWF")]
        public string DefaultSiteDomain {
            get {
                return SiteDefinition.GetDefaultSiteDomain();
            }
        }
        public static string GetDefaultSiteDomain() {
            if (_defaultSiteDomain == null) {
                SiteDefinition defaultSite = LoadSiteDefinition(null);
                _defaultSiteDomain = defaultSite.SiteDomain;
            }
            return _defaultSiteDomain;
        }
        public static string _defaultSiteDomain;

        [Category("Variables"), Caption("Default Site"), Description("Returns whether the current site is the default site for this instance of YetaWF")]
        public bool IsDefaultSite {
            get {
                return string.Compare(YetaWFManager.DefaultSiteName, this.SiteDomain, true) == 0;
            }
        }

        [UIHint("Hidden")]
        [DontSave]
        public string OriginalSiteDomain { get; set; }

        [Description("The name associated with your site, usually your company name or your name")]
        [Category("Site")]
        [Caption("Site Name")]
        [UIHint("Text80"), StringLength(MaxSiteName), Required, Trim]
        public string SiteName { get; set; }

        [Category("Site"), Caption("Port Number (Normal)"), Description("The port number used to access this site using http. The default is 80 which can be specified using -1")]
        [UIHint("IntValue6"), Range(-1, 65535), Required]
        public int PortNumber { get; set; }

        [Category("Site"), Caption("Port Number (SSL)"), Description("The port number used to access this site using https (SSL - Secure Sockets Layer). The default is 443 which can be specified using -1")]
        [UIHint("IntValue6"), Range(-1, 65535), Required]
        public int PortNumberSSL { get; set; }

        [Category("Variables"), Caption("Site Url With http"), Description("The site Url including http:")]
        public string SiteUrlHttp {
            get {
                return MakeRealUrl();
            }
        }
        [Category("Variables"), Caption("Site Url With https"), Description("The site Url including https:")]
        public string SiteUrlHttps {
            get {
                return MakeRealUrl(Secure: true);
            }
        }

        [Category("Site"), Caption("Localization"), Description("Defines whether localization and multilingual support is required, which means all text is provided through resources. Otherwise, only the default language is available")]
        [UIHint("Boolean")]
        public bool Localization { get; set; }

        [Category("Site"), Caption("Default Language"), Description("The site's default language")]
        [UIHint("LanguageId"), StringLength(LanguageData.MaxId), AdditionalMetadata("NoDefault", true), Required, Trim]
        public string DefaultLanguageId { get; set; }

        [Category("Site"), Caption("Allow Anonymous Users"), Description("Defines whether the site allows access to anonymous users - This can be disabled when a site under development is publicly accessible, so anonymous users are redirected to the login page")]
        [UIHint("Boolean")]
        public bool AllowAnonymousUsers { get; set; }



        [Category("Site"), Caption("Locked"), Description("Defines whether the site is locked for maintenance - If enabled, all users (except you) are redirected to a \'Maintenance\' page defined using Locked Url Redirect")]
        [UIHint("Boolean"), SuppressIfEqual("IsLockedExternal", true)]
        public bool Locked { get; set; }
        [UIHint("Hidden")]
        public bool IsLocked { get { return Locked; } }

        [Category("Site"), Caption("Locked"), Description("Defines whether the site is locked for maintenance - If enabled, all users (except you) are redirected to a \'Maintenance\' page defined using Locked Url Redirect - Can only be enabled/disable using Appsettings.json")]
        [UIHint("Boolean"), ReadOnly, SuppressIfEqual("IsLockedExternal", false)]
        [Data_DontSave]
        public bool LockedExternal { get; set; }
        [UIHint("Hidden")]
        public bool IsLockedExternal { get { return LockedExternal; } }

        [Category("Site"), Caption("Locked For IP Address"), Description("The only IP address that has access to the site - All others are redirected to a \'Maintenance\' page defined using Locked Url Redirect - This is typically used while maintenance is applied to a site so only one IP address has access to the site")]
        [UIHint("String"), StringLength(Globals.MaxIP), ReadOnly, SuppressIfEqual("IsLockedExternal", true)]
        public string LockedForIP { get; set; }
        [Category("Site"), Caption("Locked For IP Address"), Description("The only IP address that has access to the site - All others are redirected to a \'Maintenance\' page defined using Locked Url Redirect - This is typically used while maintenance is applied to a site so only one IP address has access to the site")]
        [UIHint("String"), StringLength(Globals.MaxIP), ReadOnly, SuppressIfEqual("IsLockedExternal", false)]
        public string LockedExternalForIP { get; set; }

        [Category("Site"), Caption("Locked Url Redirect"), Description("The page where the user is redirected when the site is locked (down for maintenance)")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local| UrlHelperEx.UrlTypeEnum.Remote), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local| UrlHelperEx.UrlTypeEnum.Remote)]
        [StringLength(Globals.MaxUrl), RequiredIf("IsLocked", true), Trim]
        public string LockedUrl { get; set; }

        public string GetLockedForIP() {
            if (LockedExternal)
                return LockedExternalForIP;
            if (Locked)
                return LockedForIP;
            return null;
        }
        public bool IsLockedAny { get { return IsLocked || IsLockedExternal; } }

        [Category("Site"), Caption("Allow Popups"), Description("Modules and pages can be displayed as popups")]
        [UIHint("Boolean")]
        public bool AllowPopups { get; set; }

        [Category("Site"), Caption("Suppress Tooltips"), Description("The class(es) on A tags that will suppress the navigation tooltip")]
        [UIHint("Text80"), CssClassesValidationAttribute, StringLength(80)]
        public string CssNoTooltips { get; set; }

        [Category("Site"), Caption("FavIcon"), Description("The default icon representing this site (a small PNG image used for favicon displays less than or equal to 64x64 pixels) shown by the web browser used to display the page - Individual pages can override this site default - This is image is down/upscaled as needed")]
        [UIHint("Image"), AdditionalMetadata("ImageType", SiteDefinition.ImageType)]
        [AdditionalMetadata("Width", 40), AdditionalMetadata("Height", 40)]
        [DontSave]
        public string FavIcon {
            get {
                if (_favIcon == null) {
                    if (FavIcon_Data != null && FavIcon_Data.Length > 0)
                        _favIcon = SiteDomain;
                }
                return _favIcon;
            }
            set {
                _favIcon = value;
            }
        }
        private string _favIcon = null;

        [Data_Binary, CopyAttribute]
        public byte[] FavIcon_Data { get; set; }

        [Category("Site"), Caption("FavIcon (Large)"), Description("The icon representing this site (a PNG image used for favicons greater than 64x64 pixels) shown by the web browser used to display the page - Individual pages can override this site default - This is image is down/upscaled as needed")]
        [UIHint("Image"), AdditionalMetadata("ImageType", SiteDefinition.LargeImageType)]
        [AdditionalMetadata("Width", 40), AdditionalMetadata("Height", 40)]
        [DontSave]
        public string FavIconLrg {
            get {
                if (_favIconLrg == null) {
                    if (FavIconLrg_Data != null && FavIconLrg_Data.Length > 0)
                        _favIconLrg = SiteDomain;
                }
                return _favIconLrg;
            }
            set {
                _favIconLrg = value;
            }
        }
        private string _favIconLrg = null;

        [Data_Binary, CopyAttribute]
        public byte[] FavIconLrg_Data { get; set; }

        [Category("Variables"), Caption("FavIcon Html"), Description("The Html used for the icon representing this site")]
        public string FavIconLink {
            get {
                return GetFavIconLinks(FavIcon_Data, FavIcon, FavIconLrg_Data, FavIconLrg);
            }
        }
        [Category("Site"), Caption("Country"), Description("The country where you/your company is located")]
        [UIHint("CountryISO3166"), StringLength(MaxCountry), Trim, Required]
        public string Country { get; set; }

        [Category("Site"), Caption("Currency"), Description("The default currency used")]
        [UIHint("CurrencyISO4217"), StringLength(CurrencyISO4217Helper.Currency.MaxId), Trim, Required]
        public string Currency { get; set; }

        [Category("Site"), Caption("Currency Format"), Description("The currency format used on this site - the default is $US if omitted")]
        [UIHint("Text20"), StringLength(20)]
        public string CurrencyFormat { get; set; }

        [Category("Site"), Caption("Currency Rounding"), Description("The number of decimal places for the currency used on this site")]
        [UIHint("IntValue2"), Range(0, 5), Required]
        public int CurrencyDecimals { get; set; }

        [Category("Variables"), Caption("Copyright"), Description("The Copyright property with evaluated substitutions")]
        public string CopyrightEvaluated {
            get {
                return Copyright.Replace("<<Year>>", Formatting.FormatDateTimeYear(DateTime.UtcNow));
            }
        }

        [Category("Pages"), Caption("Allow Static Pages"), Description("Defines whether pages marked as static pages (for anonymous users only) are served as static pages - Any page whose content doesn't changed can be marked as a static page, which results in faster page load for the end-user - Pages are marked static using the page's Page Settings (see Static Page property) - Only a deployed site uses static pages")]
        [UIHint("Boolean")]
        [Data_NewValue("(0)")]
        public bool StaticPages { get; set; }

        [Category("Pages"), Caption("Debug Mode"), Description("Defines whether all data caching and compression is disabled through Appsettings.json - typically used for debugging (can only be set using Appsettings.json)")]
        [UIHint("Boolean")]
        public bool DEBUGMODE {
            get {
                return GetDEBUGMODE();
            }
        }
        private static bool? debugMode = null;

        public static bool GetDEBUGMODE() {
            if (debugMode == null) {
                debugMode = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "DEBUG-MODE");
            }
            return (bool)debugMode;
        }

        [Category("Pages"), Caption("Allow Cache Use"), Description("Defines whether data caching is enabled (for example, client-side CSS file caching) - When developing modules and for testing purposes, you can disable all caching by setting this property to No - Otherwise, caching should be enabled for optimal performance by setting this property to Yes - This is only honored in a Debug build")]
        [UIHint("Boolean")]
        public bool AllowCacheUse { get; set; }

        [Category("Pages"), Caption("Compression"), Description("Defines whether whitespace compression is used for all pages. Individual pages can override this setting (see Page Properties)")]
        [UIHint("Boolean")]
        public bool Compression { get; set; }

        [Category("Pages"), Caption("Compress CSS Files"), Description("Defines whether minified stylesheets (CSS files) are used (Yes). Otherwise, stylesheets are not minified (No)")]
        [UIHint("Boolean")]
        public bool CompressCSSFiles { get; set; }

        [Category("Pages"), Caption("Bundle CSS Files"), Description("Defines whether stylesheets (CSS files) are bundled into one single file (excluding large non-YetaWF files like jQuery, jQuery UI, etc.)")]
        [UIHint("Boolean")]
        public bool BundleCSSFiles { get; set; }

        [Category("Pages"), Caption("Bundle CSS Files (UPS)"), Description("Defines whether stylesheets (CSS files) are bundled into one single file while processing a Unified Page Set (excluding large non-YetaWF files like jQuery, jQuery UI, etc.) - This setting applies to bundling an Ajax response while navigating to another page within a Unified Page Set")]
        [UIHint("Boolean")]
        [Data_NewValue("(0)")]
        public bool BundleCSSFilesContent { get; set; }

        [Category("Pages"), Caption("Css Location"), Description("Defines whether CSS files are included at the top or bottom of the page")]
        [UIHint("Enum")]
        [Data_NewValue("(0)")]
        public CssLocationEnum CssLocation { get; set; }

        [Category("Pages"), Caption("Compress JavaScript Files"), Description("Defines whether minified JavaScript files are used (Yes). Otherwise, JavaScript files are not minified (No)")]
        [UIHint("Boolean")]
        public bool CompressJSFiles { get; set; }

        [Category("Pages"), Caption("Bundle JavaScript Files"), Description("Defines whether JavaScript files are bundled into one single file (excluding large non-YetaWF files like jQuery, jQuery UI, etc.)")]
        [UIHint("Boolean")]
        public bool BundleJSFiles { get; set; }

        [Category("Pages"), Caption("Bundle JavaScript Files (UPS)"), Description("Defines whether JavaScript files are bundled into one single file  while processing a Unified Page Set (excluding large non-YetaWF files like jQuery, jQuery UI, etc.) - This setting applies to bundling an Ajax response while navigating to another page within a Unified Page Set")]
        [UIHint("Boolean")]
        [Data_NewValue("(0)")]
        public bool BundleJSFilesContent { get; set; }

        [Category("Pages"), Caption("JavaScript Location"), Description("Defines whether JavaScript files are included at the top or bottom of the page")]
        [UIHint("Enum")]
        [Data_NewValue("(0)")]
        public JSLocationEnum JSLocation { get; set; }

        [Category("Pages"), Caption("Disable Minimize FOUC"), Description("Normally CSS is injected to minimize the Flash Of Unstyled Content (FUOC) which can occur when JavaScript and/or CSS files are included at the bottom of the page - This feature can be disabled - Your mileage may vary (IE/Edge require this option to stay enabled)")]
        [UIHint("Boolean")]
        [Data_NewValue("(0)")]
        public bool DisableMinimizeFUOC { get; set; }

        [Category("Pages"), Caption("Use HttpHandler"), Description("Defines whether images and CSS files use an HttpHandler (can only be set using Appsettings.json)")]
        [UIHint("Boolean")]
        public bool UseHttpHandler {
            get {
                if (useHttpHandler == null) {
                    useHttpHandler = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, "UseHttpHandler", true);
                }
                return (bool)useHttpHandler;
            }
        }
        private bool? useHttpHandler = null;

        [Category("Pages"), Caption("Copyright"), Description("Defines an optional copyright notice displayed on each page, if supported by the skin used. Individual pages can override this notice - use <<Year>> for current year")]
        [UIHint("Text80"), StringLength(MaxCopyright)]
        public string Copyright { get; set; }

        // CDN
        // CDN
        // CDN

        [Category("CDN"), Caption("Use CDN (JavaScript Addons)"), Description("Defines whether a Content Delivery Network is used for some of the 3rd party packages (e.g., jQuery, jQuery-UI, KendoUI, etc.) where a CDN is available - This is typically only used for production sites - Appsettings.json (P:YetaWF_Core:UseCDNComponents) must be set to true for this setting to be honored, otherwise a CDN is not used for 3rd party packages")]
        [UIHint("Boolean")]
        [Data_NewValue("(0)")]
        public bool UseCDNComponents { get; set; }

        [Category("CDN"), Caption("Current Status"), Description("Shows whether a Content Delivery Network is currently used for some of the 3rd party packages (e.g., jQuery, jQuery-UI, KendoUI, etc.) where a CDN is available - Appsettings.json (P:YetaWF_Core:UseCDNComponents) must be set to true for the \"Use CDN (JavaScript Addons)\" setting to be honored, otherwise a CDN is not used for 3rd party packages")]
        [UIHint("Boolean"), ReadOnly]
        public bool CanUseCDNComponents { get { return Manager.CanUseCDNComponents && UseCDNComponents; } }

        [Category("CDN"), Caption("Use CDN (Site Content)"), Description("Defines whether the Content Delivery Network Url is used for the site's pages and files - This is typically only used for production sites - Appsettings.json (P:YetaWF_Core:UseCDN) must be set to true for this setting to be honored, otherwise a CDN is not used for site content")]
        [UIHint("Boolean")]
        public bool UseCDN { get; set; }

        [Category("CDN"), Caption("Current Status"), Description("Shows whether a Content Delivery Network is currently used for the site's pages and files - Appsettings.json (P:YetaWF_Core:UseCDN) must be set to true for the \"Use CDN (Site Content)\" setting to be honored, otherwise a CDN is not used for site content")]
        [UIHint("Boolean"), ReadOnly]
        public bool CanUseCDN { get { return Manager.CanUseCDN && UseCDN && HaveCDNUrl; } }

        public bool HaveCDNUrl { get { return !string.IsNullOrWhiteSpace(CDNUrl); } }

        [Category("CDN"), Caption("CDN Url"), Description("If you are using a Content Delivery Network for files located in your site, enter the CDN root Url for http:// access here - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Url"), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Remote), StringLength(Globals.MaxUrl), Trim]
        [ProcessIf("UseCDN", true)]
        [RequiredIf("UseCDN", true)]
        public string CDNUrl{ get; set; }

        [Category("CDN"), Caption("CDN Url (Secure)"), Description("If you are using a Content Delivery Network for files located in your site, enter the CDN root Url for https:// (secure) access here - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted - If no secure Url is specified, the Url defined using the CDN Url is used instead")]
        [UIHint("Url"), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Remote), StringLength(Globals.MaxUrl), Trim]
        [ProcessIf("UseCDN", true)]
        public string CDNUrlSecure { get; set; }

        public bool HaveCDNUrlSecure { get { return !string.IsNullOrWhiteSpace(CDNUrlSecure); } }

        [Category("CDN"), Caption("SiteFiles"), Description("Defines whether you want to use a Content Delivery Network for the files located in your site's /SiteFiles/[[Site,SiteDomain]]/ folder - You can use the variable { { Site,CDNUrl } } in Text modules to reference the site's root Url - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Boolean")]
        [ProcessIf("UseCDN", true)]
        public bool CDNSiteFiles { get; set; }

        [Category("CDN"), Caption("Vault"), Description("Defines whether you want to use a Content Delivery Network for the files located in your site's /Vault/[[Site,SiteDomain]]/ folder - You can use the variable { { Site,CDNUrl } } in Text modules to reference the site's root Url - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Boolean")]
        [ProcessIf("UseCDN", true)]
        public bool CDNVault { get; set; }

        [Category("CDN"), Caption("CSS/JavaScript"), Description("Defines whether you want to use a Content Delivery Network for CSS/JavaScript files located in your site's /bower_components and /node_modules folders (typically used for 3rd party files) - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Boolean")]
        [ProcessIf("UseCDN", true)]
        [Data_NewValue("(0)")]
        public bool CDNScriptsContent { get; set; }

        [Category("CDN"), Caption("Addons"), Description("Defines whether you want to use a Content Delivery Network for the files located in your site's /Addons/ folder (typically used for YetaWF files) - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Boolean")]
        [ProcessIf("UseCDN", true)]
        public bool CDNAddons { get; set; }

        [Category("CDN"), Caption("AddonsCustom"), Description("Defines whether you want to use a Content Delivery Network for the files located in your site's /AddonsCustom/ folder (typically used for your YetaWF file customizations) - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Boolean")]
        [ProcessIf("UseCDN", true)]
        public bool CDNAddonsCustom { get; set; }

        [Category("CDN"), Caption("AddonsBundles"), Description("Defines whether you want to use a Content Delivery Network for the files located in your site's /AddonsBundles/ folder (typically used for JavaScript and CSS bundles YetaWF creates) - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Boolean")]
        [ProcessIf("UseCDN", true)]
        public bool CDNAddonsBundles { get; set; }

        [Category("CDN"), Caption("FileImage"), Description("Defines whether you want to use a Content Delivery Network for images that use the Urls /File.image /FileHndlr.image - Based on whether you enabled the use of your CDN, the appropriate Url will be substituted")]
        [UIHint("Boolean")]
        [ProcessIf("UseCDN", true)]
        public bool CDNFileImage { get; set; }

        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalUseCDN { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public string OriginalCDNUrl { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public string OriginalCDNUrlSecure { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalCDNSiteFiles { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalCDNVault { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalCDNScriptsContent { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalCDNAddons { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalCDNAddonsCustom { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalCDNAddonsBundles { get; set; }
        [UIHint("Hidden")]
        [DontSave]
        public bool OriginalCDNFileImage { get; set; }

        // EMAIL
        // EMAIL
        // EMAIL

        [Category("Email"), Caption("Email Debug"), Description("Set to redirect all emails generated by the site to the site administrator - This is typically used for testing/debugging a deployed site - A site that is not marked as deployed will always send emails to the site administrator")]
        [UIHint("Boolean")]
        public bool EmailDebug { get; set; }

        [Category("Email"), Caption("Site Admin Email"), Description("The email address of the site's administrator. The SMTP server must be defined for email delivery to the site administrator")]
        [Required, UIHint("Email"), StringLength(Globals.MaxEmail), EmailValidation, Trim]
        public string AdminEmail { get; set; }
        [Category("Email"), Caption("Email Server"), Description("The email server used to send emails from this site")]
        [UIHint("SMTPServer"), AdditionalMetadata("Test", true)]
        public SMTPServer SMTP { get; set; }

        // URLS
        // URLS
        // URLS

        [Description("The home page of your site")]
        [Category("Urls")]
        [Caption("Site Home Page")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local)]
        [StringLength(Globals.MaxUrl), Required, Trim]
        public string HomePageUrl {
            get {
                if (string.IsNullOrWhiteSpace(_homePageUrl))
                    return "/";
                return _homePageUrl;
            }
            set {
                _homePageUrl = value;
            }
        }
        private string _homePageUrl = null;

        [Category("Urls"), Caption("Enforce Url"), Description("Defines whether incoming requests for the site will be redirected to the defined site Url. This allows multiple domain names to point to the same site, but all are redirected to the defined site Url, which is best for SEO (search engine optimization). When running locally (usually on a development system) using 'localhost', this property is ignored")]
        [UIHint("Boolean")]
        public bool EnforceSiteUrl { get; set; }

        [Category("Urls"), Caption("Enforce Security"), Description("Defines how page security using http/https (SSL, Secure Sockets Layer) is enforced")]
        [UIHint("Enum")]
        public PageSecurityType PageSecurity { get; set; }

        [Category("Urls"), Caption("Page Not Found"), Description("If an non-existent page is accessed, the user is redirected to this Url")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local)]
        [StringLength(Globals.MaxUrl), Trim]
        public string NotFoundUrl { get; set; }

        [Category("Urls"), Caption("Mobile Device Url"), Description("If a mobile device accesses this site, the user is redirected to this Url")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local | UrlHelperEx.UrlTypeEnum.Remote), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local | UrlHelperEx.UrlTypeEnum.Remote)]
        [StringLength(Globals.MaxUrl), Trim]
        public string MobileSiteUrl { get; set; }

        [Category("Urls"), Caption("Unsupported Browsers Url"), Description("If an unsupported browsers accesses this site, the user is redirected to this Url - If no Url is defined, browser versions are not checked")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local | UrlHelperEx.UrlTypeEnum.Remote), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local | UrlHelperEx.UrlTypeEnum.Remote)]
        [StringLength(Globals.MaxUrl), Trim]
        public string UnsupportedBrowserUrl { get; set; }

        [Category("Urls"), Caption("Login Url"), Description("The Url where the user is redirected to log into the site")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local)]
        [StringLength(Globals.MaxUrl), Trim]
        public string LoginUrl { get; set; }

        [Category("Urls"), Caption("External Account Setup Url"), Description("The Url where the user is redirected to provide local information when using an external login provider")]
        [UIHint("Url"), AdditionalMetadata("UrlType", UrlHelperEx.UrlTypeEnum.Local), UrlValidation(UrlValidationAttribute.SchemaEnum.Any, UrlHelperEx.UrlTypeEnum.Local)]
        [StringLength(Globals.MaxUrl), Trim]
        public string ExternalAccountSetupUrl { get; set; }

        // REFERENCES
        // REFERENCES
        // REFERENCES

        [Category("References"), Caption("Skin Modules"), Description("Defines modules which must be injected into all pages")]
        [UIHint("ReferencedModules")]
        [Data_Binary]
        public SerializableList<ModuleDefinition.ReferencedModule> ReferencedModules { get; set; }

        // SKIN
        // SKIN
        // SKIN

        [Category("Skin"), Caption("Default Page Skin"), Description("The default skin used to for pages - individual pages can override the default skin")]
        [UIHint("PageSkin"), AdditionalMetadata("NoDefault", true), Required, Trim]
        public SkinDefinition SelectedSkin { get; set; }

        [Category("Skin"), Caption("Default Popup Skin"), Description("The default skin used in a popup window - individual pages can override the default skin")]
        [UIHint("PopupSkin"), AdditionalMetadata("NoDefault", true), Required, Trim]
        public SkinDefinition SelectedPopupSkin { get; set; }

        [Category("Skin"), Caption("Immediate Form Errors"), Description("Defines whether errors on forms are immediately marked using warning indicators when first displayed - otherwise indicators are shown as fields are edited and after a form is first submitted")]
        [UIHint("Boolean")]
        [Data_NewValue("(0)")]
        public bool FormErrorsImmed { get; set; }

        [Category("Skin"), Caption("Default jQuery UI Skin"), Description("The default skin for jQuery-UI elements (buttons, modal dialogs, etc.) - individual pages can override the default skin")]
        [HelpLink("http://jqueryui.com/themeroller/")]
        [UIHint("jQueryUISkin"), StringLength(SkinDefinition.MaxName), AdditionalMetadata("NoDefault", true), Trim]
        public string jQueryUISkin { get; set; }

        [Category("Skin"), Caption("Default Kendo UI Skin"), Description("The default skin for Kendo UI elements (buttons, modal dialogs, etc.) - individual pages can override the default skin")]
        [HelpLink("http://demos.telerik.com/kendo-ui/themebuilder/")]
        [UIHint("KendoUISkin"), StringLength(SkinDefinition.MaxName), AdditionalMetadata("NoDefault", true), Trim]
        public string KendoUISkin { get; set; }

        [Category("Skin"), Caption("Tab Style"), Description("Defines which UI provides the tab control implementation")]
        [UIHint("Enum"), Required]
        public TabStyleEnum TabStyle { get; set; }

        // ENCRYPTION
        // ENCRYPTION
        // ENCRYPTION

        [Category("Encryption"), Caption("Public Key"), Description("The public key used to encrypt a token - This is used by this YetaWF site to encrypt/decrypt data internally")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(Globals.MaxPublicKey)]
        public string PublicKey { get; set; }

        [Category("Encryption"), Caption("Private Key"), Description("The private key used to decrypt a token - This is used by this YetaWF site to encrypt/decrypt data internally")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(Globals.MaxPrivateKey)]
        public string PrivateKey { get; set; }

        // ADDONS
        // ADDONS
        // ADDONS

        [Category("Addons"), Caption("Analytics"), Description("Add analytics JavaScript code (for example, the Universal Analytics tracking code used by Google Analytics or the code used by Clicky) - Any code that should be added at the end of the HTML page can be added here including <script></script> tags - Pages can override this setting")]
        [TextAbove("Analytics code is only available in deployed production sites and is ignored in debug builds (not marked deployed).")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxAnalytics), Trim]
        public string Analytics { get; set; }
        [Category("Addons"), Caption("Analytics (Content)"), Description("Add analytics JavaScript code that should be executed when a new page becomes active in an active Unified Page Set - Do not include <script></script> tags - Use <<Url>> to substitute the actual Url - Pages can override this setting")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxAnalytics), Trim]
        public string AnalyticsContent { get; set; }

        [Category("Addons"), Caption("Google Verification"), Description("The meta tags used by Google Webmaster Central so your site can prove to Google that you are really the site owner - You can obtain a meta tag from Google Webmaster Central for site verification - Make sure to copy the ENTIRE meta tag (including markup)")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxGoogleVerification), GoogleVerificationExpression, Trim]
        [HelpLink("http://www.google.com/webmasters/")]
        public string GoogleVerification { get; set; }

        [Category("Addons"), Caption("<HEAD>"), Description("Any tags that should be added to the <HEAD> tag of each page can be added here")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxHead), Trim]
        public string ExtraHead { get; set; }

        [Category("Addons"), Caption("<BODY> Top"), Description("Any tags that should be added to the top of the <BODY> tag of each page can be added here")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxBodyTop), Trim]
        public string ExtraBodyTop { get; set; }

        [Category("Addons"), Caption("<BODY> Bottom"), Description("Any tags that should be added to the bottom of the <BODY> tag of each page can be added here")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxBodyBottom), Trim]
        public string ExtraBodyBottom { get; set; }

        [Category("Addons"), Caption("Geo Location"), Description("Defines whether the site collects geo location information from your visitors based on their IP address (if available)")]
        [UIHint("Boolean")]
        [TextBelow(@"-<a href=""http://www.geoplugin.com/geolocation/"" target=""_blank"">IP Geolocation</a> by <a href=""http://www.geoplugin.com/"" target=""_blank"">geoPlugin</a> - By enabling geo location, you are accepting third parties&apos; terms and conditions")]
        [HelpLink("http://www.geoplugin.com/geolocation/")]
        public bool UseGeoLocation { get; set; }

        // META
        // META
        // META

        [Category("Meta"), Caption("Site Meta Tags"), Description("Defines <meta> tags that are added to ALL pages")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxMeta), Trim]
        public string SiteMetaTags { get; set; }

        [Category("Meta"), Caption("Page Meta Tags"), Description("Defines <meta> tags that are added to all pages by default but can be overridden by each page if the page defines meta tags using the PageMetaTags property")]
        [UIHint("TextArea"), AdditionalMetadata("SourceOnly", true), StringLength(MaxMeta), Trim]
        public string PageMetaTags { get; set; }

        [Category("Meta"), Caption("SiteMap Default Priority"), Description("Defines the default page priority used for the site map - Each page can override the default value using its SiteMap Priority property")]
        [UIHint("Enum")]
        [Data_NewValue("(0)")]
        public PageDefinition.SiteMapPrioritySiteEnum SiteMapPriority { get; set; }
        [Category("Meta"), Caption("SiteMap Change Frequency Default"), Description("Defines the default page change frequency - Each page can override the default value using its Change Frequency property")]
        [UIHint("Enum")]
        [Data_NewValue("(0)")]
        public PageDefinition.ChangeFrequencySiteEnum DefaultChangeFrequency { get; set; }

        // MODULE CONTROL & EDITING
        // MODULE CONTROL & EDITING
        // MODULE CONTROL & EDITING

        // RFFU: Expand and make editable
        public Guid ModuleControlServices { get { return ModuleControlServicesFallback; } }
        public Guid ModuleEditingServices { get { return ModuleEditingServicesFallback; } }
        public Guid PackageLocalizationServices { get { return PackageLocalizationServicesFallback; } }
        public Guid MenuServices { get { return MenuServicesFallback; } }

        public Guid ModuleControlServicesFallback { get { return new Guid("{96CAEAD9-068D-4b83-8F46-5269834F3B16}"); } }// ModuleControl module
        public Guid ModuleEditingServicesFallback { get { return new Guid("{ACDC1453-32BD-4de2-AB2B-7BF5CE217762}"); } }// ModuleEdit module
        public Guid PackageLocalizationServicesFallback { get { return new Guid("{b30d6119-4769-4702-88d8-585ee4ebd4a7}"); } }// LocalizeEditFileModule module
        public Guid MenuServicesFallback { get { return new Guid("{59909BB1-75F4-419f-B961-8569BB282131}"); } }// MainMenuModule module

        // PAGE CONTROL & EDITING
        // PAGE CONTROL & EDITING
        // PAGE CONTROL & EDITING

        // RFFU: Expand and make editable
        public Guid PageEditingServices { get { return PageEditingServicesFallback; } }

        public Guid PageEditingServicesFallback { get { return new Guid("{FBB3C6D3-FBD2-4ab1-BF0E-8716F3D1B052}"); } }// PageEdit module
    }
}

