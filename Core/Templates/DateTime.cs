/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class DateTime : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;

            manager.ScriptManager.AddVolatileOption("DateTime", "DateTimeFormat", YetaWF.Core.Localize.Formatting.GetFormatDateTimeFormat());
        }
    }
}
