/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Configuration;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Support {
    public static class WebConfigHelper {

        private const string IOMODE_FORMAT = "IOMode-{0}";

        public enum IOModeEnum {
            [EnumDescription("Mixed file/SQL database (web.config ConnectionStrings)")]
            Determine = 0,      // determines I/O mode based on presence/absence of connectionstring
            [EnumDescription("Use file system")]
            File = 1,           // Use file system
            [EnumDescription("Use SQL database")]
            Sql = 2,            // Use SQL tables
            //RFFU - expect additional I/O methods - don't assume we just have File/Sql
            //RFFU - It's up to the individual dataprovider to support what they want/can.
        }
        public static TYPE GetValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE)) {
            string totalKey = string.Format("P:{0}:{1}", areaName, key);
            string val = ConfigurationManager.AppSettings[totalKey];
            if (string.IsNullOrWhiteSpace(val)) return dflt;
            if (typeof(TYPE).IsEnum) return (TYPE) (object) Convert.ToInt32(val);
            if (typeof(TYPE) == typeof(bool)) {
                bool boolVal;
                if (string.Compare(val, "True", true) == 0 || val == "1")
                    boolVal = true;
                else if (string.Compare(val, "False", true) == 0 || val == "0")
                    boolVal = false;
                else
                    throw new InternalError("Invalid bool value for {0}:{1}", areaName, key);
                return (TYPE) (object) (boolVal);
            }
            if (typeof(TYPE) == typeof(int)) return (TYPE) (object) Convert.ToInt32(val);
            if (typeof(TYPE) == typeof(long)) return (TYPE) (object) Convert.ToInt64(val);
            if (typeof(TYPE) == typeof(System.TimeSpan)) return (TYPE) (object) new System.TimeSpan(Convert.ToInt64(val));
            return (TYPE) (object) val;
        }
        public static void SetValue<TYPE>(string areaName, string key, TYPE value) {
            string val;
            if (value == null || value.Equals(default(TYPE)))
                RemoveValue(areaName, key);
            else {
                if (typeof(TYPE) == typeof(TimeSpan)) {
                    TimeSpan ts = (TimeSpan) (object) value;
                    val = ts.Ticks.ToString();
                } else
                    val = value.ToString();
                SetValue(string.Format("P:{0}:{1}", areaName, key), val);
            }
        }
        public static void SetValue(string totalKey, string value) {
            Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
            AppSettingsSection appSettings = config.AppSettings;
            appSettings.Settings.Remove(totalKey);
            appSettings.Settings.Add(new KeyValueConfigurationElement(totalKey, value));
            config.Save();
        }
        public static void RemoveValue(string areaName, string key) {
            RemoveValue(string.Format("P:{0}:{1}", areaName, key));
        }
        public static void RemoveValue(string totalKey) {
            Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
            AppSettingsSection appSettings = config.AppSettings;
            appSettings.Settings.Remove(totalKey);
            config.Save();
        }
    }
}
