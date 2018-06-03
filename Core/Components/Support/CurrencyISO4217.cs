/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

// https://www.iso.org/iso-4217-currency-codes.html

namespace YetaWF.Core.Components {

    public static class CurrencyISO4217 {

        public class Currency {

            public const int MaxId = 3;
            public const string DefaultId = "USD";
            public const int DefaultMinorUnit = 2;

            public string Name { get; set; }
            public string Id { get; set; }
            public int Number { get; set; }
            public int MinorUnit { get; set; }
        }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

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
        public static async Task<List<Currency>> GetCurrenciesAsync(bool IncludeSiteCurrency = true) {
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

                List<Currency> newList = new List<Currency>();

                List<string> cts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
                foreach (var st in cts) {
                    if (st.Trim().Length > 0) {
                        string[] s = st.Trim().Split(new string[] { "," }, 4, StringSplitOptions.RemoveEmptyEntries);
                        if (s.Length != 4)
                            throw new InternalError("Invalid input in currency list - {0} - {1}", st, file);
                        newList.Add(new Currency {
                            Name = s[0],
                            Id = s[1].ToUpper(),
                            Number = Convert.ToInt32(s[2]),
                            MinorUnit = Convert.ToInt32(s[3]),
                        });
                    }
                }
                _currencyList = newList;
            }
            return _currencyList;
        }
        private static List<Currency> _currencyList = null;
    }
}
