/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
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

    public class PageLanguageId<TModel> : RazorTemplate<TModel> { }

    public static class LanguageIdHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(LanguageIdHelper), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderLanguageId(this IHtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#else
        public static HtmlString RenderLanguageId(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#endif
            bool useDefault = ! htmlHelper.GetControlInfo<bool>("", "NoDefault");
            bool allLanguages = htmlHelper.GetControlInfo<bool>("", "AllLanguages");

            List<SelectionItem<string>> list;
            if (allLanguages) {
                CultureInfo[] ci = CultureInfo.GetCultures(CultureTypes.AllCultures);
                list = (from c in ci orderby c.DisplayName select new SelectionItem<string>() {
                    Text = string.Format("{0} - {1}", c.DisplayName, c.Name),
                    Value = c.Name,
                }).ToList();
            } else {
                list = (from l in MultiString.Languages select new SelectionItem<string>() {
                    Text = l.ShortName,
                    Tooltip = l.Description,
                    Value = l.Id,
                }).ToList();
            }
            if (useDefault) {
                list.Insert(0, new SelectionItem<string> {
                    Text = __ResStr("default", "(Site Default)"),
                    Tooltip = __ResStr("defaultTT", "Use the site defined default language"),
                    Value = "",
                });
            } else {
                if (string.IsNullOrWhiteSpace(selection))
                    selection = MultiString.ActiveLanguage;
            }
            // display the languages in a drop down
            return htmlHelper.RenderDropDownSelectionList(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
    }
}
