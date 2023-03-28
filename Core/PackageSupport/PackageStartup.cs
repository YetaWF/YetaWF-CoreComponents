/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {

    public class PackageStartup : IInitializeApplicationStartup {
        /// <summary>
        /// Called when any node of a (single- or multi-instance) site is starting up.
        /// </summary>
        public async Task InitializeApplicationStartupAsync() {
            if (!YetaWFManager.IsBatchMode && !YetaWFManager.IsServiceMode)
                await Package.RegisterAllAddOnsAsync();
        }
    }

    public partial class Package {

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

        public class AddOnProduct {

            public class UsesInfo {
                public string PackageName { get; set; } = null!;
                public string AddonName { get; set; } = null!;
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
            public string Domain { get; set; } = null!;
            public string Product { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string Url { get; set; } = null!;

            public List<string> JsFiles { get; set; }
            public string JsPath { get; set; } = null!;
            public List<UsesInfo> JsUses { get; set; } = null!;
            public List<string> CssFiles { get; set; }
            public string CssPath { get; set; } = null!;
            public List<UsesInfo> CssUses { get; set; } = null!;
            public List<Type> SupportTypes { get; set; }
            public SkinCollectionInfo SkinInfo { get; set; } = null!;

            public string? SVGFolder { get; internal set; }
            public Dictionary<string, string>? SVGs { get; internal set; }
            internal string? GetSVG(string name) { return SVGs != null ? (SVGs.TryGetValue(name, out string? html) ? html : null) : null; }

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

            internal static string MakeAddOnKey(AddOnType type, string area, string? name = null) {
                if (type != AddOnType.AddonNamed)
                    if (name == null)
                        throw new InternalError("A name is required");
                return $"{GetPrefix(type)}{area}{(name != null ? $"+{name}" : "")}".ToLower();
            }
            internal static string MakeAddOnKey(AddOnType type, Package package, string? name = null) {
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

            string rootFolder = YetaWFManager.RootFolderWebProject;
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(templateFolder)) {
                List<string> folders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(templateFolder, "*.*");
                foreach (string folder in folders) {
                    if (Path.GetFileName(folder) != Globals.DataFolder)
                        await FileSystem.FileSystemProvider.DeleteDirectoryAsync(folder);
                }
            }

            Logging.AddLog("Searching assemblies");

            Products = new Dictionary<string, AddOnProduct>();

            // visit all known assemblies and see if there is a matching entry in the Addons folder
            List<Package> packages = Package.GetAvailablePackages();
            foreach (Package package in packages) {
                if (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage || package.IsDataProviderPackage) {
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
            string rootFolder = YetaWFManager.RootFolderWebProject;
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(templateFolder)) {
                List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(folder, "*.txt");
                foreach (string file in files) {
                    string newFile = Path.Combine(templateFolder, Path.GetFileName(file));
                    await FileSystem.FileSystemProvider.CopyFileAsync(file, newFile);
                }
            }
        }
        private static async Task CopySiteUpgradesAsync(Package package, string sourceFolder) {

            string rootFolder = YetaWFManager.RootFolderWebProject;
            string templateFolder = Path.Combine(rootFolder, Globals.SiteTemplates);
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(templateFolder)) {
                List<string> folders = await FileSystem.FileSystemProvider.GetDirectoriesAsync(sourceFolder, "*.*");
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
                // there are some stray folders (when templates are renamed) that may have *.min.css/js without filelistJS/CSS.txt files
                // ignore these
                if ((await FileSystem.FileSystemProvider.GetFilesAsync(folder, "*.txt")).Count == 0) {
#if UPGRADE // enable when upgrading and there are a lot of dead folders.
                    await FileSystem.FileSystemProvider.DeleteDirectoryAsync(folder);
#else
                    throw new InternalError($"Remove _Template folder with unused files: {folder}");
#endif
                } else {
                    string directoryName = Path.GetFileName(folder);
                    await RegisterTemplateAddonAsync(package, folder, directoryName);
                }
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
            AddOnProduct addon = new AddOnProduct {
                Type = type,
                Domain = package.Domain,
                Product = package.Product,
                Name = name,
                Url = Utility.PhysicalToUrl(folder),
            };
            await AddFileListsAsync(addon, package, folder);
            Products.Add(key, addon);

            if (type == AddOnType.Package || type == AddOnType.Skin)
                await LoadSVGsAsync(addon, folder);                

            Logging.AddLog("added {0} in {1}", addon.AddonKey, folder);
        }

        private static async Task LoadSVGsAsync(AddOnProduct addon, string folder) {
            string path = Path.Combine(folder, SkinAccess.SVGFolder);
            if (await FileSystem.FileSystemProvider.DirectoryExistsAsync(path)) {
                addon.SVGFolder = path;
                addon.SVGs = await SkinAccess.GetSVGsAsync(path);
            }
        }

        private static async Task AddFileListsAsync(AddOnProduct addon, Package package, string folder) {
            ReadFileInfo info = await ReadFileAsync(addon, Path.Combine(folder, Globals.Addons_JSFileList));
            addon.JsFiles = info.Lines;
            addon.JsPath = info.Files;
            addon.JsUses = info.Uses;
            info = await ReadFileAsync(addon, Path.Combine(folder, Globals.Addons_CSSFileList));
            addon.CssFiles = info.Lines;
            addon.CssPath = info.Files;
            addon.CssUses = info.Uses;
            addon.SupportTypes = await ReadSupportFileAsync(addon, package, folder);
            if (addon.Type == AddOnType.Skin) {
                SkinAccess skinAccess = new SkinAccess();
                addon.SkinInfo = await skinAccess.LoadSkinAsync(package, addon.Domain, addon.Product, Path.GetFileName(folder), folder);
            }
        }

        private static async Task<List<Type>> ReadSupportFileAsync(AddOnProduct addon, Package package, string folder) {

            List<Type> types = new List<Type>();

            if (addon.Type == AddOnType.Skin) return types;
            if (package == null) throw new InternalError("Package required");

            // build a type name based on domain name and product name - if it exists, add it
            // domainName.Modules.productName.Addons class Info
            // load the assembly/type implementing addon support
            Type? dynType = null;
            try {
                Assembly asm = package.PackageAssembly;
                string typeName;
                if (package.IsCorePackage)
                    typeName = addon.Domain + "." + addon.Product + ".Addons";
                else
                    typeName = addon.Domain + ".Modules." + addon.Product + ".Addons";
                if (addon.Type == AddOnType.Template) {
                    string templateName = Path.GetFileName(folder);
                    typeName += $".Templates.{templateName}";
                } else if (addon.Type == AddOnType.Package) {
                    typeName += ".Info";
                } else if (addon.Type == AddOnType.AddonNamed) {
                    string name = Path.GetFileName(folder);
                    typeName += "." + name;
                } else
                    throw new InternalError("Unexpected addon type {0} for {1}", addon.Type, package.Name);
                if (typeName != null)
                    dynType = asm.GetType(typeName);
            } catch (Exception) { }
            if (dynType != null) {
                types.Add(dynType);
                Logging.AddLog("Addon support dynamically added for {0}", dynType.FullName!);
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
                    Type? type = Type.GetType(line);
                    if (type == null)
                        throw new InternalError("Type {0} found in file {1} doesn't exist", line, file);
                    object? o = Activator.CreateInstance(type);
                    if (o == null)
                        throw new InternalError("Type {0} found in file {1} can't be created", line, file);
                    IAddOnSupport? addSupport = o as IAddOnSupport;
                    if (addSupport == null)
                        throw new InternalError("No IAddOnSupport interface found on type {0} found in file {1}", line, file);
                    if (type == dynType)
                        Logging.AddErrorLog("Dynamic type {0} is also added explicitly", type.FullName!);
                    //  throw new InternalError("Dynamic type {0} is also added explicitly", type.FullName);
                    types.Add(type);
                    Logging.AddLog("Addon support explicitly added for {0}", type.FullName!);
                }
            }
            return types;
        }

        private class ReadFileInfo {
            public string Files { get; set; } = null!;
            public List<string> Lines { get; set; } = null!;
            public List<AddOnProduct.UsesInfo> Uses { get; set; } = null!;
        }

        private static async Task<ReadFileInfo> ReadFileAsync(AddOnProduct addon, string file) {
            string filePath = "";
            List<string> lines = new List<string>();
            List<AddOnProduct.UsesInfo> uses = new List<AddOnProduct.UsesInfo>();
            if (await FileSystem.FileSystemProvider.FileExistsAsync(file)) {

                Logging.AddLog("Found {0}", file);

                lines = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);

                // remove comments
                lines = (from l in lines where !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l) select l.Trim()).ToList();
                lines = (from l in lines where !l.StartsWith("MVC5 ") select l).ToList();
                lines = (from l in lines select (l.StartsWith("MVC6 ") ? l.Substring(4) : l).Trim()).ToList();
                // Find a Folder directive (1 only, others are ignored)
                string? path = (from l in lines where l.StartsWith("Folder ") select l.Trim()).FirstOrDefault();
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
