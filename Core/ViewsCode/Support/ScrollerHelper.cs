﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
using System.Web.Mvc.Html;
#endif

namespace YetaWF.Core.Views.Shared {

    public class Scroller<TModel> : RazorTemplate<TModel> { }

    public static class ScrollerHelper {
#if MVC6
        public static MvcHtmlString RenderScrollerDisplay<TModel>(this IHtmlHelper<TModel> htmlHelper, string name, object model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static MvcHtmlString RenderScrollerDisplay<TModel>(this HtmlHelper<TModel> htmlHelper, string name, object model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            string uiHint = htmlHelper.GetControlInfo<string>(name, "Template");
            if (uiHint == null) throw new InternalError("No UIHint available for scroller");

            IEnumerable items = model as IEnumerable;
            if (items == null) throw new InternalError("No enumerable model available for scroller");

            HtmlBuilder hb = new HtmlBuilder();
            foreach (var item in items) {
                TagBuilder tag = new TagBuilder("div");
                tag.AddCssClass("t_item");
                tag.SetInnerHtml(htmlHelper.DisplayFor(m => item, uiHint).AsString());
                hb.Append(tag.ToString(TagRenderMode.Normal));
            }
            return MvcHtmlString.Create(hb.ToString());
        }

    }
}