/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Threading.Tasks;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Manages AppSettings.json.
    /// </summary>
    /// <remarks>This class is used exclusively to manage AppSettings.json.
    ///
    /// It retrieves values and supports saving new values.
    ///
    /// For retrieval, variables embedded in the values are substituted.
    /// See the AppSettings.json topic for more information.</remarks>
    public static class WebConfigHelper {

        private static WebConfigBaseHelper helper = new WebConfigBaseHelper();

        public static Dictionary<string, object> Variables {
            get { return helper.Variables; }
            internal set { Variables = value; }
        }

        public static Task InitAsync(string settingsFile) {
            return helper.InitAsync(settingsFile);
        }

        public static TYPE? GetValue<TYPE>(string areaName, string key, TYPE? dflt = default, bool Package = true, bool Required = false) {
            return helper.GetValue<TYPE>(areaName, key, dflt, Package, Required);
        }

        public static void SetValue<TYPE>(string areaName, string key, TYPE value, bool Package = true) {
            helper.SetValue<TYPE>(areaName, key, value, Package);
        }

        public static void SetValue(string? totalKey, string? value, bool Package = true) {
            // This is not currently used (except ::WEBCONFIG-SECTION:: which is not yet present in site templates)
            throw new InternalError("Updating Application Settings not supported");
        }

        public static Task SaveAsync() {
            return helper.SaveAsync();
        }
    }
}
