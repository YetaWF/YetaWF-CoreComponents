/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.IO;
using YetaWF.Core.Scheduler;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        public bool InstallModels(List<string> errorList) {

            bool success = true;

            // Install all dataproviders
            List<Type> models = InstallableModels;

            List<Type> ordered = GetInstallOrder();
            while (ordered.Count > 0) {
                Type type = ordered.First();
                if (!InstallOneType(errorList, type))
                    success = false;
                models.Remove(type);
                ordered.RemoveAt(0);
            }
            foreach (Type type in models) {
                if (!InstallOneType(errorList, type))
                    success = false;
            }

            // Install all scheduler items
            try {
                SchedulerSupport.Install(this);
            } catch (Exception exc) {
                errorList.Add(exc.Message);
            }
            return success;
        }

        private static bool InstallOneType(List<string> errorList, Type type) {
            bool success = true;
            object instMod = Activator.CreateInstance(type);
            using ((IDisposable)instMod) {
                IInstallableModel model = (IInstallableModel)instMod;
                List<string> list = new List<string>();
                if (!model.InstallModel(list))
                    success = false;
                errorList.AddRange(list);
                return success;
            }
        }

        public bool UninstallModels(List<string> errorList) {

            bool success = true;

            // Uninstall all dataproviders
            List<Type> models = InstallableModels;
            foreach (Type type in models) {
                object instMod = Activator.CreateInstance(type);
                using ((IDisposable)instMod) {
                    IInstallableModel model = (IInstallableModel)instMod;
                    List<string> list = new List<string>();
                    if (!model.UninstallModel(list))
                        success = false;
                    errorList.AddRange(list);
                }
            }
            // Uninstall all scheduler items
            try {
                SchedulerSupport.Uninstall(this);
            } catch (Exception exc) {
                errorList.Add(exc.Message);
            }
            return success;
        }

        public static void AddSiteData() {
            // Add site specific data for the current (usually new) site
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                List<Type> models = package.InstallableModels;
                foreach (Type type in models) {
                    object instMod = Activator.CreateInstance(type);
                    using ((IDisposable)instMod) {
                        IInstallableModel model = (IInstallableModel)instMod;
                        model.AddSiteData();
                    }
                }
            }
        }
        public static void RemoveSiteData(string siteFolder) {
            // remove site specific data for the current site
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                List<Type> models = package.InstallableModels;
                foreach (Type type in models) {
                    object instMod = Activator.CreateInstance(type);
                    using ((IDisposable)instMod) {
                        IInstallableModel model = (IInstallableModel)instMod;
                        model.RemoveSiteData();
                    }
                }
            }
            // remove the site's data folder
            try {
                Directory.Delete(siteFolder, true);
            } catch (Exception) { }
        }
    }
}
