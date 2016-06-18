/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class MultiString : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            scripts.AddConfigOption("MultiString", "Localization", manager.CurrentSite.Localization);

            scripts.AddLocalization("MultiString", "Languages", YetaWF.Core.Models.MultiString.LanguageIdList);
            scripts.AddLocalization("MultiString", "NeedDefaultText", this.__ResStr("NeedDefaultText", "Please enter text for the default language before switching to another language."));
        }
    }
}
