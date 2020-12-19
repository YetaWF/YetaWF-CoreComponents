/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages AppSettings.json.
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

        public async Task InitAsync(string settingsFile) {
            if (!File.Exists(settingsFile)) // use local file system as we need this during initialization
                throw new InternalError("Settings file not found ({0})", settingsFile);
            SettingsFile = settingsFile;
            Settings = Utility.JsonDeserialize(File.ReadAllText(SettingsFile)); // use local file system as we need this during initialization

            Variables = new Dictionary<string, object>();

            string? env = Environment.GetEnvironmentVariable("YETAWF_DEPLOYSUFFIX");
            if (!string.IsNullOrWhiteSpace(env)) {
                Variables.Add("deploysuffix", env.ToLower());
                Variables.Add("DEPLOYSUFFIX", env.ToUpper());
            }

            // Process Includes
            JObject vars = Settings["Includes"];
            if (vars != null) {
                foreach (JToken var in vars.Children()) {
                    JProperty p = (JProperty)var;
                    string file = (string)p.Value;
                    await ProcessIncludeAsync(settingsFile, file, Variables);
                }
            }

            Variables varSubst = new Variables(null, Variables);

            vars = Settings["Variables"];
            if (vars != null) {
                foreach (JToken var in vars.Children()) {
                    JProperty p = (JProperty)var;
                    string s = varSubst.ReplaceVariables((string)p.Value);
                    Variables.Add(p.Name, s);
                }
            }
        }

        private async Task ProcessIncludeAsync(string settingsFile, string file, Dictionary<string, object> variables) {
            string folder = Path.GetDirectoryName(settingsFile)!;
            string includeFile = Path.Combine(folder, file);
            WebConfigBaseHelper configInclude = new WebConfigBaseHelper();
            await configInclude.InitAsync(includeFile);
            foreach (KeyValuePair<string, object> var in configInclude.Variables) {
                Variables[var.Key] = var.Value;
            }
        }

        private string SettingsFile = null!;
        private dynamic Settings = null!;

        protected internal Dictionary<string, object> Variables = null!;

        public TYPE? GetValue<TYPE>(string areaName, string key, TYPE? dflt = default, bool Package = true, bool Required = false) {
            dynamic val;
            try {
                // try environment variable first
                dynamic? env;
                if (Package)
                    env = Environment.GetEnvironmentVariable($"YETAWF_P_{areaName.ToUpper()}_{key.ToUpper()}");
                else
                    env = Environment.GetEnvironmentVariable($"YETAWF_{areaName.ToUpper()}_{key.ToUpper()}");
                if (env != null) {
                    val = Convert.ChangeType(env, typeof(TYPE));
                } else {
                    if (Package)
                        val = Settings["Application"]["P"][areaName];
                    else
                        val = Settings["Application"][areaName];
                    if (val == null) {
                        if (Required)
                            throw new InternalError($"The required entry {key} {(Package ? $"Application:P:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                        return dflt;
                    }
#if DEBUG
                    dynamic v = val[$"{DEBUG_PREFIX}{key}"];// try with debug prefix first
                    if (v != null)
                        val = v;
                    else
                        val = val[key];
#else
                    val = val[key]; // in release builds only use explicit key
#endif
                    if (val != null)
                        val = val.ToObject<TYPE>();
                }
                if (val == null) {
                    if (Required)
                        throw new InternalError($"The required entry {key} {(Package ? $"Application:P:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                    return dflt;
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
                    if (string.IsNullOrWhiteSpace(s)) {
                        if (Required)
                            throw new InternalError($"The required {(Package ? $"Application:Package:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                    }
                    return (TYPE)(object)s;
                }
            }
            return (TYPE)val;
        }

        public void SetValue<TYPE>(string areaName, string key, TYPE? value, bool Package = true) {
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
        public async Task SaveAsync() {
            string s = Utility.JsonSerialize(Settings, Indented: true);
            if (YetaWFManager.IsSync())
                File.WriteAllText(SettingsFile, s);
            else
                await File.WriteAllTextAsync(SettingsFile, s);
        }
    }
}
