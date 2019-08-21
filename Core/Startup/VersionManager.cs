/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {

    public class VersionManagerStartup : IInitializeApplicationStartup {
        /// <summary>
        /// Called when any node of a (single- or multi-instance) site is starting up.
        /// </summary>
        public async Task InitializeApplicationStartupAsync() {
            if (!YetaWFManager.IsBatchMode && !YetaWFManager.IsServiceMode)
                await VersionManager.RegisterAllAddOnsAsync();
        }
    }

    public static class VersionManager {

        public enum AddOnType {
            [EnumDescription("Package", "Package Support Addon")]
            Package = 0,
            [EnumDescription("Component", "Component Addon")]
            Template = 1,
            [EnumDescription("Skin", "Skin Addon")]
            Skin = 2,
            [EnumDescription("Javascript", "Named Addon")]
            AddonNamed = 3,
        }

        private const string SkinPrefix = "_sk+";
        private const string TemplatePrefix = "_t+";
        private const string PackagePrefix = "_pkg+";
        private const string AddonPrefix = "_add+";

        private const string NotUsedPrefix = "notused_";

        public class AddOnProduct {

            public class UsesInfo {
                public string PackageName { get; set; }
                public string AddonName { get; set; }
            }

            public AddOnProduct() {
                JsFiles = new List<string>();
                CssFiles = new List<string>();
                SupportTypes = new List<Type>();
            }

            public AddOnType Type { get; set; }

            public string AddonKey {
                get {
                    return MakeAddOnKey(Type, AreaName, Name);
                }
            }

            public string AreaName { get { return $"{Domain}_{Product}"; } }
            public string Domain { get; set; }
            public string Product { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }

            public List<string> JsFiles { get; set; }
            public string JsPath { get; set; }
            public List<UsesInfo> JsUses { get; set; }
            public List<string> CssFiles { get; set; }
            public string CssPath { get; set; }
            public List<UsesInfo> CssUses { get; set; }
            public List<Type> SupportTypes { get; set; }
            public SkinCollectionInfo SkinInfo { get; set; }

            public string Prefix { get { return GetPrefix(Type); } }
            public static string GetPrefix(AddOnType type) {
                switch (type) {
                    case AddOnType.Package: return PackagePrefix;
                    case AddOnType.Template: return TemplatePrefix;
                    case AddOnType.AddonNamed: return AddonPrefix;
                    case AddOnType.Skin: return SkinPrefix;
                    default: throw new InternalError("Invalid entry type {0}", type);
                }
            }

            internal static string MakeAddOnKey(AddOnType type, string area, string name = null) {
                if (type != AddOnType.AddonNamed)
                    if (name == null)
                        throw new InternalError("A name is required");
                return $"{GetPrefix(type)}{area}{(name != null ? $"+{name}" : "")}".ToLower();
            }
            internal static string MakeAddOnKey(AddOnType type, Package package, string name = null) {
                return MakeAddOnKey(type, package.AreaName, name);
            }
            /// <summary>
            /// The Url where an addon's files are located (this does not reflect the Folder directive that could be used in filelistJS/CSS.txt)
            /// </summary>
            public string GetAddOnUrl() {
                return Url + "/";
            }
            /// <summary>
            /// The Url where an addon's javascript files are located (this could be changed using the Folder directive in the filelistJS.txt file)
            /// </summary>
            public string GetAddOnJsUrl() {
                string url;
                if (string.IsNullOrWhiteSpace(JsPath)) {
                    url = Url;
                } else {
                    url = JsPath;
                }
                if (!url.EndsWith("/"))
                    url = $"{url}/";
                return url;
            }
            /// <summary>
            /// The Url where an addon's css/scss files are located (this could be changed using the Folder directive in the filelistCSS.txt file)
            /// </summary>
            public string GetAddOnCssUrl() {
                if (string.IsNullOrWhiteSpace(CssPath)) {
                    return Url + "/";
                } else if (CssPath.StartsWith("\\")) {
                    return CssPath.Replace("\\", "/");
                } else {
                    return Url + "/" + CssPath;
                }
            }

            internal static void GetSkinComponents(string skinCollection, out string domainName, out string productName, out string skinName) {
                // a skin is a string of this format "domain/product/skinname"
                string[] s = skinCollection.Split(new char[] { '/' }, 3, StringSplitOptions.None);
                if (s.Length != 3) throw new InternalError("Invalid skin {0}", skinCollection);
                domainName = s[0];
                productName = s[1];
                skinName = s[2];
            }
        }

        private static Dictionary<string, AddOnProduct> Products = new Dictionary<string, AddOnProduct>();

        public static List<AddOnProduct> GetAvailableSkinCollections() {
            if (_skinCollections == null)
                _skinCollections = (from p in Products where p.Value.AddonKey.StartsWith(SkinPrefix) select p.Value).ToList();
            return _skinCollections;
        }
        static List<AddOnProduct> _skinCollections = null;

        /// <summary>
        /// Returns information about all known addons.
        /// </summary>
        /// <returns>List of addons.</returns>
        /// <remarks>This is used by YetaWF Core and Dashboard modules and is not intended for general use.</remarks>
        public static List<AddOnProduct> GetAvailableAddOns() {
            List<AddOnProduct> list = (from p in Products select p.Value).ToList();
            return list;
        }

        // VERSIONS
        // VERSIONS
        // VERSIONS

        /// <summary>
        /// Returns a specific named addon's installed and used version.
        /// </summary>
        public static AddOnProduct TryFindAddOnNamedVersion(string areaName, string name) {
            AddOnProduct product;
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.AddonNamed, areaName, name), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific named addon's installed and used version information.
        /// </summary>
        public static AddOnProduct FindAddOnNamedVersion(string areaName, string name) {
            AddOnProduct version = TryFindAddOnNamedVersion(areaName, name);
            if (version == null)
                throw new InternalError($"Addon {areaName} {name} not registered.");
            return version;
        }

        /// <summary>
        /// Returns a specific template's installed and used version information.
        /// </summary>
        public static AddOnProduct TryFindTemplateVersion(string areaName, string templateName) {
            AddOnProduct product;
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.Template, areaName, templateName), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific template's installed and used version information.
        /// </summary>
        public static AddOnProduct FindTemplateVersion(string areaName, string templateName) {
            AddOnProduct version = TryFindTemplateVersion(areaName, templateName);
            if (version == null)
                throw new InternalError($"Template {templateName} not registered in {areaName}");
            return version;
        }

        /// <summary>
        /// Returns a specific package's installed and used version information.
        /// </summary>
        public static AddOnProduct TryFindPackageVersion(string areaName) {
            AddOnProduct product;
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.Package, areaName, "_Main"), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific package's installed and used version information.
        /// </summary>
        public static AddOnProduct FindPackageVersion(string areaName) {
            AddOnProduct version = TryFindPackageVersion(areaName);
            if (version == null)
                throw new InternalError($"Module not registered for {areaName}");
            return version;
        }
        /// <summary>
        /// Returns a specific skin's installed and used version information.
        /// </summary>
        public static AddOnProduct TryFindSkinVersion(string skinCollection) {
            AddOnProduct product;
            string domainName, productName, skinName;
            VersionManager.AddOnProduct.GetSkinComponents(skinCollection, out domainName, out productName, out skinName);
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.Skin, $"{domainName}_{productName}", skinName), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific skin's installed and used version information.
        /// </summary>
        public static AddOnProduct FindSkinVersion(string skinCollection) {
            AddOnProduct version = TryFindSkinVersion(skinCollection);
            // if the skin doesn't exist return the fallback skin
            if (version == null) {
                // if the skin doesn't exist, use the default skin (it better be there)
                skinCollection = SkinAccess.FallbackSkinCollectionName;
                version = TryFindSkinVersion(skinCollection);
                if (version == null)
                    throw new InternalError("Skin collection {0} doesn't exist", skinCollection);
            }
            return version;
        }
        /// <summary>
        /// Returns a specific skin's installed and used version information.
        /// </summary>
        public static AddOnProduct FindSkinVersion(ref SkinDefinition skinDef, bool popup) {
            AddOnProduct version = TryFindSkinVersion(skinDef.Collection);
            // if the skin doesn't exist return the fallback skin
            if (version == null) {
                // if the skin doesn't exist, use the default skin (it better be there)
                skinDef = new SkinDefinition() {
                    Collection = popup ? SkinAccess.FallbackPopupSkinCollectionName : SkinAccess.FallbackSkinCollectionName,
                    FileName = popup ? SkinAccess.FallbackPopupFileName : SkinAccess.FallbackPageFileName,
                };
                version = TryFindSkinVersion(popup ? SkinAccess.FallbackPopupSkinCollectionName : SkinAccess.FallbackSkinCollectionName);
                if (version == null)
                    throw new InternalError("Skin collection {0} doesn't exist", skinDef.Collection);
            }
            return version;
        }

        // URLS
        // URLS
        // URLS

        /// <summary>
        /// Returns the Url to the specific template's addon folder
        /// </summary>
        public static string GetAddOnTemplateUrl(string areaName, string templateName) {
            AddOnProduct addon = FindTemplateVersion(areaName, templateName);
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific template's addon folder
        /// </summary>
        public static string TryGetAddOnTemplateUrl(string areaName,  string templateName) {
            AddOnProduct addon = TryFindTemplateVersion(areaName, templateName);
            if (addon == null) return string.Empty;
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific package's addon folder
        /// </summary>
        public static string GetAddOnPackageUrl(string areaName) {
            AddOnProduct addon = FindPackageVersion(areaName);
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific package's addon folder
        /// </summary>
        public static string TryGetAddOnPackageUrl(string areaName) {
            AddOnProduct addon = TryFindPackageVersion(areaName);
            if (addon == null) return string.Empty;
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific skin's addon folder
        /// </summary>
        public static string GetAddOnSkinUrl(string skinCollection) {
            AddOnProduct addon = FindSkinVersion(skinCollection);
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific module's addon folder
        /// </summary>
        public static string GetAddOnNamedUrl(string areaName, string name) {
            AddOnProduct addon = FindAddOnNamedVersion(areaName, name);
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific product's addon folder
        /// </summary>
        public static string TryGetAddOnNamedUrl(string areaName, string name) {
            AddOnProduct addon = TryFindAddOnNamedVersion(areaName, name);
            if (addon == null) return string.Empty;
            return addon.GetAddOnUrl();
        }

        /// <summary>
        ///  Convert an addon url to a custom addon url (which has a site specific override)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetCustomUrlFromUrl(string url) {
            if (url.StartsWith(Globals.AddOnsUrl)) {
                return AddOnsCustomUrl + YetaWFManager.Manager.CurrentSite.SiteDomain + url.Substring(Globals.AddOnsUrl.Length);
            } else if (url.StartsWith(Globals.NodeModulesUrl)) {
                return AddOnsCustomUrl + YetaWFManager.Manager.CurrentSite.SiteDomain + url;
            } else if (url.StartsWith(Globals.BowerComponentsUrl)) {
                return AddOnsCustomUrl + YetaWFManager.Manager.CurrentSite.SiteDomain + url;
            }
            throw new InternalError("Url {0} doesn't start with {1} or {2}", url, Globals.AddOnsUrl, Globals.NodeModulesUrl);
        }

        // LOAD
        // LOAD
        // LOAD

        /// <summary>
        /// Returns the path to the site's Addons folder
        /// </summary>
        private static string AddOnsFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolder, Globals.AddOnsFolder);
            }
        }
        /// <summary>
        /// Returns the URL of the Addons folder
        /// </summary>
        public static string AddOnsUrl {
            get {
                return Globals.AddOnsUrl + "/";
            }
        }
        /// <summary>
        /// Returns the URL of the AddonsCustomization folder
        /// </summary>
        public static string AddOnsCustomUrl {
            get {
                return Globals.AddOnsCustomUrl + "/";
            }
        }

        /// <summary>
        /// Locates all addons and registers them at application startup.
        /// </summary>
        public static async Task RegisterAllAddOnsAsync() {

            if (YetaWFManager.IsBatchMode || YetaWFManager.IsServiceMode)
                return;

            Logging.AddLog("Locating addons");

            if (!await FileSystem.FileSystemProvider.DirectoryExistsAsync(AddOnsFolder)) {
                Logging.AddLog("Creating addons folder {0}", AddOnsFolder);
                await FileSystem.FileSystemProvider.CreateDirectoryAsync(AddOnsFolder);
            }

            Logging.AddLog("Removing Upgrade folders");

            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);
            List<string> folders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(templateFolder, "*.*");
            foreach (string folder in folders) {
                if (Path.GetFileName(folder) != Globals.DataFolder)
                    await FileSystem.FileSystemProvider.DeleteDirectoryAsync(folder);
            }

            Logging.AddLog("Searching assemblies");

            Products = new Dictionary<string, AddOnProduct>();

            // visit all known assemblies and see if there is a matching entry in the Addons folder
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                if (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage) {
                    string addonsPath = Path.Combine(AddOnsFolder, package.LanguageDomain);
                    string addonsProductPath = Path.Combine(addonsPath, package.Product);
                    if (!await FileSystem.FileSystemProvider.DirectoryExistsAsync(addonsPath))
                        await FileSystem.FileSystemProvider.CreateDirectoryAsync(addonsPath);
                    if (await package.GetHasSourceAsync()) {
                        // Make a symlink to the source code for the addons of this package
                        // make sure it's symlink not regular folder (which can occur when upgrading from bin to source package)
                        string to = Path.Combine(package.PackageSourceRoot, Globals.AddOnsFolder);
                        if (!await FileSystem.FileSystemProvider.DirectoryExistsAsync(addonsProductPath) || !await Package.IsPackageSymLinkAsync(addonsProductPath)) {
                            await FileSystem.FileSystemProvider.DeleteDirectoryAsync(addonsProductPath);
                            if (!await Package.CreatePackageSymLinkAsync(addonsProductPath, to))
                                throw new InternalError("Couldn't create symbolic link from {0} to {1} - You will have to investigate the failure and manually create the link", addonsProductPath, to);
                        }
                    } else {
                        // no source
                    }
                    Logging.AddLog("Searching {0} for addon files", addonsProductPath);
                    await RegisterAllProductsAsync(package, addonsProductPath);
                }
            }

            Logging.AddLog("Completed locating addons");
        }

        private static async Task RegisterAllProductsAsync(Package package, string asmFolder) {
            // find all addons for this package
            List<string> addonFolders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(asmFolder);
            foreach (var folder in addonFolders) {
                string directoryName = Path.GetFileName(folder);
                if (string.Compare(directoryName, "_Main", true) == 0) {
                    // main module addon (for all modules in this assembly
                    await RegisterPackageAddonAsync(package, folder);
                } else if (string.Compare(directoryName, "_Templates", true) == 0) {
                    await RegisterTemplatesAsync(package, folder);
                } else if (string.Compare(directoryName, "_Addons", true) == 0) {
                    await RegisterAddonsAsync(package, folder);
                } else if (string.Compare(directoryName, "_Skins", true) == 0) {
                    await RegisterSkinsAsync(package, folder);
                } else if (string.Compare(directoryName, "_SiteTemplates", true) == 0) {
                    await CopySiteTemplatesAsync(folder);
                    await CopySiteUpgradesAsync(package, folder);
                } else if (directoryName.StartsWith("_")) {
                    // reserved for future use and 3rd party
                } else {
                    throw new InternalError("Unexpected folder {0} in {1}", directoryName, folder);
                }
            }
        }

        private static async Task CopySiteTemplatesAsync(string folder) {
            List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(folder, "*.txt");
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);
            foreach (string file in files) {
                string newFile = Path.Combine(templateFolder, Path.GetFileName(file));
                await FileSystem.FileSystemProvider.CopyFileAsync(file, newFile);
            }
        }
        private static async Task CopySiteUpgradesAsync(Package package, string sourceFolder) {

            List<string> folders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(sourceFolder, "*.*");

            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);

            foreach (string folder in folders) {
                string upgradeVersion = Path.GetFileName(folder);
                string newFolder = Path.Combine(templateFolder, package.AreaName, upgradeVersion);

                List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(folder, "*.zip");
                if (files.Count > 0)
                    await FileSystem.FileSystemProvider.CreateDirectoryAsync(newFolder);
                foreach (string file in files) {
                    string newFile = Path.Combine(templateFolder, newFolder, Path.GetFileName(file));
                    await FileSystem.FileSystemProvider.CopyFileAsync(file, newFile);
                }
            }
        }

        private static async Task RegisterSkinsAsync(Package package, string asmFolder) {
            List<string> addonFolders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(asmFolder);
            foreach (var folder in addonFolders) {
                string directoryName = Path.GetFileName(folder);
                await RegisterSkinAddonAsync(package, folder, directoryName);
            }
        }

        private static async Task RegisterAddonsAsync(Package package, string asmFolder) {
            List<string> addonFolders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(asmFolder);
            foreach (var folder in addonFolders) {
                string directoryName = Path.GetFileName(folder);
                await RegisterNamedAddonAsync(package, folder, directoryName);
            }
        }
        private static async Task RegisterTemplatesAsync(Package package, string asmFolder) {
            List<string> templateFolders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(asmFolder);
            foreach (var folder in templateFolders) {
                string directoryName = Path.GetFileName(folder);
                await RegisterTemplateAddonAsync(package, folder, directoryName);
            }
        }

        private static async Task RegisterTemplateAddonAsync(Package package, string folder, string templateName) {
            await RegisterAnyAddonAsync(AddOnType.Template, package, folder, templateName);
        }
        private static async Task RegisterNamedAddonAsync(Package package, string folder, string addonName) {
            await RegisterAnyAddonAsync(AddOnType.AddonNamed, package, folder, addonName);
        }
        private static async Task RegisterPackageAddonAsync(Package package, string folder) {
            await RegisterAnyAddonAsync(AddOnType.Package, package, folder, "_Main");
        }
        private static async Task RegisterSkinAddonAsync(Package package, string folder, string skin) {
            await RegisterAnyAddonAsync(AddOnType.Skin, package, folder, skin);
        }

        private static async Task RegisterAnyAddonAsync(AddOnType type, Package package, string folder, string name) {
            string key = AddOnProduct.MakeAddOnKey(type, package, name);
            if (Products.ContainsKey(key))
                throw new InternalError($"Key {key} already exists for area {package.AreaName}");
            AddOnProduct version = new AddOnProduct {
                Type = type,
                Domain = package.Domain,
                Product = package.Product,
                Name = name,
                Url = Utility.PhysicalToUrl(folder),
            };
            await AddFileListsAsync(version, package, folder);
            Products.Add(key, version);
            Logging.AddLog("added {0} in {1}", version.AddonKey, folder);
        }

        private static async Task AddFileListsAsync(AddOnProduct version, Package package, string folder) {
            ReadFileInfo info = await ReadFileAsync(version, Path.Combine(folder, Globals.Addons_JSFileList));
            version.JsFiles = info.Lines;
            version.JsPath = info.Files;
            version.JsUses = info.Uses;
            info = await ReadFileAsync(version, Path.Combine(folder, Globals.Addons_CSSFileList));
            version.CssFiles = info.Lines;
            version.CssPath = info.Files;
            version.CssUses = info.Uses;
            version.SupportTypes = await ReadSupportFileAsync(version, package, folder);
            if (version.Type == AddOnType.Skin) {
                SkinAccess skinAccess = new SkinAccess();
                version.SkinInfo = await skinAccess.ParseSkinFileAsync(version.Domain, version.Product, Path.GetFileName(folder), folder);
            }
        }

        private static async Task<List<Type>> ReadSupportFileAsync(AddOnProduct version, Package package, string folder) {

            List<Type> types = new List<Type>();

            if (version.Type == AddOnType.Skin) return types;
            if (package == null) throw new InternalError("Package required");

            // build a type name based on domain name and product name - if it exists, add it
            // domainName.Modules.productName.Addons class Info
            // load the assembly/type implementing addon support
            Type dynType = null;
            try {
                Assembly asm = package.PackageAssembly;
                string typeName;
                if (package.IsCorePackage)
                    typeName = version.Domain + "." + version.Product + ".Addons";
                else
                    typeName = version.Domain + ".Modules." + version.Product + ".Addons";
                if (version.Type == AddOnType.Template) {
                    string templateName = Path.GetFileName(folder);
                    typeName += ".Templates." + templateName;
                } else if (version.Type == AddOnType.Package) {
                    typeName += ".Info";
                } else if (version.Type == AddOnType.AddonNamed) {
                    string name = Path.GetFileName(folder);
                    typeName += "." + name;
                } else
                    throw new InternalError("Unexpected version type {0} for {1}", version.Type, package.Name);
                if (typeName != null)
                    dynType = asm.GetType(typeName);
            } catch (Exception) { }
            if (dynType != null) {
                types.Add(dynType);
                Logging.AddLog("Addon support dynamically added for {0}", dynType.FullName);
            }
            string file = Path.Combine(folder, Globals.Addons_SupportFileList);

            // also add explicitly defined support types
            if (YetaWFManager.DiagnosticsMode) {
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(file))
                    return types;
            }
            List<string> lines;
            try {
                lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
            } catch (Exception) {
                return types;
            }
            foreach (var line in lines) {
                if (!string.IsNullOrWhiteSpace(line)) {
                    Type type = Type.GetType(line);
                    if (type == null)
                        throw new InternalError("Type {0} found in file {1} doesn't exist", line, file);
                    object o = Activator.CreateInstance(type);
                    if (o == null)
                        throw new InternalError("Type {0} found in file {1} can't be created", line, file);
                    IAddOnSupport addSupport = o as IAddOnSupport;
                    if (addSupport == null)
                        throw new InternalError("No IAddOnSupport interface found on type {0} found in file {1}", line, file);
                    if (type == dynType)
                        Logging.AddErrorLog("Dynamic type {0} is also added explicitly", type.FullName);
                    //  throw new InternalError("Dynamic type {0} is also added explicitly", type.FullName);
                    types.Add(type);
                    Logging.AddLog("Addon support explicitly added for {0}", type.FullName);
                }
            }
            return types;
        }

        private class ReadFileInfo {
            public string Files { get; set; }
            public List<string> Lines { get; set; }
            public List<AddOnProduct.UsesInfo> Uses { get; set; }
        }

        private static async Task<ReadFileInfo> ReadFileAsync(AddOnProduct version, string file) {
            string filePath = "";
            List<string> lines = new List<string>();
            List<AddOnProduct.UsesInfo> uses = new List<AddOnProduct.UsesInfo>();
            if (await FileSystem.FileSystemProvider.FileExistsAsync(file)) {

                Logging.AddLog("Found {0}", file);

                lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);

                // remove comments
                lines = (from l in lines where !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l) select l.Trim()).ToList();

                // remove MVC5/MVC6 lines that don't match current version
