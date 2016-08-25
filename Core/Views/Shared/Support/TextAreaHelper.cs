﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class TextArea<TModel> : RazorTemplate<TModel> { }

    public static class TextAreaHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static MvcHtmlString RenderTextArea(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {

            Manager.AddOnManager.AddAddOnGlobal("ckeditor.com", "ckeditor");

            TagBuilder tag = new TagBuilder("textarea");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride);

            tag.SetInnerText(text);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString RenderTextAreaDisplay(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {

            Manager.AddOnManager.AddAddOnGlobal("ckeditor.com", "ckeditor");

            TagBuilder tag = new TagBuilder("div");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Anonymous: true, Validation: false);

            bool sourceOnly = htmlHelper.GetControlInfo<bool>("", "SourceOnly", false);
            if (sourceOnly) {
                if (string.IsNullOrWhiteSpace(text))
                    text = "&nbsp;"; //so the div is not empty
                else {
                    tag.SetInnerText(text);
                    text = tag.InnerHtml;
                    text = text.Replace("\r\n", "<br/>");
                    text = text.Replace("\n", "<br/>");
                }
            }
            tag.InnerHtml = text;

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

    }
}
