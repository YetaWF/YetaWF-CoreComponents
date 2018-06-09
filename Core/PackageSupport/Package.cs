/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Identity;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using YetaWF.PackageAttributes;
using System.Threading.Tasks;
#if MVC6
#else
using System.Web.Compilation;
#endif

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
            if (packageName == "YetaWF.Core")
                throw new InternalError("You can't specify the YetaWF.Core package version. When exporting a package, the YetaWF Core version is saved and the package can only be imported on a YetaWF instance with the same or newer version. This means that when you are developing packages for distribution, you have to do so using the oldest YetaWF version that you intend to support.");

            if (!string.IsNullOrWhiteSpace(minVersion) && !string.IsNullOrWhiteSpace(maxVersion)) {
                if (Package.CompareVersion(minVersion, maxVersion) > 0) throw new InternalError("The specified minimum version {0} is larger than the minimum version {1}", minVersion, maxVersion);
            }
        }
        public string PackageName { get; private set; }
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
        public Type OrderType { get; private set; }
        public int Order { get; private set; }
    }

    /// <summary>
    /// Service level of a package used to define its main purpose and is used to determine installation order (lowest value installed first).
    /// </summary>
    public enum ServiceLevelEnum {
        Unknown = -1,
        Core = 0,
        CachingProvider = 50, // Cache, FileSystem
        LowLevelServiceProvider = 100, // Language, Identity (can't use higher services,like Scheduling)
        SchedulerProvider = 150, // Scheduler
        ServiceProvider = 200, // Logging
        ServiceConsumer = 300, // not currently used
        Module = 1000, // all modules
        ModuleConsumer = 2000, // not currently used
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
    public class PublicPartialViewsAttribute : Attribute { //$$$can this be removed?
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
#if MVC6
        public /* Only so startup code can access */
#else
        private
#endif
            Package(Assembly assembly) {
            PackageAssembly = assembly;
        }

        public Assembly PackageAssembly { get; private set; }

        /// <summary>
        /// Returns all packages referenced by this YetaWF instance, i.e., the website (this excludes templates, utilities)
        /// </summary>
        public static DataProviderGetRecords<Package> GetAvailablePackages(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters) {
            List<Package> packages = (from p in GetAvailablePackages() select p).ToList();// copy
            //packages.AddRange(GetTemplatePackages());
            //packages.AddRange(GetUtilityPackages());
            return DataProviderImpl<Package>.GetRecords(packages, skip, take, sort, filters);
        }

        /// <summary>
        /// Returns all packages referenced by this YetaWF instance, i.e., the website (this excludes templates, utilities)
        /// </summary>
        public static List<Package> GetAvailablePackages() {
            if (_availablePackages == null) {
                ICollection assemblies = null;
#if MVC6
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
#else
                if (!YetaWFManager.HaveManager || YetaWFManager.Manager.HostUsed != YetaWFManager.BATCHMODE) {
                    // this will only work in ASP.NET apps
                    assemblies = BuildManager.GetReferencedAssemblies();
                } else {
                    // Get all currently loaded assemblies - make sure this is outside asp.net app
                    assemblies = AppDomain.CurrentDomain.GetAssemblies();
                }
#endif
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

        public static async Task<List<Package>> GetTemplatePackagesAsync() {
            string sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Templates", "Templates");
            List<Package> packages = await FindPackages(Path.Combine(YetaWFManager.RootFolderSolution, sourceFolder), csAssemblyTemplateRegex);
            return packages;
        }
        public static async Task<List<Package>> GetUtilityPackagesAsync() {
            string sourceFolder = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "SourceFolder_Utilities", "Utilities");
            List<Package> packages = await FindPackages(Path.Combine(YetaWFManager.RootFolderSolution, sourceFolder), csAssemblyUtilityRegex);
            return packages;
        }

        private static readonly Regex csAssemblyTemplateRegex = new Regex(@"\[\s*assembly\s*\:\s*Package\s*\(\s*PackageTypeEnum\s*\.\s*Template\s*,\s*\""(?'asm'[A-Za-z0-9_\.]+)""\s*\)\s*\]", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex csAssemblyUtilityRegex = new Regex(@"\[\s*assembly\s*\:\s*Package\s*\(\s*PackageTypeEnum\s*\.\s*Utility\s*,\s*\""(?'asm'[A-Za-z0-9_\.]+)""\s*\)\s*\]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static async Task<List<Package>> FindPackages(string packageRoot, Regex regex) {
            List<Package> packages = new List<Package>();
            if (!await FileSystem.FileSystemProvider.DirectoryExistsAsync(packageRoot)) return packages;
            List<string> packageDirs = await FileSystem.FileSystemProvider.GetDirectoriesAsync(packageRoot);
            foreach (string dir in packageDirs) {
                string propsFile = Path.Combine(dir, SOURCE_IDENTIFIER);
                if (await FileSystem.FileSystemProvider.FileExistsAsync(propsFile)) {
                    string target = await ExtractAssemblyNameAsync(propsFile, regex);
                    if (string.IsNullOrWhiteSpace(target))
                        throw new InternalError("Folder {0} does not define an assembly name in {1}", dir, SOURCE_IDENTIFIER);
                    string asmPath = Path.Combine(dir, "Bin", target);
                    if (!await FileSystem.FileSystemProvider.FileExistsAsync(asmPath))
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
        private static async Task<string> ExtractAssemblyNameAsync(string propsFile, Regex regex) {
            string contents = await FileSystem.FileSystemProvider.ReadAllTextAsync(propsFile);
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
        /// If a package object is created using the constructor Package(assembly) and the assembly is not a YetaWF package, this returns false.
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
        public async Task<bool> GetHasSourceAsync() {
            return await FileSystem.FileSystemProvider.DirectoryExistsAsync(PackageSourceRoot);
        }

        /// <summary>
        /// The domain name of the company owning this package. Periods (".") are not allowed. Top-level domains are only added if your company doesn't own the .com domain.
        /// </summary>
        /// <remarks>
        /// The Domain name specified cannot include a period (".").
        /// For example, if your company owns the domain "Softelvdm.com", use "Softelvdm" as domain name (no top-level domain .com as that is assumed).
        /// If your company owns "MyCompany.com", use "MyCompany". If your company owns "MyCompany.net" but NOT "MyCompany.com", you must include the top-level domain as part yof your domain (but WITHOUT periods), as in "MyCompanyNet".
        /// If your company owns "SomeCompany.co.uk" use "SomeCompanyCoUk").
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
        /// Your company name, in displayable form.
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
        /// Store Url for purchasable packages.
        /// </summary>
        /// <remarks>Not used by YetaWF. Can be used by third-party packages.</remarks>
        public string StoreLink {
            get {
                if (_storeLink == null) {
                    PackageInfoAttribute attr = (PackageInfoAttribute)Attribute.GetCustomAttribute(PackageAssembly, typeof(PackageInfoAttribute));
                    if (attr != null)
                        _storeLink = attr.StoreLink;
                    else
                        _storeLink = "";
                }
                return _storeLink;
            }
        }
        private string _storeLink;


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
                return Path.Combine(YetaWFManager.RootFolder, Globals.AddOnsFolder, Domain, Product);
            }
        }

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
                if (_installableModels == null)
                    _installableModels = GetClassesInPackage<IInstallableModel>();
                return _installableModels;
            }
        }
        private List<Type> _installableModels;

        /// <summary>
        /// Return a list of all types in all packages that support an interface or are derived from the specified type.
        /// </summary>
        /// <typeparam name="TYPE">Type used to filter the classes.</typeparam>
        /// <param name="OrderByServiceLevel">Sort by service level (low to high).</param>
        /// <param name="serviceLevel">Filter by service level (or ServiceLevelEnum.Unknown for all classes).</param>
        /// <returns>List of classes.</returns>
        public static List<Type> GetClassesInPackages<TYPE>(bool OrderByServiceLevel = false, ServiceLevelEnum ServiceLevel = ServiceLevelEnum.Unknown) {
            List<Type> list = new List<Type>();
            List<Package> packages = Package.GetAvailablePackages();
            if (OrderByServiceLevel)
                packages = (from p in packages orderby (int)p.ServiceLevel select p).ToList();
            if (ServiceLevel != ServiceLevelEnum.Unknown)
                packages = (from p in packages where ServiceLevel == p.ServiceLevel select p).ToList();

            foreach (Package package in packages) {
                Type[] typesInAsm;
                try {
                    typesInAsm = package.PackageAssembly.GetTypes();
                } catch (ReflectionTypeLoadException ex) {
                    typesInAsm = ex.Types;
                }
                Type[] classTypes = typesInAsm.Where(type => IsOfType<TYPE>(type)).ToArray<Type>();
                list.AddRange(classTypes);
            }
            return list;
        }
        /// <summary>
        /// Return a list of all classes in one package that support an interface or are derived from the specified type.
        /// </summary>
        /// <typeparam name="TYPE">Type used to filter the classes.</typeparam>
        /// <returns>List of classes.</returns>
        public List<Type> GetClassesInPackage<TYPE>() {
            List<Type> list = new List<Type>();
            Type[] typesInAsm;
            try {
                typesInAsm = PackageAssembly.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
                typesInAsm = ex.Types;
            }
            Type[] classTypes = typesInAsm.Where(type => IsOfType<TYPE>(type)).ToArray<Type>();
            list.AddRange(classTypes);
            return list;
        }
        private static bool IsOfType<TYPE>(Type type) {
            if (!IsPublicClass(type))
                return false;
            return typeof(TYPE).IsAssignableFrom(type);
        }
        private static bool IsPublicClass(Type type) {
            return (type != null && type.IsPublic && type.IsClass && !type.IsAbstract && !type.IsGenericType);
        }

        /// <summary>
        /// Compares two version strings.
        /// </summary>
        /// <param name="vers1">First version string.</param>
        /// <param name="vers2">Second version string.</param>
        /// <returns>0 for equality, -1 if the first version is less than the second, 1 if the first version is greater than the second.</returns>
        /// <remarks>
        /// Version strings have the format n.n.n, normally 3 components, but this function supports any number of components, including versions strings with unequal number of components.
        /// </remarks>
        public static int CompareVersion(string vers1, string vers2) {
            string[] svers1 = string.IsNullOrWhiteSpace(vers1) ? new string[] { } : vers1.Split(new char[] { '.', ',', ' ' }, 5, StringSplitOptions.RemoveEmptyEntries);
            string[] svers2 = string.IsNullOrWhiteSpace(vers2) ? new string[] { } : vers2.Split(new char[] { '.', ',', ' ' }, 5, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0 ; i < 4; ++i) {
                if (svers1.Length <= i) {
                    if (svers2.Length <= i)
                        return 0; // first and second are out of elements -> equal
                    else
                        return -1; // second has more elements -> second is longer (greater)
                }
                if (svers2.Length <= i) // first has more elements (greater)
                    return 1;
                int v1 = Convert.ToInt32(svers1[i]);
                int v2 = Convert.ToInt32(svers2[i]);
                if (v1 > v2) return 1;
                else if (v1 < v2) return -1;
            }
            return 0;
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

        /// <summary>
        /// Used to cache a package's license data. This is available to package implementers and is not used by YetaWF.
        /// </summary>
        public dynamic CachedLicenseData { get; set; }

        /// <summary>
        /// The Asp.Net Mvc version for which this package was built
        /// </summary>
        public YetaWFManager.AspNetMvcVersion AspNetMvc {
            get {
#if MVC6
                return YetaWFManager.AspNetMvcVersion.MVC6;
#else
                return YetaWFManager.AspNetMvcVersion.MVC5;
#endif
            }
        }
    }
}
