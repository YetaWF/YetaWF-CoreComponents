/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

// https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2

namespace YetaWF.Core.Components {

    public class CountryISO3166Startup : IInitializeApplicationStartup {
        public Task InitializeApplicationStartupAsync() {
            return CountryISO3166.ReadCountryListAsync();
        }
    }
    public static class CountryISO3166 {

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

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

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
                country = Manager.CurrentSite.Country;
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
            string addressType = CountryISO3166.CountryToAddressType(country);
            if (addressType == CountryISO3166.Country.US)
                return city + ", " + state + " " + zip;
            else if (addressType == CountryISO3166.Country.Zip1)
                return (string.IsNullOrWhiteSpace(zip) ? "" : zip + " ") + city;
            else if (addressType == CountryISO3166.Country.ZipLast)
                return (string.IsNullOrWhiteSpace(city) ? "" : city + " ") + zip;
#if EXAMPLE
            else if (addressType == "DE")
                return (string.IsNullOrWhiteSpace(zip) ? "" : zip + " ") + city;
#endif
            //else if (addressType == CountryISO3166.Country.Generic)
            return (string.IsNullOrWhiteSpace(city) ? "" : city + " ") + zip;
        }

        public static List<Country> GetCountries(bool IncludeSiteCountry = true) {
            List<Country> countries = CountryList.ToList();
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

        private static List<Country> CountryList { get; set; }

        internal static async Task ReadCountryListAsync() {
            Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;// Core package
            string url = VersionManager.GetAddOnTemplateUrl(package.AreaName, "CountryISO3166");
            string customUrl = VersionManager.GetCustomUrlFromUrl(url);

            string path = YetaWFManager.UrlToPhysical(url);
            string customPath = YetaWFManager.UrlToPhysical(customUrl);

            string file = Path.Combine(path, "Countries.txt");
            string customFile = Path.Combine(customPath, "Countries.txt");
            if (await FileSystem.FileSystemProvider.FileExistsAsync(customFile))
                file = customFile;
            else if (!await FileSystem.FileSystemProvider.FileExistsAsync(file))
                throw new InternalError("File {0} not found", file);

            CountryList = new List<Country>();

            List<string> cts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
            foreach (var st in cts) {
                if (st.Trim().Length > 0) {
                    string[] s = st.Trim().Split(new string[] { "+" }, 5, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length < 4 || s.Length > 5)
                        throw new InternalError("Invalid input in country list - {0} - {1}", st, file);
                    CountryList.Add(new Country {
                        Name = s[0],
                        Id = s[1].ToUpper(),
                        Id3 = s[2].ToUpper(),
                        Number = s[3],
                        AddressType = s.Length > 4 ? s[4] : Country.Generic,
                    });
                }
            }
            CountryList = CountryList.OrderBy(m => m.Name).ToList();
        }
    }
}
