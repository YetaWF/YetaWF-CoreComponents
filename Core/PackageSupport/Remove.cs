﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        public async Task<bool> RemoveAsync(string packageName, List<string> errorList) {

            bool status = true;

            Package package = Package.GetPackageFromPackageName(packageName);
            if (!await package.UninstallModelsAsync(errorList))
                return false;

            // now remove all files associated with this package

            // Addons for this product
            status = RemoveEmptyFoldersUp(AddonsFolder, errorList);

            // Assembly
            Uri file = new Uri(package.PackageAssembly.CodeBase);
            string asmFile = file.LocalPath;

            // Extra assemblies
            string[] extraAsms = Directory.GetFiles(Path.GetDirectoryName(asmFile), Path.GetFileNameWithoutExtension(asmFile) + ".*.dll");
            foreach (var extraAsm in extraAsms) {
                try {
                    File.Delete(extraAsm);
                } catch (Exception exc) {
                    if (!(exc is FileNotFoundException)) {
                        errorList.Add(__ResStr("cantRemoveExtraAsm", "Can't delete file {0}: {1}", extraAsm, exc.Message));
                        status = false;
                    }
                }
            }
            // finally delete the assembly
            try {
                File.Delete(asmFile);
            } catch (Exception exc) {
                if (!(exc is FileNotFoundException)) {
                    errorList.Add(__ResStr("cantRemoveMainAsm", "Can't delete file {0}: {1}", asmFile, exc.Message));
                    status = false;
                }
            }

            return status;
        }

        /// <summary>
        /// Remove folders starting at the specified folder. All contents (files/directories) are removed.
        /// Then remove the folder itself if it's empty and move up the hierarchy and keep deleting the folder we jsut visited if it's empty
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="errorList"></param>
        private bool RemoveEmptyFoldersUp(string folder, List<string> errorList) {
            // first delete all content from this folder
            try {
                Directory.Delete(folder, true);
            } catch (Exception exc) {
                if (!(exc is DirectoryNotFoundException)) {
                    errorList.Add(__ResStr("cantRemove", "Package addons folder {0} could not be deleted: {1}", folder, exc.Message));
                    return false;
                }
            }
            // now delete the folder itself if it's empty
            folder = Path.GetDirectoryName(folder);
            while (Directory.Exists(folder) && Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0) {
                try {
                    Directory.Delete(folder);
                } catch (Exception exc) {
                    if (!(exc is DirectoryNotFoundException)) {
                        errorList.Add(__ResStr("cantRemoveHierarchy", "Package addons folder {0} could not be deleted: {1}", folder, exc.Message));
                        return false;
                    }
                }
                folder = Path.GetDirectoryName(folder);
            }
            return true;
        }
    }
}
