/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Web.Mvc;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public static class ListOfStringsHelper {

        public static MvcHtmlString RenderListOfStringsDisplay(this HtmlHelper<object> htmlHelper, string name, List<string> model, object HtmlAttributes = null) {
            HtmlBuilder hb = new HtmlBuilder();
            if (model == null || model.Count == 0 || (model.Count == 1 && string.IsNullOrWhiteSpace(model[0]))) return MvcHtmlString.Empty;

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
            return MvcHtmlString.Create(hb.ToString());
        }
    }
}
