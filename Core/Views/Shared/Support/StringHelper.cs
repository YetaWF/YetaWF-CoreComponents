/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class String<TModel> : RazorTemplate<TModel> { }

    public static class StringHelper {

        public static MvcHtmlString RenderStringDisplay(this HtmlHelper<object> htmlHelper, string name, object text, int dummy = 0, object HtmlAttributes = null) {
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
