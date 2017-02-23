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

    public class TextAreaSimple<TModel> : RazorTemplate<TModel> { }

    public static class TextAreaSimpleHelper {
#if MVC6
        public static MvcHtmlString RenderTextAreaSimple(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static MvcHtmlString RenderTextAreaSimple(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            TagBuilder tag = new TagBuilder("textarea");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride);
            tag.SetInnerText(text);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }
#if MVC6
        public static MvcHtmlString RenderTextAreaSimpleDisplay(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static MvcHtmlString RenderTextAreaSimpleDisplay(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            TagBuilder tag = new TagBuilder("div");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Anonymous: true, Validation: false);

            if (string.IsNullOrWhiteSpace(text))
                text = "&nbsp;"; //so the div is not empty
            else {
                tag.SetInnerText(text);
                text = tag.GetInnerHtml();
                text = text.Replace("\r\n", "<br/>");
                text = text.Replace("\n", "<br/>");
            }
            tag.SetInnerHtml(text);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }
    }
}
