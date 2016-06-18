/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {

    public class VersionManagerStartup : IInitializeApplicationStartup {
        public void InitializeApplicationStartup() {
            Logging.AddLog("Removing all compiled files");
            RemoveFolderContents(YetaWFManager.RootFolder, new List<string> {
                "*" + Globals.Compiled + ".*",
                "*._ci_*"
            });
            Logging.AddLog("Completed removing all compiled files");
            VersionManager.RegisterAllAddOns();
        }
        private static void RemoveFolderContents(string targetPath, List<string> RemovePatterns = null) {
            string[] files;
            try {
                files = Directory.GetFiles(targetPath);
            } catch (Exception) {// fails for symlinks
                return;
            }
            foreach (string file in files) {
                foreach (string remPatt in RemovePatterns) {
                    if (Operators.LikeString(file, remPatt, Microsoft.VisualBasic.CompareMethod.Text)) {
                        Logging.AddLog("Removing file {0}", file);
                        File.Delete(file);
                        break;
                    }
                }
            }
            string[] dirs = Directory.GetDirectories(targetPath);
            foreach (string dir in dirs)
                RemoveFolderContents(dir, RemovePatterns);
        }
    }

    public class VersionManager {

        public enum AddOnType {
            Module = 0,
            Template = 1,
            Skin = 2,
            AddonJS = 3,
            AddonJSGlobal = 4,
        }

        private const string SkinPrefix = "_skins+";
        private const string TemplatePrefix = "_templates+";
        private const string ModulePrefix = "_modules+";
        private const string AddonPrefix = "_addon+";
        private const string AddonGlobalPrefix = "_global+";

        private const string NotUsedPrefix = "notused_";

        public class AddOnProduct {
            public AddOnProduct() {
                JsFiles = new List<string>();
                CssFiles = new List<string>();
                SupportTypes = new List<Type>();
            }

            public AddOnType Type { get; set; }

            public string AddonKey {
                get {
                    return MakeAddOnKey(Type, Domain, Product, Name);
                }
            }

            public string Domain { get; set; }
            public string Product { get; set; }
            public string Version { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }

            public List<string> JsFiles { get; set; }
            public string JsPath { get; set; }
            public List<string> CssFiles { get; set; }
            public string CssPath { get; set; }
            public List<Type> SupportTypes { get; set; }
            public SkinCollectionInfo SkinInfo { get; set; }

            public string Prefix { get { return GetPrefix(Type); } }
            public static string GetPrefix(AddOnType type) {
                switch (type) {
                    case AddOnType.Module: return ModulePrefix;
                    case AddOnType.Template: return TemplatePrefix;
                    case AddOnType.AddonJS: return AddonPrefix;
                    case AddOnType.AddonJSGlobal: return AddonGlobalPrefix;
                    case AddOnType.Skin: return SkinPrefix;
                    default: throw new InternalError("Invalid entry type {0}", type);
                }
            }

            internal static string MakeAddOnKey(AddOnType type, string domain, string product, string name = null) {
                if (type != AddOnType.AddonJS && type != AddOnType.AddonJSGlobal)
                    if (name == null)
                        throw new InternalError("A name is required");
                return (GetPrefix(type) + domain + "+" + product + (name != null ? "+" + name : "")).ToLower();
            }
            internal static string MakeAddOnKey(AddOnType type, Package package, string name = null) {
                return MakeAddOnKey(type, package.Domain, package.Product, name);
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
                if (string.IsNullOrWhiteSpace(JsPath)) {
                    return Url + "/";
                } else if (JsPath.StartsWith("\\")) {
                    return JsPath.Replace("\\", "/");
                } else {
                    return Url + "/" + JsPath;
                }
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
            List<AddOnProduct> list = (from p in Products where p.Value.AddonKey.StartsWith(SkinPrefix) select p.Value).ToList();
            return list;
        }

        // VERSIONS
        // VERSIONS
        // VERSIONS

        /// <summary>
        /// Returns a specific addon's installed and used version.
        /// </summary>
        public static AddOnProduct TryFindAddOnVersion(string domainName, string productName, string name) {
            AddOnProduct product;
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.AddonJS, domainName, productName, name), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific addon's installed and used version information.
        /// </summary>
        public static AddOnProduct FindAddOnVersion(string domainName, string productName, string name) {
            AddOnProduct version = TryFindAddOnVersion(domainName, productName, name);
            if (version == null)
                throw new InternalError("Addon Domain/Product/Name {0}/{1}/{2} not registered.", domainName, productName, name);
            return version;
        }

        /// <summary>
        /// Returns a specific global addon's installed and used version.
        /// </summary>
        public static AddOnProduct TryFindAddOnGlobalVersion(string domainName, string productName) {
            AddOnProduct product;
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.AddonJSGlobal, domainName, productName), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific global addon's installed and used version information.
        /// </summary>
        public static AddOnProduct FindAddOnGlobalVersion(string domainName, string productName) {
            AddOnProduct version = TryFindAddOnGlobalVersion(domainName, productName);
            if (version == null)
                throw new InternalError("Global addon Domain/Product {0}/{1} not registered.", domainName, productName);
            return version;
        }

        public enum KendoAddonTypeEnum {
            Core = 0,
            Pro = 1,
        }

        /// <summary>
        /// Returns the installed Kendo addon
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public static AddOnProduct KendoAddon {
            get {
                if (_kendoAddon == null) {
                    AddOnProduct addonCore = TryFindAddOnGlobalVersion("telerik.com", "Kendo_UI_Core");
                    AddOnProduct addonPro = TryFindAddOnGlobalVersion("telerik.com", "Kendo_UI_Pro");
                    if (addonPro != null) {
                        _kendoAddonType = KendoAddonTypeEnum.Pro;
                        _kendoAddon = addonPro;
                    } else if (addonCore != null) {
                        _kendoAddonType = KendoAddonTypeEnum.Core;
                        _kendoAddon = addonCore;
                    } else
                        throw new InternalError("Found neither Kendo UI Pro nor Kendo UI Core");
                }
                return _kendoAddon;
            }
        }
        /// <summary>
        /// Returns the installed Kendo addon type (Core or Pro)
        /// </summary>
        public static KendoAddonTypeEnum KendoAddonType {
            get {
                AddOnProduct prod = KendoAddon;// side effect - determine Kendo type
                return _kendoAddonType;
            }
        }
        private static AddOnProduct _kendoAddon = null;
        private static KendoAddonTypeEnum _kendoAddonType;

        /// <summary>
        /// Returns a specific template's installed and used version information.
        /// </summary>
        public static AddOnProduct TryFindTemplateVersion(string domainName, string productName, string templateName) {
            AddOnProduct product;
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.Template, domainName, productName, templateName), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific template's installed and used version information.
        /// </summary>
        public static AddOnProduct FindTemplateVersion(string domainName, string productName, string templateName) {
            AddOnProduct version = TryFindTemplateVersion(domainName, productName, templateName);
            if (version == null)
                throw new InternalError("Template {0} not registered in {1}.{2}.", templateName, domainName, productName);
            return version;
        }

        /// <summary>
        /// Returns a specific module's installed and used version information.
        /// </summary>
        public static AddOnProduct TryFindModuleVersion(string domainName, string productName, string module = "_Main") {
            AddOnProduct product;
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.Module, domainName, productName, module), out product))
                return null;
            return product;
        }
        /// <summary>
        /// Returns a specific module's installed and used version information.
        /// </summary>
        public static AddOnProduct FindModuleVersion(string domainName, string productName, string module = "_Main") {
            AddOnProduct version = TryFindModuleVersion(domainName, productName, module);
            if (version == null)
                throw new InternalError("Module {0} not registered in {1}.{2}.", module, domainName, productName);
            return version;
        }

        /// <summary>
        /// Returns a specific skin's installed and used version information.
        /// </summary>
        public static AddOnProduct TryFindSkinVersion(string skinCollection) {
            AddOnProduct product;
            string domainName, productName, skinName;
            VersionManager.AddOnProduct.GetSkinComponents(skinCollection, out domainName, out productName, out skinName);
            if (!Products.TryGetValue(AddOnProduct.MakeAddOnKey(AddOnType.Skin, domainName, productName, skinName), out product))
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
                skinDef = new SkinDefinition();
                version = TryFindSkinVersion(skinDef.Collection);
                if (version == null)
                    throw new InternalError("Skin collection {0} doesn't exist", skinDef.Collection);
            }
            return version;
        }

        // URLS
        // URLS
        // URLS

        /// <summary>
        /// Returns the Url to the specific template's addon folder (including version)
        /// </summary>
        public static string GetAddOnTemplateUrl(string domainName, string productName, string templateName) {
            AddOnProduct addon = FindTemplateVersion(domainName, productName, templateName);
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific template's addon folder (including version)
        /// </summary>
        public static string TryGetAddOnTemplateUrl(string domainName, string productName,  string templateName) {
            AddOnProduct addon = TryFindTemplateVersion(domainName, productName, templateName);
            if (addon == null) return string.Empty;
            return addon.GetAddOnUrl();
        }

        /// <summary>
        /// Returns the Url to the specific module's addon folder (including version)
        /// </summary>
        public static string GetAddOnModuleUrl(string domainName, string productName, string module = "_Main") {
            AddOnProduct addon = FindModuleVersion(domainName, productName, module);
            return addon.GetAddOnUrl();
        }
        /// <summary>
        /// Returns the Url to the specific product's addon folder (including version)
        /// </summary>
        public static string TryGetAddOnModuleUrl(string domainName, string productName, string module = "_Main") {
            AddOnProduct addon = TryFindModuleVersion(domainName, productName, module);
            if (addon == null) return string.Empty;
            return addon.GetAddOnUrl();
        }

        /// <summary>
        /// Returns the Url to the specific skin's addon folder (including version)
        /// </summary>
        public static string GetAddOnSkinUrl(string skinCollection) {
            AddOnProduct addon = FindSkinVersion(skinCollection);
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
            }
            if (url.StartsWith(Globals.NugetContentsUrl)) {
                return AddOnsCustomUrl + YetaWFManager.Manager.CurrentSite.SiteDomain + url.Substring(Globals.NugetContentsUrl.Length);
            }
            throw new InternalError("Url {0} doesn't start with {1} or {2}", url, Globals.AddOnsUrl, Globals.NugetContentsUrl);
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
        /// Returns the path to the site's Views folder
        /// </summary>
        private static string AreasFolder {
            get {
                return Path.Combine(YetaWFManager.RootFolder, Globals.AreasFolder);
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
        /// Returns the URL of the Nuget scripts folder
        /// </summary>
        public static string NugetScriptsUrl {
            get {
                return Globals.NugetScriptsUrl;
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
        public static void RegisterAllAddOns() {

            Logging.AddLog("Locating addons");

            if (!File.Exists(AddOnsFolder)) {
                Logging.AddLog("Creating addons folder {0}", AddOnsFolder);
                Directory.CreateDirectory(AddOnsFolder);
            }

            Products = new Dictionary<string, AddOnProduct>();

            Logging.AddLog("Searching assemblies");

            // visit all known assemblies and see if there is a matching entry in the Addons folder
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                if (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage) {
                    string addonsPath = Path.Combine(AddOnsFolder, package.Domain, package.Product);
                    string addonsVersPath = Path.Combine(addonsPath, package.Version);
                    if (package.HasSource) {
                        // first check if ANY version of this package exists in the Addon folder
                        {
                            if (!Directory.Exists(addonsPath))
                                Directory.CreateDirectory(addonsPath);
                            if (!Directory.Exists(addonsVersPath)) {
                                // Make a symlink to the source code for the addons of this package
                                string to = Path.Combine(package.PackageSourceRoot, Globals.AddOnsFolder);
                                if (!Package.CreatePackageSymLink(addonsVersPath, to))
                                    throw new InternalError("Couldn't create symbolic link from {0} to {1} - You will have to investigate the failure and manually create the link", addonsVersPath, to);
                            }
                        }
                        // Make a symlink to the views for this package
                        {
                            string to = Path.Combine(package.PackageSourceRoot, Globals.ViewsFolder);
                            if (Directory.Exists(to)) {// skins and some modules don't have views
                                string viewsPath = Path.Combine(AreasFolder, package.AreaName);
                                if (!Directory.Exists(viewsPath))
                                    Directory.CreateDirectory(viewsPath);
                                viewsPath = Path.Combine(viewsPath, Globals.ViewsFolder);
                                if (!Directory.Exists(viewsPath)) {
                                    if (!Package.CreatePackageSymLink(viewsPath, to))
                                        throw new InternalError("Couldn't create symbolic link from {0} to {1} - You will have to investigate the failure and manually create the link", viewsPath, to);
                                }
                            }
                        }
                    } else {
                        // no source
                        bool f = package.HasSource;
                    }
                    Logging.AddLog("Searching {0} for addon files", addonsVersPath);
                    RegisterAllProducts(package, addonsVersPath);
                }
            }

            Logging.AddLog("Completed locating addons");
        }

        private static void RegisterAllProducts(Package package, string asmFolder) {
            // find all addons for this package
            string[] addonFolders = Directory.GetDirectories(asmFolder);
            foreach (var folder in addonFolders) {
                string directoryName = Path.GetFileName(folder);
                if (string.Compare(directoryName, "_Main", true) == 0) {
                    // main module addon (for all modules in this assembly
                    RegisterModuleAddon(package, folder, "_Main");
                } else if (string.Compare(directoryName, "_Templates", true) == 0) {
                    RegisterTemplates(package, folder);
                } else if (string.Compare(directoryName, "_Addons", true) == 0) {
                    RegisterAddons(package, folder);
                } else if (string.Compare(directoryName, "_Skins", true) == 0) {
                    RegisterSkins(package, folder);
                } else if (string.Compare(directoryName, Globals.GlobalJavaScript, true) == 0) {
                    RegisterGlobalAddons(package, folder);
                } else
                    throw new InternalError("Unexpected folder {0} in {1}", directoryName, folder);
            }
        }

        private static void RegisterSkins(Package package, string asmFolder) {
            string[] addonFolders = Directory.GetDirectories(asmFolder);
            foreach (var folder in addonFolders) {
                string directoryName = Path.GetFileName(folder);
                RegisterSkinAddon(package, folder, directoryName);
            }
        }

        private static void RegisterAddons(Package package, string asmFolder) {
            string[] addonFolders = Directory.GetDirectories(asmFolder);
            foreach (var folder in addonFolders) {
                string directoryName = Path.GetFileName(folder);
                RegisterRegularAddon(package, folder, directoryName);
            }
        }

        private static void RegisterGlobalAddons(Package package, string asmFolder) {
            string[] domainFolders = Directory.GetDirectories(asmFolder);
            foreach (var domainFolder in domainFolders) {
                string domain = Path.GetFileName(domainFolder);
                string[] productFolders = Directory.GetDirectories(domainFolder);
                foreach (var productFolder in productFolders) {
                    string product = Path.GetFileName(productFolder);
                    string[] versionFolders = Directory.GetDirectories(productFolder);
                    foreach (var versionFolder in versionFolders) {
                        string versionNumber = Path.GetFileName(versionFolder);
                        if (versionNumber.StartsWith(NotUsedPrefix, StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        string key = AddOnProduct.MakeAddOnKey(AddOnType.AddonJSGlobal, domain, product);
                        AddOnProduct version = new AddOnProduct {
                            Type = AddOnType.AddonJSGlobal,
                            Domain = domain,
                            Product = product,
                            Url = YetaWFManager.PhysicalToUrl(versionFolder),
                        };
                        AddFileLists(version, null, versionFolder);
                        Products.Add(key, version);
                        Logging.AddLog("added {0} in {1}", version.AddonKey, versionFolder);
                    }
                }
            }
        }
        private static void RegisterTemplates(Package package, string asmFolder) {
            string[] templateFolders = Directory.GetDirectories(asmFolder);
            foreach (var folder in templateFolders) {
                string directoryName = Path.GetFileName(folder);
                RegisterTemplateAddon(package, folder, directoryName);
            }
        }

        private static void RegisterTemplateAddon(Package package, string folder, string templateName) {
            RegisterAnyAddon(AddOnType.Template, package, folder, templateName);
        }
        private static void RegisterRegularAddon(Package package, string folder, string addonName) {
            RegisterAnyAddon(AddOnType.AddonJS, package, folder, addonName);
        }
        private static void RegisterModuleAddon(Package package, string folder, string module) {
            RegisterAnyAddon(AddOnType.Module, package, folder, module);
        }
        private static void RegisterSkinAddon(Package package, string folder, string skin) {
            RegisterAnyAddon(AddOnType.Skin, package, folder, skin);
        }

        private static void RegisterAnyAddon(AddOnType type, Package package, string folder, string name) {
            string key = AddOnProduct.MakeAddOnKey(type, package, name);
            if (Products.ContainsKey(key))
                throw new InternalError("Key {0} already exists for {1}.{2}", key, package.Domain, package.Product);
            AddOnProduct version = new AddOnProduct {
                Type = type,
                Domain = package.Domain,
                Product = package.Product,
                Name = name,
                Url = YetaWFManager.PhysicalToUrl(folder),
            };
            AddFileLists(version, package, folder);
            Products.Add(key, version);
            Logging.AddLog("added {0} in {1}", version.AddonKey, folder);
        }

        private static void AddFileLists(AddOnProduct version, Package package, string folder) {
            string filePath;
            version.JsFiles = ReadFile(version, Path.Combine(folder, Globals.Addons_JSFileList), out filePath);
            version.JsPath = filePath;
            version.CssFiles = ReadFile(version, Path.Combine(folder, Globals.Addons_CSSFileList), out filePath);
            version.CssPath = filePath;
            version.SupportTypes = ReadSupportFile(version, package, folder);
            if (version.Type == AddOnType.Skin) {
                SkinAccess skinAccess = new SkinAccess();
                version.SkinInfo = skinAccess.ParseSkinFile(version.Domain, version.Product, Path.GetFileName(folder), folder);
            }
        }

        private static List<Type> ReadSupportFile(AddOnProduct version, Package package, string folder) {

            List<Type> types = new List<Type>();

            if (version.Type == AddOnType.Skin || version.Type == AddOnType.AddonJSGlobal) return types;
            if (package == null) throw new InternalError("Package required");

            // build a type name based on domain name and product name - if it exists, add it
            // domainName.Modules.productName.Addons class Info
            // load the assembly/type implementing logging
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
                } else if (version.Type == AddOnType.Module) {
                    typeName += ".Info";
                } else if (version.Type == AddOnType.AddonJS) {
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
            if (!File.Exists(file)) return types;
            List<string> lines = File.ReadLines(file).ToList<string>();
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

        private static List<string> ReadFile(AddOnProduct version, string file, out string filePath) {
            filePath = "";
            List<string> lines = new List<string>();
            if (File.Exists(file)) {
                Logging.AddLog("Found {0}", file);
                lines = File.ReadLines(file).ToList<string>();
                string path = (from l in lines where l.StartsWith("Folder ") select l.Trim()).FirstOrDefault();
                if (path != null) {
                    if (version.Type != AddOnType.AddonJSGlobal)
                        throw new InternalError("The Folder directive can only be used in a global addon");
                    path = path.Substring(6).Trim();
                    if (path.StartsWith("\\")) {
                        if (!path.EndsWith("\\")) path = path + "\\";
                    } else if (!path.EndsWith("/")) {
                        path = path + "/";
                    }
                    if (!string.IsNullOrWhiteSpace(path)) {
                        filePath = path;
                    }
                }
                lines = (from l in lines where !l.StartsWith("#") && !l.StartsWith("Folder ") && !string.IsNullOrWhiteSpace(l) select l.Trim()).ToList();
            }
            return lines;
        }
    }
}
