/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public static class ListOfStringsHelper {
#if MVC6
        public static HtmlString RenderListOfStringsDisplay(this IHtmlHelper htmlHelper, string name, List<string> model, object HtmlAttributes = null) {
#else
        public static HtmlString RenderListOfStringsDisplay(this HtmlHelper<object> htmlHelper, string name, List<string> model, object HtmlAttributes = null) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            if (model == null || model.Count == 0 || (model.Count == 1 && string.IsNullOrWhiteSpace(model[0]))) return HtmlStringExtender.Empty;

            hb.Append("<div class='yt_listofstrings t_display'>");

            string delim = htmlHelper.GetControlInfo<string>("", "Delimiter", ", ");

            bool first = true;
            foreach (var s in model) {
                if (first)
                    first = false;
                else
                    hb.Append(delim);
                hb.Append(YetaWFManager.HtmlEncode(s));
            }
            hb.Append("</div>");
            return hb.ToHtmlString();
        }
    }
}
