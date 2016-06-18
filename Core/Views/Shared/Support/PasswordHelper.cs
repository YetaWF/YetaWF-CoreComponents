/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Web.Mvc;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class Password<TModel> : RazorTemplate<TModel> { }

    public static class PasswordHelper {

        public static MvcHtmlString RenderPassword(this HtmlHelper<object> htmlHelper, string name, string text, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: Validation);

            // handle StringLengthAttribute as maxlength
            Type containerType = htmlHelper.ViewData.ModelMetadata.ContainerType;
            string propertyName = htmlHelper.ViewData.ModelMetadata.PropertyName;
            PropertyData propData = ObjectSupport.GetPropertyData(containerType, propertyName);
            StringLengthAttribute lenAttr = propData.TryGetAttribute<StringLengthAttribute>();
            if (lenAttr != null) {
                int maxLength = lenAttr.MaximumLength;
                if (maxLength > 0 && maxLength <= 8000)
                    tag.MergeAttribute("maxlength", maxLength.ToString());
            }

            // text
            tag.MergeAttribute("type", "password");
            tag.MergeAttribute("value", text);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }
    }
}
