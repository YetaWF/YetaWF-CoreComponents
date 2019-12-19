/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages Appsettings.json.
    /// </summary>
    /// <remarks>This class is used exclusively to manage Appsettings.json.
    ///
    /// It retrieves values and supports saving new values.
    ///
    /// For retrieval, variables embedded in the values are substituted.
    /// See the Appsettings.json topic for more information.</remarks>
    public static class WebConfigHelper {

        public static Task InitAsync(string settingsFile) {
            if (!File.Exists(settingsFile)) // use local file system as we need this during initialization
                throw new InternalError("Appsettings.json file not found ({0})", settingsFile);
            SettingsFile = settingsFile;
            Settings = Utility.JsonDeserialize(File.ReadAllText(SettingsFile)); // use local file system as we need this during initialization

            Variables = new Dictionary<string, object>();

            string env = Environment.GetEnvironmentVariable("YETAWF_DEPLOYSUFFIX");
            if (!string.IsNullOrWhiteSpace(env)) {
                Variables.Add("deploysuffix", env.ToLower());
                Variables.Add("DEPLOYSUFFIX", env.ToUpper());
            }

            Variables varSubst = new Variables(null, Variables);

            JObject vars = Settings["Variables"];
            if (vars != null) {
                foreach (JToken var in vars.Children()) {
                    JProperty p = (JProperty)var;
                    string s = varSubst.ReplaceVariables((string)p.Value);
                    Variables.Add(p.Name, s);
                }
            }
            return Task.CompletedTask;
        }

        private static string SettingsFile;
        private static dynamic Settings;

        public static Dictionary<string, object> Variables;

#if COMPARE
        // compares web.config to appsettings.json
        public static TYPE GetValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE), bool Package = true, bool Required = false) {
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
        public static TYPE GetValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE), bool Package = true, bool Required = false) {
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
                if (Required)
                    throw new InternalError($"The required {(Package ? $"Application:Package:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                return dflt;
            }
            if (typeof(TYPE) == typeof(string)) {
                if (string.IsNullOrWhiteSpace((string)val)) {
                    if (Required)
                        throw new InternalError($"The required {(Package ? $"Application:Package:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                    return dflt;
                } else {
                    Variables varSubst = new Variables(null, Variables);
                    string s = varSubst.ReplaceVariables((string)val);
                    return (TYPE)(object)s;
                }
            } else if (typeof(TYPE).IsEnum) {
                return (TYPE)(object)Convert.ToInt32(val);
            } else if (typeof(TYPE) == typeof(bool)) {
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
#if MVC6
        public static async Task SaveAsync() {
            string s = Utility.JsonSerialize(Settings, Indented: true);
            if (YetaWFManager.IsSync())
                File.WriteAllText(SettingsFile, s);
            else
                await File.WriteAllTextAsync(SettingsFile, s);
        }
#else
        public static Task SaveAsync() {
            string s = Utility.JsonSerialize(Settings, Indented: true);
            File.WriteAllText(SettingsFile, s);
            return Task.CompletedTask;
        }
#endif
    }
}
