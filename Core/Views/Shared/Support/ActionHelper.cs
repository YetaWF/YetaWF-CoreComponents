/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Addons.Templates;
using YetaWF.Core.Menus;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class ActionIcons<TModel> : RazorTemplate<TModel> { }

    public static class ActionHelper {

        public static MvcHtmlString RenderActionIcons(this HtmlHelper<object> htmlHelper, string name, MenuList actions) {

            if (!string.IsNullOrEmpty(name))
                throw new InternalError("Field name not supported for ActionIcons");
            return actions.Render(null, ActionIcons.CssActionIcons);
        }
    }
}
