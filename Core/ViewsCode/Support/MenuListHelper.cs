/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Menus;
using YetaWF.Core.Modules;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {
    public static class MenuListHelper {
#if MVC6
        public static async Task<HtmlString> RenderAsync(this IHtmlHelper htmlHelper, MenuList menuList, string id = null, string cssClass = null, ModuleAction.RenderEngineEnum RenderEngine = ModuleAction.RenderEngineEnum.JqueryMenu) {
#else
        public static async Task<HtmlString> RenderAsync(this HtmlHelper htmlHelper, MenuList menuList, string id = null, string cssClass = null, ModuleAction.RenderEngineEnum RenderEngine = ModuleAction.RenderEngineEnum.JqueryMenu) {
#endif
            return await menuList.RenderAsync(htmlHelper, id, cssClass, RenderEngine: RenderEngine);
        }
    }
}
