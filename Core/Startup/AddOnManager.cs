﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web;
#endif

namespace YetaWF.Core.Addons {

    public interface IAddOnSupport {
        void AddSupport(YetaWFManager manager);
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
        /// Add an addon (normal)
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="productName"></param>
        /// <param name="args"></param>
        /// <param name="name"></param>
        public void AddAddOn(string domainName, string productName, string name, params object[] args) {
            if (Manager.IsPostRequest) return;
            VersionManager.AddOnProduct version = VersionManager.FindAddOnVersion(domainName, productName, name);
            if (_AddedProducts.Contains(version)) return;
            _AddedProducts.Add(version);
            Manager.ScriptManager.AddAddOn(version, args);
            Manager.CssManager.AddAddOn(version, args);
        }

        /// <summary>
        /// Add an addon (global)
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="productName"></param>
        /// <param name="args"></param>
        public void AddAddOnGlobal(string domainName, string productName, params object[] args) {
            if (Manager.IsPostRequest) return;
            VersionManager.AddOnProduct version = VersionManager.FindAddOnGlobalVersion(domainName, productName);
            if (_AddedProducts.Contains(version)) return;
            _AddedProducts.Add(version);
            Manager.ScriptManager.AddAddOn(version, args);
            Manager.CssManager.AddAddOn(version, args);
        }

        public enum UrlType {
            Js = 0, // the location of the javascript files
            Css = 1, // the location of the css/scss/less files
            Base = 2, // the location of filelistJS/CSS.txt
        }

        public static string GetAddOnGlobalUrl(string domainName, string productName, UrlType type) {
            VersionManager.AddOnProduct version = VersionManager.FindAddOnGlobalVersion(domainName, productName);
            switch (type) {
                case UrlType.Js: return version.GetAddOnJsUrl();
                case UrlType.Css: return version.GetAddOnCssUrl();
                default:
                case UrlType.Base: return version.GetAddOnUrl();
            }
        }
        /// <summary>
        /// Add a template - ignores non-existent templates.
        /// </summary>
        /// <remarks>
        /// If the template name ends in a number, it could be a template with ending numeric variations (like Text20, Text40, Text80)
        /// which are all the same template. However, if we find an installed addon template that ends in the exact name (including number) we use that first.
        /// </remarks>
        public void AddTemplate(string domainName, string productName, string templateName) {
            if (Manager.IsPostRequest) return;
            string templateNameBasic = templateName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            VersionManager.AddOnProduct version;
            if (templateName != templateNameBasic) {
                // try template name including number first
                version = VersionManager.TryFindTemplateVersion(domainName, productName, templateName);
                if (version != null) {
                    if (_AddedProducts.Contains(version)) return;
                    _AddedProducts.Add(version);
                    Manager.ScriptManager.AddAddOn(version);
                    Manager.CssManager.AddAddOn(version);
                    return;
                }
            }
            version = VersionManager.TryFindTemplateVersion(domainName, productName, templateNameBasic);
            if (version != null) {
                if (_AddedProducts.Contains(version)) return;
                _AddedProducts.Add(version);
                Manager.ScriptManager.AddAddOn(version);
                Manager.CssManager.AddAddOn(version);
            }
        }

        /// <summary>
        /// Add a core template - ignores non-existent templates
        /// </summary>
        public void AddTemplate(string templateName) {
            AddTemplate(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Domain, YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Product, templateName);
        }

        /// <summary>
        /// Add a template given a uihint - ignores non-existent templates
        /// </summary>
        /// <param name="uiHintTemplate"></param>
        public void AddTemplateFromUIHint(string uiHintTemplate) {
            if (Manager.IsPostRequest) return;
            if (string.IsNullOrWhiteSpace(uiHintTemplate)) return;

            Manager.AddOnManager.CheckInvokedTemplate(uiHintTemplate);

            int firstIndex = uiHintTemplate.IndexOf("_");
            if (firstIndex < 0) {
                // standard template
                AddTemplate(uiHintTemplate);
            } else {
                // domain_product_name template
                string[] parts = uiHintTemplate.Split(new char[] { '_' }, 3);
                if (parts.Length != 3) throw new InternalError("Unexpected error");
                AddTemplate(parts[0], parts[1], parts[2]);
            }
        }

