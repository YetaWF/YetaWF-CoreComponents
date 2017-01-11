/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {
    public class Popups : IAddOnSupport {

        public const int DefaultPopupWidth = 900;
        public const int DefaultPopupHeight = 600;

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;

            scripts.AddVolatileOption("Popups", "AllowPopups", manager.CurrentSite.AllowPopups);
            scripts.AddConfigOption("Popups", "DefaultPopupWidth", DefaultPopupWidth);
            scripts.AddConfigOption("Popups", "DefaultPopupHeight", DefaultPopupHeight);
        }
    }
}
