/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {
    public class Forms : IAddOnSupport {

        public const string UniqueIdPrefix = "__UniqueIdPrefix";

        // Http request Form[] variables
        public const string ConditionPropertyName = "conditionpropertyname";
        public const string ConditionPropertyValue = "conditionpropertyvalue";
        public const string ConditionPropertyValueLow = "conditionpropertyvaluelow";
        public const string ConditionPropertyValueHigh = "conditionpropertyvaluehigh";

        // Forms support
        public const string CssFormPartial = "yform-partial";
        public const string CssFormAjax = "yform-ajax";
        public const string CssFormNoSubmit = "yform-nosubmit";// added to individual fields to suppress submission
        public const string CssFormNoSubmitContents = "yform-nosubmitcontents";// added to divs to suppress submission of contained fields (usually grids)
        public const string CssFormCancel = "yform-cancel"; // used for cancel button
        public const string CssWarningIcon = "yform-warningicon";
        public const string CssDataApplyButton = "data-apply-button";// used as attribute for Apply button (input[type=submit])

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();

            manager.AddOnManager.AddAddOnGlobal("bassistance.de", "jquery-validation");
            manager.AddOnManager.AddAddOnGlobal("microsoft.com", "jquery_unobtrusive_validation");
            manager.AddOnManager.AddAddOnGlobal("gist.github.com_remi_957732", "jquery_validate_hooks");

            scripts.AddLocalization("Forms", "AjaxError", this.__ResStr("AjaxError", "An error occurred processing this form:(+nl)(+nl){0} - {1}"));
            scripts.AddLocalization("Forms", "AjaxErrorTitle", this.__ResStr("AjaxErrorTitle", "Form Error"));
            scripts.AddLocalization("Forms", "FormErrors", this.__ResStr("FormErrors", ""));

            scripts.AddConfigOption("Forms", "UniqueIdPrefix", UniqueIdPrefix);
            scripts.AddConfigOption("Forms", "RequestVerificationToken", "__RequestVerificationToken");

            scripts.AddConfigOption("Forms", "ConditionPropertyName", ConditionPropertyName);
            scripts.AddConfigOption("Forms", "ConditionPropertyValue", ConditionPropertyValue);
            scripts.AddConfigOption("Forms", "ConditionPropertyValueLow", ConditionPropertyValueLow);
            scripts.AddConfigOption("Forms", "ConditionPropertyValueHigh", ConditionPropertyValueHigh);

            scripts.AddConfigOption("Forms", "CssFormPartial", CssFormPartial);
            scripts.AddConfigOption("Forms", "CssFormAjax", CssFormAjax);
            scripts.AddConfigOption("Forms", "CssFormNoSubmit", CssFormNoSubmit);
            scripts.AddConfigOption("Forms", "CssFormNoSubmitContents", CssFormNoSubmitContents);
            scripts.AddConfigOption("Forms", "CssFormCancel", CssFormCancel);
            scripts.AddConfigOption("Forms", "CssDataApplyButton", CssDataApplyButton);
            scripts.AddConfigOption("Forms", "CssWarningIcon", CssWarningIcon);
            string url = skinImages.FindIcon_Package("#WarningIcon", package);
            scripts.AddConfigOption("Forms", "CssWarningIconUrl", url);

            scripts.AddVolatileOption("Forms", "TabStyle", (int) manager.CurrentSite.TabStyle);
            
        }
    }
}
