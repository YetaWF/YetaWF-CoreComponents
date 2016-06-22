/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web.Compilation;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Identity;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using YetaWF.PackageAttributes;

namespace YetaWF.Core.Packages {

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresPackageAttribute : Attribute {
        /// <summary>
        /// Defines that the current package requires another package.
        /// </summary>
        /// <remarks>
        /// This is used in a package's AssemblyInfo.cs file.
        /// </remarks>
        /// <param name="packageName">The name of the required package name. Must follow the naming standard: domain.product eg. YetaWF.Scheduler</param>
        /// <param name="minVersion">Optional. The required minimum version (n.n.n).</param>
        /// <param name="maxVersion">Optional. The required maximum version (n.n.n).</param>
        public RequiresPackageAttribute(string packageName, string minVersion = "", string maxVersion = "") {
            PackageName = packageName;
            MinVersion = minVersion;
            MaxVersion = maxVersion;
            if (!string.IsNullOrWhiteSpace(minVersion) && !string.IsNullOrWhiteSpace(maxVersion)) {
                if (Package.CompareVersion(minVersion, maxVersion) > 0) throw new InternalError("The specified minimum version {0} is larger than the minimum version {1}", minVersion, maxVersion);
            }
        }
        public string PackageName { get; private set; }
        public string MinVersion { get; private set; }
        public string MaxVersion { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresAddOnAttribute : Attribute {
        /// <summary>
        /// Defines that the current packages requires a non-global addon (TODO: REFERENCE).
        /// </summary>
        /// <param name="domain">The domain name of the addon's origin, e.g., softelvdm. (TODO: REFERENCE)</param>
        /// <param name="product">The product name of the addon, e.g., Core. (TODO: REFERENCE)</param>
        /// <param name="name">The name for the addon. (TODO: REFERENCE)</param>
        /// <param name="minVersion">Optional. The required minimum version (n.n.n).</param>
        /// <param name="version"></param>
        /// <param name="maxVersion">Optional. The required maximum version (n.n.n).</param>
        public RequiresAddOnAttribute(string domain, string product, string name, string version = "", string minVersion = "", string maxVersion = "") {
            Domain = domain;
            Product = product;
            Name = name;
            Version = version;
            if (string.IsNullOrWhiteSpace(version)) {
                if (!string.IsNullOrWhiteSpace(minVersion) || !string.IsNullOrWhiteSpace(minVersion))
                    throw new InternalError("If the version is omitted, minimum version and maximum version cannot be specified", minVersion, maxVersion);
            } else {
                MinVersion = string.IsNullOrWhiteSpace(minVersion) ? version : minVersion;
                MaxVersion = maxVersion;
                if (!string.IsNullOrWhiteSpace(minVersion) && !string.IsNullOrWhiteSpace(maxVersion)) {
                    if (Package.CompareVersion(minVersion, maxVersion) > 0) throw new InternalError("The specified minimum version {0} is greater than the maximum version {1}", minVersion, maxVersion);
                }
                if (!string.IsNullOrWhiteSpace(minVersion)) {
                    if (Package.CompareVersion(version, minVersion) < 0) throw new InternalError("The specified version {0} is smaller than the minimum version {1}", version, minVersion);
                }
                if (!string.IsNullOrWhiteSpace(maxVersion)) {
                    if (Package.CompareVersion(version, maxVersion) > 0) throw new InternalError("The specified version {0} is greater than the maximum version {1}", version, maxVersion);
                }
            }
        }
        public string Domain { get; private set; }
        public string Product { get; private set; }
        public string Version { get; private set; }
        public string Name { get; private set; }
        public string MinVersion { get; private set; }
        public string MaxVersion { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RequiresAddOnGlobalAttribute : Attribute {
        /// <summary>
        /// Defines that the current packages requires a non-global addon (TODO: REFERENCE).
        /// </summary>
        /// <param name="domain">The domain name of the addon's origin, e.g., softelvdm. (TODO: REFERENCE)</param>
        /// <param name="product">The product name of the addon, e.g., Core. (TODO: REFERENCE)</param>
        /// <param name="minVersion">Optional. The required minimum version (n.n.n).</param>
        /// <param name="maxVersion">Optional. The required maximum version (n.n.n).</param>
        public RequiresAddOnGlobalAttribute(string domain, string product, string minVersion = "", string maxVersion = "") {
            Domain = domain;
            Product = product;
            MinVersion = minVersion;
            MaxVersion = maxVersion;
            if (!string.IsNullOrWhiteSpace(MinVersion) && !string.IsNullOrWhiteSpace(MaxVersion)) {
                if (Package.CompareVersion(MinVersion, MaxVersion) < 0) throw new InternalError("The specified minimum version {0} is greater than the maximum version {1}", minVersion, maxVersion);
            }
        }
        public string Domain { get; private set; }
        public string Product { get; private set; }
        public string MinVersion { get; private set; }
        public string MaxVersion { get; private set; }
    }

    /// <summary>
    /// Attribute class used in a package's AssemblyInfo.cs file to specify an installation order for its types (classes) implementing IInstallableModel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class InstallOrderAttribute : Attribute {
        /// <summary>
        /// Order in which to install classes (IInstallableModel) within this package.
        /// </summary>
        /// <param name="type">The type that should be installed.</param>
        /// <remarks>
        /// All types listed are installed first, in the order of their InstallOrderAttributes.
        /// If a type is omitted it is installed last. If multiple types are omitted, their initialization order is undefined, but follows any types that are listed.
        /// This is used in a package's AssemblyInfo.cs file.
        /// </remarks>
        /// <param name="order"></param>
        public InstallOrderAttribute(Type type, [CallerLineNumber]int order = 0) {
            OrderType = type;
            Order = order;
        }
        public Type OrderType { get; set; }
        public int Order { get; set; }
    }

    /// <summary>
    /// Service level of a package used to define its main purpose and is used to determine installation order (lowest value installed first).
    /// </summary>
    public enum ServiceLevelEnum {
        Unknown = -1,
        Core = 0,
        LowLevelServiceProvider = 100,
        ServiceProvider = 200,
        ServiceConsumer = 300,
        Module = 1000,
        ModuleConsumer = 2000,
    }
    /// <summary>
    /// Attribute class used in a package's AssemblyInfo.cs file to define its purpose and at which point the package is installed, relative to other packages.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class ServiceLevelAttribute : Attribute {
        /// <summary>
        /// Order in which to install packages.
        /// </summary>
        /// <remarks>
        /// This is used in a package's AssemblyInfo.cs file and defines its purpose and at which point the package is installed, relative to other packages.
        /// </remarks>
        public ServiceLevelAttribute(ServiceLevelEnum level = ServiceLevelEnum.Module) {
            Level = level;
        }
        public ServiceLevelEnum Level { get; private set; }
    }

    /// <summary>
    /// Attribute class used in a package's AssemblyInfo.cs file to define whether the package exposes public partial views (display/editor templates)
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class PublicPartialViewsAttribute : Attribute {
        /// <summary>
        /// Defines whether the package exposes public partial views (display/editor templates).
        /// </summary>
        public PublicPartialViewsAttribute() { }
    }

    /// <summary>
    /// Package class, used to describe a YetaWF package, containing modules and skins.
    /// </summary>
    public partial class Package {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        private const string SOURCE_IDENTIFIER = "Properties\\AssemblyInfo.cs";

        private Package(Assembly assembly) {
            PackageAssembly = assembly;
        }

        public Assembly PackageAssembly { get; private set; }

        /// <summary>
        /// Returns all packages referenced by this YetaWF instance, i.e., the website (this excludes templates, utilities)
        /// </summary>
        public static List<Package> GetAvailablePackages(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, out int total) {
            List<Package> packages = (from p in GetAvailablePackages() select p).ToList();// copy
            //packages.AddRange(GetTemplatePackages());
            //packages.AddRange(GetUtilityPackages());
            return DataProviderImpl<Package>.GetRecords(packages, skip, take, sort, filters, out total);
        }
        /// <summary>
        /// Returns all packages referenced by this YetaWF instance, i.e., the website (this excludes templates, utilities)
        /// </summary>
        public static List<Package> GetAvailablePackages() {
            if (_availablePackages == null) {
                ICollection assemblies = null;
                if (YetaWFManager.Manager.HostUsed != YetaWFManager.BATCHMODE) {
                    // this will only work in ASP.NET apps
                    assemblies = BuildManager.GetReferencedAssemblies();
                } else {
                    // Get all currently loaded assemblies - make sure this is outside asp.net app
                    assemblies = AppDomain.CurrentDomain.GetAssemblies();
                }
                if (assemblies == null)
                    throw new InternalError("Unable to obtain referenced assemblies");

                _availablePackages = new List<Package>();
                foreach (Assembly assembly in assemblies) {
                    Package package = new Package(assembly);
                    if (package.IsValid)
                        _availablePackages.Add(package);
                }
            }
            return (from p in _availablePackages select p).ToList();// copy
        }
        // We statically hold a reference to ALL available, referenced packages- this is necessary for package information caching (notably localization)
        private static List<Package> _availablePackages = null;

        //public static List<Package> GetTemplatePackages() {
        //    string sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Templates", "Templates");
        //    List<Package> packages = FindPackages(Path.Combine(YetaWFManager.RootFolder, "..", sourceFolder), csAssemblyTemplateRegex);
        //    return packages;
        //}
        //public static List<Package> GetUtilityPackages() {
        //    string sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Utilities", "Utilities");
        //    List<Package> packages = FindPackages(Path.Combine(YetaWFManager.RootFolder, "..", sourceFolder), csAssemblyUtilityRegex);
        //    return packages;
        //}

        //private static readonly Regex csAssemblyTemplateRegex = new Regex(@"\[\s*assembly\s*\:\s*Package\s*\(\s*PackageTypeEnum\s*\.\s*Template\s*,\s*\""(?'asm'[A-Za-z0-9_\.]+)""\s*\)\s*\]", RegexOptions.Compiled | RegexOptions.Multiline);
        //private static readonly Regex csAssemblyUtilityRegex = new Regex(@"\[\s*assembly\s*\:\s*Package\s*\(\s*PackageTypeEnum\s*\.\s*Utility\s*,\s*\""(?'asm'[A-Za-z0-9_\.]+)""\s*\)\s*\]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static List<Package> FindPackages(string packageRoot, Regex regex) {
            List<Package> packages = new List<Package>();
            if (!Directory.Exists(packageRoot)) return packages;
            string[] packageDirs = Directory.GetDirectories(packageRoot);
            foreach (string dir in packageDirs) {
                string propsFile = Path.Combine(dir, SOURCE_IDENTIFIER);
                if (File.Exists(propsFile)) {
                    string target = ExtractAssemblyName(propsFile, regex);
                    if (string.IsNullOrWhiteSpace(target))
                        throw new InternalError("Folder {0} does not define an assembly name in {1}", dir, SOURCE_IDENTIFIER);
                    string asmPath = Path.Combine(dir, "Bin", target);
                    if (!File.Exists(asmPath))
                        throw new InternalError("Package assembly {0} not found", asmPath);
                    Assembly asm = null;
                    try {
                        asm = Assembly.LoadFile(asmPath);
                    } catch (Exception) { asm = null; }
                    if (asm != null)
                        packages.Add(new Package(asm));
                }
            }
            return packages;
        }
        private static string ExtractAssemblyName(string propsFile, Regex regex) {
            string contents = File.ReadAllText(propsFile);
            Match m = regex.Match(contents);
            if (!m.Success)
                throw new InternalError("No assembly name found in {0}", propsFile);
            return m.Groups["asm"].Value;
        }

        /// <summary>
        /// Given an assembly, return the Package object.
        /// </summary>
        /// <param name="assembly">An assembly for which the Package object is to be returned.</param>
        /// <returns>The Package object, or null if it does not exist.</returns>
        public static Package TryGetPackageFromAssembly(Assembly assembly) {
            Package package = (from p in GetAvailablePackages() where p.PackageAssembly.FullName == assembly.FullName select p).FirstOrDefault();
            if (package == null) {
                // special handling for assemblies that are not in referenced assemblies but are still accessible (like PackageAttributes)
                package = new Package(assembly);
                if (!package.IsValid)
                    return null;
                _availablePackages.Add(package);
            }
            return package;
        }
        /// <summary>
        /// Given an assembly, return the Package object.
        /// </summary>
        /// <param name="assembly">An assembly for which the Package object is to be returned.</param>
        /// <returns>The Package object.</returns>
        public static Package GetPackageFromAssembly(Assembly assembly) {
            Package package = TryGetPackageFromAssembly(assembly);
            if (package == null) throw new InternalError("Package assembly {0} not found", assembly.FullName);
            return package;
        }
        /// <summary>
        /// Given a type, return the Package object.
        /// </summary>
        /// <param name="type">The type for which the implementing Package object is to be returned.</param>
        /// <returns>The Package object, or null if it does not exist.</returns>
        public static Package TryGetPackageFromType(Type type) {
            return TryGetPackageFromAssembly(type.Assembly);
        }
        /// <summary>
        /// Given a type, return the Package object.
        /// </summary>
        /// <param name="type">The type for which the implementing Package object is to be returned.</param>
        /// <returns>The Package object.</returns>
        public static Package GetPackageFromType(Type type) {
            return GetPackageFromAssembly(type.Assembly);
        }
        /// <summary>
        /// Given an object, return the Package object.
        /// </summary>
        /// <param name="obj">The object for which the implementing Package object is to be returned.</param>
        /// <returns>The Package object.</returns>
        public static Package GetCurrentPackage(object obj) {
            return GetPackageFromAssembly(obj.GetType().Assembly);
        }

        /// <summary>
        /// Given a package name, return the Package object.
        /// </summary>
        /// <param name="shortName">The package name for which the Package object is to be returned.</param>
        /// <returns>The Package object.</returns>
        public static Package GetPackageFromPackageName(string shortName /*, bool Utilities = false, bool Templates = false*/) {
            Package package = (from p in GetAvailablePackages() where p.Name == shortName select p).FirstOrDefault();
            if (package != null) return package;
            //if (Templates) {
            //    package = (from p in GetTemplatePackages() where p.Name == shortName select p).FirstOrDefault();
            //    if (package != null) return package;
            //}
            //if (Utilities) {
            //    package = (from p in GetUtilityPackages() where p.Name == shortName select p).FirstOrDefault();
            //    if (package != null) return package;
            //}
            throw new InternalError("Package assembly {0} not found", shortName);
        }

        /// <summary>
        /// The package name.
        /// </summary>
        public string Name {
            get {
                return PackageAssembly.FullName.Split(new char[] {','},2)[0];
            }
        }

        /// <summary>
        /// Returns whether the package object is valid.
        /// </summary>
        /// <remarks>
        /// If an package object is created using the constructor Package(assembly) and the assembly is not a YetaWF package, this returns false.
        /// </remarks>
        public bool IsValid {
            get {
                return IsModulePackage || IsCorePackage || IsCoreAssemblyPackage || IsSkinPackage || IsDataProviderPackage /*|| IsTemplatePackage || IsUtilityPackage*/;
            }
        }
        /// <summary>
        /// Returns whether the package is a module package.
        /// </summary>
        public bool IsModulePackage {
            get {
                return PackageType == PackageTypeEnum.Module;
            }
        }
        /// <summary>
        /// Returns whether the package is a core package.
        /// </summary>
        public bool IsCorePackage {
            get {
                return PackageType == PackageTypeEnum.Core;
            }
        }
        /// <summary>
        /// Returns whether the package is a core assembly package.
        /// </summary>
        public bool IsCoreAssemblyPackage {
            get {
                return PackageType == PackageTypeEnum.CoreAssembly;
            }
        }
        /// <summary>
        /// Returns whether the package is a data provider package.
        /// </summary>
        public bool IsDataProviderPackage {
            get {
                return PackageType == PackageTypeEnum.DataProvider;
            }
        }
        /// <summary>
        /// Returns whether the package is a skin package.
        /// </summary>
        public bool IsSkinPackage {
            get {
                return PackageType == PackageTypeEnum.Skin;
            }
        }
        //public bool IsTemplatePackage {
        //    get {
        //        return PackageType == PackageTypeEnum.Template;
        //    }
        //}
        //public bool IsUtilityPackage {
        //    get {
        //        return PackageType == PackageTypeEnum.Utility;
        //    }
        //}
        /// <summary>
        /// Returns the package type.
        /// </summary>
        public PackageTypeEnum PackageType {
            get {
                if (_packageType == null) {
                    _packageType = PackageTypeEnum.Unknown;
                    PackageAttribute attr = (PackageAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageAttribute));
                    if (attr != null)
                        _packageType = attr.PackageType;
                }
                return (PackageTypeEnum)_packageType;
            }
        }
        private PackageTypeEnum? _packageType;

        public bool HasPublicPartialViews {
            get {
                PublicPartialViewsAttribute attr = (PublicPartialViewsAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PublicPartialViewsAttribute));
                return attr != null;
            }
        }

