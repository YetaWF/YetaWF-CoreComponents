/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core {

    public static class Globals {

        public const string RUNTIME = "net7.0"; // runtime
        public const string RUNTIMEVERSION = "7.0";

        public const int MaxIP = 40;
        public const int MaxUser = 100;
        public const int MaxPswd = 100;
        public const int MaxUrl = 400;
        public const int MaxEmail = 100;
        public const int MaxColor = 20;
        public const int MaxState = 2; // US State
        public const int MaxProvince = 2; // CA Province
        public const int MaxPublicKey = 1500;
        public const int MaxPrivateKey = 1500;
        public const int MaxRSAToken = 20;
        public const int MaxFileName = 200;// longest file name WITHOUT path
        public const int MaxPath = 1000; // longest filename including path
        public const int MaxPhoneNumber = 20;
        public const int MaxTimeZone = 80;

        public const int ChDateTime = 20;
        public const int ChDate = 12;
        public const int ChBoolean = 5;
        public const int ChUrl = 40;
        public const int ChEmail = 30;
        public const int ChColor = 5;
        public const int ChUserName = 35;
        public const int ChGuid = 34;
        public const int ChIntValue = 10;
        public const int ChIntValue4 = 8;
        public const int ChID = 10;
        public const int ChId = 10;
        public const int ChIPAddress = 20;
        public const int ChIPAddressWithLookup = 20;
        public const int ChPhoneNumber = 15;
        public const int ChTimeZone = 20;

        // Data Folder
        public const string DataFolder = "Data";
        public const string DataLocalFolder = "DataLocal";
        public const string SitesFolder = "Sites";
        public const string SiteTemplates = "SiteTemplates";
        public const string VaultPrivateFolder = "VaultPrivate";
        public const string VaultFolder = "Vault";

        public const string UpdateIndicatorFile = "UpdateIndicator.txt"; // filename of the file signaling a global package update/create
        public const string UpgradeLogFile = "UpgradeLogFile.txt"; // log file created during YetaWF upgrade
        public const string StartupLogFile = "StartupLogFile.txt"; // log file created during YetaWF startup
        public const string PackageMap = "PackageMap.txt";// currently installed packages

        public const string DontDeployMarker = "dontdeploy.txt"; // if seen in a folder, its files will not be deployed by DeploySite

        public const string DefaultCountry = "United States";

        // Url parts
        public const string ApiPrefix = "/!api";// api prefix (built-in api)
        public const string PageUrl = "/!Page/";
        public const string ModuleUrl = "/!Mod/";
        public const string Link_OriginList = nameof(Endpoints.ModuleEndpoints.ModuleSubmitData.__OriginList);//$$$ remove
        public const string Link_InPopup = nameof(Endpoints.ModuleEndpoints.ModuleSubmitData.__InPopup); // we're in a popup
        public const string Link_ToPopup = "!ToPopup"; // we're going into a popup
        public const string Link_CurrentUrl = "__CurrentUrl";
        public const string Link_PageControl = nameof(Endpoints.ModuleEndpoints.ModuleSubmitData.__Pagectl); // show page control module
        public const string Link_SubmitIsApply = nameof(Endpoints.ModuleEndpoints.ModuleSubmitData.__Apply); // a submit button was clicked and should be handled as Apply
        public const string Link_SubmitIsReload = nameof(Endpoints.ModuleEndpoints.ModuleSubmitData.__Reload); // a submit button was clicked and should be handled as a form reload
        public const string Link_EditMode = "!Edit"; // site edit mode
        public const string Link_ForceSite = "!Domain"; // force a specific site
        public const string Link_ScrollLeft = "!Left";
        public const string Link_ScrollTop = "!Top";
        public const string Link_Language = "!Lang"; // site language

        public const string Session_Permanent = "##perm##_";
        public const string Session_Superuser = Session_Permanent + "superuser"; // this is a superuser (saved in session state)
        public const string Session_ActiveDevice = Session_Permanent + "activedevice"; // device mobile/desktop (saved in session state)
        public const string Session_Need2FA = "YetaWF_Core_Need2FA"; // user needs to set up two-step authentication
        public const string Session_Need2FARedirect = "YetaWF_Core_Need2FARedirect"; // user needs redirect to set up two-step authentication

        // String/name prefix
        public const string ModuleClassSuffix = "Module";

        // Pane
        public const string MainPane = "Main";

        // Roles
        public const string Role_User = "User"; // every logged on user that does not need to set up two-step authentication has this role
        public const string Role_User2FA = "User (two-step authentication setup required)"; // every logged on user that needs to set up two-step authentication has this role
        public const string Role_UserDemo = "Demo";// a user with limited (demo) functionality
        public const string Role_Anonymous = "Anonymous"; // every user that is not logged on has this role
        public const string Role_Superuser = "Superuser"; // controls ALL sites
        public const string Role_Administrator = "Administrator"; // controls ONE site
        public const string Role_Editor = "Editor"; // can edit ONE site

        // Addons
        public const string AddonsFolder = "Addons";
        public const string AddonsUrl = "/Addons";
        public const string BundlesFolder = "Bundles";
        public const string AddonsBundlesUrl = "/Addons/Bundles";
        public const string AddonsCustomUrl = "/Addons/Custom";
        public const string Addons_TemplatesDirectoryName = "_Templates";
        public const string Addons_ModulesDirectoryName = "_Modules";
        public const string Addons_SkinsDirectoryName = "_Skins";

        public const string Addons_JSFileList = "filelistJS.txt";
        public const string Addons_CSSFileList = "filelistCSS.txt";
        public const string Addons_SupportFileList = "Support.txt";

        public const string SiteFilesUrl = "/SiteFiles/";
        public const string VaultUrl = "/" + VaultFolder + "/";
        public const string VaultPrivateUrl = "/" + VaultPrivateFolder + "/";

        public const string NodeModulesFolder = "node_modules";
        public const string NodeModulesUrl = "/node_modules/";

        public const string MaintenanceFolder = "Maintenance";

        // Module format strings
        public const string PermanentModuleNameFormat = "{0}.{1}";

        // CSS classes
        // Pane
        public const string CssPaneTag = "yPaneTag";
        // Action Menu
        public const string CssGridActionMenu = "yGridActionMenu";
        // Modules (generic)
        public const string CssModule = "yModule";
        public const string CssModuleCurrent = "yModule-current";
        public const string CssModuleMenu = "yModuleMenu";
        public const string CssModuleMenuEditIcon = "yModuleMenuEditIcon";
        public const string CssModuleMenuContainer = "yModuleMenuContainer";
        public const string CssModuleLinksContainer = "yModuleLinksContainer";
        /// <summary>
        /// CSS class that can be added to tags to prevent them from being printed. 
        /// </summary>
        /// <remarks>Using CSS styling (in *.scss) is preferred.
        /// This style should only be used in code where printability is generated dynamically, typically based on settings.</remarks>
        public const string CssModuleNoPrint = "yNoPrint";
        /// <summary>
        /// CSS class that can be added to tags to only display them when printed. Only effective in Site View mode and is ignored in Site Edit mode. 
        /// </summary>
        /// <remarks>
        /// Using CSS styling (in *.scss) is preferred.
        /// This style should only be used in code where printability is generated dynamically, typically based on settings.</remarks>
        public const string CssModulePrintOnly = "yPrintOnly";
        /// <summary>
        /// CSS class that can be added to tags to render them suitable as a noticeable section.
        /// </summary>
        /// <remarks>Adds background/foreground colors, border and padding to allow it to be noticeable.</remarks>
        public const string CssDivAlert = "yDivAlert";
        /// <summary>
        /// CSS class that can be added to tags to render them suitable as a small, noticeable section.
        /// </summary>
        /// <remarks>Adds background/foreground colors, border and padding to allow it to be noticeable.</remarks>
        public const string CssDivSmallAlert = "yDivSmallAlert";
        /// <summary>
        /// CSS class that can be added to tags to render them suitable as a noticeable warning section.
        /// </summary>
        /// <remarks>Adds background/foreground colors, border and padding to allow it to be a noticeable warning.</remarks>
        public const string CssDivWarning = "yDivWarning";
        /// <summary>
        /// CSS class that can be added to tags to render them suitable as a small, noticeable warning section.
        /// </summary>
        /// <remarks>Adds background/foreground colors, border and padding to allow it to be a noticeable warning.</remarks>
        public const string CssDivSmallWarning = "yDivSmallWarning";
        /// <summary>
        /// CSS class that can be added to tags to render them so they're only visible to administrators and superusers.
        /// </summary>
        /// <remarks>Adds background/foreground colors, border and padding.</remarks>
        public const string CssDivAdmin = "yDivAdmin";
        /// <summary>
        /// CSS class that can be added to tags to render them as a small section, only visible to administrators and superusers.
        /// </summary>
        /// <remarks>Adds background/foreground colors, border and padding.</remarks>
        public const string CssDivSmallAdmin = "yDivSmallAdmin";

        /// <summary>
        /// When used in generated HTML, the HTML between LazyHTMLOptimization and <see cref="YetaWF.Core.Globals.LazyHTMLOptimizationEnd"/> are optimized less aggressively.
        /// </summary>
        /// <remarks>Aggressive whitespace optimization removes whitespace between adjoining tags (i.e., the spaces between &lt;div&gt; &lt;img ...&gt; are removed).
        /// Between LazyHTMLOptimization and <see cref="YetaWF.Core.Globals.LazyHTMLOptimizationEnd"/> whitespace between adjoining tags is not removed.</remarks>
        public const string LazyHTMLOptimization = "<!--LazyWSF-->";
        /// <summary>
        /// When used in generated HTML, the HTML between <see cref="YetaWF.Core.Globals.LazyHTMLOptimization"/> and LazyHTMLOptimizationEnd are optimized less aggressively.
        /// </summary>
        /// <remarks>Aggressive whitespace optimization removes whitespace between adjoining tags (i.e., the spaces between &lt;div&gt; &lt;img ...&gt; are removed).
        /// Between <see cref="YetaWF.Core.Globals.LazyHTMLOptimization"/> and LazyHTMLOptimizationEnd whitespace between adjoining tags is not removed.</remarks>
        public const string LazyHTMLOptimizationEnd = "<!--LazyWSFEnd-->";
    }
}
