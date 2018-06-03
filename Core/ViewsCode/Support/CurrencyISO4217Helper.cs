/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using System.Threading.Tasks;
using YetaWF.Core.Components;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

// https://www.iso.org/iso-4217-currency-codes.html

namespace YetaWF.Core.Views.Shared {

    public class CurrencyISO4217<TModel> : RazorTemplate<TModel> { }

    public static class CurrencyISO4217Helper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(CurrencyISO4217Helper), name, defaultValue, parms); }

#if MVC6
        public static async Task<HtmlString> RenderCurrencyISO4217DisplayAsync(this IHtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#else
        public static async Task<HtmlString> RenderCurrencyISO4217DisplayAsync(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#endif
            string currency = await IdToCurrencyAsync(selection, AllowMismatch: true);
            return new HtmlString(currency);
        }

#if MVC6
        public static async Task<HtmlString> RenderCurrencyISO4217Async(this IHtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#else
        public static async Task<HtmlString> RenderCurrencyISO4217Async(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#endif
            bool includeSiteCurrency;
            if (!htmlHelper.TryGetParentModelSupportProperty<bool>(name, "SiteCurrency", out includeSiteCurrency))
                includeSiteCurrency = true;

            List<Currency> currencies = await CurrencyISO4217.GetCurrenciesAsync(IncludeSiteCurrency: includeSiteCurrency);
            List<SelectionItem<string>> list = (from l in currencies select new SelectionItem<string>() {
                Text = l.Name,
                Value = l.Id,
            }).ToList();
            list.Insert(0, new SelectionItem<string> {
                Text = __ResStr("default", "(select)"),
                Value = "",
            });
            return await htmlHelper.RenderDropDownSelectionListAsync(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
    }
}
