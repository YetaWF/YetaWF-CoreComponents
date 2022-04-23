/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Runtime.CompilerServices;

namespace YetaWF.PackageAttributes {
    /// <summary>
    /// Used with the PackageAttribute assembly attribute to define a package's purpose.
    /// </summary>
    public enum PackageTypeEnum {
        /// <summary>
        /// Used internally.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The package is a module package containing modules.
        /// </summary>
        Module = 1,
        /// <summary>
        /// The package is a skin package containing one or more skin definitions.
        /// </summary>
        Skin = 2,
        /// <summary>
        /// The package is a core package. Used by YetaWF.
        /// </summary>
        Core = 3,
        /// <summary>
        /// The package is a core assembly package. Used by YetaWF.
        /// </summary>
        CoreAssembly = 4,
        /// <summary>
        /// The package is a data provider package. Used by YetaWF.
        /// </summary>
        DataProvider = 5,
        /// <summary>
        /// The package is a utility package. Used by YetaWF.
        /// </summary>
        Utility = 6,
        /// <summary>
        /// The package is a Visual Studio template package. Used by YetaWF.
        /// </summary>
        Template = 7,
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class PackageAttribute : Attribute {
        /// <summary>
        /// Assembly attribute used to define the basic purpose of the package.
        /// </summary>
        /// <param name="domain">The domain name (without www, http, .com or page), eg. softelvdm</param>
        /// <param name="type">The package type.</param>
        /// <param name="sourceFile">This should not be used as it is used internally by YetaWF to determine whether a package is a source code or binary package.</param>
        /// <param name="LanguageDomain">The domain name for localization resources (without www, http, .com or page), eg. softelvdm. If not provided, the <paramref name="domain"/> parameter is used instead.</param>
        /// <remarks>Every YetaWF package must provide a PackageAttribute attribute to define its basic purpose.</remarks>
        public PackageAttribute(PackageTypeEnum type, string domain, string? LanguageDomain = null, [CallerFilePath] string? sourceFile = null) {
            PackageType = type;
            Domain = domain;
            SourceFile = sourceFile;
            if (string.IsNullOrWhiteSpace(LanguageDomain))
                this.LanguageDomain = domain;
            else
                this.LanguageDomain = LanguageDomain;
        }
        public PackageTypeEnum PackageType { get; private set; }
        public string Domain { get; private set; }
        public string LanguageDomain { get; private set; }
        public string? SourceFile { get; private set; }
    }

    /// <summary>
    /// Assembly attribute used to provide various public Urls for a package.
    /// </summary>
    /// <remarks>Every YetaWF package should provide a PackageInfoAttribute attribute to define public Urls.</remarks>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class PackageInfoAttribute : Attribute {
        public PackageInfoAttribute(string updateServerLink, string infoLink, string supportLink, string releaseNoticeLink, string licenseLink, string storeLink) {
            InfoLink = infoLink;
            UpdateServerLink = updateServerLink;
            SupportLink = supportLink;
            ReleaseNoticeLink = releaseNoticeLink;
            LicenseLink = licenseLink;
            StoreLink = storeLink;
        }
        public PackageInfoAttribute(string updateServerLink, string infoLink, string supportLink, string releaseNoticeLink, string licenseLink) {
            InfoLink = infoLink;
            UpdateServerLink = updateServerLink;
            SupportLink = supportLink;
            ReleaseNoticeLink = releaseNoticeLink;
            LicenseLink = licenseLink;
        }
        /// <summary>
        /// Package information Url.
        /// </summary>
        /// <remarks>This can be displayed as a help link by modules using ModuleDefinition.ShowHelp.</remarks>
        public string InfoLink { get; private set; }
        /// <summary>
        /// Not used. Will be removed or renamed.
        /// </summary>
        public string UpdateServerLink { get; private set; }
        /// <summary>
        /// Support information Url.
        /// </summary>
        public string SupportLink { get; private set; }
        /// <summary>
        /// Release notice Url.
        /// </summary>
        /// <remarks>Not used by YetaWF. Can be used by third-party packages.</remarks>
        public string ReleaseNoticeLink { get; private set; }
        /// <summary>
        /// License information Url.
        /// </summary>
        /// <remarks>Not used by YetaWF. Can be used by third-party packages.</remarks>
        public string LicenseLink { get; private set; }
        /// <summary>
        /// Store Url for purchasable packages.
        /// </summary>
        /// <remarks>Not used by YetaWF. Can be used by third-party packages.</remarks>
        public string? StoreLink { get; private set; }
    }
}
