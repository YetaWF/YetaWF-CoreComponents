/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

namespace YetaWF.Core {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JSAttribute : Attribute { }

    public class Globals : IInitializeApplicationStartup {

        public const int MaxIP = 15; // "xxx.xxx.xxx.xxx"
        public const int MaxUser = 100;
        public const int MaxPswd = 100;
        public const int MaxUrl = 400;
        public const int MaxEmail = 100;
        public const int MaxState = 2; // US State
        public const int MaxPublicKey = 1500;
        public const int MaxPrivateKey = 1500;
        public const int MaxRSAToken = 20;
        public const int MaxFileName = 200;// longest file name WITHOUT path
        public const int MaxPath = 1000; // longest filename including path
        public const int MaxPhoneNumber = 20;

        public const int ChDateTime = 20;
        public const int ChDate = 12;
        public const int ChBoolean = 5;
        public const int ChUrl = 40;
        public const int ChEmail = 30;
        public const int ChUserName = 25;
        public const int ChGuid = 34;
        public const int ChIntValue = 10;
        public const int ChIntValue4 = 8;
        public const int ChID = 10;
        public const int ChId = 10;
        public const int ChIPAddress = 20;
        public const int ChIPAddressWithLookup = 20;
        public const int ChTimeZone = 20;

        public void InitializeApplicationStartup() {
            // Generate globals javascript equivalent
            string outputUrl = VersionManager.FindAddOnVersion("YetaWF", "Core", "Basics").GetAddOnJsUrl() + "Globals.js";
            ConvertGlobalsToJavascript.Convert(YetaWFManager.UrlToPhysical(outputUrl), this, "YGlobals");
        }

        // Data Folder
        public const string DataFolder = "Data";
        public const string SitesFolder = "Sites";
        public const string GlobalJavaScript = "_JS";
        public const string TempFiles = "TempFiles";
        public const string SiteTemplates = "SiteTemplates";
        public const string SiteTemplatesData = SiteTemplates + "\\Data";
        public const string VaultPrivateFolder = "VaultPrivate";
        public const string VaultFolder = "Vault";

        public const string UpdateIndicatorFile = "UpdateIndicator.txt"; // filename of the file signaling a global package update/create
        public const string UpgradeLogFile = "UpgradeLogFile.txt"; // log file created during YetaWF upgrade
        public const string PackageMap = "PackageMap.txt";// currently installed packages

        public const string DontDeployMarker = "dontdeploy.txt"; // if seen in a folder, its files will not be deployed by CopySite

        public const string DefaultCurrencyFormat = "$ #,0.00";

        // Url parts
             public const string PageUrl = "/!Page/";
             public const string ModuleUrl = "/!Mod/";
        [JS] public const string Link_OriginList = "!OriginList"; // chain of urls
        [JS] public const string Link_InPopup = "!InPopup"; // we're in a popup
        [JS] public const string Link_ToEditMode = "!ToEditMode"; // force this mode
        [JS] public const string Link_ToPopup = "!ToPopup"; // we're going into a popup
        [JS] public const string Link_ShowPageControlKey = "!Pagectl"; // show page control module
        [JS] public const string Link_ShowPageControlValue = "y";
        [JS] public const string Link_ShowPageControl = Link_ShowPageControlKey +"="+ Link_ShowPageControlValue; // show page control module
        [JS] public const string Link_CharInfo = "!ci"; // character info (char width, char height) for module issuing req.
        [JS] public const string Link_SubmitIsApply = "!Apply"; // a submit button was clicked and should be handled as Apply
        [JS] public const string Link_TempEditMode = "!Edit"; // temp. edit mode
        [JS] public const string Link_TempNoEditMode = "!Noedit"; // temp. no edit mode
             public const string Link_ForceSite = "!Domain"; // force a specific site
        [JS] public const string Link_ScrollLeft = "!Left";
        [JS] public const string Link_ScrollTop = "!Top";
             public const string Link_Language = "!Lang"; // site language

        public const string Session_Permanent = "##perm##_";
        public const string Session_Superuser = Session_Permanent + "superuser"; // this is a superuser (saved in session state)
        public const string Session_ActiveDevice = Session_Permanent + "activedevice"; // device mobile/desktop (saved in session state)

        // String/name prefix
        public const string ModuleClassSuffix = "Module";

