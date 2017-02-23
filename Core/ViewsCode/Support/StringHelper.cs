/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class String<TModel> : RazorTemplate<TModel> { }

    public static class StringHelper {
#if MVC6
        public static MvcHtmlString RenderStringDisplay(this IHtmlHelper htmlHelper, string name, object text, int dummy = 0, object HtmlAttributes = null) {
#else
        public static MvcHtmlString RenderStringDisplay(this HtmlHelper<object> htmlHelper, string name, object text, int dummy = 0, object HtmlAttributes = null) {
#endif
            if (text == null)
                text = "";
            string t = text.ToString();
            if (string.IsNullOrWhiteSpace(t))
                return MvcHtmlString.Empty;
            t = YetaWFManager.HtmlEncode(t);
            return MvcHtmlString.Create(t);
        }
    }
}
