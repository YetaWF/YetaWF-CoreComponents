﻿/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.IO;
using YetaWF.Core.Components;
#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web;
#endif

namespace YetaWF.Core.Addons {

    public interface IAddOnSupport {
        Task AddSupportAsync(YetaWFManager manager);
    }

    public class AddOnManager {

        public AddOnManager(YetaWFManager manager) { Manager = manager; }
        protected YetaWFManager Manager { get; private set; }

        public class Module {
            public string InvokingCss { get; set; }
            public bool AllowInPopup { get; set; }
            public bool AllowInAjax { get; set; }
            public Type ModuleType { get; set; }
            public Guid ModuleGuid { get; set; }
            public List<string> Templates { get; set; }

            public Module() {
                Templates = new List<string>();
            }
        }

        private List<VersionManager.AddOnProduct> _AddedProducts = new List<VersionManager.AddOnProduct>();
        private List<Module> _AddedInvokedCssModules = new List<Module>();

        private static List<Module> UniqueInvokedCssModules = new List<Module>();

        /// <summary>
        /// Add a named addon (normal).
        /// </summary>
        /// <param name="domainName">The package domain name.</param>
        /// <param name="productName">The package product name.</param>
        /// <param name="args">Any optional arguments supported by the addon.</param>
        /// <param name="name">The name of the addon.</param>
        /// <remarks>Named addons are located in the package folder ./Addons/_Addons/name.
        /// Will fail if the addon doesn't exist.</remarks>
        public async Task AddAddOnNamedAsync(string areaName, string name, params object[] args) {
            if (Manager.IsPostRequest) return;
            VersionManager.AddOnProduct version = VersionManager.FindAddOnNamedVersion(areaName, name);
            if (_AddedProducts.Contains(version)) return;
            _AddedProducts.Add(version);
            await Manager.ScriptManager.AddAddOnAsync(version, args);
            await Manager.CssManager.AddAddOnAsync(version, args);
        }
        internal async Task AddAddOnNamedJavaScriptAsync(string areaName, string name, params object[] args) {
            if (Manager.IsPostRequest) return;
            VersionManager.AddOnProduct version = VersionManager.FindAddOnNamedVersion(areaName, name);
            if (_AddedProducts.Contains(version)) return;
            //_AddedProducts.Add(version); // do not add, only partial, script manager will catch duplicates
            await Manager.ScriptManager.AddAddOnAsync(version, args);
        }
        internal async Task AddAddOnNamedCssAsync(string areaName, string name, params object[] args) {
            if (Manager.IsPostRequest) return;
            VersionManager.AddOnProduct version = VersionManager.FindAddOnNamedVersion(areaName, name);
            if (_AddedProducts.Contains(version)) return;
            // _AddedProducts.Add(version); // do not add, only partial, css manager will catch duplicates
            await Manager.CssManager.AddAddOnAsync(version, args);
        }
        /// <summary>
        /// Returns the Url of a named addon.
        /// </summary>
        /// <param name="domainName">The domain name of the addon owner.</param>
        /// <param name="productName">The product name of the addon.</param>
        /// <param name="name">The name of the addon.</param>
        /// <returns></returns>
        public string GetAddOnNamedUrl(string areaName, string name) {
            VersionManager.AddOnProduct version = VersionManager.FindAddOnNamedVersion(areaName, name);
            return version.GetAddOnUrl();
        }
        /// <summary>
        /// Add a named addon (normal) if it exists.
        /// </summary>
        /// <param name="domainName">The package domain name.</param>
        /// <param name="productName">The package product name.</param>
        /// <param name="args">Any optional arguments supported by the addon.</param>
        /// <param name="name">The name of the addon.</param>
        /// <remarks>Named addons are located in the package folder ./Addons/_Addons/name.</remarks>
        public async Task TryAddAddOnNamedAsync(string areaName, string name, params object[] args) {
            if (Manager.IsPostRequest) return;
            VersionManager.AddOnProduct version = VersionManager.TryFindAddOnNamedVersion(areaName, name);
            if (version == null) return;
            if (_AddedProducts.Contains(version)) return;
            _AddedProducts.Add(version);
            await Manager.ScriptManager.AddAddOnAsync(version, args);
            await Manager.CssManager.AddAddOnAsync(version, args);
        }

