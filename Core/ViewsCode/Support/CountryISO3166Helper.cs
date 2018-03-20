/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
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

        public class Country {
            public string Name { get; set; }
            public string Id { get; set; }
            public string Id3 { get; set; }
            public string Number { get; set; }
            public string AddressType { get; set; }

            public const string US = "US";
            public const string Zip1 = "Zip1";
            public const string ZipLast = "ZipLast";
            public const string Generic = "Generic";
        }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(CountryISO3166Helper), name, defaultValue, parms); }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static async Task<HtmlString> RenderCountryISO3166Async(this IHtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#else
        public static async Task<HtmlString> RenderCountryISO3166Async(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
#endif
            bool includeSiteCountry;
            if (!htmlHelper.TryGetParentModelSupportProperty<bool>(name, "SiteCountry", out includeSiteCountry))
                includeSiteCountry = true;

            List<Country> countries = GetCountries(IncludeSiteCountry: includeSiteCountry);
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
        /// <summary>
        /// Convert a country name to an ISO 3166 two character Id.
        /// </summary>
        /// <param name="country">The country name.</param>
        /// <param name="AllowMismatch">true to return a default value if the country name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Two character Id.</returns>
        public static string CountryToId(string country, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(country))
                country = Manager.CurrentSite.Country;
            string id = (from c in GetCountries() where c.Name == country select c.Id).FirstOrDefault();
            if (id != null)
                return id;
            if (AllowMismatch) {
                id = (from c in GetCountries() where c.Name == Manager.CurrentSite.Country select c.Id).FirstOrDefault();
                if (id != null)
                    return id;
            }
            throw new InternalError("Invalid country {0}", country);
        }
        /// <summary>
        /// Convert an ISO 3166 two character id into a country name.
        /// </summary>
        /// <param name="id">The two character Id.</param>
        /// <returns>The country name.</returns>
        public static string IdToCountry(string id, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(id))
                return Manager.CurrentSite.Country;
            string country = (from c in GetCountries() where c.Id == id select c.Name).FirstOrDefault();
            if (AllowMismatch)
                return Manager.CurrentSite.Country;
            throw new InternalError("Invalid country id {0}", id);
        }
        /// <summary>
        /// Convert a country name to an ISO 3166 three character Id.
        /// </summary>
        /// <param name="country">The country name.</param>
        /// <param name="AllowMismatch">true to return a default value if the country name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Three character Id.</returns>
        public static string CountryToId3(string country, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(country))
                country = Manager.CurrentSite.Country;
            string id = (from c in GetCountries() where c.Name == country select c.Id3).FirstOrDefault();
            if (id != null)
                return id;
            if (AllowMismatch) {
                id = (from c in GetCountries() where c.Name == Manager.CurrentSite.Country select c.Id3).FirstOrDefault();
                if (id != null)
                    return id;
            }
            throw new InternalError("Invalid country {0}", country);
        }
        /// <summary>
        /// Convert an ISO 3166 three character id into a country name.
        /// </summary>
        /// <param name="id">The three character Id.</param>
        /// <returns>The country name.</returns>
        public static string Id3ToCountry(string id, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(id))
                return Manager.CurrentSite.Country;
            string country = (from c in GetCountries() where c.Id3 == id select c.Name).FirstOrDefault();
            if (AllowMismatch)
                return Manager.CurrentSite.Country;
            throw new InternalError("Invalid country id {0}", id);
        }
        /// <summary>
        /// Convert a country name to an ISO 3166 three digit number.
        /// </summary>
        /// <param name="country">The country name.</param>
        /// <param name="AllowMismatch">true to return a default value if the country name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Three digit number.</returns>
        public static string CountryToNumber(string country, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(country))
                country = Manager.CurrentSite.Country;
            string number = (from c in GetCountries() where c.Name == country select c.Number).FirstOrDefault();
            if (number != null)
                return number;
            if (AllowMismatch) {
                number = (from c in GetCountries() where c.Name == Manager.CurrentSite.Country select c.Number).FirstOrDefault();
                if (number != null)
                    return number;
            }
            throw new InternalError("Invalid country {0}", country);
        }
        /// <summary>
        /// Convert an ISO 3166 three digit number into a country name.
        /// </summary>
        /// <param name="number">The three digit number.</param>
        /// <returns>The country name.</returns>
        public static string NumberToCountry(string number, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(number))
                return Manager.CurrentSite.Country;
            string country = (from c in GetCountries() where c.Number == number select c.Name).FirstOrDefault();
            if (AllowMismatch)
                return Manager.CurrentSite.Country;
            throw new InternalError("Invalid country number {0}", number);
        }
        /// <summary>
        /// Determine a country's address type.
        /// </summary>
        /// <param name="country">The country.</param>
        /// <returns>The address type.</returns>
        public static string CountryToAddressType(string country) {
            if (string.IsNullOrWhiteSpace(country))
                return Manager.CurrentSite.Country;
            if (string.IsNullOrWhiteSpace(country))
                return null;
            return (from c in GetCountries() where c.Name == country select c.AddressType).FirstOrDefault();
        }
        /// <summary>
        /// Given a country name, combine the city, state and zip fields for user display.
        /// </summary>
        /// <param name="country">The country name.</param>
        /// <param name="city">The city portion of the address.</param>
        /// <param name="state">The state portion of the address.</param>
        /// <param name="zip">The zip code/postal code portion of the address.</param>
        /// <returns></returns>
        public static string CombineCityStateZip(string country, string city, string state, string zip) {
            string addressType = CountryISO3166Helper.CountryToAddressType(country);
            if (addressType == CountryISO3166Helper.Country.US)
                return city + ", " + state + " " + zip;
            else if (addressType == CountryISO3166Helper.Country.Zip1)
                return (string.IsNullOrWhiteSpace(zip) ? "" : zip + " ") + city;
            else if (addressType == CountryISO3166Helper.Country.ZipLast)
                return (string.IsNullOrWhiteSpace(city) ? "" : city + " ") + zip;
#if EXAMPLE
            else if (addressType == "DE")
                return (string.IsNullOrWhiteSpace(zip) ? "" : zip + " ") + city;
#endif
            //else if (addressType == CountryISO3166Helper.Country.Generic)
            return (string.IsNullOrWhiteSpace(city) ? "" : city + " ") + zip;
        }

        private static List<Country> GetCountries(bool IncludeSiteCountry = true) {
            List<Country> countries = ReadCountryList().OrderBy(m => m.Name).ToList();
            if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.Country)) {
                Country mainCountry = (from c in countries where c.Name == Manager.CurrentSite.Country select c).FirstOrDefault();
                if (mainCountry != null) {
                    countries.Remove(mainCountry);
                    if (!IncludeSiteCountry) {
                        // don't include site country
                    } else {
                        // move site country to the top of the list
                        countries.Insert(0, mainCountry);
                    }
                }
            }
            return countries;
        }
        private static List<Country> ReadCountryList() {
            if (_countryList == null) {
                lock (_lockObject) { // short-term lock to build cached country list
                    Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                    string url = VersionManager.GetAddOnTemplateUrl(package.Domain, package.Product, "CountryISO3166");
                    string customUrl = VersionManager.GetCustomUrlFromUrl(url);

                    string path = YetaWFManager.UrlToPhysical(url);
                    string customPath = YetaWFManager.UrlToPhysical(customUrl);

                    string file = Path.Combine(path, "Countries.txt");
                    string customFile = Path.Combine(customPath, "Countries.txt");
                    if (File.Exists(customFile))
                        file = customFile;
                    else if (!File.Exists(file))
                        throw new InternalError("File {0} not found", file);

                    _countryList = new List<Country>();

                    string[] cts = File.ReadAllLines(file);
                    foreach (var st in cts) {
                        if (st.Trim().Length > 0) {
                            string[] s = st.Trim().Split(new string[] { "+" }, 5, StringSplitOptions.RemoveEmptyEntries);
                            if (s.Length < 4 || s.Length > 5)
                                throw new InternalError("Invalid input in country list - {0} - {1}", st, file);
                            _countryList.Add(new Country {
                                Name = s[0],
                                Id = s[1].ToUpper(),
                                Id3 = s[2].ToUpper(),
                                Number = s[3],
                                AddressType = s.Length > 4 ? s[4] : Country.Generic,
                            });
                        }
                    }
                }
            }
            return _countryList;
        }
        private static object _lockObject = new object();
        private static List<Country> _countryList = null;
    }
}
