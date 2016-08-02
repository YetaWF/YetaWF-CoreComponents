/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Runtime.CompilerServices;

namespace YetaWF.PackageAttributes
{
    public enum PackageTypeEnum {
        Unknown = 0,
        Module = 1,
        Skin = 2,
        Core = 3,
        CoreAssembly = 4,
        DataProvider = 5,
        Utility = 6,
        Template = 7,
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class PackageAttribute : Attribute {
        /// <summary>
        ///
        /// </summary>
        /// <param name="domain">The domain name (without www, http, .com or page), eg. softelvdm</param>
        /// <param name="type"></param>
        /// <param name="sourceFile"></param>
        public PackageAttribute(PackageTypeEnum type, string domain, [CallerFilePath] string sourceFile = null) {
            PackageType = type;
            Domain = domain;
            SourceFile = sourceFile;
        }
        public PackageTypeEnum PackageType { get; private set; }
        public string Domain { get; private set; }
        public string SourceFile { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class SkinAttribute : Attribute {
        /// <summary>
        ///
        /// </summary>
        /// <param name="domain">The domain name (without www, http, .com or page), eg. softelvdm</param>
        public SkinAttribute(string domain) {
            Domain = domain;
        }
        public string Domain { get; private set; }
    }
    [AttributeUsage(AttributeTargets.Assembly)]
    public class PackageInfoAttribute : Attribute {
        public PackageInfoAttribute() { }
        public PackageInfoAttribute(string updateServerLink, string infoLink, string supportLink, string releaseNoticeLink, string licenseLink) {
            InfoLink = infoLink;
            UpdateServerLink = updateServerLink;
            SupportLink = supportLink;
            ReleaseNoticeLink = releaseNoticeLink;
            LicenseLink = licenseLink;
        }
        public string InfoLink { get; private set; }
        public string UpdateServerLink { get; private set; }
        public string SupportLink { get; private set; }
        public string ReleaseNoticeLink { get; private set; }
        public string LicenseLink { get; private set; }
    }
}