        // Pane
        public const string MainPane = "Main";

        // Roles
        public const string Role_User = "User"; // every logged on user has this role
        public const string Role_Anonymous = "Anonymous"; // every user that is not logged on has this role
        public const string Role_Superuser = "Superuser"; // controls ALL sites
        public const string Role_Administrator = "Administrator"; // controls ONE site
        public const string Role_Editor = "Editor"; // can edit ONE site

        // Addons
        public const string AddOnsFolder = "AddOns";
        public const string AddOnsUrl = "/AddOns";
        public const string AddonsBundlesFolder = "AddonsBundles";
        public const string AddonsBundlesUrl = "/AddonsBundles";
        public const string AddOnsCustomUrl = "/AddOnsCustom";
        public const string Addons_TemplatesDirectoryName = "_Templates";
        public const string Addons_ModulesDirectoryName = "_Modules";
        public const string Addons_SkinsDirectoryName = "_Skins";
        public const string SiteFilesUrl = "/SiteFiles/";
        public const string VaultUrl = "/" + VaultFolder + "/";
        public const string VaultPrivateUrl = "/" + VaultPrivateFolder + "/";

        private const string NugetContentsFolderMVC6 = "lib";
        private const string NugetContentsFolderMVC5 = "Content";
        private const string NugetScriptsFolderMVC6 = "lib";
        private const string NugetScriptsFolderMVC5 = "Scripts";
        private const string NugetScriptsUrlMVC6 = "/" + NugetScriptsFolder + "/";
        private const string NugetScriptsUrlMVC5 = "/Scripts/";
#if MVC6
        public const string NugetContentsFolder = NugetContentsFolderMVC6;
        public const string NugetScriptsFolder = NugetScriptsFolderMVC6;
#else
        public const string NugetContentsFolder = NugetContentsFolderMVC5;
        public const string NugetScriptsFolder = NugetScriptsFolderMVC5;
#endif
        public const string NugetContentsUrl = "/" + NugetContentsFolder + "/";
        public const string NugetScriptsUrl = "/" + NugetScriptsFolder + "/";
        public const string Compiled = "._cmpld_";
        public const string TempImagesFolder = "_img_";

        public const string Addons_JSFileList = "filelistJS.txt";
        public const string Addons_CSSFileList = "filelistCSS.txt";
        public const string Addons_SupportFileList = "Support.txt";

        // Areas
        public const string AreasFolder = "Areas";
        public const string ViewsFolder = "Views";

        // Module format strings
        public const string PermanentModuleNameFormat = "{0}.{1}";
        public const string RVD_ModuleDefinition = "ModuleDefinition";

        // CSS classes
        // Page/Edit Control (generic)
        public const string CssPageControlDiv = "yPageControlDiv";
        public const string CssPageControlButton = "yPageControlButton";
        public const string CssEditControlDiv = "yEditControlDiv";
        public const string CssEditControlButton = "yEditControlButton";
        public const string CssPaneTag = "yPaneTag";
        // Modules (generic)
        public const string CssModule = "yModule";
        public const string CssModuleCurrent = "yModule-current";
        public const string CssModuleMenu = "yModuleMenu";
        public const string CssModuleMenuEditIcon = "yModuleMenuEditIcon";
        public const string CssModuleMenuContainer = "yModuleMenuContainer";
        public const string CssModuleLinksContainer = "yModuleLinksContainer";
        public const string CssModuleLinks = "yModuleLinks"; // module menu action menu
        public const string CssModuleNoPrint = "yNoPrint";
        public const string CssModulePrintOnly = "yPrintOnly";
        // A noticeable div with an error/real warning
        public const string CssDivAlert = "yDivAlert";
        public const string CssDivSmallAlert = "yDivSmallAlert";
        // A noticeable div
        public const string CssDivWarning = "yDivWarning";
        public const string CssDivSmallWarning = "yDivSmallWarning";
        // A div seen by admin/superusers only
        public const string CssDivAdmin = "yDivAdmin";
        public const string CssDivSmallAdmin = "yDivSmallAdmin";

        // HTML comments (WhitespaceFilter directive)
        public const string LazyHTMLOptimization = "<!--LazyWSF-->";
        public const string LazyHTMLOptimizationEnd = "<!--LazyWSFEnd-->";
    }
}
