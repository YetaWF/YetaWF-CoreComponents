/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages Appsettings.json.
    /// </summary>
    /// <remarks>This class is used exclusively to manage json settings files.
    ///
    /// It retrieves values and supports saving new values.
    ///
    /// For retrieval, variables embedded in the values are substituted.
    /// </remarks>
    public class WebConfigBaseHelper {

#if DEBUG
        public const string DEBUG_PREFIX = "[Debug]";
#endif

        public Task InitAsync(string settingsFile) {
            if (!File.Exists(settingsFile)) // use local file system as we need this during initialization
                throw new InternalError("Settings file not found ({0})", settingsFile);
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

        private string SettingsFile;
        private dynamic Settings;

        public Dictionary<string, object> Variables;

        public TYPE GetValue<TYPE>(string areaName, string key, TYPE dflt = default(TYPE), bool Package = true, bool Required = false) {
            dynamic val;
            try {
                // try environment variable first
                string env;
                if (Package)
                    env = Environment.GetEnvironmentVariable($"YETAWF_P_{areaName.ToUpper()}_{key.ToUpper()}");
                else
                    env = Environment.GetEnvironmentVariable($"YETAWF_{areaName.ToUpper()}_{key.ToUpper()}");
                if (env != null) {
                    val = env;
                } else {
                    if (Package)
                        val = Settings["Application"]["P"][areaName];
                    else
                        val = Settings["Application"][areaName];
                    if (val == null) return dflt;
#if DEBUG
                    dynamic v = val[$"{DEBUG_PREFIX}{key}"];// try with debug prefix first
                    if (v != null)
                        val = v;
                    else
                        val = val[key];
#else
                    val = val[key]; // in release builds only use explicit key
#endif
                    if (val == null) return dflt;
                    val = val.Value;
                }
            } catch (Exception) {
                if (Required)
                    throw new InternalError($"The required entry {key} {(Package ? $"Application:P:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
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
                int intEnum;
                if (int.TryParse(val.ToString(), out intEnum))
                    return (TYPE)(object)intEnum;
                else {
                    object newVal = null;
                    try {
                        newVal = Enum.Parse(typeof(TYPE), val.ToString());
                    } catch (Exception) {
                        newVal = Enum.ToObject(typeof(TYPE), val);
                    }
                    return (TYPE)(object)newVal;
                }
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
            else if (typeof(TYPE) == typeof(long?)) return val != null ? (TYPE)(object)Convert.ToInt64(val) : default(TYPE);
            else if (typeof(TYPE) == typeof(TimeSpan)) return (TYPE)(object)new TimeSpan(Convert.ToInt64(val));
            else if (typeof(TYPE) == typeof(Guid)) return (TYPE)(object)new Guid(val);
            return (TYPE)(object)val;
        }

        public void SetValue<TYPE>(string areaName, string key, TYPE value, bool Package = true) {
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
#if MVC6
        public async Task SaveAsync() {
            string s = Utility.JsonSerialize(Settings, Indented: true);
            if (YetaWFManager.IsSync())
                File.WriteAllText(SettingsFile, s);
            else
                await File.WriteAllTextAsync(SettingsFile, s);
        }
#else
        public Task SaveAsync() {
            string s = Utility.JsonSerialize(Settings, Indented: true);
            File.WriteAllText(SettingsFile, s);
            return Task.CompletedTask;
        }
#endif
    }
}
