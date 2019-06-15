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

    /// <summary>
    /// This static class offers access to the list of countries
    /// and implements a number of services to convert between different country IDs.
    /// </summary>
    /// <remarks>The list of countries is cached. Any changes to the list require a site restart.
    ///
    /// The list of countries is located at .\CoreComponents\Core\Addons\_Templates\CountryISO3166\Countries.txt.
    /// </remarks>
    public static class CountryISO3166 {

        /// <summary>
        /// An instance of this class describes one country.
        /// </summary>
        public class Country {
            /// <summary>
            /// The user displayable name of the country.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The ISO 3166 two character ID of the country.
            /// </summary>
            public string Id { get; set; }
            /// <summary>
            /// The ISO 3166 three character ID of the country.
            /// </summary>
            public string Id3 { get; set; }
            /// <summary>
            /// The ISO 3166 three digit number of the country.
            /// </summary>
            public string Number { get; set; }
            /// <summary>
            /// Defines the address type typically used by the country.
            /// </summary>
            /// <remarks>
            /// Possible values are US, Zip1, ZipLast, Generic.
            /// There values can be used to display an address in a suitable format.
            /// US represents a US address in the format typically used in the US: city, state ZIPcode.
            /// Zip1 represents an address with a zipcode or postal code in front of the city name.
            /// ZipLast represents an address with a zipcode or postal code after the city name.
            /// Generic represents an address just a city name (which may include postal code information).
            /// </remarks>
            public string AddressType { get; set; }

            /// <summary>
            /// Used with the Country.AddressType property.
            /// Represents a US address in the format typically used in the US: city, state ZIPcode.
            /// </summary>
            public const string US = "US";
            /// <summary>
            /// Used with the Country.AddressType property.
            /// Represents an address with a zipcode or postal code in front of the city name.
            /// </summary>
            public const string Zip1 = "Zip1";
            /// <summary>
            /// Used with the Country.AddressType property.
            /// Represents an address with a zipcode or postal code after the city name.
            /// </summary>
            public const string ZipLast = "ZipLast";
            /// <summary>
            /// Used with the Country.AddressType property.
            /// Represents an address just a city name (which may include postal code information).
            /// </summary>
            public const string Generic = "Generic";
        }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Converts a country name to an ISO 3166 two character ID.
        /// </summary>
        /// <param name="country">The country name.</param>
        /// <param name="AllowMismatch">true to return a default value if the country doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the two character country ID.</returns>
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
        /// Converts an ISO 3166 two character ID into a country name.
        /// </summary>
        /// <param name="id">The two character ID.</param>
        /// <param name="AllowMismatch">true to return a default value if the country name doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the country name.</returns>
        public static string IdToCountry(string id, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(id))
                return Manager.CurrentSite.Country;
            string country = (from c in GetCountries() where c.Id == id select c.Name).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(country))
                return country;
            if (AllowMismatch)
                return Manager.CurrentSite.Country;
            throw new InternalError("Invalid country id {0}", id);
        }

        /// <summary>
        /// Converts a country name to an ISO 3166 three character ID.
        /// </summary>
        /// <param name="country">The country name.</param>
        /// <param name="AllowMismatch">true to return a default value if the country doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the three character country ID.</returns>
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
        /// Converts an ISO 3166 three character ID into a country name.
        /// </summary>
        /// <param name="id">The three character ID.</param>
        /// <param name="AllowMismatch">true to return a default value if the country doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the country name.</returns>
        public static string Id3ToCountry(string id, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(id))
                return Manager.CurrentSite.Country;
            string country = (from c in GetCountries() where c.Id3 == id select c.Name).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(country))
                return country;
            if (AllowMismatch)
                return Manager.CurrentSite.Country;
            throw new InternalError("Invalid country id {0}", id);
        }

        /// <summary>
        /// Converts a country name to an ISO 3166 three digit number.
        /// </summary>
        /// <param name="country">The country name.</param>
        /// <param name="AllowMismatch">true to return a default value if the country doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the three digit country number.</returns>
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
        /// Converts an ISO 3166 three digit number into a country name.
        /// </summary>
        /// <param name="number">The three digit number.</param>
        /// <param name="AllowMismatch">true to return a default value if the country doesn't exist, false otherwise (throws an error).</param>
        /// <returns>Returns the country name.</returns>
        public static string NumberToCountry(string number, bool AllowMismatch = false) {
            if (string.IsNullOrWhiteSpace(number))
                return Manager.CurrentSite.Country;
            string country = (from c in GetCountries() where c.Number == number select c.Name).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(country))
                return country;
            if (AllowMismatch)
                return Manager.CurrentSite.Country;
            throw new InternalError("Invalid country number {0}", number);
        }

        /// <summary>
        /// Determine a country's address type.
        /// </summary>
        /// <param name="country">The country.</param>
        /// <returns>Returns the address type.</returns>
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
        /// <returns>Returns the information formatted based on the country's AddressType.</returns>
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

        /// <summary>
        /// Returns a collection of all countries.
        /// </summary>
        /// <param name="IncludeSiteCountry">true to include the country where the YetaWF site is located, false otherwise.
        /// If the country is included, it is moved to the top of the list.</param>
        /// <returns>Returns a collection of all countries.</returns>
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

        private static List<Country> CountryList {
            get {
                if (_CountryList == null)
                    _CountryList = CountryISO3166.ReadCountryListAsync().Result;// ok, only wait once, cached
                return _CountryList;
            }
        }
        private static List<Country> _CountryList = null;

        internal static async Task<List<Country>> ReadCountryListAsync() {
            string file;
            if (YetaWFManager.Manager.HostUsed == YetaWFManager.BATCHMODE) {

                file = ".\\Countries.txt";

            } else {
                Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;// Core package
                string url = VersionManager.GetAddOnTemplateUrl(package.AreaName, "CountryISO3166");
                string customUrl = VersionManager.GetCustomUrlFromUrl(url);

                string path = Utility.UrlToPhysical(url);
                string customPath = Utility.UrlToPhysical(customUrl);

                file = Path.Combine(path, "Countries.txt");
                string customFile = Path.Combine(customPath, "Countries.txt");
                if (await FileSystem.FileSystemProvider.FileExistsAsync(customFile))
                    file = customFile;
                else if (!await FileSystem.FileSystemProvider.FileExistsAsync(file))
                    throw new InternalError("File {0} not found", file);
            }

            List<Country> countryList = new List<Country>();

            List<string> cts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
            foreach (var st in cts) {
                if (st.Trim().Length > 0) {
                    string[] s = st.Trim().Split(new string[] { "+" }, 5, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length < 4 || s.Length > 5)
                        throw new InternalError("Invalid input in country list - {0} - {1}", st, file);
                    countryList.Add(new Country {
                        Name = s[0],
                        Id = s[1].ToUpper(),
                        Id3 = s[2].ToUpper(),
                        Number = s[3],
                        AddressType = s.Length > 4 ? s[4] : Country.Generic,
                    });
                }
            }
            return countryList.OrderBy(m => m.Name).ToList();
        }
    }
}