        public enum UrlType {
            Js = 0, // the location of the javascript files
            Css = 1, // the location of the css/scss/less files
            Base = 2, // the location of filelistJS/CSS.txt
        }
        /// <summary>
        /// Add a template - ignores non-existent templates.
        /// </summary>
        /// <remarks>
        /// If the template name ends in a number, it could be a template with ending numeric variations (like Text20, Text40, Text80)
        /// which are all the same template. However, if we find an installed addon template that ends in the exact name (including number) we use that first.
        /// </remarks>
        public async Task AddTemplateAsync(string areaName, string templateName) {
            if (Manager.IsPostRequest) return;
            string templateNameBasic = templateName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            VersionManager.AddOnProduct version;
            if (templateName != templateNameBasic) {
                // try template name including number first
                version = VersionManager.TryFindTemplateVersion(areaName, templateName);
                if (version != null) {
                    if (_AddedProducts.Contains(version)) return;
                    _AddedProducts.Add(version);
                    await Manager.ScriptManager.AddAddOnAsync(version);
                    await Manager.CssManager.AddAddOnAsync(version);
                    return;
                }
            }
            version = VersionManager.TryFindTemplateVersion(areaName, templateNameBasic);
            if (version != null) {
                if (_AddedProducts.Contains(version)) return;
                _AddedProducts.Add(version);
                await Manager.ScriptManager.AddAddOnAsync(version);
                await Manager.CssManager.AddAddOnAsync(version);
            }
        }

        /// <summary>
        /// Add a template given a uihint - ignores non-existent templates
        /// </summary>
        /// <param name="uiHintTemplate"></param>
        public async Task AddTemplateFromUIHintAsync(string uiHintTemplate) {
            if (Manager.IsPostRequest) return;
            if (string.IsNullOrWhiteSpace(uiHintTemplate)) return;

            Manager.AddOnManager.CheckInvokedTemplate(uiHintTemplate);

            int firstIndex = uiHintTemplate.IndexOf("_");
            if (firstIndex < 0) {
                // standard template
                await YetaWFCoreRendering.AddTemplateAsync(uiHintTemplate);
            } else {
                // domain_product_name template
                string[] parts = uiHintTemplate.Split(new char[] { '_' }, 3);
                if (parts.Length != 3) throw new InternalError($"Template name invalid - {uiHintTemplate}");
                await AddTemplateAsync($"{parts[0]}_{parts[1]}", parts[2]);
            }
        }

        /// <summary>
        /// Add a module - ignores non-existent modules.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <remarks>Adds the the associated Javascript/Css for the module's package and all required packages.</remarks>
        public async Task AddModuleAsync(ModuleDefinition module) {
            if (Manager.IsPostRequest) return;
            Package modPackage = Package.GetCurrentPackage(module);
            await AddPackageAsync(modPackage, new List<Package>());
        }
        /// <summary>
        /// Add a package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="packagesFound">Returns a list of all added packages, including dependent packages.</param>
        /// <remarks>Adds the the associated Javascript/Css for the module's package and all required packages.</remarks>
        /// <returns></returns>
        public async Task AddPackageAsync(Package package, List<Package> packagesFound) {
            // Add the package
            if (!packagesFound.Contains(package)) {
                packagesFound.Add(package);
                VersionManager.AddOnProduct version = VersionManager.TryFindPackageVersion(package.AreaName);
                if (version == null || _AddedProducts.Contains(version)) return;
                _AddedProducts.Add(version);
                await Manager.ScriptManager.AddAddOnAsync(version);
                await Manager.CssManager.AddAddOnAsync(version);
                // Also add all packages this module requires
                List<string> packageNames = package.GetRequiredPackages();
                foreach (var name in packageNames) {
                    Package p = Package.GetPackageFromPackageName(name);
                    await AddPackageAsync(p, packagesFound);
                }
            }
        }

        /// <summary>
        /// Add a skin
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <param name="args"></param>
        public async Task AddSkinAsync(string skinCollection, params object[] args) {
            Manager.Verify_NotPostRequest();
            VersionManager.AddOnProduct version = VersionManager.FindSkinVersion(skinCollection);
            if (_AddedProducts.Contains(version)) return;
            _AddedProducts.Add(version);
            await Manager.ScriptManager.AddAddOnAsync(version, args);
            await Manager.CssManager.AddAddOnAsync(version, args);
        }

