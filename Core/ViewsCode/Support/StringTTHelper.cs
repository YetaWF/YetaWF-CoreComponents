/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class StringTT {
        public string Text { get; set; }
        public string Tooltip { get; set; }
    }

    public static class StringTTHelper {
#if MVC6
        public static HtmlString RenderStringTTDisplay(this IHtmlHelper htmlHelper, string name, StringTT model) {
#else
        public static HtmlString RenderStringTTDisplay(this HtmlHelper htmlHelper, string name, StringTT model) {
#endif
            return RenderStringTTDisplay(htmlHelper, name, model.Text, model.Tooltip);
        }
#if MVC6
        public static HtmlString RenderStringTTDisplay(IHtmlHelper htmlHelper, string name, string text, string tooltip = null) {
#else
        public static HtmlString RenderStringTTDisplay(HtmlHelper htmlHelper, string name, string text, string tooltip = null) {
#endif
            TagBuilder tag = new TagBuilder("span");
            htmlHelper.FieldSetup(tag, name, Validation: false, Anonymous: true);

            if (!string.IsNullOrWhiteSpace(tooltip))
                tag.Attributes.Add(Basics.CssTooltipSpan, tooltip);
            if (!string.IsNullOrWhiteSpace(text))
                tag.SetInnerText(text);
            return tag.ToHtmlString(TagRenderMode.Normal);

        }
#if MVC6
        public static HtmlString StringTTDisplay(this IHtmlHelper htmlHelper, StringTT model)
#else
        public static HtmlString StringTTDisplay(this HtmlHelper htmlHelper, StringTT model)
#endif
        {
            return StringTTDisplay(htmlHelper, model.Text, model.Tooltip);
        }
#if MVC6
        public static HtmlString StringTTDisplay(this IHtmlHelper htmlHelper, string text, string tooltip)
#else
        public static HtmlString StringTTDisplay(this HtmlHelper htmlHelper, string text, string tooltip)
#endif
        {
            TagBuilder tag = new TagBuilder("span");
            if (!string.IsNullOrWhiteSpace(tooltip))
                tag.Attributes.Add(Basics.CssTooltipSpan, tooltip);
            if (!string.IsNullOrWhiteSpace(text))
                tag.SetInnerText(text);
            return tag.ToHtmlString(TagRenderMode.Normal);

        }
    }
}
