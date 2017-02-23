/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
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

    public class Guid<TModel> : RazorTemplate<TModel> { }

    public static class GuidHelper {
#if MVC6
        public static MvcHtmlString RenderGuid(this IHtmlHelper htmlHelper, string name, Guid? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#else
        public static MvcHtmlString RenderGuid(this HtmlHelper<object> htmlHelper, string name, Guid? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#endif
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: Validation);
            if (model != null)
                tag.MergeAttribute("value", ((Guid)model).ToString());
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }
    }
}
