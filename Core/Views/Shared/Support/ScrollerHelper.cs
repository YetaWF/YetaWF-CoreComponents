/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class Scroller<TModel> : RazorTemplate<TModel> { }

    public static class ScrollerHelper {

        public static MvcHtmlString RenderScrollerDisplay<TModel>(this HtmlHelper<TModel> htmlHelper, string name, object model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {

            string uiHint = htmlHelper.GetControlInfo<string>(name, "Template");
            if (uiHint == null) throw new InternalError("No UIHint available for scroller");

            IEnumerable items = model as IEnumerable;
            if (items == null) throw new InternalError("No enumerable model available for scroller");

            HtmlBuilder hb = new HtmlBuilder();
            foreach (var item in items) {
                TagBuilder tag = new TagBuilder("div");
                tag.AddCssClass("t_item");
                tag.InnerHtml = htmlHelper.DisplayFor(m => item, uiHint).ToString();
                hb.Append(tag.ToString());
            }
            return MvcHtmlString.Create(hb.ToString());
        }

    }
}
