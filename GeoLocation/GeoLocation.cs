/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.GeoLocation {

    /// <summary>
    /// Manages geolocation information as provided by https://www.geoplugin.com/.
    /// </summary>
    public class GeoLocation {

        /// <summary>
        /// Constructor.
        /// </summary>
        public GeoLocation() { }

        /// <summary>
        /// An instance of the UserInfo class defines geolocation information for an IP address, retrieved using the GeoLocation.GetUserInfoAsync method.
        /// </summary>
        public class UserInfo {
            /// <summary>
            /// Defines the IP address.
            /// </summary>
            public string IPAddress { get; set; } = null!;
            //public string HostName { get; set; }
            /// <summary>
            /// Defines the longitude where the IP address is located.
            /// </summary>
            public float Latitude { get; set; }
            /// <summary>
            /// Defines the latitude where the IP address is located.
            /// </summary>
            public float Longitude { get; set; }
            /// <summary>
            /// Defines the region where the IP address is located.
            /// </summary>
            public string Region { get; set; } = null!;
            /// <summary>
            /// Defines the region code where the IP address is located.
            /// </summary>
            public string RegionCode { get; set; } = null!;
            /// <summary>
            /// Defines the region name where the IP address is located.
            /// </summary>
            public string RegionName { get; set; } = null!;
            /// <summary>
            /// Defines the city where the IP address is located.
            /// </summary>
            public string City { get; set; } = null!;
            /// <summary>
            /// Defines the country code where the IP address is located.
            /// </summary>
            public string CountryCode { get; set; } = null!;
            /// <summary>
            /// Defines the county where the IP address is located.
            /// </summary>
            public string CountryName { get; set; } = null!;
            /// <summary>
            /// Defines the continent code where the IP address is located.
            /// </summary>
            public string ContinentCode { get; set; } = null!;
            /// <summary>
            /// Defines the currency used where the IP address is located.
            /// </summary>
            public string CurrencyCode { get; set; } = null!;
            /// <summary>
            /// Defines the currency symbol used where the IP address is located.
            /// </summary>
            public string CurrencySymbol { get; set; } = null!;
        }

        private const int MAXREQUESTSPERMINUTE = 120 -10; // geoplugin allows 120 requests/minute, we subtract a safety margin

        private static object _lockObject = new object();

        private static DateTime InitialRequestTime { get; set; } = DateTime.Now;// Local time
        private static int RemainingRequests { get; set; } = MAXREQUESTSPERMINUTE;

        private static readonly HttpClientHandler Handler = new HttpClientHandler {
            AllowAutoRedirect = true,
            UseCookies = false,
        };
        private static readonly HttpClient Client = new HttpClient(Handler, true) {
            Timeout = new TimeSpan(0, 0, 20),
        };

        /// <summary>
        /// Returns the number of remaining requests that can be made before the limit is exceeded.
        /// </summary>
        /// <returns>Returns the number of remaining requests that can be made before the limit is exceeded.</returns>
        /// <remarks>Geolocation is a free service provided by http://www.geoplugin.net/.
        /// The available requests per minute are capped.
        /// To insure that this limit isn't exceeded, the GetRemainingRequests method should be used to determine whether requests can still be made.
        ///
        /// The number of remaining requests changes at regular intervals.
        /// </remarks>
        public int GetRemainingRequests() {
            lock (_lockObject) { // local lock to protect RemainingRequests
                if (InitialRequestTime < DateTime.Now.AddMinutes(-1)) {
                    InitialRequestTime = DateTime.Now;
                    RemainingRequests = MAXREQUESTSPERMINUTE;
                }
                return RemainingRequests;
            }
        }

        /// <summary>
        /// Retrieves geolocation information for a specified IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address for which geolocation is to be returned.</param>
        /// <returns>Returns the geolocation information or null if none is available.
        ///
        /// An exception occurs if no requests can be issued.</returns>
        public async Task<UserInfo> GetUserInfoAsync(string ipAddress) {
            UserInfo info = new UserInfo();

            // make sure we have any remaining requests available
            lock (_lockObject) { // local lock to protect RemainingRequests
                if (RemainingRequests <= 0)
                    throw new InternalError("Too many requests per minute");
                RemainingRequests = RemainingRequests - 1;
            }

            // Get host name (from IP) - TOO SLOW
            //string hostName = null;
            //try {
            //    IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
            //    hostName = hostEntry.HostName;
            //} catch { }

            // extract just IP address in case there is a port #
            string[] s = ipAddress.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            ipAddress = s[0];

            info.IPAddress = ipAddress;
            //info.HostName = hostName;

            // Get geolocation data from https://www.geoplugin.com/
            GeoData? geoData = await GetGeoDataAsync(ipAddress);
            if (geoData != null) {
                try {
                    info.Latitude = Convert.ToSingle(geoData.geoplugin_latitude);
                    info.Longitude = Convert.ToSingle(geoData.geoplugin_longitude);
                } catch (Exception) {
                    info.Latitude = 0;
                    info.Longitude = 0;
                }
                info.Region = geoData.geoplugin_region;
                info.RegionCode = geoData.geoplugin_regionCode;
                info.RegionName = geoData.geoplugin_regionName;
                info.City = geoData.geoplugin_city;
                info.CountryCode = geoData.geoplugin_countryCode;
                info.CountryName = geoData.geoplugin_countryName;
                info.ContinentCode = geoData.geoplugin_continentCode;
                info.CurrencyCode = geoData.geoplugin_currencyCode;
                info.CurrencySymbol = geoData.geoplugin_currencySymbol_UTF8;
            }
            return info;
        }

        internal class GeoData {
            public string geoplugin_request { get; set; } = null!;
            public int geoplugin_status { get; set; }
            public string geoplugin_credit { get; set; } = null!;
            public string geoplugin_city { get; set; } = null!;
            public string geoplugin_region { get; set; } = null!;
            public string geoplugin_areaCode { get; set; } = null!;
            public string geoplugin_dmaCode { get; set; } = null!;
            public string geoplugin_countryCode { get; set; } = null!;
            public string geoplugin_countryName { get; set; } = null!;
            public string geoplugin_continentCode { get; set; } = null!;
            public string geoplugin_latitude { get; set; } = null!;
            public string geoplugin_longitude { get; set; } = null!;
            public string geoplugin_regionCode { get; set; } = null!;
            public string geoplugin_regionName { get; set; } = null!;
            public string geoplugin_currencyCode { get; set; } = null!;
            public string geoplugin_currencySymbol { get; set; } = null!;
            public string geoplugin_currencySymbol_UTF8 { get; set; } = null!;
            public float geoplugin_currencyConverter { get; set; }
        }

        private async Task<GeoData?> GetGeoDataAsync(string? ipAddress) {

            if (ipAddress == "127.0.0.1")
                return null;

            GeoData? geoData = null;
            try {
                string url = $"http://www.geoplugin.net/json.gp?ip={ipAddress}";
                string? resp = null;

                using (var request = new HttpRequestMessage()) {
                    if (YetaWFManager.IsSync()) {
                        resp = Client.GetStringAsync(url).Result;
                    } else {
                        resp = await Client.GetStringAsync(url);
                    }
                    if (string.IsNullOrWhiteSpace(resp))
                        throw new InternalError($"Unable to obtain geodata");
                    geoData = Utility.JsonDeserialize<GeoData>(resp);
                }
            } catch (Exception exc) {
                Logging.AddErrorLog("geoplugin failed - {0} - ip address {1}", ErrorHandling.FormatExceptionMessage(exc), ipAddress);
                return null;
            }
            //if (geoData.geoplugin_status != 200) {
            //    Logging.AddErrorLog("geoplugin_status {0} for ip address {1}", geoData.geoplugin_status, ipAddress);
            //    return null;
            //}
            return geoData;
        }
    }
}
