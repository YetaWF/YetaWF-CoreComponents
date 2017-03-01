/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class Guid<TModel> : RazorTemplate<TModel> { }

    public static class GuidHelper {
#if MVC6
        public static HtmlString RenderGuid(this IHtmlHelper htmlHelper, string name, Guid? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#else
        public static HtmlString RenderGuid(this HtmlHelper<object> htmlHelper, string name, Guid? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#endif
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: Validation);
            if (model != null)
                tag.MergeAttribute("value", ((Guid)model).ToString());
            return tag.ToHtmlString(TagRenderMode.SelfClosing);
        }
    }
}