        /// <summary>
        /// Add a site's skin customizations
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <param name="args"></param>
        public async Task AddSkinCustomizationAsync(string skinCollection) {
            Manager.Verify_NotPostRequest();

            // check cache
            string url;
            if (CustomizationCache.TryGetValue(skinCollection, out url)) {
                if (string.IsNullOrWhiteSpace(url))
                    return; // no customization
                await Manager.CssManager.AddFileAsync(true, url);
                return;
            }

            // try to find customization
            url = string.Format("{0}/{1}/Custom.scss", Globals.AddOnsCustomUrl, Manager.CurrentSite.SiteDomain);
            if (await FileSystem.FileSystemProvider.FileExistsAsync(YetaWFManager.UrlToPhysical(url))) {
                AddCache(skinCollection, url);
                await Manager.CssManager.AddFileAsync(true, url);
            } else {
                url = string.Format("{0}/{1}/Custom.css", Globals.AddOnsCustomUrl, Manager.CurrentSite.SiteDomain);
                if (await FileSystem.FileSystemProvider.FileExistsAsync(YetaWFManager.UrlToPhysical(url))) {
                    AddCache(skinCollection, url);
                    await Manager.CssManager.AddFileAsync(true, url);
                }
            }

            string domainName, productName, skinName;
            VersionManager.AddOnProduct.GetSkinComponents(skinCollection, out domainName, out productName, out skinName);
            url = string.Format("{0}/{1}/{2}/{3}/{4}/{5}/Custom.scss", Globals.AddOnsCustomUrl, Manager.CurrentSite.SiteDomain, domainName, productName, Globals.Addons_SkinsDirectoryName, skinName);
            if (await FileSystem.FileSystemProvider.FileExistsAsync(YetaWFManager.UrlToPhysical(url))) {
                AddCache(skinCollection, url);
                await Manager.CssManager.AddFileAsync(true, url);
            } else {
                url = string.Format("{0}/{1}/{2}/{3}/{4}/{5}/Custom.css", Globals.AddOnsCustomUrl, Manager.CurrentSite.SiteDomain, domainName, productName, Globals.Addons_SkinsDirectoryName, skinName);
                if (await FileSystem.FileSystemProvider.FileExistsAsync(YetaWFManager.UrlToPhysical(url))) {
                    AddCache(skinCollection, url);
                    await Manager.CssManager.AddFileAsync(true, url);
                }
            }
            AddCache(skinCollection, "");// mark cache as not found
        }
        // Caching for skin customizations
        private static Dictionary<string, string> CustomizationCache = new Dictionary<string, string>();

        private static void AddCache(string skinCollection, string url) {
            if (!YetaWFManager.Manager.Deployed) return;
            try { // could fail if already added
                CustomizationCache.Add(skinCollection, url);
            } catch (Exception) { }
        }

        public void AddUniqueInvokedCssModule(Type modType, Guid guid, List<string> templates, string invokingCss, bool AllowInPopup, bool AllowInAjax) {
            UniqueInvokedCssModules.Add(new Module { AllowInPopup = AllowInPopup, AllowInAjax = AllowInAjax, Templates = templates ?? new List<string>(), InvokingCss = invokingCss, ModuleType = modType, ModuleGuid = guid });
        }
        public List<Module> GetUniqueInvokedCssModules() {
            return UniqueInvokedCssModules;
        }

        public string CheckInvokedCssModule(string css) {
            if (string.IsNullOrWhiteSpace(css)) return css;
            string[] classes = css.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string cls in classes) {
                Module mod = (from m in UniqueInvokedCssModules where m.InvokingCss == cls select m).FirstOrDefault();
                AddInvokedCssModule(mod);
            }
            return css;
        }
        public void CheckInvokedTemplate(string template) {
            if (string.IsNullOrWhiteSpace(template)) return;
            Module mod = (from m in UniqueInvokedCssModules where m.Templates.Contains(template) select m).FirstOrDefault();
            AddInvokedCssModule(mod);
        }
        public void AddExplicitlyInvokedModules(SerializableList<ModuleDefinition.ReferencedModule> list) {
            if (list == null) return;
            foreach (ModuleDefinition.ReferencedModule l in list) {
                Module mod = (from m in UniqueInvokedCssModules where m.ModuleGuid == l.ModuleGuid select m).FirstOrDefault();
                AddInvokedCssModule(mod);
            }
        }
        internal void CheckInvokedModule(ModuleDefinition dataMod) {
            Module mod = (from m in UniqueInvokedCssModules where m.ModuleGuid == dataMod.ModuleGuid select m).FirstOrDefault();
            AddInvokedCssModule(mod);
        }

        internal void AddInvokedCssModule(Module mod) {
            if (mod != null) {
                if (!_AddedInvokedCssModules.Contains(mod))
                    _AddedInvokedCssModules.Add(mod);
            }
        }
        internal List<Module> GetAddedUniqueInvokedCssModules() {
            // make a copy in case the invoked modules try to register additional modules (which are then ignored)
            return (from a in _AddedInvokedCssModules select a).ToList();
        }
        public bool HasModuleReference(Guid guid) {
            return (from m in _AddedInvokedCssModules where guid == m.ModuleGuid select m).FirstOrDefault() != null;
        }

        /// <summary>
        /// Read a file
        /// </summary>
        public async Task<string> GetFileAsync(string path, object replacements = null) {
            string file = "";
            try {
                file = await FileSystem.FileSystemProvider.ReadAllTextAsync(YetaWFManager.UrlToPhysical(path));
            } catch (System.Exception) { }
            QueryHelper query = QueryHelper.FromAnonymousObject(replacements);
            foreach (QueryHelper.Entry entry in query.Entries)
                file = file.Replace("$" + entry.Key + "$", entry.Value.ToString());
            return file;
        }
    }
}