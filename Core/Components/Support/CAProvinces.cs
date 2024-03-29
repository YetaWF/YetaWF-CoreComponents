﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
    /// This static class offers access to the list of Canadian provinces.
    ///
    /// This class is not used by applications. It is reserved for component implementation.
    /// </summary>
    public static class CAProvince {

        /// <summary>
        /// Retrieves the list of Canadian provinces in a format suitable for rendering in a dropdownlist component.
        /// </summary>
        /// <returns>List of Canadian provinces.</returns>
        /// <remarks>The list is cached. Any changes to the list require a site restart.
        ///
        /// The list of provinces is located at .\CoreComponents\Core\Addons\_Templates\CAProvince\CAProvinces.txt
        /// </remarks>
        public static async Task<List<SelectionItem<string>>> ReadProvincesListAsync() {
            if (_provincesList == null) {
                Package package = YetaWF.Core.AreaRegistration.CurrentPackage;// Core package
                string url = Package.GetAddOnTemplateUrl(package.AreaName, "CAProvince");
                string path = Utility.UrlToPhysical(url);
                string file = Path.Combine(path, "CAProvinces.txt");
                List<SelectionItem<string>> newList = new List<SelectionItem<string>>();
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file)) throw new InternalError("File {0} not found", file);

                List<string> sts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
                foreach (var st in sts) {
                    string[] s = st.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length != 2)
                        throw new InternalError("Invalid input in CA provinces list - {0}", file);
                    newList.Add(new SelectionItem<string> { Text = s[1], Value = s[0].ToUpper() });
                }
                _provincesList = newList;
            }
            return _provincesList;
        }
        private static List<SelectionItem<string>>? _provincesList = null;
    }
}
