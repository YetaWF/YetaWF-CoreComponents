/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class Date : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;

            scripts.AddVolatileOption("Date", "DateFormat", YetaWF.Core.Localize.Formatting.GetFormatDateFormat());
        }
    }
}
