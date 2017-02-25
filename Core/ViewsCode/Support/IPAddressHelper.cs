﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class IPAddress<TModel> : RazorTemplate<TModel> { }

    public static class IPAddressHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(IPAddressHelper), name, defaultValue, parms); }
#if MVC6
        public static MvcHtmlString RenderIPAddressDisplay(this IHtmlHelper htmlHelper, string name, string ipAddress, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
#else
        public static MvcHtmlString RenderIPAddressDisplay(this HtmlHelper htmlHelper, string name, string ipAddress, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            if (string.IsNullOrWhiteSpace(ipAddress)) return MvcHtmlString.Empty;

            hb.Append(ipAddress);

            bool lookup = htmlHelper.GetControlInfo<bool>("", "Lookup", true);
            if (lookup) {
                ModuleDefinition modDisplay = ModuleDefinition.Load(new Guid("{ad95564e-8eb7-4bcb-be64-dc6f1cd6b55d}"), AllowNone: true);
                if (modDisplay != null) {
                    ModuleAction actionDisplay = modDisplay.GetModuleAction("DisplayHostName", null, ipAddress);
                    if (modDisplay != null)
                        hb.Append(actionDisplay.Render(ModuleAction.RenderModeEnum.IconsOnly));
                    actionDisplay = modDisplay.GetModuleAction("DisplayGeoData", null, ipAddress);
                    if (modDisplay != null)
                        hb.Append(actionDisplay.Render(ModuleAction.RenderModeEnum.IconsOnly));
                }
            }
            return MvcHtmlString.Create(hb.ToString());
        }
    }
}