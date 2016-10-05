﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class Text<TModel> : RazorTemplate<TModel> { }

    public static class TextBoxHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(TextBoxHelper), name, defaultValue, parms); }

        public static MvcHtmlString RenderTextBox(this HtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {

            Manager.AddOnManager.AddTemplate("Text");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.maskedtextbox.min.js");

            HtmlBuilder hb = new HtmlBuilder();

            bool copy = htmlHelper.GetControlInfo<bool>("", "Copy", false);

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: Validation);

            // handle StringLengthAttribute as maxlength
            PropertyData propData = ObjectSupport.GetPropertyData(htmlHelper.ViewData.ModelMetadata.ContainerType, htmlHelper.ViewData.ModelMetadata.PropertyName);
            StringLengthAttribute lenAttr = propData.TryGetAttribute<StringLengthAttribute>();
            if (lenAttr != null) {
                int maxLength = lenAttr.MaximumLength;
                if (maxLength > 0 && maxLength <= 8000)
                    tag.MergeAttribute("maxlength", maxLength.ToString());
            }
            // text
            tag.MergeAttribute("type", "text");
            tag.MergeAttribute("value", text);
            tag.MergeAttribute("autocomplete", "on");

            hb.Append(tag.ToString(TagRenderMode.StartTag));

            if (copy) {
                Manager.AddOnManager.AddAddOnGlobal("clipboardjs.com", "clipboard");// add clipboard support
                SkinImages skinImages = new SkinImages();
                string imageUrl = skinImages.FindIcon_Template("Copy.png", YetaWF.Core.Controllers.AreaRegistration.CurrentPackage, "Text");
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, title: __ResStr("ttCopy", "Copy to Clipboard"), alt: __ResStr("altCopy", "Copy to Clipboard"));
                tagImg.AddCssClass("yt_text_copy");
                hb.Append(tagImg.ToString(TagRenderMode.StartTag));
            }
            return MvcHtmlString.Create(hb.ToString());
        }
        public static MvcHtmlString RenderTextBoxDisplay(this HtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {

            Manager.AddOnManager.AddTemplate("Text");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.maskedtextbox.min.js");

            HtmlBuilder hb = new HtmlBuilder();

            bool copy = htmlHelper.GetControlInfo<bool>("", "Copy", true);
            bool rdonly = htmlHelper.GetControlInfo<bool>("", "ReadOnly", false);

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: false, Anonymous: true);

            tag.MergeAttribute("type", "text");
            tag.MergeAttribute("value", text);
            if (copy || rdonly)
                tag.MergeAttribute("readonly", "readonly");
            else
                tag.MergeAttribute("disabled", "disabled");

            hb.Append(tag.ToString(TagRenderMode.StartTag));

            if (copy) {
                Manager.AddOnManager.AddAddOnGlobal("clipboardjs.com", "clipboard");// add clipboard support
                SkinImages skinImages = new SkinImages();
                string imageUrl = skinImages.FindIcon_Template("Copy.png", YetaWF.Core.Controllers.AreaRegistration.CurrentPackage, "Text");
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, title: __ResStr("ttCopy", "Copy to Clipboard"), alt: __ResStr("altCopy", "Copy to Clipboard"));
                tagImg.AddCssClass("yt_text_copy");
                hb.Append(tagImg.ToString(TagRenderMode.StartTag));
            }
            return MvcHtmlString.Create(hb.ToString());
        }
    }
}
;