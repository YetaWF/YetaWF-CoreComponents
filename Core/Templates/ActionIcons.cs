/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class ActionIcons : IAddOnSupport {

        public const string CssActionIcons = "yActionIcons"; // action icons

        public void AddSupport(YetaWFManager manager) {
            ScriptManager scripts = manager.ScriptManager;
            scripts.AddLocalization("ActionIcons", "CssActionIcons", CssActionIcons);
        }
    }
}
