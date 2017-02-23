/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
        public static HtmlString Render(this IHtmlHelper htmlHelper, MenuList menuList, string id = null, string cssClass = null, ModuleAction.RenderEngineEnum RenderEngine = ModuleAction.RenderEngineEnum.JqueryMenu) {
#else
        public static HtmlString Render(this HtmlHelper htmlHelper, MenuList menuList, string id = null, string cssClass = null, ModuleAction.RenderEngineEnum RenderEngine = ModuleAction.RenderEngineEnum.JqueryMenu) {
#endif
            return menuList.Render(htmlHelper, id, cssClass, RenderEngine: RenderEngine);
        }
    }
}
