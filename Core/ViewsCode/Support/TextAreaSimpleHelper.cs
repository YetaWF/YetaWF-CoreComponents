/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class TextAreaSimple<TModel> : RazorTemplate<TModel> { }

    public static class TextAreaSimpleHelper {
#if MVC6
        public static HtmlString RenderTextAreaSimple(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static HtmlString RenderTextAreaSimple(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            TagBuilder tag = new TagBuilder("textarea");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride);
            tag.SetInnerText(text);

            return tag.ToHtmlString(TagRenderMode.Normal);
        }
#if MVC6
        public static HtmlString RenderTextAreaSimpleDisplay(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static HtmlString RenderTextAreaSimpleDisplay(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
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

            return tag.ToHtmlString(TagRenderMode.Normal);
        }
    }
}
