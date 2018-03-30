/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using System.Threading.Tasks;
using YetaWF.Core.IO;
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

        public class Currency {

            public const int MaxId = 3;
            public const string DefaultId = "USD";
            public const int DefaultMinorUnit = 2;

            public string Name { get; set; }
            public string Id { get; set; }
            public int Number { get; set; }
            public int MinorUnit { get; set; }
        }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(CurrencyISO4217Helper), name, defaultValue, parms); }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

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

            List<Currency> currencies = await GetCurrenciesAsync(IncludeSiteCurrency: includeSiteCurrency);
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
        /// <summary>
        /// Convert a currency name to an ISO 4217 three character Id.
        /// </summary>
        /// <param name="currency">The currency name.</param>
        /// <param name="AllowMismatch">true to return a default value if the currency name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Three character Id.</returns>
        public static async Task<string> CurrencyToIdAsync(string currency, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(currency))
                return Manager.CurrentSite.Currency;
            string id = (from c in await GetCurrenciesAsync() where c.Name == currency select c.Id).FirstOrDefault();
            if (id != null)
                return id;
            if (AllowMismatch)
                return Manager.CurrentSite.Currency;
            throw new InternalError("Invalid currency {0}", currency);
        }
        /// <summary>
        /// Convert an ISO 4217 three character id into a currency name.
        /// </summary>
        /// <param name="id">The three character Id.</param>
        /// <returns>The currency name.</returns>
        public static async Task<string> IdToCurrencyAsync(string id, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(id))
                id = Manager.CurrentSite.Currency;
            string currency = (from c in await GetCurrenciesAsync() where c.Id == id select c.Name).FirstOrDefault();
            if (AllowMismatch) {
                currency = (from c in await GetCurrenciesAsync() where c.Id == Manager.CurrentSite.Currency select c.Name).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(currency))
                    return currency;
            }
            throw new InternalError("Invalid currency Id {0}", id);
        }
        /// <summary>
        /// Convert a three character currency Id to an ISO 4217 numeric Id.
        /// </summary>
        /// <param name="currency">The three character currency Id.</param>
        /// <param name="AllowMismatch">true to return a default value if the currency name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Numeric Id.</returns>
        public static async Task<int> CurrencyIdToNumberAsync(string id, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(id))
                id = Manager.CurrentSite.Currency;
            int number = (from c in await GetCurrenciesAsync() where c.Name == id select c.Number).FirstOrDefault();
            if (number != 0)
                return number;
            if (AllowMismatch) {
                number = (from c in await GetCurrenciesAsync() where c.Id == Manager.CurrentSite.Currency select c.Number).FirstOrDefault();
                if (number != 0)
                    return number;
            }
            throw new InternalError("Invalid currency Id {0}", id);
        }
        /// <summary>
        /// Convert an ISO 4217 numeric Id into a three character currency Id.
        /// </summary>
        /// <param name="id">The numeric Id.</param>
        /// <returns>The three character currency Id.</returns>
        public static async Task<string> NumberToCurrencyIdAsync(int number, bool AllowMismatch = false) {
            if (number == 0)
                return Manager.CurrentSite.Currency;
            string currency = (from c in await GetCurrenciesAsync() where c.Number == number select c.Name).FirstOrDefault();
            if (AllowMismatch) {
                currency = (from c in await GetCurrenciesAsync() where c.Id == Manager.CurrentSite.Currency select c.Name).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(currency))
                    return currency;
            }
            throw new InternalError("Invalid currency number {0}", number);
        }
        private static async Task<List<Currency>> GetCurrenciesAsync(bool IncludeSiteCurrency = true) {
            List<Currency> currencies = (await ReadCurrencyListAsync()).OrderBy(m => m.Name).ToList();
            if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.Currency)) {
                Currency mainCurrency = (from c in currencies where c.Id == Manager.CurrentSite.Currency select c).FirstOrDefault();
                if (mainCurrency != null) {
                    currencies.Remove(mainCurrency);
                    if (!IncludeSiteCurrency) {
                        // don't include site currency
                    } else {
                        // move site currency to the top of the list
                        currencies.Insert(0, mainCurrency);
                    }
                }
            }
            return currencies;
        }
        private static async Task<List<Currency>> ReadCurrencyListAsync() {
            if (_currencyList == null) {
                using (await _lockObject.LockAsync()) { // short-term lock to vuild cached country list
                    Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                    string url = VersionManager.GetAddOnTemplateUrl(package.Domain, package.Product, "CurrencyISO4217");
                    string customUrl = VersionManager.GetCustomUrlFromUrl(url);

                    string path = YetaWFManager.UrlToPhysical(url);
                    string customPath = YetaWFManager.UrlToPhysical(customUrl);

                    string file = Path.Combine(path, "Currencies.txt");
                    string customFile = Path.Combine(customPath, "Currencies.txt");
                    if (await FileSystem.FileSystemProvider.FileExistsAsync(customFile))
                        file = customFile;
                    else if (!await FileSystem.FileSystemProvider.FileExistsAsync(file))
                        throw new InternalError("File {0} not found", file);

                    _currencyList = new List<Currency>();

                    List<string> cts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
                    foreach (var st in cts) {
                        if (st.Trim().Length > 0) {
                            string[] s = st.Trim().Split(new string[] { "," }, 4, StringSplitOptions.RemoveEmptyEntries);
                            if (s.Length != 4)
                                throw new InternalError("Invalid input in currency list - {0} - {1}", st, file);
                            _currencyList.Add(new Currency {
                                Name = s[0],
                                Id = s[1].ToUpper(),
                                Number = Convert.ToInt32(s[2]),
                                MinorUnit = Convert.ToInt32(s[3]),
                            });
                        }
                    }
                }
            }
            return _currencyList;
        }
        private static AsyncLock _lockObject = new AsyncLock();
        private static List<Currency> _currencyList = null;
    }
}
