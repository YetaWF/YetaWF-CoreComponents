/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons.Templates;
using YetaWF.Core.Menus;
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

    public class ActionIcons<TModel> : RazorTemplate<TModel> { }

    public static class ActionHelper {

#if MVC6
        public static HtmlString RenderActionIcons(this IHtmlHelper htmlHelper, string name, MenuList actions) {
#else
        public static HtmlString RenderActionIcons(this HtmlHelper<object> htmlHelper, string name, MenuList actions) {
#endif
            if (!string.IsNullOrEmpty(name))
                throw new InternalError("Field name not supported for ActionIcons");
            return actions.Render(htmlHelper, null, ActionIcons.CssActionIcons);
        }
    }
}
