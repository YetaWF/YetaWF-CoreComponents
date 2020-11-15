/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    /// <summary>
    /// This static class offers access to the list of currencies
    /// and implements a number of services to convert between different currency IDs.
    /// </summary>
    /// <remarks>The list of currencies is cached. Any changes to the list require a site restart.
    ///
    /// The list of currencies is located at .\CoreComponents\Core\Addons\_Templates\CurrencyISO4217\Currencies.txt.
    /// </remarks>
    public static class CurrencyISO4217 {

        /// <summary>
        /// An instance of this class describes one currency.
        /// </summary>
        public class Currency {

            /// <summary>
            /// The maximum length in characters of a ISO 4217 character currency ID.
            /// </summary>
            public const int MaxId = 3;
            /// <summary>
            /// The default currency ID.
            /// </summary>
            public const string DefaultId = "USD";
            /// <summary>
            /// The default number of digits for the fractional portion of the currency.
            /// </summary>
            public const int DefaultMinorUnit = 2;

            /// <summary>
            /// The name of the currency.
            /// </summary>
            public string Name { get; set; } = null!;
            /// <summary>
            /// The ISO 4217 three character currency ID.
            /// </summary>
            public string Id { get; set; } = null!;
            /// <summary>
            /// The ISO 4217 numeric Id.
            /// </summary>
            public int Number { get; set; }
            /// <summary>
            /// The number of digits for the fractional portion of the currency.  Can be 0 or -1 if there is no fractional portion.
            /// </summary>
            public int MinorUnit { get; set; }
        }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Converts a currency name to an ISO 4217 three character ID.
        /// </summary>
        /// <param name="currency">The currency name.</param>
        /// <param name="AllowMismatch">true to return a default value if the currency doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns a three character currency ID.</returns>
        public static async Task<string> CurrencyToIdAsync(string currency, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(currency))
                return Manager.CurrentSite.Currency;
            string? id = (from c in await GetCurrenciesAsync() where c.Name == currency select c.Id).FirstOrDefault();
            if (id != null)
                return id;
            if (AllowMismatch)
                return Manager.CurrentSite.Currency;
            throw new InternalError($"Invalid currency {currency}");
        }
        /// <summary>
        /// Converts an ISO 4217 three character currency ID into a currency name.
        /// </summary>
        /// <param name="id">The three character currency ID.</param>
        /// <param name="AllowMismatch">true to return a default value if the currency doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the currency name.</returns>
        public static async Task<string> IdToCurrencyAsync(string id, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(id))
                id = Manager.CurrentSite.Currency;
            string? currency = (from c in await GetCurrenciesAsync() where c.Id == id select c.Name).FirstOrDefault();
            if (AllowMismatch) {
                currency = (from c in await GetCurrenciesAsync() where c.Id == Manager.CurrentSite.Currency select c.Name).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(currency))
                    return currency;
            }
            throw new InternalError($"Invalid currency Id {id}");
        }
        /// <summary>
        /// Convert a three character currency ID to an ISO 4217 numeric ID.
        /// </summary>
        /// <param name="id">The three character currency ID.</param>
        /// <param name="AllowMismatch">true to return a default value if the currency name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the numeric currency ID.</returns>
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
            throw new InternalError($"Invalid currency Id {id}");
        }
        /// <summary>
        /// Converts an ISO 4217 numeric currency ID into a three character currency ID.
        /// </summary>
        /// <param name="number">The numeric currency ID.</param>
        /// <param name="AllowMismatch">true to return a default value if the currency name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the three character currency ID.</returns>
        public static async Task<string> NumberToCurrencyIdAsync(int number, bool AllowMismatch = false) {
            if (number == 0)
                return Manager.CurrentSite.Currency;
            string? currency = (from c in await GetCurrenciesAsync() where c.Number == number select c.Name).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(currency))
                return currency;
            if (AllowMismatch) {
                currency = (from c in await GetCurrenciesAsync() where c.Id == Manager.CurrentSite.Currency select c.Name).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(currency))
                    return currency;
            }
            throw new InternalError($"Invalid currency number {number}");
        }
        /// <summary>
        /// Returns a collection of all currencies.
        /// </summary>
        /// <param name="IncludeSiteCurrency">true to include the default currency for the YetaWF, false otherwise.
        /// If the default currency is included, it is moved to the top of the list.</param>
        /// <returns>Returns a collection of all currencies.</returns>
        public static async Task<List<Currency>> GetCurrenciesAsync(bool IncludeSiteCurrency = true) {
            List<Currency> currencies = (await ReadCurrencyListAsync()).OrderBy(m => m.Name).ToList();
            if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.Currency)) {
                Currency? mainCurrency = (from c in currencies where c.Id == Manager.CurrentSite.Currency select c).FirstOrDefault();
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
                Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;// Core package
                string url = VersionManager.GetAddOnTemplateUrl(package.AreaName, "CurrencyISO4217");
                string customUrl = VersionManager.GetCustomUrlFromUrl(url);

                string path = Utility.UrlToPhysical(url);
                string customPath = Utility.UrlToPhysical(customUrl);

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
        private static List<Currency>? _currencyList = null;
    }
}
