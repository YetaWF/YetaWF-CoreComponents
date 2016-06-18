/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Web.Mvc;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class Guid<TModel> : RazorTemplate<TModel> { }

    public static class GuidHelper {

        public static MvcHtmlString RenderGuid(this HtmlHelper<object> htmlHelper, string name, Guid? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: Validation);
            if (model != null)
                tag.MergeAttribute("value", ((Guid)model).ToString());
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }
    }
}