        /// <summary>
        /// Add a module - ignores non-existent modules
        /// </summary>
        /// <param name="module"></param>
        public void AddModule(ModuleDefinition module) {
            if (Manager.IsPostRequest) return;
            Package modPackage = Package.GetCurrentPackage(module);
            AddPackage(modPackage, new List<Package>());
        }
        private void AddPackage(Package modPackage, List<Package> packagesFound) {
            string domain = modPackage.Domain;
            string product = modPackage.Product;
            // Add the package
            if (!packagesFound.Contains(modPackage)) {
                packagesFound.Add(modPackage);
                VersionManager.AddOnProduct version = VersionManager.TryFindModuleVersion(domain, product);
                if (version == null || _AddedProducts.Contains(version)) return;
                _AddedProducts.Add(version);
                Manager.ScriptManager.AddAddOn(version);
                Manager.CssManager.AddAddOn(version);
                // Also add all packages this module requires
                List<string> packageNames = modPackage.GetRequiredPackages();
                foreach (var name in packageNames) {
                    Package package = Package.GetPackageFromPackageName(name);
                    AddPackage(package, packagesFound);
                }
            }
        }

        /// <summary>
        /// Add a skin
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <param name="args"></param>
        public void AddSkin(string skinCollection, params object[] args) {
            Manager.Verify_NotPostRequest();
            VersionManager.AddOnProduct version = VersionManager.FindSkinVersion(skinCollection);
            if (_AddedProducts.Contains(version)) return;
            _AddedProducts.Add(version);
            Manager.ScriptManager.AddAddOn(version, args);
            Manager.CssManager.AddAddOn(version, args);
        }

        /// <summary>
        /// Add a site's skin customizations
        /// </summary>
        /// <param name="skinCollection"></param>
        /// <param name="args"></param>
        public void AddSkinCustomization(string skinCollection, params object[] args) {
            Manager.Verify_NotPostRequest();
            string domainName, productName, skinName;
            VersionManager.AddOnProduct.GetSkinComponents(skinCollection, out domainName, out productName, out skinName);
            string url = string.Format("{0}/{1}/{2}/{3}/{4}/{5}/Custom.css", Globals.AddOnsCustomUrl, Manager.CurrentSite.SiteDomain, domainName, productName, Globals.Addons_SkinsDirectoryName, skinName);
            if (File.Exists(YetaWFManager.UrlToPhysical(url)))
                Manager.CssManager.AddFile(true, url);
            url = string.Format("{0}/{1}/{2}/{3}/{4}/{5}/Custom.scss", Globals.AddOnsCustomUrl, Manager.CurrentSite.SiteDomain, domainName, productName, Globals.Addons_SkinsDirectoryName, skinName);
            if (File.Exists(YetaWFManager.UrlToPhysical(url)))
                Manager.CssManager.AddFile(true, url);
        }

        public void AddStandardAddOns() {
            AddAddOnGlobal("jquery.com", "jquery");
            AddAddOnGlobal("jqueryui.com", "jqueryui");
            AddAddOnGlobal("medialize.github.io", "URI.js");// for client-side Url manipulation
            AddAddOnGlobal("necolas.github.io", "normalize");
        }

        public void AddSkinBasedAddOns() {
            // Find the jquery theme
            SkinAccess skinAccess = new SkinAccess();
            string skin = Manager.CurrentPage.jQueryUISkin;
            if (string.IsNullOrWhiteSpace(skin))
                skin = Manager.CurrentSite.jQueryUISkin;
            string themeFolder = skinAccess.FindJQueryUISkin(skin);
            AddAddOnGlobal("jqueryui.com", "jqueryui-themes", themeFolder);
            //if (Manager.UsingBootstrap)
            //    AddAddOnGlobal("github.com.arschmitz", "jqueryui-bootstrap-adapter");

            // Find Kendo UI theme
            skin = Manager.CurrentPage.KendoUISkin;
            if (string.IsNullOrWhiteSpace(skin))
                skin = Manager.CurrentSite.KendoUISkin;
            string internalTheme = skinAccess.FindKendoUISkin(skin);
            Manager.ScriptManager.AddAddOn(VersionManager.KendoAddon, internalTheme);
            Manager.CssManager.AddAddOn(VersionManager.KendoAddon, internalTheme);
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

        /// <summary>
        /// Read a file
        /// </summary>
        public HtmlString GetFile(string path, object replacements = null) {
            string file = "";
            try {
                file = File.ReadAllText(YetaWFManager.UrlToPhysical(path));
            } catch (System.Exception) { }
            QueryHelper query = QueryHelper.FromAnonymousObject(replacements);
            foreach (QueryHelper.Entry entry in query.Entries)
                file = file.Replace("$" + entry.Key + "$", entry.Value.ToString());
            return new HtmlString(file);
        }
    }
}