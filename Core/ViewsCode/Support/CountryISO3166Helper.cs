/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Components;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

// https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2

namespace YetaWF.Core.Views.Shared {

    public class CountryISO3166<TModel> : RazorTemplate<TModel> { }

    public static class CountryISO3166Helper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(CountryISO3166Helper), name, defaultValue, parms); }

#if MVC6
        public static async Task<HtmlString> RenderCountryISO3166Async(this IHtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#else
        public static async Task<HtmlString> RenderCountryISO3166Async(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#endif
            bool includeSiteCountry;
            if (!htmlHelper.TryGetParentModelSupportProperty<bool>(name, "SiteCountry", out includeSiteCountry))
                includeSiteCountry = true;

            List<CountryISO3166.Country> countries = CountryISO3166.GetCountries(IncludeSiteCountry: includeSiteCountry);
            List<SelectionItem<string>> list = (from l in countries select new SelectionItem<string>() {
                Text = l.Name,
                Value = l.Name,
            }).ToList();
            list.Insert(0, new SelectionItem<string> {
                Text = __ResStr("default", "(select)"),
                Value = "",
            });
            return await htmlHelper.RenderDropDownSelectionListAsync(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
    }
}
