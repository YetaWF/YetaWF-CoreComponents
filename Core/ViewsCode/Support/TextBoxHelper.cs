﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class Text<TModel> : RazorTemplate<TModel> { }

    public static class TextBoxHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(TextBoxHelper), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderTextBox(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#else
        public static HtmlString RenderTextBox(this HtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#endif
            Manager.AddOnManager.AddTemplate("Text");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.maskedtextbox.min.js");

            HtmlBuilder hb = new HtmlBuilder();

            bool useKendo = !Manager.IsRenderingGrid;

            bool copy = htmlHelper.GetControlInfo<bool>("", "Copy", false);
            //string mask = htmlHelper.GetControlInfo<string>("", "Mask", null);

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: Validation);
            //string id = null;
            //if (!string.IsNullOrWhiteSpace(mask)) {
            //    id = htmlHelper.MakeId(tag);
            //}

            // handle StringLengthAttribute as maxlength
            PropertyData propData = ObjectSupport.GetPropertyData(htmlHelper.ViewData.ModelMetadata.ContainerType, htmlHelper.ViewData.ModelMetadata.PropertyName);
            StringLengthAttribute lenAttr = propData.TryGetAttribute<StringLengthAttribute>();
            if (lenAttr != null) {
#if DEBUG
                if (tag.Attributes.ContainsKey("maxlength"))
                    throw new InternalError("Both StringLengthAttribute and maxlength specified - {0}", name);//$$$$$
#endif
                int maxLength = lenAttr.MaximumLength;
                if (maxLength > 0 && maxLength <= 8000)
                    tag.MergeAttribute("maxlength", maxLength.ToString());
            }
#if DEBUG
            if (lenAttr == null && !tag.Attributes.ContainsKey("maxlength")) {
                throw new InternalError("No max string length given using StringLengthAttribute or maxlength - {0}", name);//$$$$$
            }
#endif
            // text
            tag.MergeAttribute("type", "text");
            tag.MergeAttribute("value", text);
            tag.MergeAttribute("autocomplete", "on");
            if (!useKendo)
                tag.AddCssClass("ybrowsercontrols");

            hb.Append(tag.ToString(TagRenderMode.StartTag));

            if (copy) {
                Manager.AddOnManager.AddAddOnGlobal("clipboardjs.com", "clipboard");// add clipboard support
                SkinImages skinImages = new SkinImages();
                string imageUrl = skinImages.FindIcon_Template("Copy.png", YetaWF.Core.Controllers.AreaRegistration.CurrentPackage, "Text");
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, title: __ResStr("ttCopy", "Copy to Clipboard"), alt: __ResStr("altCopy", "Copy to Clipboard"));
                tagImg.AddCssClass("yt_text_copy");
                hb.Append(tagImg.ToString(TagRenderMode.StartTag));
            }
            // 4/10/2017 can't use mask as there is a focusout hang on submit TODO: Investigate whose problem this is
            //if (!string.IsNullOrWhiteSpace(mask)) {
            //    ScriptBuilder sb = new ScriptBuilder();
            //    sb.Append("$('#{0}').kendoMaskedTextBox({{ mask: '{1}' }});\n", id, YetaWFManager.JserEncode(mask));
            //    Manager.ScriptManager.AddLastDocumentReady(sb);
            //}
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderTextBoxDisplay(this IHtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null) {
#else
        public static HtmlString RenderTextBoxDisplay(this HtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null) {
#endif
            Manager.AddOnManager.AddTemplate("Text");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.maskedtextbox.min.js");

            HtmlBuilder hb = new HtmlBuilder();

            bool copy = htmlHelper.GetControlInfo<bool>("", "Copy", true);
            bool rdonly = htmlHelper.GetControlInfo<bool>("", "ReadOnly", false);

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: false, Anonymous: true);

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
            return hb.ToHtmlString();
        }
    }
}
;