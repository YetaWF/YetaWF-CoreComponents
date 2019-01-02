/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace YetaWF.Core.Support {

    public static class WebConfigHelper {

        public static Task InitAsync(string settingsFile) {
            if (!File.Exists(settingsFile)) // use local file system as we need this during initialization
                throw new InternalError("Appsettings.json file not found ({0})", settingsFile);
            SettingsFile = settingsFile;
            Settings = YetaWFManager.JsonDeserialize(File.ReadAllText(SettingsFile)); // use local file system as we need this during initialization
            return Task.CompletedTask;
        }

        private static string SettingsFile;
        private static dynamic Settings;

#if COMPARE
        // compares web.config to appsettings.json
        public static TYPE GetValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE), bool Package = true) {
            TYPE valNew = GetNewValue<TYPE>(areaName, key, dflt, Package);

            string totalKey;
            if (Package)
                totalKey = string.Format("P:{0}:{1}", areaName, key);
            else
                totalKey = string.Format("{0}:{1}", areaName, key);
            TYPE oldVal;
            string oldS = ConfigurationManager.AppSettings[totalKey];
            if (string.IsNullOrWhiteSpace(oldS)) { oldVal = (TYPE)(object)dflt; } else if (typeof(TYPE).IsEnum) oldVal = (TYPE)(object)Convert.ToInt32(oldS);
            else if (typeof(TYPE) == typeof(bool)) {
                bool boolVal;
                if (string.Compare(oldS, "True", true) == 0 || oldS == "1")
                    boolVal = true;
                else if (string.Compare(oldS, "False", true) == 0 || oldS == "0")
                    boolVal = false;
                else
                    throw new InternalError("Invalid bool value for {0}:{1}", areaName, key);
                oldVal = (TYPE)(object)boolVal;
            } else if (typeof(TYPE) == typeof(int)) oldVal = (TYPE)(object)Convert.ToInt32(oldS);
            else if (typeof(TYPE) == typeof(long)) oldVal = (TYPE)(object)Convert.ToInt64(oldS);
            else if (typeof(TYPE) == typeof(System.TimeSpan)) oldVal = (TYPE)(object)new System.TimeSpan(Convert.ToInt64(oldS));
            else
                oldVal = (TYPE)(object)oldS;

            if (!object.Equals(oldVal, valNew))
                valNew = valNew;// set breakpoint here
            return valNew;
        }
        private static TYPE GetNewValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE), bool Package = true) {
#else
        public static TYPE GetValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE), bool Package = true) {
#endif
            dynamic val;
            try {
                if (Package)
                    val = Settings["Application"]["P"][areaName];
                else
                    val = Settings["Application"][areaName];
                if (val == null) return dflt;
                val = val[key];
                if (val == null) return dflt;
                val = val.Value;
            } catch (Exception) {
                return dflt;
            }
            if (typeof(TYPE) == typeof(string)) {
                if (string.IsNullOrWhiteSpace((string)val))
                    return dflt;
                else
                    return (TYPE)val;
            } else if (typeof(TYPE).IsEnum) return (TYPE)(object)Convert.ToInt32(val);
            else if (typeof(TYPE) == typeof(bool)) {
                bool boolVal;
                if (val.GetType() == typeof(string)) {
                    string s = (string)val;
                    if (string.Compare(s, "True", true) == 0 || s == "1")
                        boolVal = true;
                    else if (string.Compare(s, "False", true) == 0 || s == "0")
                        boolVal = false;
                    else
                        throw new InternalError("Invalid bool value for {0}:{1}", areaName, key);
                } else
                    boolVal = Convert.ToBoolean(val);
                return (TYPE)(object)(boolVal);
            } else if (typeof(TYPE) == typeof(int)) return (TYPE)(object)Convert.ToInt32(val);
            else if (typeof(TYPE) == typeof(long)) return (TYPE)(object)Convert.ToInt64(val);
            else if (typeof(TYPE) == typeof(TimeSpan)) return (TYPE)(object)new TimeSpan(Convert.ToInt64(val));
            else if (typeof(TYPE) == typeof(Guid)) return (TYPE)(object)new Guid(val);
            return (TYPE)(object)val;
        }

        public static void SetValue<TYPE>(string areaName, string key, TYPE value, bool Package = true) {
            JObject jObj;
            if (Package)
                jObj = (JObject)Settings["Application"]["P"];
            else
                jObj = (JObject)Settings["Application"];
            JObject jArea = (JObject)jObj[areaName];
            if (jArea == null) {
                jObj.Add(areaName, new JObject());
                jArea = (JObject)jObj[areaName];
            }
            JToken jKey = jArea[key];
            if (jKey == null) {
                if (value != null)
                    jArea.Add(key, JToken.FromObject(value));
            } else {
                if (value != null)
                    jArea[key] = JToken.FromObject(value);
                else
                    jArea[key] = null;
            }
        }
        public static void SetValue(string totalKey, string value, bool Package = true) {
            // This is not currently used (except ::WEBCONFIG-SECTION:: which is not yet present in site templates)
            throw new InternalError("Updating Application Settings not supported");
        }
        public static Task SaveAsync() {
            string s = YetaWFManager.JsonSerialize(Settings, Indented: true);
            File.WriteAllText(SettingsFile, s);
            return Task.CompletedTask;
        }
    }
}
