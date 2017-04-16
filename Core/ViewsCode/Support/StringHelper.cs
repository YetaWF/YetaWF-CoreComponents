/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    public class String<TModel> : RazorTemplate<TModel> { }

    public static class StringHelper {
#if MVC6
        public static HtmlString RenderStringDisplay(this IHtmlHelper htmlHelper, string name, object text, int dummy = 0, object HtmlAttributes = null) {
#else
        public static HtmlString RenderStringDisplay(this HtmlHelper<object> htmlHelper, string name, object text, int dummy = 0, object HtmlAttributes = null) {
#endif
            if (text == null)
                text = "";
            string t = text.ToString();
            if (string.IsNullOrWhiteSpace(t))
                return HtmlStringExtender.Empty;
            t = YetaWFManager.HtmlEncode(t);
            return new HtmlString(t);
        }
    }
}
