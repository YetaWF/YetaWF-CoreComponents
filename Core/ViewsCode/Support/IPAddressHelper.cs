/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class IPAddress<TModel> : RazorTemplate<TModel> { }

    public static class IPAddressHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(IPAddressHelper), name, defaultValue, parms); }
#if MVC6
        public static async Task<HtmlString> RenderIPAddressDisplayAsync(this IHtmlHelper htmlHelper, string name, string ipAddress, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
#else
        public static async Task<HtmlString> RenderIPAddressDisplayAsync(this HtmlHelper htmlHelper, string name, string ipAddress, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            if (string.IsNullOrWhiteSpace(ipAddress)) return HtmlStringExtender.Empty;

            hb.Append(ipAddress);

            bool lookup = htmlHelper.GetControlInfo<bool>("", "Lookup", true);
            if (lookup) {
                ModuleDefinition modDisplay = await ModuleDefinition.LoadAsync(new Guid("{ad95564e-8eb7-4bcb-be64-dc6f1cd6b55d}"), AllowNone: true);
                if (modDisplay != null) {
                    ModuleAction actionDisplay = await modDisplay.GetModuleActionAsync("DisplayHostName", null, ipAddress);
                    if (modDisplay != null)
                        hb.Append(await actionDisplay.RenderAsync(ModuleAction.RenderModeEnum.IconsOnly));
                    actionDisplay = await modDisplay.GetModuleActionAsync("DisplayGeoData", null, ipAddress);
                    if (modDisplay != null)
                        hb.Append(await actionDisplay.RenderAsync(ModuleAction.RenderModeEnum.IconsOnly));
                }
            }
            return hb.ToHtmlString();
        }
    }
}
