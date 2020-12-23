/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {

    public class Forms : IAddOnSupport {

        public const string UniqueIdCounters = "__UniqueIdCounters";

        // Forms support
        public const string CssFormPartial = "yform-partial";
        public const string CssFormAjax = "yform-ajax";
        public const string CssFormNoSubmit = "yform-nosubmit";// added to individual fields or <form> tag to suppress submission
        public const string CssFormNoSubmitContents = "yform-nosubmitcontents";// added to divs to suppress submission of contained fields (usually grids)
        public const string CssFormCancel = "yform-cancel"; // used for cancel button
        public const string CssWarningIcon = "yform-warningicon";
        //TODO: This is misnamed. It's an HTML tag attribute, NOT css.
        public const string CssDataApplyButton = "data-apply-button";// used as attribute for Apply button (input[type=submit])

        public static string CssWarningIconUrl { get; private set; } = null!;

        public async Task AddSupportAsync(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            Package package = YetaWF.Core.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();

            // Global form related items (not implementation specific)

            scripts.AddLocalization("Forms", "AjaxError", this.__ResStr("AjaxError", "An error occurred processing this form:(+nl)(+nl){0} - {1}"));
            scripts.AddLocalization("Forms", "AjaxNotAuth", this.__ResStr("AjaxNotAuth", "Not Authorized"));
            scripts.AddLocalization("Forms", "AjaxConnLost", this.__ResStr("AjaxConnLost", "Server Connection Lost"));
            scripts.AddLocalization("Forms", "AjaxErrorTitle", this.__ResStr("AjaxErrorTitle", "Error"));
            scripts.AddLocalization("Forms", "FormErrors", this.__ResStr("FormErrors", ""));

            scripts.AddConfigOption("Forms", "UniqueIdCounters", UniqueIdCounters);
            scripts.AddConfigOption("Forms", "RequestVerificationToken", "__RequestVerificationToken");

            // Validation (not implementation specific) used by validation attributes

            // Css used which is global to YetaWF (not implementation specific)

            scripts.AddConfigOption("Forms", "CssFormPartial", CssFormPartial);
            scripts.AddConfigOption("Forms", "CssFormAjax", CssFormAjax);
            scripts.AddConfigOption("Forms", "CssFormNoSubmit", CssFormNoSubmit);
            scripts.AddConfigOption("Forms", "CssFormNoSubmitContents", CssFormNoSubmitContents);
            scripts.AddConfigOption("Forms", "CssFormCancel", CssFormCancel);
            scripts.AddConfigOption("Forms", "CssDataApplyButton", CssDataApplyButton);
            scripts.AddConfigOption("Forms", "CssWarningIcon", CssWarningIcon);

            CssWarningIconUrl = await skinImages.FindIcon_PackageAsync("WarningIcon.png", package);
            scripts.AddConfigOption("Forms", "CssWarningIconUrl", CssWarningIconUrl);

            // UI settings - global to YetaWF

            scripts.AddVolatileOption("Forms", "TabStyle", (int)manager.CurrentSite.TabStyle);
        }
    }
}
