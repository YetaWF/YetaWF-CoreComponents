/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Models.Attributes;
#if MVC6
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
#else
using System.Collections.Generic;
using System.Configuration;
#endif

namespace YetaWF.Core.Support {

    public static class WebConfigHelper {

        private const string IOMODE_FORMAT = "IOMode-{0}";

        public enum IOModeEnum {
            [EnumDescription("Mixed file/SQL database (web.config/appsettings.json ConnectionStrings)")]
            Determine = 0,      // determines I/O mode based on presence/absence of connectionstring
            [EnumDescription("Use file system")]
            File = 1,           // Use file system
            [EnumDescription("Use SQL database")]
            Sql = 2,            // Use SQL tables
            //RFFU - expect additional I/O methods - don't assume we just have File/Sql
            //RFFU - It's up to the individual dataprovider to support what they want/can.
        }
#if MVC6
        public static void Init(IConfigurationRoot configuration, string appSettingsFile) {
            Configuration = configuration;
            AppSettingsFile = appSettingsFile;
            JavaScriptSerializer jser = new JavaScriptSerializer();
            Settings = jser.Deserialize<dynamic>(File.ReadAllText(AppSettingsFile));
        }

        private static IConfigurationRoot Configuration;
        private static string AppSettingsFile;
        private static dynamic Settings;
#else
#endif
        public static TYPE GetValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE), bool Package = true) {
            string totalKey;
            if (Package)
                totalKey = string.Format("P:{0}:{1}", areaName, key);
            else
                totalKey = string.Format("{0}:{1}", areaName, key);
#if MVC6
            string val = Configuration.GetSection("Application")[totalKey];
#else
            string val = ConfigurationManager.AppSettings[totalKey];
#endif
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

        public static void SetValue<TYPE>(string areaName, string key, TYPE value, bool Package = true) {
#if MVC6
            if (Package) {
                dynamic appSettings = Settings["Application"];
                dynamic packageSettings = appSettings["P"];
                dynamic areaSettings = packageSettings[areaName];
                areaSettings[key] = (object)value;
            } else
                throw new NotSupportedException();
#else
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
#endif
        }
        public static void SetValue(string totalKey, string value, bool Package = true) {
#if MVC6
            // This is not currently used (except ::WEBCONFIG-SECTION:: which is not yet present in site templates)
            throw new InternalError("Updating Application Settings not supported");
#else
            Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
            AppSettingsSection appSettings = config.AppSettings;
            appSettings.Settings.Remove(totalKey);
            appSettings.Settings.Add(new KeyValueConfigurationElement(totalKey, value));
            config.Save();
#endif
        }
#if MVC6
#else
        public static void RemoveValue(string areaName, string key) {
            RemoveValue(string.Format("P:{0}:{1}", areaName, key));
        }
        public static void RemoveValue(string totalKey) {
            Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
            AppSettingsSection appSettings = config.AppSettings;
            appSettings.Settings.Remove(totalKey);
            config.Save();
        }
#endif
        public static void Save() {
#if MVC6
            // MVC6 needs an explicit Save() call
            JavaScriptSerializer jser = new JavaScriptSerializer();
            string s = jser.Serialize(Settings);
            File.WriteAllText(AppSettingsFile, s);
#else
            // settings are immediately saved in SetValue()
#endif
        }
    }
}
