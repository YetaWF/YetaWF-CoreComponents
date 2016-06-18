/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class IntValue<TModel> : RazorTemplate<TModel> { }

    public static class IntValueHelper {

        public static MvcHtmlString RenderIntValue(this HtmlHelper<object> htmlHelper, string name, int value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride);

            tag.MergeAttribute("type", "text");
            tag.MergeAttribute("value", value.ToString());

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }

    }
}
