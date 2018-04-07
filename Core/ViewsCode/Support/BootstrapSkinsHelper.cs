/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class PageBootstrapSkins<TModel> : RazorTemplate<TModel> { }

    public static class BootstrapSkinsHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(BootstrapSkinsHelper), name, defaultValue, parms); }
#if MVC6
        public static async Task<HtmlString> RenderBootstrapSkinsAsync(this IHtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#else
        public static async Task<HtmlString> RenderBootstrapSkinsAsync(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#endif
            // get all available skins
            SkinAccess skinAccess = new SkinAccess();
            List<SelectionItem<string>> list = (from theme in await skinAccess.GetBootstrapThemeListAsync() select new SelectionItem<string>() {
                Text = theme.Name,
                Tooltip = theme.Description,
                Value = theme.Name,
            }).ToList();

            bool useDefault = !htmlHelper.GetControlInfo<bool>("", "NoDefault");
            if (useDefault)
                list.Insert(0, new SelectionItem<string> {
                    Text = __ResStr("default", "(Site Default)"),
                    Tooltip = __ResStr("defaultTT", "Use the site defined default theme"),
                    Value = "",
                });
            else if (selection == null)
                selection = await SkinAccess.GetBootstrapDefaultSkinAsync();

            // display the skins in a drop down
            return await htmlHelper.RenderDropDownSelectionListAsync(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
    }
}
