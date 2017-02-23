/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class StringTT {
        public string Text { get; set; }
        public string Tooltip { get; set; }
    }

    public static class StringTTHelper {
#if MVC6
        public static MvcHtmlString RenderStringTTDisplay(this IHtmlHelper htmlHelper, string name, StringTT model) {
#else
        public static MvcHtmlString RenderStringTTDisplay(this HtmlHelper htmlHelper, string name, StringTT model) {
#endif
            return RenderStringTTDisplay(htmlHelper, name, model.Text, model.Tooltip);
        }
#if MVC6
        public static MvcHtmlString RenderStringTTDisplay(IHtmlHelper htmlHelper, string name, string text, string tooltip = null) {
#else
        public static MvcHtmlString RenderStringTTDisplay(HtmlHelper htmlHelper, string name, string text, string tooltip = null) {
#endif
            TagBuilder tag = new TagBuilder("span");
            htmlHelper.FieldSetup(tag, name, Validation: false, Anonymous: true);

            if (!string.IsNullOrWhiteSpace(tooltip))
                tag.Attributes.Add(Basics.CssTooltipSpan, tooltip);
            if (!string.IsNullOrWhiteSpace(text))
                tag.SetInnerText(text);
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));

        }
    }
}
