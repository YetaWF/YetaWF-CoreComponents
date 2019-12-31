/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Addons {

    public class Basics : IAddOnSupport {

        public const bool DEBUGBUILD =
#if DEBUG
            true;
#else
            false;
#endif

        // Action (part of Basics)
        public const string CssActionLink = "yaction-link";// every ModuleAction has this class
        public const string CssPopupLink = "ypopup-link";// every popup link has this class
        public const string CssNoTooltip = "yNoToolTip";
        public const string CssTooltip = "data-tooltip";// a tooltip in a specific location (with other classes or container)
        public const string CssTooltipSpan = "data-tooltipspan";// a tooltip in a <span>
        public const string CssActionTrack = "yTrack";// added to <a> tag tracks clicks in Visitors log (use with static files)
        public const string CssLegend = "data-legend";
        public const string CssConfirm = "data-confirm";
        public const string CssPleaseWait = "data-pleasewait";
        public const string CssDontAddToOriginList = "data-no-origin";
        public const string CssAddModuleContext = "data-module-context";
        public const string CookieToReturn = "CookieToReturn";
        public const string CookieDoneCssAttr = "data-cookie-done";
        public const string CookieDone = "xfercomplete-cookie";
        public const string CssSaveReturnUrl = "data-save-return";
        public const string PostAttr = "data-post";
        public const string CssOuterWindow = "data-outerwindow";
        public const string CssAttrDataSpecialEdit = "data-specialedit";
        public const string CssAttrActionButton = "data-button";
        public const string CssExtraData = "data-extradata";

        // used on url to select specific module
        public const string ModuleGuid = "__ModuleGuid";

        // templates
        public const string TemplateName = "__TemplateName";
        public const string TemplateAction = "__TemplateAction";
        public const string TemplateExtraData = "__TemplateExtraData";

        // defaults
        public const int DefaultPleaseWaitWidth = 400;
        public const int DefaultPleaseWaitHeight = 0;
        public const int DefaultAlertWaitWidth = 400;
        public const int DefaultAlertWaitHeight = 0;
        public const int DefaultAlertYesNoWidth = 400;
        public const int DefaultAlertYesNoHeight = 0;
        public const int DefaultTooltipWidth = 300;
        public const string DefaultTooltipPosition = "top";

        public const string AjaxJavascriptReturn = "JS:";
        public const string AjaxJSONReturn = "JSON:";
        public const string AjaxJavascriptReloadPage = "JSReloadPage:";
        public const string AjaxJavascriptReloadModule = "JSReloadModule:";
        public const string AjaxJavascriptReloadModuleParts = "JSReloadModuleParts:";

        public const string AjaxJavascriptErrorReturn = "JSERROR:";

        public Task AddSupportAsync(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;

            scripts.AddConfigOption("Basics", "DEBUGBUILD", DEBUGBUILD);

            // Site settings
            scripts.AddVolatileOption("Basics", "JSLocation", manager.CurrentSite.JSLocation == Site.JSLocationEnum.Top);
            scripts.AddVolatileOption("Basics", "CssLocation", manager.CurrentSite.CssLocation == Site.CssLocationEnum.Top);
            scripts.AddVolatileOption("Basics", "CacheVersion", YetaWFManager.CacheBuster);

            // User language
            scripts.AddVolatileOption("Basics", "Language", manager.GetUserLanguage());

            // Button Text
            scripts.AddLocalization("Basics", "CloseButtonText", this.__ResStr("CloseButtonText", "Close"));
            scripts.AddLocalization("Basics", "OKButtonText", this.__ResStr("OKButtonText", "OK"));
            scripts.AddLocalization("Basics", "YesButtonText", this.__ResStr("YesButtonText", "Yes"));
            scripts.AddLocalization("Basics", "NoButtonText", this.__ResStr("NoButtonText", "No"));

            // Popup Text
            scripts.AddLocalization("Basics", "PleaseWaitText", this.__ResStr("PleaseWaitText", ""));
            scripts.AddLocalization("Basics", "PleaseWaitTitle", this.__ResStr("PleaseWaitTitle", "Please Wait"));
            scripts.AddLocalization("Basics", "DefaultAlertYesNoTitle", this.__ResStr("DefaultAlertYesNoTitle", "Confirmation Required"));
            scripts.AddLocalization("Basics", "DefaultAlertTitle", this.__ResStr("DefaultAlertTitle", "Warning"));
            scripts.AddLocalization("Basics", "DefaultErrorTitle", this.__ResStr("DefaultErrorTitle", "Error"));
            scripts.AddLocalization("Basics", "DefaultSuccessTitle", this.__ResStr("DefaultSuccessTitle", "Success"));

            // Links
            scripts.AddLocalization("Basics", "OpenNewWindowTT", this.__ResStr("openNewWindowTT", "Click to visit this page on {0} (opens a new window)"));

            // Page/Module Edit Control
            scripts.AddVolatileOption("Basics", "OriginList", manager.OriginList ?? new List<Origin>());
            scripts.AddVolatileOption("Basics", "EditModeActive", manager.EditMode);
            scripts.AddVolatileOption("Basics", "PageControlVisible", manager.PageControlShown);
            scripts.AddVolatileOption("Basics", "IsInPopup", manager.IsInPopup);

            // Css
            scripts.AddConfigOption("Basics", "CssTooltip", CssTooltip);
            scripts.AddConfigOption("Basics", "CssTooltipSpan", CssTooltipSpan);
            scripts.AddConfigOption("Basics", "CssLegend", CssLegend);
            scripts.AddConfigOption("Basics", "CssPopupLink", CssPopupLink);
            scripts.AddConfigOption("Basics", "CssConfirm", CssConfirm);
            scripts.AddConfigOption("Basics", "CssPleaseWait", CssPleaseWait);
            scripts.AddConfigOption("Basics", "CssDontAddToOriginList", CssDontAddToOriginList);
            scripts.AddConfigOption("Basics", "CssAddModuleContext", CssAddModuleContext);
            scripts.AddConfigOption("Basics", "CssAttrDataSpecialEdit", CssAttrDataSpecialEdit);
            scripts.AddConfigOption("Basics", "CssAttrActionButton", CssAttrActionButton);
            scripts.AddConfigOption("Basics", "ModuleGuid", ModuleGuid);// ModuleGuid for form

            // volatile css
            // add classes that don't use tooltips
            string css = CssManager.CombineCss(CssNoTooltip, manager.CurrentSite.CssNoTooltips);
            scripts.AddVolatileOption("Basics", "CssNoTooltips", css);

            scripts.AddConfigOption("Basics", "TemplateName", TemplateName);
            scripts.AddConfigOption("Basics", "TemplateAction", TemplateAction);
            scripts.AddConfigOption("Basics", "TemplateExtraData", TemplateExtraData);

            scripts.AddConfigOption("Basics", "AjaxJavascriptErrorReturn", AjaxJavascriptErrorReturn);
            scripts.AddConfigOption("Basics", "DefaultPleaseWaitWidth", DefaultPleaseWaitWidth);
            scripts.AddConfigOption("Basics", "DefaultPleaseWaitHeight", DefaultPleaseWaitHeight);
            scripts.AddConfigOption("Basics", "DefaultAlertWaitWidth", DefaultAlertWaitWidth);
            scripts.AddConfigOption("Basics", "DefaultAlertWaitHeight", DefaultAlertWaitHeight);
            scripts.AddConfigOption("Basics", "DefaultAlertYesNoWidth", DefaultAlertYesNoWidth);
            scripts.AddConfigOption("Basics", "DefaultAlertYesNoHeight", DefaultAlertYesNoHeight);
            scripts.AddConfigOption("Basics", "DefaultTooltipWidth", DefaultTooltipWidth);
            scripts.AddConfigOption("Basics", "DefaultTooltipPosition", DefaultTooltipPosition);

            scripts.AddConfigOption("Basics", "CookieDoneCssAttr", CookieDoneCssAttr);
            scripts.AddConfigOption("Basics", "CookieDone", CookieDone);
            scripts.AddConfigOption("Basics", "CookieToReturn", CookieToReturn);
            scripts.AddConfigOption("Basics", "PostAttr", PostAttr);
            scripts.AddConfigOption("Basics", "CssOuterWindow", CssOuterWindow);

            scripts.AddConfigOption("Basics", "CssSaveReturnUrl", CssSaveReturnUrl);

            scripts.AddConfigOption("Basics", "AjaxJavascriptReturn", AjaxJavascriptReturn);
            scripts.AddConfigOption("Basics", "AjaxJSONReturn", AjaxJSONReturn);
            scripts.AddConfigOption("Basics", "AjaxJavascriptReloadPage", AjaxJavascriptReloadPage);
            scripts.AddConfigOption("Basics", "AjaxJavascriptReloadModule", AjaxJavascriptReloadModule);
            scripts.AddConfigOption("Basics", "AjaxJavascriptReloadModuleParts", AjaxJavascriptReloadModuleParts);
            scripts.AddLocalization("Basics", "IncorrectServerResp", this.__ResStr("IncorrectServerResp", "Incorrect server response: Expecting a javascript return"));

            scripts.AddConfigOption("Basics", nameof(Globals.Link_OriginList), Globals.Link_OriginList); // chain of urls
            scripts.AddConfigOption("Basics", nameof(Globals.Link_InPopup), Globals.Link_InPopup); // we're in a popup
            scripts.AddConfigOption("Basics", nameof(Globals.Link_ToEditMode), Globals.Link_ToEditMode); // force this mode
            scripts.AddConfigOption("Basics", nameof(Globals.Link_ToPopup), Globals.Link_ToPopup); // we're going into a popup
            scripts.AddConfigOption("Basics", nameof(Globals.Link_PageControl), Globals.Link_PageControl); // show page control module
            scripts.AddConfigOption("Basics", nameof(Globals.Link_CharInfo), Globals.Link_CharInfo); // character info (char width, char height) for module issuing req.
            scripts.AddConfigOption("Basics", nameof(Globals.Link_SubmitIsApply), Globals.Link_SubmitIsApply); // a submit button was clicked and should be handled as Apply
            scripts.AddConfigOption("Basics", nameof(Globals.Link_SubmitIsReload), Globals.Link_SubmitIsReload); // a submit button was clicked and should be handled as a form reload
            scripts.AddConfigOption("Basics", nameof(Globals.Link_EditMode), Globals.Link_EditMode); // site edit mode
            scripts.AddConfigOption("Basics", nameof(Globals.Link_NoEditMode), Globals.Link_NoEditMode); // site display mode
            scripts.AddConfigOption("Basics", nameof(Globals.Link_ScrollLeft), Globals.Link_ScrollLeft);
            scripts.AddConfigOption("Basics", nameof(Globals.Link_ScrollTop), Globals.Link_ScrollTop);

            return Task.CompletedTask;
        }
    }
}
