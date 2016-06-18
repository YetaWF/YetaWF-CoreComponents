/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class Text<TModel> : RazorTemplate<TModel> { }

    public static class TextBoxHelper {

        public static MvcHtmlString RenderTextBox(this HtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {

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

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag));
        }
        public static MvcHtmlString RenderTextBoxDisplay(this HtmlHelper htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: false, Anonymous: true);

            tag.MergeAttribute("type", "text");
            tag.MergeAttribute("value", text);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag));
        }

    }
}
