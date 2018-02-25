/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Net;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {
    public class GeoLocation {

        public GeoLocation(YetaWFManager manager) { Manager = manager; }
        public GeoLocation() { Manager = null; }

        protected YetaWFManager Manager { get; private set; }

        public class UserInfo {
            public string IPAddress { get; set; }
            //public string HostName { get; set; }
            public float Latitude { get; set; }
            public float Longitude { get; set; }
            public string Region { get; set; }
            public string RegionCode { get; set; }
            public string RegionName { get; set; }
            public string City { get; set; }
            public string CountryCode { get; set; }
            public string CountryName { get; set; }
            public string ContinentCode { get; set; }
            public string CurrencyCode { get; set; }
            public string CurrencySymbol { get; set; }
        }

        public UserInfo GetCurrentUserInfo() {
            if (Manager == null) throw new InternalError("Must initialize with Manager instance");
            UserInfo info = Manager.SessionSettings.SiteSettings.GetValue<UserInfo>("YetaWF_Core_GeoLocationUserInfo");
            if (info == null) {
                string ipAddress = Manager.UserHostAddress;
                info = GetUserInfo(ipAddress);
                // save what we got in session storage so we don't need to retrieve it again
                Manager.SessionSettings.SiteSettings.SetValue<UserInfo>("YetaWF_Core_GeoLocationUserInfo", info);
                Manager.SessionSettings.SiteSettings.Save();
            }
            return info;
        }

        public UserInfo GetUserInfo(string ipAddress) {
            UserInfo info = new UserInfo();

            // Get host name (from IP) - TOO SLOW
            //string hostName = null;
            //try {
            //    IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
            //    hostName = hostEntry.HostName;
            //} catch { }

            info.IPAddress = ipAddress;
            //info.HostName = hostName;

            // Get geolocation data from http://www.geoplugin.net/
            GeoData geoData = GetGeoData(ipAddress);
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

        public class GeoData {
            public string geoplugin_request { get; set; }
            public int geoplugin_status { get; set; }
            public string geoplugin_credit { get; set; }
            public string geoplugin_city { get; set; }
            public string geoplugin_region { get; set; }
            public string geoplugin_areaCode { get; set; }
            public string geoplugin_dmaCode { get; set; }
            public string geoplugin_countryCode { get; set; }
            public string geoplugin_countryName { get; set; }
            public string geoplugin_continentCode { get; set; }
            public string geoplugin_latitude { get; set; }
            public string geoplugin_longitude { get; set; }
            public string geoplugin_regionCode { get; set; }
            public string geoplugin_regionName { get; set; }
            public string geoplugin_currencyCode { get; set; }
            public string geoplugin_currencySymbol { get; set; }
            public string geoplugin_currencySymbol_UTF8 { get; set; }
            public float geoplugin_currencyConverter { get; set; }
        }

        private GeoData GetGeoData(string ipAddress) {

            if (ipAddress == "127.0.0.1")
                return null;
            UriBuilder uri = new UriBuilder(string.Format("http://www.geoplugin.net/json.gp?ip={0}", ipAddress));
            GeoData geoData = null;
            try {
                var http = (HttpWebRequest)WebRequest.Create(uri.ToString());
                http.Accept = "application/json";
                http.Method = "GET";
                System.Net.WebResponse resp;
                resp = http.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                string response = sr.ReadToEnd().Trim();
                geoData = YetaWFManager.JsonDeserialize<GeoData>(response);
            } catch (Exception exc) {
                Logging.AddErrorLog("geoplugin failed - {0} - ip address {1}", exc.Message, ipAddress);
                return null;
            }
            if (geoData.geoplugin_status != 200) {
                Logging.AddErrorLog("geoplugin_status {0} for ip address {1}", geoData.geoplugin_status, ipAddress);
                return null;
            }
            return geoData;
        }
    }
}