#if MVC6
                lines = (from l in lines where !l.StartsWith("MVC5 ") select l).ToList();
                lines = (from l in lines select (l.StartsWith("MVC6 ") ? l.Substring(4) : l).Trim()).ToList();
#else
                lines = (from l in lines where !l.StartsWith("MVC6 ") select l).ToList();
                lines = (from l in lines select (l.StartsWith("MVC5 ") ? l.Substring(4) : l).Trim()).ToList();
#endif
                // Find a Folder directive (1 only, others are ignored)
                string path = (from l in lines where l.StartsWith("Folder ") select l.Trim()).FirstOrDefault();
                if (path != null) {
                    path = path.Substring(6).Trim();
                    if (path.StartsWith("/")) {
                        if (!path.EndsWith("/")) path = path + "/";
                    } else if (!path.EndsWith("/")) {
                        path = path + "/";
                    }
                    if (!string.IsNullOrWhiteSpace(path)) {
                        filePath = path;
                    }
                }

                // Find Uses directives
                List<string> usesLines = (from l in lines where l.StartsWith("Uses ") select l.Substring(4).Trim()).ToList();
                foreach (string usesLine in usesLines) {
                    string[] parts = usesLine.Split(new char[] { ',' }, StringSplitOptions.None);
                    if (parts.Length != 2)
                        throw new InternalError($"Invalid Uses statement in file {file}");
                    uses.Add(new AddOnProduct.UsesInfo {
                        PackageName = parts[0].Trim(),
                        AddonName = parts[1].Trim(),
                    });
                }

                // remove comments, Folder and whitespace
                lines = (from l in lines where !l.StartsWith("#") && !l.StartsWith("Folder ") && !l.StartsWith("Uses ") && !string.IsNullOrWhiteSpace(l) select l.Trim()).ToList();

            }
            return new ReadFileInfo {
                Lines = lines,
                Files = filePath,
                Uses = uses,
            };
        }
    }
}
