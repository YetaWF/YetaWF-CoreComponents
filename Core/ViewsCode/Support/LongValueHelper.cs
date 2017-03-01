/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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

    public class LongValue<TModel> : RazorTemplate<TModel> { }

    public static class LongValueHelper {
#if MVC6
        public static HtmlString RenderLongValue(this IHtmlHelper htmlHelper, string name, long value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static HtmlString RenderLongValue(this HtmlHelper<object> htmlHelper, string name, long value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride);

            tag.MergeAttribute("type", "text");
            tag.MergeAttribute("value", value.ToString());

            return tag.ToHtmlString(TagRenderMode.SelfClosing);
        }

    }
}
