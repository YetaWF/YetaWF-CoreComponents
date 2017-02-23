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

    public class TextArea<TModel> : RazorTemplate<TModel> { }

    public static class TextAreaHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static MvcHtmlString RenderTextArea(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static MvcHtmlString RenderTextArea(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            Manager.AddOnManager.AddAddOnGlobal("ckeditor.com", "ckeditor");

            TagBuilder tag = new TagBuilder("textarea");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride);

            tag.SetInnerText(text);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }
#if MVC6
        public static MvcHtmlString RenderTextAreaDisplay(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static MvcHtmlString RenderTextAreaDisplay(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append(Globals.LazyHTMLOptimization);

            TagBuilder tag = new TagBuilder("div");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Anonymous: true, Validation: false);

            bool encode = htmlHelper.GetControlInfo<bool>("", "Encode", true);
            if (encode) {
                if (string.IsNullOrWhiteSpace(text))
                    text = "&nbsp;"; //so the div is not empty
                else {
                    tag.SetInnerText(text);
                    text = tag.GetInnerHtml();
                    text = text.Replace("\r\n", "<br/>");
                    text = text.Replace("\n", "<br/>");
                }
            } else {
                if (string.IsNullOrWhiteSpace(text))
                    text = "&nbsp;"; //so the div is not empty
            }
            tag.SetInnerHtml(text);

            hb.Append(tag.ToString(TagRenderMode.Normal));
            hb.Append(Globals.LazyHTMLOptimizationEnd);

            return MvcHtmlString.Create(hb.ToString());
        }
    }
}
