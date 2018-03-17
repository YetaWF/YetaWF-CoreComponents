/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons.Templates;
using YetaWF.Core.Menus;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Modules;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using System.Threading.Tasks;
using System;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class ActionIcons<TModel> : RazorTemplate<TModel> { }

    public static class ActionHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ActionHelper), name, defaultValue, parms); }
#if MVC6
        public static async Task<HtmlString> RenderActionIconsAsync(this IHtmlHelper htmlHelper, string name, MenuList actions) {
#else
        public static async Task<HtmlString> RenderActionIconsAsync(this HtmlHelper<object> htmlHelper, string name, MenuList actions) {
#endif
            if (!string.IsNullOrEmpty(name))
                throw new InternalError("Field name not supported for ActionIcons");
            GridHelper.GridActionsEnum actionStyle = GridHelper.GridActionsEnum.Icons;
            if (actions.Count > 1) {
                GridHelper.GridActionsEnum gridActionStyle;
                if (htmlHelper.TryGetControlInfo<GridHelper.GridActionsEnum>("", "GridActionsEnum", out gridActionStyle))
                    actionStyle = gridActionStyle;
                else
                    actionStyle = UserSettings.GetProperty<GridHelper.GridActionsEnum>("GridActions");
            }
            switch (actionStyle) {
                default:
                case GridHelper.GridActionsEnum.Icons:
                    return await actions.RenderAsync(htmlHelper, null, ActionIcons.CssActionIcons);
                case GridHelper.GridActionsEnum.DropdownMenu: {
                        MenuList menuActions = actions;
                        menuActions.RenderMode = ModuleAction.RenderModeEnum.NormalMenu;

                        HtmlBuilder hb = new HtmlBuilder();
                        string id = Manager.UniqueId();
                        string idButton = id + "_btn";
                        string idMenu = id + "_menu";
                        hb.Append("<button id=\"{0}\" type=\"button\" class=\"yt_actionicons\">{1}<span class=\"k-icon k-i-arrow-60-down\"></span></button>", idButton, GetDropdownActionString());
                        hb.Append(await menuActions.RenderAsync(htmlHelper, idMenu, Globals.CssGridActionMenu));

                        ScriptBuilder sb = new ScriptBuilder();
                        sb.Append("YetaWF_TemplateActionIcons.initMenu('{0}', $('#{1}'), $('#{2}'));", id, idButton, idMenu);

                        hb.Append(Manager.ScriptManager.AddNow(sb.ToString()).ToString());
                        return hb.ToHtmlString();
                    }
            }
        }
        public static string GetDropdownActionString() {
            return __ResStr("dropdownText", "Manage");
        }
        public static int GetDropdownActionWidthInChars() {
            string s = __ResStr("dropdownWidth", "12");
            return Convert.ToInt32(s);
        }
    }
}
