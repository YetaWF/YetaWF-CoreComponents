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

    public class LongValue<TModel> : RazorTemplate<TModel> { }

    public static class LongValueHelper {
#if MVC6
        public static MvcHtmlString RenderLongValue(this IHtmlHelper htmlHelper, string name, long value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static MvcHtmlString RenderLongValue(this HtmlHelper<object> htmlHelper, string name, long value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride);

            tag.MergeAttribute("type", "text");
            tag.MergeAttribute("value", value.ToString());

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }

    }
}
