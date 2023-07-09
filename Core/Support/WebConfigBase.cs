/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

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
            Settings = JsonNode.Parse(File.ReadAllText(SettingsFile)) ?? throw new InternalError("Settings file ({0}) could not be parsed", settingsFile); ; // use local file system as we need this during initialization

            Variables = new Dictionary<string, object>();

            string? env = Environment.GetEnvironmentVariable("YETAWF_DEPLOYSUFFIX");
            if (!string.IsNullOrWhiteSpace(env)) {
                Variables.Add("deploysuffix", env.ToLower());
                Variables.Add("DEPLOYSUFFIX", env.ToUpper());
            }

            // Process Includes
            JsonNode? includes = Settings["Includes"];
            if (includes != null) {
                JsonElement elemIncludes = JsonSerializer.Deserialize<JsonElement>(includes);
                foreach (JsonProperty prop in elemIncludes.EnumerateObject()) {
                    if (prop.Name == "Files") {
                        string? file = prop.Value.GetString();
                        if (file != null)
                            await ProcessIncludeAsync(settingsFile, file, Variables);
                    } else
                        throw new InternalError($"Unexpected property {prop.Name} in Includes section");
                }
            }

            // Process Variables
            JsonNode? variables = (JsonNode?)Settings["Variables"];
            if (variables != null) {
                JsonElement elemVars = JsonSerializer.Deserialize<JsonElement>(variables);
                Variables varSubst = new Variables(null, Variables);
                foreach (JsonProperty prop in elemVars.EnumerateObject()) {
                    string s = varSubst.ReplaceVariables(prop.Value.GetString());
                    Variables.Add(prop.Name, s);
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
        private JsonNode Settings = null!;

        protected internal Dictionary<string, object> Variables = null!;

        public TYPE? GetValue<TYPE>(string areaName, string key, TYPE? dflt = default, bool Package = true, bool Required = false) {
            JsonElement? elem;
            object? val = null;
            try {
                // try environment variable first
                string? env;
                if (Package)
                    env = Environment.GetEnvironmentVariable($"YETAWF_P_{areaName.ToUpper()}_{key.ToUpper()}");
                else
                    env = Environment.GetEnvironmentVariable($"YETAWF_{areaName.ToUpper()}_{key.ToUpper()}");
                if (env != null) {
                    val = Convert.ChangeType(env, typeof(TYPE));
                } else {
                    if (Package)
                        elem = Settings.GetElementFromList("Application", "P", areaName);
                    else
                        elem = Settings.GetElementFromList("Application", areaName);
                    if (elem == null) {
                        if (Required)
                            throw new InternalError($"The required entry {key} {(Package ? $"Application:P:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                        return dflt;
                    }
                    string? v;
#if DEBUG
                    v = ((JsonElement)elem).GetPropertyValue($"{DEBUG_PREFIX}{key}");// try with debug prefix first
                    if (v == null)
                        v = ((JsonElement)elem).GetPropertyValue(key);
#else
                    v = ((JsonElement)elem).GetPropertyValue(key); // in release builds only use explicit key
#endif
                    if (v != null) {
                        Type t = typeof(TYPE);
                        if (t.IsEnum && v.GetType() == typeof(string)) {
                            try {
                                // try number first
                                int enumVal = Convert.ToInt32(v);
                                val = enumVal;
                            } catch (Exception) {
                                // could be a legit string
                                if (!Enum.TryParse(t, v, out val))
                                    val = Convert.ChangeType(v, t);
                            }
                        } else if (t == typeof(Guid)) {
                            val = new Guid(v);
                        } else {
                            val = Convert.ChangeType(v, t);
                        }
                    }
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
                        throw new InternalError($"The required {(Package ? $"Application:P:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                    return dflt;
                } else {
                    Variables varSubst = new Variables(null, Variables);
                    string s = varSubst.ReplaceVariables((string)val);
                    if (string.IsNullOrWhiteSpace(s)) {
                        if (Required)
                            throw new InternalError($"The required {(Package ? $"Application:P:{areaName}" : $"Application:{areaName}")} was not found in {SettingsFile}");
                    }
                    return (TYPE)(object)s;
                }
            }
            return (TYPE)val;
        }

        public void SetValue<TYPE>(string areaName, string key, TYPE? value, bool Package = true) {
            JsonNode sectionNode;
            if (Package)
                sectionNode = Settings["Application"]?["P"] ?? throw new InternalError("Application:P section not found");
            else
                sectionNode = Settings["Application"] ?? throw new InternalError("Application section not found");
            JsonNode? areaNode = sectionNode[areaName];
            if (areaNode == null) {
                areaNode = sectionNode[areaName] = new JsonObject();
            }
            areaNode[key] = value?.ToString();
        }
        public async Task SaveAsync() {
            string s = Utility.JsonSerialize(Settings, Utility._JsonSettingsIndented);
            if (YetaWFManager.IsSync())
                File.WriteAllText(SettingsFile, s);
            else
                await File.WriteAllTextAsync(SettingsFile, s);
        }
    }

    public static class JsonElementExtender {

        public static JsonElement? GetElementFromList(this JsonNode node, params string[] names) {
            foreach (var name in names) {
                JsonNode? childNode = node[name];
                if (childNode == null) return null;
                node = childNode;
            }
            JsonElement elem = JsonSerializer.Deserialize<JsonElement>(node);
            return elem;
        }

        public static string? GetPropertyValue(this JsonElement el, string name) {
            if (!el.TryGetProperty(name, out var prop))
                return null;
            if (prop.ValueKind == JsonValueKind.Null) 
                return null;
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return prop.GetRawText();
        }
    }
}
