/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        /// <summary>
        /// Removes a package, including data, files and the assembly implementing the package.
        /// </summary>
        /// <param name="packageName">The package name (e.g., YetaWF.Text).</param>
        /// <param name="errorList">A collection of messages.</param>
        /// <returns>Returns true if successful, false otherwise.</returns>
        /// <remarks>
        /// If assemblies are in use, they may not be removed by this method.
        ///
        /// The package is removed from the website. Source code implementing the assembly and any project references are not removed.
        /// </remarks>
        public async Task<bool> RemoveAsync(string packageName, List<string> errorList) {

            Package package = Package.GetPackageFromPackageName(packageName);
            if (!await package.UninstallModelsAsync(errorList))
                return false;

            // now remove all files associated with this package

            // Addons for this product
            bool status = await RemoveEmptyFoldersUpAsync(AddonsFolder, errorList);

            // Assembly
            Uri file = new Uri(package.PackageAssembly.Location);
            string asmFile = file.LocalPath;

            // Extra assemblies
            List<string> extraAsms = await FileSystem.FileSystemProvider.GetFilesAsync(Path.GetDirectoryName(asmFile)!, Path.GetFileNameWithoutExtension(asmFile) + ".*.dll");
            foreach (var extraAsm in extraAsms) {
                try {
                    await FileSystem.FileSystemProvider.DeleteFileAsync(extraAsm);
                } catch (Exception exc) {
                    if (!(exc is FileNotFoundException)) {
                        errorList.Add(__ResStr("cantRemoveExtraAsm", "Can't delete file {0}: {1}", extraAsm, ErrorHandling.FormatExceptionMessage(exc)));
                        status = false;
                    }
                }
            }
            // finally delete the assembly
            try {
                await FileSystem.FileSystemProvider.DeleteFileAsync(asmFile);
            } catch (Exception exc) {
                if (!(exc is FileNotFoundException)) {
                    errorList.Add(__ResStr("cantRemoveMainAsm", "Can't delete file {0}: {1}", asmFile, ErrorHandling.FormatExceptionMessage(exc)));
                    status = false;
                }
            }

            return status;
        }

        /// <summary>
        /// Removes folders starting at the specified folder. All contents (files/directories) are removed.
        /// Then removes the folder itself if it's empty and moves up the hierarchy and keeps deleting the folder just visited if it's empty
        /// </summary>
        /// <param name="folder">The folder to remove.</param>
        /// <param name="errorList">A collection of messages.</param>
        private async Task<bool> RemoveEmptyFoldersUpAsync(string folder, List<string> errorList) {
            // first delete all content from this folder
            try {
                await FileSystem.FileSystemProvider.DeleteDirectoryAsync(folder);
            } catch (Exception exc) {
                if (!(exc is DirectoryNotFoundException)) {
                    errorList.Add(__ResStr("cantRemove", "Package addons folder {0} could not be deleted: {1}", folder, ErrorHandling.FormatExceptionMessage(exc)));
                    return false;
                }
            }
            // now delete the folder itself if it's empty
            folder = Path.GetDirectoryName(folder) !;
            while (await FileSystem.FileSystemProvider.DirectoryExistsAsync(folder) && (await FileSystem.FileSystemProvider.GetFilesAsync(folder)).Count == 0 &&
                    (await FileSystem.FileSystemProvider.GetDirectoriesAsync(folder)).Count == 0) {
                try {
                    await FileSystem.FileSystemProvider.DeleteDirectoryAsync(folder);
                } catch (Exception exc) {
                    if (!(exc is DirectoryNotFoundException)) {
                        errorList.Add(__ResStr("cantRemoveHierarchy", "Package addons folder {0} could not be deleted: {1}", folder, ErrorHandling.FormatExceptionMessage(exc)));
                        return false;
                    }
                }
                folder = Path.GetDirectoryName(folder) !;
            }
            return true;
        }
    }
}
