/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class TextArea<TModel> : RazorTemplate<TModel> { }

    public static class TextAreaHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static async Task<HtmlString> RenderTextAreaAsync(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null) {
#else
        public static async Task<HtmlString> RenderTextAreaAsync(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null) {
#endif
            await Manager.AddOnManager.AddAddOnGlobalAsync("ckeditor.com", "ckeditor");

            TagBuilder tag = new TagBuilder("textarea");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes);

            tag.SetInnerText(text);

            return tag.ToHtmlString(TagRenderMode.Normal);
        }
#if MVC6
        public static HtmlString RenderTextAreaDisplay(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null) {
#else
        public static HtmlString RenderTextAreaDisplay(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null) {
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

            return hb.ToHtmlString();
        }
    }
}
