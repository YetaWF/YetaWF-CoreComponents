/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Addons;

namespace YetaWF.Core.Views.Shared {

    public class StringTT {
        public string Text { get; set; }
        public string Tooltip { get; set; }
    }

    public static class StringTTHelper {

        public static MvcHtmlString RenderStringTTDisplay(this HtmlHelper htmlHelper, string name, StringTT model) {
            return RenderStringTTDisplay(htmlHelper, name, model.Text, model.Tooltip);
        }

        public static MvcHtmlString RenderStringTTDisplay(HtmlHelper htmlHelper, string name, string text, string tooltip = null) {

            TagBuilder tag = new TagBuilder("span");
            htmlHelper.FieldSetup(tag, name, Validation: false, Anonymous: true);

            if (!string.IsNullOrWhiteSpace(tooltip))
                tag.Attributes.Add(Basics.CssTooltipSpan, tooltip);
            if (!string.IsNullOrWhiteSpace(text))
                tag.SetInnerText(text);
            return MvcHtmlString.Create(tag.ToString());

        }
    }
}
