/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Menus;
using YetaWF.Core.Modules;

namespace YetaWF.Core.Views.Shared {
    public static class MenuListHelper {
        public static MvcHtmlString Render(this HtmlHelper htmlHelper, MenuList menuList, string id = null, string cssClass = null, ModuleAction.RenderEngineEnum RenderEngine = ModuleAction.RenderEngineEnum.JqueryMenu) {
            return menuList.Render(htmlHelper, id, cssClass, RenderEngine: RenderEngine);
        }
    }
}