        /// <summary>
        /// Returns the source file path of the package's AssemblyInfo.cs file. This is used to determine the location of a source package on a development system.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string SourceFile {
            get {
                if (_sourceFile == null) {
                    PackageAttribute attr = (PackageAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageAttribute));
                    if (attr == null)
                        throw new InternalError("Package {0} has no source file location", Name);
                    _sourceFile = attr.SourceFile;
                }
                return _sourceFile;
            }
        }
        private string _sourceFile;

        /// <summary>
        /// Returns the root path of the package's source files. This is used to determine the location of a source package on a development system.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string PackageSourceRoot {
            get {
                const string sourceEnd = SOURCE_IDENTIFIER; // source file always ends in this string
                string path = SourceFile;
                if (!path.EndsWith(sourceEnd, StringComparison.OrdinalIgnoreCase)) throw new InternalError("Package error - source file name {0} doesn't end in {1}", SourceFile, sourceEnd);
                return path.Substring(0, path.Length - sourceEnd.Length);
            }
        }
        /// <summary>
        /// Returns whether the current package is a source code package (includes source code). Source code package are only found on development systems.
        /// </summary>
        public bool HasSource {
            get {
                return Directory.Exists(PackageSourceRoot);
            }
        }

        /// <summary>
        /// The domain name of the company owning this package. Periods (".") are not allowed. Top-level domains are only added if your company doesn't own the .com domain.
        /// </summary>
        /// <remarks>
        /// The Domain name specified cannot include a period (".").
        /// For example, if your company owns the domain "Softelvdm.com", use "Softelvdm" as domain name (no top-level domain .com as that is assumed).
        /// If your comapny owns "MyComany.com", use "MyCompany". If your company owns "MyCompany.net" but NOT "MyCompany.com", you must include the top-level domain as part yof your domain (but WITHOUT periods), as in "MyCompanyNet".
        /// If your comapny owns "SomeCompany.co.uk" use "SomeCompanyCoUk").
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string Domain {
            get {
                if (_domain == null) {
                    PackageAttribute attr = (PackageAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageAttribute));
                    if (attr == null)
                        throw new Error(__ResStr("noDomain", "Incorrectly packaged module - no domain available - Package {0}", Name));
                    _domain = attr.Domain;
                }
                return _domain;
            }
        }
        private string _domain;

        /// <summary>
        /// Your comany name, in displayable form.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string CompanyDisplayName {
            get {
                if (_companyDisplayName == null) {
                    AssemblyCompanyAttribute attr = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(AssemblyCompanyAttribute));
                    if (attr == null)
                        throw new Error(__ResStr("noCompany", "Incorrectly packaged module - no company name available - Package {0}", Name));
                    _companyDisplayName = attr.Company;
                }
                return _companyDisplayName;
            }
        }
        private string _companyDisplayName;

        /// <summary>
        /// The description for the current package.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string Description {
            get {
                if (_description == null) {
                    AssemblyDescriptionAttribute attr = (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(AssemblyDescriptionAttribute));
                    if (attr == null)
                        throw new Error(__ResStr("noDesc", "Incorrectly packaged module - no description available - Package {0}", Name));
                    _description = attr.Description;
                }
                return _description;
            }
        }
        private string _description;

        /// <summary>
        /// The MVC Area name used by the current domain.
        /// </summary>
        public string AreaName {
            get {
                //if (IsUtilityPackage)
                //    return "YetaWF Utility " + Domain;
                //else
                    return Domain + "_" + Product;
            }
        }
        /// <summary>
        /// The product name.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string Product {
            get {
                if (_product == null) {
                    AssemblyProductAttribute attr = (AssemblyProductAttribute) Attribute.GetCustomAttribute(PackageAssembly, typeof(AssemblyProductAttribute));
                    if (attr == null)
                        throw new Error(__ResStr("noName", "Incorrectly packaged module - no product name available - Package {0}", Name));
                    _product = attr.Product;
                }
                return _product;
            }
        }
        private string _product;

        /// <summary>
        /// The product version.
        /// </summary>
        /// <remarks>Versions have 3 components, major, minor, build: m.n.r - Revision is not used by YetaWF versions.</remarks>
        public string Version {
            get {
                if (_version == null) {
                    Version v = PackageAssembly.GetName().Version;
                    _version = string.Join(".", v.Major, v.Minor, v.Build); // our version is 1.x.x only (3 segments)
                }
                return _version;
            }
        }
        private string _version;

        /// <summary>
        /// The Url for information about this package.
        /// </summary>
        public string InfoLink {
            get {
                if (_infoLink == null) {
                    PackageInfoAttribute attr = (PackageInfoAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageInfoAttribute));
                    if (attr != null)
                        _infoLink = attr.InfoLink;
                    else
                        _infoLink = "";
                }
                return _infoLink;
            }
        }
        private string _infoLink;

        /// <summary>
        /// The Url for the update server for this package.
        /// </summary>
        /// <remarks>This is not currently used.</remarks>
        public string UpdateServerLink {
            get {
                if (_updateServerLink == null) {
                    PackageInfoAttribute attr = (PackageInfoAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageInfoAttribute));
                    if (attr != null)
                        _updateServerLink = attr.UpdateServerLink;
                    else
                        _updateServerLink = "";
                }
                return _updateServerLink;
            }
        }
        private string _updateServerLink;

        /// <summary>
        /// The Url for support information for this package.
        /// </summary>
        public string SupportLink {
            get {
                if (_supportLink == null) {
                    PackageInfoAttribute attr = (PackageInfoAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageInfoAttribute));
                    if (attr != null)
                        _supportLink = attr.SupportLink;
                    else
                        _supportLink = "";
                }
                return _supportLink;
            }
        }
        private string _supportLink;

        /// <summary>
        /// The Url for the release notice for this package.
        /// </summary>
        public string ReleaseNoticeLink {
            get {
                if (_releaseNoticeLink == null) {
                    PackageInfoAttribute attr = (PackageInfoAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageInfoAttribute));
                    if (attr != null)
                        _releaseNoticeLink = attr.ReleaseNoticeLink;
                    else
                        _releaseNoticeLink = "";
                }
                return _releaseNoticeLink;
            }
        }
        private string _releaseNoticeLink;

        /// <summary>
        /// The Url for the license information for this package.
        /// </summary>
        public string LicenseLink {
            get {
                if (_licenseLink == null) {
                    PackageInfoAttribute attr = (PackageInfoAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageInfoAttribute));
                    if (attr != null)
                        _licenseLink = attr.LicenseLink;
                    else
                        _licenseLink = "";
                }
                return _licenseLink;
            }
        }
        private string _licenseLink;

        /// <summary>
        /// The resources owned by this package.
        /// </summary>
        public List<ResourceAttribute> Resources {
            get {
                // no need to cache, only used once during app initialization
                return ((ResourceAttribute[]) Attribute.GetCustomAttributes(PackageAssembly, typeof(ResourceAttribute))).ToList<ResourceAttribute>();
            }
        }

        /// <summary>
        /// The path to the Addon folder for this package.
        /// </summary>
        public string AddonsFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolder, Globals.AddOnsFolder, Domain, Product, Version);
            }
        }

        //private List<VersionManager.AddOnProduct> GetRequiredAddOnVersions() {
        //    List<VersionManager.AddOnProduct> addons = new List<VersionManager.AddOnProduct>();

        //    // normal addons
        //    RequiresAddOnAttribute[] attrs = (RequiresAddOnAttribute[]) Attribute.GetCustomAttributes(PackageAssembly, typeof(RequiresAddOnAttribute));
        //    if (attrs != null) {
        //        foreach (var attr in attrs) {
        //            VersionManager.AddOnProduct addon = VersionManager.FindAddOnVersion(attr.Domain, attr.Product, attr.Name);
        //            addons.Add(addon);
        //        }
        //    }
        //    // global addons
        //    RequiresAddOnGlobalAttribute[] globalAttrs = (RequiresAddOnGlobalAttribute[]) Attribute.GetCustomAttributes(PackageAssembly, typeof(RequiresAddOnGlobalAttribute));
        //    if (globalAttrs != null) {
        //        foreach (var attr in globalAttrs) {
        //            VersionManager.AddOnProduct addon = VersionManager.FindAddOnGlobalVersion(attr.Domain, attr.Product);
        //            addons.Add(addon);
        //        }
        //    }
        //    return addons;
        //}

        /// <summary>
        /// Returns a list of names of all packages required by the current package.
        /// </summary>
        public List<string> GetRequiredPackages() {
            List<string> requiredPackages = new List<string>();

            RequiresPackageAttribute[] attrs = (RequiresPackageAttribute[]) Attribute.GetCustomAttributes(PackageAssembly, typeof(RequiresPackageAttribute));
            if (attrs != null) {
                foreach (var attr in attrs) {
                    requiredPackages.Add(attr.PackageName);
                }
            }
            return requiredPackages;
        }
        /// <summary>
        /// Returns a list of all types in the current package, in the order they should be installed.
        /// </summary>
        /// <remarks>Only explicitly defined types using InstallOrderAttributes are listed.</remarks>
        public List<Type> GetInstallOrder() {
            List<Type> order = new List<Type>();
            InstallOrderAttribute[] attrs = (InstallOrderAttribute[]) Attribute.GetCustomAttributes(PackageAssembly, typeof(InstallOrderAttribute));
            if (attrs != null)
                order = (from a in attrs orderby a.Order select a.OrderType).ToList();
            return order;
        }
        /// <summary>
        /// Returns a list of all installable models in the current package, defined using the IInstallableModel interface.
        /// </summary>
        /// <remarks>Only explicitly defined types using InstallOrderAttributes are listed.</remarks>
        public List<Type> InstallableModels {
            get {
                if (_installableModels == null) {
                    _installableModels = new List<Type>();
                    Type[] typesInAsm;
                    try {
                        typesInAsm = PackageAssembly.GetTypes();
                    } catch (ReflectionTypeLoadException ex) {
                        typesInAsm = ex.Types;
                    }
                    foreach(var t in typesInAsm)
                        AddModelType(t);
                }
                return _installableModels;
            }
        }
        private bool AddModelType(Type type) {
            if (!TypeIsPublicClass(type))
                return false;
            if (!typeof(IInstallableModel).IsAssignableFrom(type))
                return false;

            object obj = Activator.CreateInstance(type);
            IInstallableModel model = obj as IInstallableModel;

            _installableModels.Add(type);
            return true;
        }
        private static bool TypeIsPublicClass(Type type) {
            return (type != null && type.IsPublic && type.IsClass && !type.IsAbstract);
        }

        private List<Type> _installableModels;

        /// <summary>
        /// Compares two version strings.
        /// </summary>
        /// <param name="vers1">First version string.</param>
        /// <param name="vers2">Second version string.</param>
        /// <returns>0 for equality, -1 if the first version is less than the second, 1 if the first version is greater than the second.</returns>
        /// <remarks>
        /// Version strings have the format n.n.n, normally 3 components, but this function supports any number of components, including versions string s with unequal number of components.
        /// </remarks>
        public static int CompareVersion(string vers1, string vers2) {
            string[] svers1 = vers1.Split(new char[] { '.', ',' });
            string[] svers2 = vers2.Split(new char[] { '.', ',' });
            for (int i = 0 ; ; ++i) {
                if (svers1.Length <= i) {
                    if (svers2.Length <= i)
                        return 0; // first and second are out of elements -> equal
                    else
                        return 1; // second has more elements -> second is longer (greater)
                }
                if (svers2.Length <= i) // first has more elements (greater)
                    return -1;
                int v1 = Convert.ToInt32(svers1[i]);
                int v2 = Convert.ToInt32(svers2[i]);
                if (v1 > v2) return 1;
                else if (v1 < v2) return -1;
            }
        }
        /// <summary>
        /// Defines the package's purpose.
        /// </summary>
        /// <remarks>
        /// This is used in a package's AssemblyInfo.cs file and defines its purpose and at which point the package is installed, relative to other packages.
        /// </remarks>
        public ServiceLevelEnum ServiceLevel {
            get {
                if (_serviceLevel == ServiceLevelEnum.Unknown) {
                    ServiceLevelAttribute attr = (ServiceLevelAttribute) Attribute.GetCustomAttribute(PackageAssembly, typeof(ServiceLevelAttribute));
                    if (attr == null)
                        _serviceLevel = ServiceLevelEnum.Module;
                    else
                        _serviceLevel = attr.Level;
                }
                return _serviceLevel;
            }
        }
        private ServiceLevelEnum _serviceLevel = ServiceLevelEnum.Unknown;

        /// <summary>
        /// Used to cache a package's localized data. This is used by the LocalizationDataProvider.
        /// </summary>
        public object CachedLocalization { get; set; }
    }
}
