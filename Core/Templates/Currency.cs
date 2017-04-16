/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class Currency : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;

            scripts.AddVolatileOption("Currency", "CurrencyFormat", YetaWF.Core.Localize.Formatting.GetFormatCurrencyFormat());
        }
    }
}
