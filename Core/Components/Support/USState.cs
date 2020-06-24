/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Maintains a list of all US states.
    /// </summary>
    /// <remarks>The list of US states is cached. Any changes to the list require a site restart.
    ///
    /// The list of US states is located at .\CoreComponents\Core\Addons\_Templates\USState\USStates.txt.</remarks>
    public static class USState {

        /// <summary>
        /// Returns the list of US states.
        /// </summary>
        /// <returns>Returns a list of US states suitable for use in a dropdownlist.</returns>
        /// <remarks>The list is cached. Any changes to the list require a site restart.
        ///
        /// The list of US states is located at .\CoreComponents\Core\Addons\_Templates\USState\USStates.txt.</remarks>
        public static async Task<List<SelectionItem<string>>> ReadStatesListAsync() {
            if (_statesList == null) {
                List<SelectionItem<string>> list = new List<SelectionItem<string>>();
                Dictionary<string, string> states = await ReadUSStatesListAsync();
                foreach (string abbrev in states.Keys) {
                    list.Add(new SelectionItem<string> { Text = states[abbrev], Value = abbrev });
                }
                _statesList = list;
            }
            return _statesList;
        }
        private static List<SelectionItem<string>> _statesList = null;

        /// <summary>
        /// Get a state's displayable name from a state abbreviation (2 characters).
        /// </summary>
        /// <param name="abbrev">A 2 character state abbreviation.</param>
        /// <returns>Returns the state's displayable name.</returns>
        public static async Task<string> GetStateNameAsync(string abbrev) {
            Dictionary<string, string> states = await ReadUSStatesListAsync();
            string name = states[abbrev?.ToUpper()];
            return name ?? "??";
        }

        /// <summary>
        /// Returns the list of US states.
        /// </summary>
        /// <returns>Returns a dictionary of US states. The key represents the state abbreviation. The value is the displayable state name.</returns>
        /// <remarks>The dictionary is cached. Any changes to the dictionary require a site restart.
        ///
        /// The list of US states is located at .\CoreComponents\Core\Addons\_Templates\USState\USStates.txt.</remarks>
        public static async Task<Dictionary<string,string>> ReadUSStatesListAsync() {
            if (_usStatesList == null) {
                Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;// Core package
                string url = VersionManager.GetAddOnTemplateUrl(package.AreaName, "USState");
                string path = Utility.UrlToPhysical(url);
                string file = Path.Combine(path, "USStates.txt");

                Dictionary<string, string> dict = new Dictionary<string, string>();

                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file)) throw new InternalError("US States file not found");

                List<string> sts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
                foreach (var st in sts) {
                    string[] s = st.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length != 2)
                        throw new InternalError("Invalid input in US states list - {0}", file);
                    dict.Add(s[0].ToUpper(), s[1]);
                }
                _usStatesList = dict;
            }
            return _usStatesList;
        }
        private static Dictionary<string, string> _usStatesList = null;
    }
}
