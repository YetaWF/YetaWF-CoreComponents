/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    public static class USState {

        public static async Task<List<SelectionItem<string>>> ReadStatesListAsync() {
            if (_statesList == null) {
                Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;// Core package
                string url = VersionManager.GetAddOnTemplateUrl(package.Domain, package.Product, "USState");
                string path = YetaWFManager.UrlToPhysical(url);
                string file = Path.Combine(path, "USStates.txt");
                List<SelectionItem<string>> newList = new List<SelectionItem<string>>();
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file)) throw new InternalError("File {0} not found", file);

                List<string> sts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
                foreach (var st in sts) {
                    string[] s = st.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length != 2)
                        throw new InternalError("Invalid input in US states list - {0}", file);
                    newList.Add(new SelectionItem<string> { Text = s[1], Value = s[0].ToUpper() });
                }
                _statesList = newList;
            }
            return _statesList;
        }
        private static List<SelectionItem<string>> _statesList = null;
    }
}
