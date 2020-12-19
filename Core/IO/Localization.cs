/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;

namespace YetaWF.Core.IO {

    /// <summary>
    /// This class provides access to the localization data provider.
    /// The localization data provider sets all access methods (like Load, SaveAsync, etc.) during application startup.
    ///
    /// These methods should never be called by applications. They are intended for framework use only and some specialized modules that edit or display localization resources.
    /// </summary>
    /// <remarks>
    /// All properties must be provided by a localization data provider during application startup.
    /// The properties in this class provide access to the localization data provider. A localization data provider must be accessed through members of this class ONLY.
    ///
    /// The localization data provider is truly plug and play. The default package YetaWF.DataProvider.Localization implements management of the localization files in the .\Localization and .\LocalizationCustom folders.
    /// By replacing the package with another that provides equivalent functionality through the Load, SaveAsync, etc. properties, a different repository could be used.
    /// </remarks>
    public static class Localization {

        public enum Location {
            /// <summary>
            /// Localization resources which are for the default YetaWF language (US-English).
            /// These localization resources are embedded in the source code.
            /// </summary>
            DefaultResources = 0,   // the resources generated from source code
            /// <summary>
            /// Installed package localization resources. There can be multiple different localization resources for one package in various languages.
            /// </summary>
            InstalledResources = 1, // resources installed with package (language specific)
            /// <summary>
            /// Custom package localization resources. There can be multiple different custom localization resources for one package in various languages.
            /// </summary>
            CustomResources = 2,    // site-specific custom resources (modified from InstalledResources or DefaultResources)
            /// <summary>
            /// Used when loading localization resources to merge all resources into one, usually cached localization resource.
            /// The expected precedence of localization resources during merging is custom first, then installed, followed by default.
            /// </summary>
            Merge = 3
        }

        /// <summary>
        /// A method that loads package specific localization resources (Package) for a specific Type (string) from the specified location (Location) and
        /// returns a YetaWF.Core.Localize.LocalizationData object.
        /// </summary>
        public static Func<Package, string, Location, LocalizationData?> Load { get; set; }
        /// <summary>
        /// A method that saves package (Package) specific localization resources (YetaWF.Core.Localize.LocalizationData) for a specific Type (string) to the specified location (Location).
        /// </summary>
        public static Func<Package, string, Location, LocalizationData?, Task> SaveAsync { get; set; }
        /// <summary>
        /// Remove/clear all package (Package) specific localization resources for the specified language (string).
        /// </summary>
        public static Func<Package, string, Task> ClearPackageDataAsync { get; set; }
        /// <summary>
        /// Retrieves the files for package (Package) specific localization resources (YetaWF.Core.Localize.LocalizationData) for a specific Type (string). Specify (bool) true to get real files names, false for file names without extension.
        /// </summary>
        public static Func<Package, string, bool, Task<List<string>>> GetFilesAsync { get; set; }

        /// <summary>
        /// Static constructor. Installs default accessors which when called indicate that there is no installed localization data provider.
        /// </summary>
        static Localization() {
            Load = DefaultLoad;
            SaveAsync = DefaultSaveAsync;
            ClearPackageDataAsync = DefaultClearPackageDataAsync;
            GetFilesAsync = DefaultGetFilesAsync;
        }
        private static LocalizationData? DefaultLoad(Package package, string type, Location location) {
            if (!LocalizationSupport.UseLocalizationResources) return null;
            throw new NotImplementedException();
        }
        private static Task DefaultSaveAsync(Package package, string type, Location location, LocalizationData? data) {
            throw new NotImplementedException();
        }
        private static Task DefaultClearPackageDataAsync(Package package, string language) {
            throw new NotImplementedException();
        }
        private static Task<List<string>> DefaultGetFilesAsync(Package package, string language, bool rawName) {
            throw new NotImplementedException();
        }
    }
}
