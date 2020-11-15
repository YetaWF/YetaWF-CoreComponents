/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Scheduler;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {

    public partial class Package {

        /* private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); } */

        public async Task<bool> InstallModelsAsync(List<string> errorList, string? lastSeenVersion = null) {

            bool success = true;

            // Install all dataproviders
            List<Type> models = InstallableModels;

            List<Type> ordered = GetInstallOrder();
            while (ordered.Count > 0) {
                Type type = ordered.First();
                if (!await InstallOneTypeAsync(errorList, type))
                    success = false;
                models.Remove(type);
                ordered.RemoveAt(0);
            }
            foreach (Type type in models) {
                if (!await InstallOneTypeAsync(errorList, type))
                    success = false;
            }

            // perform data upgrade
            if (!string.IsNullOrWhiteSpace(lastSeenVersion)) {
                models = InstallableModels;
                foreach (Type type in models) {
                    if (!await UpgradeOneTypeAsync(errorList, type, lastSeenVersion))
                        success = false;
                }
            }

            // Install all scheduler items
            try {
                await SchedulerSupport.InstallAsync(this);
            } catch (Exception exc) {
                errorList.Add(ErrorHandling.FormatExceptionMessage(exc));
            }
            return success;
        }

        private static async Task<bool> UpgradeOneTypeAsync(List<string> errorList, Type type, string lastSeenVersion) {
            bool success = true;
            object instMod = Activator.CreateInstance(type) ! ;
            using ((IDisposable)instMod) {
                if (instMod as IInstallableModel2 != null) {
                    IInstallableModel2 model = (IInstallableModel2)instMod;
                    List<string> list = new List<string>();
                    if (!await model.UpgradeModelAsync(list, lastSeenVersion))
                        success = false;
                    errorList.AddRange(list);
                }
                return success;
            }
        }

        private static async Task<bool> InstallOneTypeAsync(List<string> errorList, Type type) {
            bool success = true;
            object instMod = Activator.CreateInstance(type) ! ;
            using ((IDisposable)instMod) {
                IInstallableModel model = (IInstallableModel)instMod;
                List<string> list = new List<string>();
                if (!await model.InstallModelAsync(list))
                    success = false;
                errorList.AddRange(list);
                return success;
            }
        }

        public async Task<bool> UninstallModelsAsync(List<string> errorList) {

            bool success = true;

            // Uninstall all dataproviders
            List<Type> models = InstallableModels;
            foreach (Type type in models) {
                object instMod = Activator.CreateInstance(type) ! ;
                using ((IDisposable)instMod) {
                    IInstallableModel model = (IInstallableModel)instMod;
                    List<string> list = new List<string>();
                    if (!await model.UninstallModelAsync(list))
                        success = false;
                    errorList.AddRange(list);
                }
            }
            // Uninstall all scheduler items
            try {
                await SchedulerSupport.UninstallAsync(this);
            } catch (Exception exc) {
                errorList.Add(ErrorHandling.FormatExceptionMessage(exc));
            }
            return success;
        }

        public static async Task AddSiteDataAsync() {
            // Add site specific data for the current (usually new) site
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                List<Type> models = package.InstallableModels;
                foreach (Type type in models) {
                    object instMod = Activator.CreateInstance(type) ! ;
                    using ((IDisposable)instMod) {
                        IInstallableModel model = (IInstallableModel)instMod;
                        await model.AddSiteDataAsync();
                    }
                }
            }
        }
        public static async Task RemoveSiteDataAsync(string siteFolder) {
            // remove site specific data for the current site
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                List<Type> models = package.InstallableModels;
                foreach (Type type in models) {
                    object instMod = Activator.CreateInstance(type) ! ;
                    using ((IDisposable)instMod) {
                        IInstallableModel model = (IInstallableModel)instMod;
                        await model.RemoveSiteDataAsync();
                    }
                }
            }
            // remove the site's data folder
            await FileSystem.FileSystemProvider.DeleteDirectoryAsync(siteFolder);
        }
    }
}
