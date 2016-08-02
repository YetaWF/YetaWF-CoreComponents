/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
