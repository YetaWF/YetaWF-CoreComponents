/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Search;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.IO;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using YetaWF.Core.Controllers.Shared;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
#endif

namespace YetaWF.Core.Modules {

    // Interface to derived module type dataprovider
    public interface IModuleDefinitionIO : IDisposable {
        Task SaveModuleDefinitionAsync(ModuleDefinition mod);
        Task<ModuleDefinition> LoadModuleDefinitionAsync(Guid key);
    }

    public partial class ModuleDefinition {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition), name, defaultValue, parms); }

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        protected static bool HaveManager { get { return YetaWFManager.HaveManager; } }

        // MODULE INFO
        // MODULE INFO
        // MODULE INFO

        [Category("About")]
        [Description("The internal, permanent module name")]
        [Caption("Permanent Module Name")]
        public string PermanentModuleName {
            get {
                GetModuleInfo();
                return string.Format(Globals.PermanentModuleNameFormat, Domain, ClassName);
            }
        }

        [Category("About")]
        [Description("The displayable module name")]
        [Caption("Module Display Name")]
        public string ModuleDisplayName {
            get {
                GetModuleInfo();
                return ModuleName;
            }
        }
        [Category("About")]
        [Description("The internal company name of the module's publisher")]
        [Caption("Company Name")]
        public string CompanyName {
            get {
                GetModuleInfo();
                return _CompanyName;
            }
        }
        [Category("About")]
        [Description("The displayable company name of the module's publisher")]
        [Caption("Company Display Name")]
        public string CompanyDisplayName {
            get {
                GetModuleInfo();
                return _CompanyDisplayName;
            }
        }
        [Category("About")]
        [Description("The domain name of the product or company publishing the module")]
        [Caption("Domain")]
        public string Domain {
            get {
                GetModuleInfo();
                return _Domain;
            }
        }
        [Category("About")]
        [Description("The MVC area name of the module")]
        [Caption("Area")]
        public string Area {
            get {
                GetModuleInfo();
                return _Area;
            }
        }
        [Category("About")]
        [Description("The module's product name")]
        [Caption("Product")]
        public string Product {
            get {
                GetModuleInfo();
                return _Product;
            }
        }
        [Category("About")]
        [Description("The module version")]
        [Caption("Version")]
        public string Version {
            get {
                GetModuleInfo();
                return _Version;
            }
        }

        [Category("About")]
        [Description("The module's class name")]
        [Caption("Class Name")]
        public string ClassName {
            get {
                if (string.IsNullOrEmpty(_ClassName)) {
                    _ClassName = GetType().Name;
                }
                return _ClassName;
            }
        }
        private string _ClassName { get; set; }

        [Category("About")]
        [Description("The module's full class name")]
        [Caption("Class Name (Full)")]
        public string FullClassName {
            get {
                return GetType().FullName;
            }
        }

        [Category("About")]
        [Description("The module name")]
        [Caption("Module Name")]
        public string ModuleName {
            get {
                GetModuleInfo();
                return _ModuleName;
            }
        }

        private const string MODULE_NAMESPACE = "(xcompanyx.Modules.xproductx.Modules)";

        private void GetModuleInfo() {
            if (string.IsNullOrEmpty(_ModuleName)) {

                Type type = GetType();
                Package package = Package.GetPackageFromAssembly(type.Assembly);
                string ns = type.Namespace;

                if (type == typeof(ModuleDefinition)) {
                    // we're creating a ModuleDefinition - this is done while serializing
                    _CompanyName = "YetaWF";
                    _Product = "YetaWF";
                    _ModuleName = "(n/a)";
                    _Area = "(n/a)";
                    _CompanyDisplayName = "YetaWF";
                    _Domain = "YetaWF.com";
                    _Version = "(n/a)";
                } else {
                    string[] s = ns.Split(new char[] { '.' }, 4);
                    if (s.Length != 4)
                        throw new InternalError("Module namespace '{0}' must have 4 components - {1}", ns, MODULE_NAMESPACE);
                    _CompanyName = s[0];
                    if (s[1] != "Modules")
                        throw new InternalError("Module namespace '{0}' must have 'Modules' as second component", ns);
                    if (s[2] != package.Product)
                        throw new InternalError("Module namespace '{0}' must have the product name as third component", ns);
                    _Product = s[2];
                    if (s[3] != "Modules")
                        throw new InternalError("Module namespace '{0}' must have 'Modules' as fourth component", ns);

                    if (!ClassName.EndsWith("Module"))
                        throw new InternalError("Module {0} class name doesn't end in ...Module");
                    _ModuleName = ClassName.Substring(0, ClassName.Length - "Module".Length);

                    _Area = package.AreaName;
                    _CompanyDisplayName = package.CompanyDisplayName;
                    _Domain = package.Domain;
                    _Version = package.Version;
                }
            }
        }
        private string _Product { get; set; }
        private string _Area { get; set; }
        private string _CompanyName { get; set; }
        private string _Domain { get; set; }
        private string _ModuleName { get; set; }
        private string _Version { get; set; }
        private string _CompanyDisplayName { get; set; }

        [Category("Variables")]
        [Description("Displays whether the module is a unique module")]
        [Caption("IsModuleUnique")]
        public bool IsModuleUnique {
            get {
                UniqueModuleAttribute attr = (UniqueModuleAttribute) Attribute.GetCustomAttribute(GetType(), typeof(UniqueModuleAttribute));
                if (attr == null) return false;
                return attr.Value == UniqueModuleStyle.UniqueOnly;
            }
        }

        // MODULE ACTION/CONTROLLER/AREA
        // MODULE ACTION/CONTROLLER/AREA
        // MODULE ACTION/CONTROLLER/AREA

        //[Description("The MVC action invoking this module")]
        //[Caption("Action")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations"), Category("Variables")]
        public string Action {
            get {
                if (string.IsNullOrEmpty(_Action)) {
                    string action = ClassName;
                    if (!action.EndsWith(Globals.ModuleClassSuffix)) {
                        if (GetType() == typeof(ModuleDefinition)) // don't throw an error for the base class
                            return null;
                        throw new InternalError("Module {0} is using an invalid class name - should end in \"...{1}\".", action, Globals.ModuleClassSuffix);
                    }
                    _Action = action.Substring(0, action.Length - Globals.ModuleClassSuffix.Length); // remove trailing Module
                }
                return _Action;
            }
        }
        private string _Action { get; set; }

        //[Category("Variables")]
        //[Description("The MVC controller invoking this module")]
        //[Caption("Controller")]
        public string Controller {
            get {
                return GetType().Name;
            }
        }

        // FIND
        // FIND
        // FIND

        /// <summary>
        /// Find a designed module given a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Module or null if not found</returns>
        public static async Task<ModuleDefinition> FindDesignedModuleAsync(string url) {
            Guid guid = GetGuidFromUrl(url);
            if (guid == Guid.Empty) return null;
            try {
                return await LoadAsync(guid, AllowNone: true);
            } catch (Exception) {
                return null;
            }
        }
        private static Guid GetGuidFromUrl(string url) {
            Guid moduleGuid = Guid.Empty;
            url = url.Trim().ToLower();
            if (url.StartsWith(Globals.ModuleUrl.ToLower())) {
                url = url.Substring(Globals.ModuleUrl.Length);
                if (!Guid.TryParse(url, out moduleGuid))
                    return Guid.Empty;
            }
            return moduleGuid;
        }

        // LOAD/SAVE
        // LOAD/SAVE
        // LOAD/SAVE

        [Category("Variables")]
        [Caption("Has Settings")]
        [Description("Defines whether the module has settings that can be edited and saved")]
        [DontSave]
        public virtual bool ModuleHasSettings { get { return true; } }

        // this must be provided by a dataprovider during app startup (this loads module information (including derived types))
        [DontSave]
        public static Func<Guid, Task<ModuleDefinition>> LoadModuleDefinitionAsync { get; set; }
        [DontSave]
        public static Func<ModuleDefinition, IModuleDefinitionIO, Task> SaveModuleDefinitionAsync { get; set; }
        [DontSave]
        public static Func<Guid, Task<bool>> RemoveModuleDefinitionAsync { get; set; }
        [DontSave]
        public static Func<Guid, Task<ILockObject>> LockModuleAsync { get; set; }
        [DontSave]
        public static Func<ModuleBrowseInfo, Task> GetModulesAsync { get; set; }
        public class ModuleBrowseInfo {
            public int Skip { get; set; }
            public int Take { get; set; }
            public List<DataProviderSortInfo> Sort { get; set; }
            public List<DataProviderFilterInfo> Filters { get; set; }
            // return info
            public int Total { get; set; }
            public List<ModuleDefinition> Modules { get; set; }
        }

        // this is provided by a specific derived module type - its dataprovider reads/writes specific module types
        // Must be disposed after use
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        private IModuleDefinitionIO DataProvider {
            get {
                if (_dataProvider == null)
                    _dataProvider = GetDataProvider();
                if (_dataProvider == null)
                    throw new InternalError("Module {0} doesn't have a data provider", GetType().FullName);
                return _dataProvider;
            }
        }
        private IModuleDefinitionIO _dataProvider;

        // Must be disposed after use
        public virtual IModuleDefinitionIO GetDataProvider() {
            throw new InternalError("Module {0} doesn't have a data provider", GetType().FullName);
        }

        /// <summary>
        /// Loads a module's definition.
        /// This loads unique and non-unique, designed and installed modules, as long as the guid exists.
        /// Modules can always be loaded even if they haven't been saved yet, as long as the guid exists.
        /// If a perm guid is used for a non-unique module a new TEMPORARY module is created
        /// </summary>
        public static async Task<ModuleDefinition> LoadAsync(Guid moduleGuid, bool AllowNone = false) {
            // load it as an already saved module
            ModuleDefinition mod = null;
            try {
                mod = await LoadModuleDefinitionAsync(moduleGuid);
            } catch (Exception) {
                mod = null;
                if (!AllowNone)
                    throw;
            }
            if (mod == null) {
                // if it hasn't been saved yet, check if this is a permanent module guid for a unique module
                Type type = InstalledModules.TryFindModule(moduleGuid);
                if (type == null) {
                    if (AllowNone)
                        return null;
                    throw new InternalError("Designed module {0} not found", moduleGuid);
                }
                // this can be a unique or nonunique module
                mod = ModuleDefinition.Create(type);
                mod.ModuleGuid = moduleGuid;
            }
            mod.Temporary = false;
            return mod;
        }

        /// <summary>
        /// Find an installed module given a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<ModuleDefinition> LoadByUrlAsync(string url) {
            Guid guid = GetGuidFromUrl(url);
            if (guid == Guid.Empty) return null;
            return await ModuleDefinition.LoadAsync(guid, AllowNone: true);
        }

        /// <summary>
        /// Saves a module definition.
        /// This saves unique and non-unique, designed and installed modules
        /// </summary>
        public async Task SaveAsync() {
            if (Temporary) throw new InternalError("Temporary modules cannot be saved");
            await SaveModuleDefinitionAsync(this, DataProvider);
            List<PageDefinition> pages = await PageDefinition.GetPagesFromModuleAsync(ModuleGuid);
            await YetaWFManager.Manager.StaticPageManager.RemovePagesAsync(pages);
        }
        // Used to update properties before a module is saved
        public virtual Task ModuleSavingAsync() { return Task.CompletedTask; }
        // Used to act before a module is removed
        public virtual Task ModuleRemovingAsync() { return Task.CompletedTask; }

        /// <summary>
        /// Creates a new designed module. Remember to set the ModuleGuid property after creating the module.
        /// </summary>
        public static ModuleDefinition CreateNewDesignedModule(Guid permanentGuid, string name, MultiString title) {
            Type type = InstalledModules.TryFindModule(permanentGuid);
            if (type == null)
                throw new InternalError("Guid {0} is not an installed module", permanentGuid);
            ModuleDefinition module = ModuleDefinition.Create(type);
            if (!string.IsNullOrWhiteSpace(name))
                module.Name = name;
            if (!string.IsNullOrWhiteSpace(title))
                module.Title = title;
            // Caller must update ModuleGuid
            return module;
        }

        /// <summary>
        /// Create a unique module.
        /// </summary>
        /// <param name="modType"></param>
        /// <returns></returns>
        public static async Task<ModuleDefinition> CreateUniqueModuleAsync(Type modType) {
            ModuleDefinition mod = ModuleDefinition.Create(modType);
            if (!mod.IsModuleUnique)
                throw new InternalError($"Non-unique module type {modType.FullName} requested in {nameof(CreateUniqueModuleAsync)}");

            ModuleDefinition existingMod = await ModuleDefinition.LoadAsync(mod.PermanentGuid, AllowNone: true);
            if (existingMod != null)
                return existingMod;

            mod.Temporary = false;
            return mod;
        }

        /// <summary>
        /// Removes a module definition.
        /// </summary>
        /// <param name="moduleGuid"></param>
        public static async Task<bool> TryRemoveAsync(Guid moduleGuid) {
            return await RemoveModuleDefinitionAsync(moduleGuid);
        }

        /// <summary>
        /// Creates a new module definition.
        /// This creates an instance of a module from a known assembly and module type.
        /// Applications should not create members using this method. It is reserved for internal functions.
        /// </summary>
        public static ModuleDefinition Create(string assembly, string type) {
            // load the assembly/type to create a new module
            Type tp = null;
            try {
                Assembly asm = Assemblies.Load(assembly);
                tp = asm.GetType(type);
            } catch (Exception) {
                throw new InternalError("Can't create module {0}, {1}", assembly, type);
            }
            return Create(tp);
        }

        private static ModuleDefinition Create(Type type, Guid? moduleGuid = null) {
            object obj = Activator.CreateInstance(type);
            if (obj == null)
                throw new InternalError("Can't create module {0}", type.Name);
            ModuleDefinition module = obj as ModuleDefinition;
            if (module == null)
                throw new InternalError("Type {0} is not a module", type.Name);
            if (moduleGuid != null)
                module.ModuleGuid = (Guid) moduleGuid;
            return module;
        }

        public static string GetModuleDataFolder(Guid modGuid) {
            return Path.Combine(Manager.SiteFolder, ModuleDefinition.BaseFolderName, modGuid.ToString()) + "_Data";
        }
        [Category("Variables")]
        [Description("The module's data folder used to store additional data")]
        [Caption("Data Folder")]
        public string ModuleDataFolder {
            get {
                return ModuleDefinition.GetModuleDataFolder(ModuleGuid);
            }
        }
        public static string BaseFolderName { get { return "YetaWF_Modules"; } }

        [Category("Variables")]
        [Description("The Url of the module's addon folder")]
        [Caption("AddOn Folder")]
        public string AddOnModuleUrl {
            get {
                return VersionManager.TryGetAddOnPackageUrl(Domain, Product);
            }
        }

        // ACTIONS
        // ACTIONS
        // ACTIONS

        public virtual async Task<List<ModuleAction>> RetrieveModuleActionsAsync() {
            if (_moduleActions == null)
                _moduleActions = await GetAllModuleActionsAsync();
            return (from a in _moduleActions select a).ToList();// return a copy
        }
        private List<ModuleAction> _moduleActions;

        /// <summary>
        /// Retrieve a known module action with parameters.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="parms">Parameters (action dependent).</param>
        /// <returns>An action. May be null if not authorized.</returns>
        public async Task<ModuleAction> GetModuleActionAsync(string name, params object[] parms) {
            if (string.IsNullOrWhiteSpace(name))
                throw new InternalError("Missing action name");
            MethodInfo mi = GetType().GetMethod($"GetAction_{name}");
            ModuleAction action = null;
            if (mi != null) {
                action = (ModuleAction)mi.Invoke(this, parms);
                if (action == null)
                    return null;
            }
            if (action == null) {
                mi = GetType().GetMethod($"GetAction_{name}Async");
                if (mi == null)
                    throw new InternalError("Action name {0} doesn't exist", "GetAction_" + name);
                action = await ((Task<ModuleAction>)mi.Invoke(this, parms));
                if (action == null)
                    return null;
            }
            if (string.IsNullOrWhiteSpace(action.Url))
                action.Url = $"/{Area}/{Controller}/{name}";
            return action;
        }

        /// <summary>
        /// Retrieve a known module action with parameters.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="parms">Parameters (action dependent).</param>
        /// <returns>A list of actions.</returns>
        /// <returns>An action. May be null if not authorized.</returns>
        public async Task<List<ModuleAction>> GetModuleActionsAsync(string name, params object[] parms) {
            if (string.IsNullOrWhiteSpace(name))
                throw new InternalError("Missing action name");
            MethodInfo mi = GetType().GetMethod($"GetAction_{name}");
            List<ModuleAction> actions = null;
            if (mi != null) {
                actions = (List<ModuleAction>)mi.Invoke(this, parms);
                if (actions == null)
                    return null;
            }
            if (actions == null) {
                mi = GetType().GetMethod($"GetAction_{name}Async");
                if (mi == null)
                    throw new InternalError("Action name {0} doesn't exist", "GetAction_" + name);
                actions = await ((Task<List<ModuleAction>>)mi.Invoke(this, parms));
                if (actions == null)
                    return null;
            }
            foreach (ModuleAction action in actions) {
                if (string.IsNullOrWhiteSpace(action.Url))
                    action.Url = $"/{Area}/{Controller}/{name}";
            }
            return actions;
        }

        /// <summary>
        /// Populates the module actions
        /// </summary>
        protected async Task<List<ModuleAction>> GetAllModuleActionsAsync() {
            List<ModuleAction> moduleActions = new List<ModuleAction>();

            MethodInfo[] mi = GetType().GetMethods(BindingFlags.Public|BindingFlags.Instance);
            foreach (var m in mi) {
                string name = m.Name;
                if (!name.StartsWith("GetAction_"))
                    continue;
                name = name.Substring(10);
                ParameterInfo[] parms = m.GetParameters();
                if (parms != null && parms.Length > 0)
                    continue;

                ModuleAction action = null;
                if (m.ReturnType == typeof(ModuleAction)) {
                    action = (ModuleAction)m.Invoke(this, new object[] { });
                } else if (m.ReturnType == typeof(Task<ModuleAction>)) {
                    action = await (Task<ModuleAction>)m.Invoke(this, new object[] { });
                }
                if (action != null) {
                    if (string.IsNullOrWhiteSpace(action.Url))
                        action.Url = $"/{Area}/{Controller}/{name}";
                    moduleActions.Add(action);
                }
            }
            return moduleActions;
        }

        protected async Task<string> CustomIconAsync(string iconName) {
            SkinImages skinImg = new SkinImages();
            return await skinImg.FindIcon_PackageAsync(iconName, Package.GetCurrentPackage(this));
        }

        // RENDERING
        // RENDERING
        // RENDERING


#if MVC6
        public async Task<HtmlString> RenderModuleAsync(IHtmlHelper htmlHelper)
#else
        public async Task<HtmlString> RenderModuleAsync(HtmlHelper htmlHelper)
#endif
        {
            if (!Visible && !Manager.EditMode) return HtmlStringExtender.Empty;

            // determine char dimensions for current skin
            SkinAccess skinAccess = new SkinAccess();
            int charWidth, charHeight;
            skinAccess.GetModuleCharacterSizes(this, out charWidth, out charHeight);
            Manager.NewCharSize(charWidth, charHeight);

            // execute action
            ModuleDefinition oldMod = Manager.CurrentModule;
            Manager.CurrentModule = this;
            Manager.WantFocus = this.WantFocus;

            RouteValueDictionary rvd = new RouteValueDictionary();
            rvd.Add(Globals.RVD_ModuleDefinition, this);

            string moduleHtml = null;
            try {
#if MVC6
                if (!string.IsNullOrEmpty(Area))
                    moduleHtml = (await htmlHelper.ActionAsync(this, Action, Controller, Area, rvd)).ToString();
                else
                    moduleHtml = (await htmlHelper.ActionAsync(this, Action, Controller, rvd)).ToString();
#else
                YetaWFManager.Syncify(() => {
                    if (!string.IsNullOrEmpty(Area))
                        rvd.Add("Area", Area);
                    moduleHtml = htmlHelper.Action(Action, Controller, rvd).ToString();
                    return Task.CompletedTask;
                });
#endif
            } catch (Exception exc) {
                // Only mvc5 catches all exceptions here. Some Mvc6 errors are handled in HtmlHelper.Action() because of their async nature.
                HtmlBuilder hb = ProcessModuleError(exc, ModuleName);
                moduleHtml = hb.ToString();
            }

            Manager.WantFocus = false;
            Manager.CurrentModule = oldMod;
            if (string.IsNullOrEmpty(moduleHtml) && !Manager.EditMode && !Manager.RenderingUniqueModuleAddons)
                return HtmlStringExtender.Empty; // if the module contents are empty, we bail

            await Manager.AddOnManager.AddModuleAsync(this);

            if (string.IsNullOrEmpty(moduleHtml) && !Manager.EditMode /* && Manager.RenderingUniqueModuleAddons*/)
                return HtmlStringExtender.Empty; // if the module contents are empty, we bail

            bool showTitle = ShowTitle;
            bool showMenu = true;
            bool showAction = true;
            if (Manager.IsInPopup) {
                showMenu = false; // no menus in popups
                if (Manager.CurrentPage.Temporary) {
                    // a temporary page only has one module so we'll use the module title as the page title.
                    showTitle = false;
                } else if (Manager.CurrentPage.ModuleDefinitions.Count == 1) {
                    // a permanent page can have one or more modules - if there is just one module, we'll use the module title as page title
                    showTitle = false;
                } else {
                    ; // a page with multiple modules is expected to have a valid page title
                }
            }
            if (Manager.CurrentPage.Temporary) {
                // add the module's temporary page css class
                if (!string.IsNullOrWhiteSpace(this.TempPageCssClass)) {
                    string tempCss = YetaWFManager.JserEncode(this.TempPageCssClass);
                    Manager.ScriptManager.AddLast(
$"var $body = $('body');" +
$"$body.removeClass($body.attr('data-pagecss'));" + // remove existing page specific classes
$"$body.addClass('{tempCss}');" + // add our new class(es)
$"$body.attr('data-pagecss', '{tempCss}');"// remember so we can remove them for the next page
                    );
                }
            }

            string containerHtml = (await skinAccess.MakeModuleContainerAsync(this, moduleHtml, ShowTitle: showTitle, ShowMenu: showMenu, ShowAction: showAction)).ToString();

            if (!Manager.RenderingUniqueModuleAddons) {
                string title = Manager.PageTitle;
                if (string.IsNullOrWhiteSpace(title)) {
                    // if a page has no title, use the title of the first module in the Main pane
                    PageDefinition.ModuleList mods = Manager.CurrentPage.ModuleDefinitions.GetModulesForPane(Globals.MainPane);
                    if (mods.Count > 0) {
                        try { // the module could be damaged
                            title = (await mods[0].GetModuleAsync()).Title;
                        } catch (Exception) { }
                    }
                    // if the title is still not available, simply use the very first module (any pane)
                    if (string.IsNullOrWhiteSpace(title)) {
                        try { // the module could be damaged
                            if (Manager.CurrentPage.ModuleDefinitions.Count > 1 && this == await Manager.CurrentPage.ModuleDefinitions[0].GetModuleAsync())
                                title = Title;
                        } catch (Exception) { }
                    }
                    Manager.PageTitle = title;
                }
            }

            Manager.LastUpdated = this.DateUpdated;

            //DEBUG:  containerHtml has entire module

            Manager.PopCharSize();

            Manager.AddOnManager.AddExplicitlyInvokedModules(ReferencedModules);

            return new HtmlString(containerHtml);
        }

        /// <summary>
        /// Ajax invoked modules - used to render REFERENCED modules during ajax calls
        /// </summary>

#if MVC6
        public async Task<HtmlString> RenderReferencedModule_AjaxAsync(IHtmlHelper htmlHelper)
#else
        public async Task<HtmlString> RenderReferencedModule_AjaxAsync(HtmlHelper htmlHelper)
#endif
        {
            // execute action
            ModuleDefinition oldMod = Manager.CurrentModule;
            Manager.CurrentModule = this;

            RouteValueDictionary rvd = new RouteValueDictionary();
            rvd.Add(Globals.RVD_ModuleDefinition, this);

            string moduleHtml = null;
#if MVC6
            if (!string.IsNullOrEmpty(Area))
                moduleHtml = (await htmlHelper.ActionAsync(this, Action, Controller, Area, rvd)).ToString();
            else
                moduleHtml = (await htmlHelper.ActionAsync(this, Action, Controller, rvd)).ToString();
#else
            YetaWFManager.Syncify(() => {
                if (!string.IsNullOrEmpty(Area))
                    rvd.Add("Area", Area);
                moduleHtml = htmlHelper.Action(Action, Controller, rvd).ToString();
                return Task.CompletedTask;
            });
#endif
            Manager.CurrentModule = oldMod;
            if (string.IsNullOrEmpty(moduleHtml) && !Manager.EditMode)
                return HtmlStringExtender.Empty; // if the module contents are empty, we bail

            await Manager.AddOnManager.AddModuleAsync(this);

            return new HtmlString(moduleHtml);
        }

        public static HtmlBuilder ProcessModuleError(Exception exc, string name) {
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("<div class='{0}'>", Globals.CssDivAlert);
#if DEBUG
            hb.Append(__ResStr("modErr", "An error occurred in module {0}:<br/>", YetaWFManager.HtmlEncode(name)));
#endif
            // skip first exception (because it's not user friendly)
            if (!string.IsNullOrWhiteSpace(ErrorHandling.FormatExceptionMessage(exc)) && exc.InnerException != null) exc = exc.InnerException;            
            hb.Append(YetaWFManager.HtmlEncode(ErrorHandling.FormatExceptionMessage(exc)));
            hb.Append("</div>");
            if (Manager.CurrentResponse.StatusCode == 200)
                Manager.CurrentResponse.StatusCode = 500; // mark as error if we don't already have an error code (usually from MarkNotFound)
            return hb;
        }

        public HtmlString TitleHtml {
            get {
                if (string.IsNullOrWhiteSpace(Title))
                    return HtmlStringExtender.Empty;
                TagBuilder tag = new TagBuilder("h1");
                tag.SetInnerText(Title);
                return tag.ToHtmlString(TagRenderMode.Normal);
            }
        }

        public async Task<string> GetModuleMenuHtmlAsync() {
            if (ShowModuleMenu)
                return (await RenderModuleMenuAsync()).ToString();
            else
                return "";
        }

        public async Task<string> GetActionMenuHtmlAsync() {
            if (ShowActionMenu)
                return (await RenderModuleLinksAsync(ModuleAction.RenderModeEnum.NormalLinks, Globals.CssModuleLinksContainer)).ToString();
            else
                return "";
        }
        public async Task<string> GetActionTopMenuHtmlAsync() {
            if (ShowTitle && ShowTitleActions)
                return (await RenderModuleLinksAsync(ModuleAction.RenderModeEnum.IconsOnly, Globals.CssModuleLinksContainer)).ToString();
            else
                return "";
        }

        [Category("Variables"), Caption("Show Module Menu"), Description("Displays whether the module menu is shown for this module")]
        public virtual bool ShowModuleMenu { get { return true; } }

        [Category("Variables")]
        [Description("Displays whether the action menu is shown for this module")]
        [Caption("Show Action Menu")]
        public virtual bool ShowActionMenu { get { return true; } }

        // CONFIGURATION (only used for Configuration modules)
        // CONFIGURATION (only used for Configuration modules)
        // CONFIGURATION (only used for Configuration modules)

        public virtual DataProviderImpl GetConfigDataProvider() {
            if (configDPthrowError)
                throw new InternalError("Module {0} is not a configuration module", GetType().FullName);
            else
                return null;
        }
        private bool configDPthrowError = true;

        public DataProviderImpl TryGetConfigDataProvider() {
            configDPthrowError = false;// avoid exception spam
            DataProviderImpl dpImpl = GetConfigDataProvider();
            configDPthrowError = true;
            return dpImpl;
        }

        /// <summary>
        /// Returns configuration data if the module is a configuration module.
        /// </summary>
        /// <remarks>This is only used for variable substitution in site templates (hence no optimization).</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This is a catastrophic error so we must abort")]
        public object ConfigData {
            get {
                DataProviderImpl dataProvider = TryGetConfigDataProvider();
                if (dataProvider == null) return new { };
                using (dataProvider) {
                    Type typeDP = dataProvider.GetType();
                    // get the config data
                    MethodInfo mi = typeDP.GetMethod("GetConfig");
                    if (mi == null) throw new InternalError("Data provider {0} doesn't implement a GetConfig method for a configuration module", typeDP.FullName);
                    object config = mi.Invoke(dataProvider, null);
                    return config;
                }
           }
        }

        // Method used to save initial settings from site templates
        public void UpdateConfigProperty(string name, object value) {
            using (DataProviderImpl dataProvider = GetConfigDataProvider()) {
                Type typeDP = dataProvider.GetType();
                // get the config data
                MethodInfo mi = typeDP.GetMethod("GetConfig");
                if (mi == null) throw new InternalError("Data provider {0} doesn't implement a GetConfig method for a configuration module", typeDP.FullName);
                object config = mi.Invoke(dataProvider, null);
                // update the property
                Type configType = config.GetType();
                PropertyInfo pi = ObjectSupport.TryGetProperty(configType, name);
                if (pi == null) throw new InternalError("Configuration {0} doesn't offer a {1} property", configType.FullName, name);
                pi.SetValue(config, value);

                mi = typeDP.GetMethod("UpdateConfig");
                if (mi == null) throw new InternalError("Data provider {0} doesn't implement a UpdateConfig method for a configuration module", typeDP.FullName);
                mi.Invoke(dataProvider, new object[] { config });
            }
        }

        // REFERENCES
        // REFERENCES
        // REFERENCES

        /// <summary>
        /// Method used in site templates to add a module reference to the current page.
        /// </summary>
        /// <param name="modGuid">The Guid of the unique module to be added to the page's referenced modules.</param>
        public void AddModuleReference(PageDefinition page, Guid modGuid) {
            if ((from m in page.ReferencedModules where m.ModuleGuid == modGuid select m).FirstOrDefault() == null)
                page.ReferencedModules.Add(new ReferencedModule { ModuleGuid = modGuid });
        }

        // AUTHORIZATION
        // AUTHORIZATION
        // AUTHORIZATION

        public enum AllowedEnum {
            [EnumDescription(" ", "Not specified")]
            NotDefined = 0,
            [EnumDescription("Yes", "Allowed")]
            Yes = 1,
            [EnumDescription("No", "Forbidden")]
            No = 2,
        };

        public class AllowedRole {
            public int RoleId { get; set; }
            public AllowedEnum View { get; set; }
            public AllowedEnum Edit { get; set; }
            public AllowedEnum Remove { get; set; }
            public AllowedEnum Extra1 { get; set; }
            public AllowedEnum Extra2 { get; set; }
            public AllowedEnum Extra3 { get; set; }
            public AllowedEnum Extra4 { get; set; }
            public AllowedEnum Extra5 { get; set; }
            public bool IsEmpty() { return View == AllowedEnum.NotDefined && Edit == AllowedEnum.NotDefined && Remove == AllowedEnum.NotDefined && Extra1 == AllowedEnum.NotDefined && Extra2 == AllowedEnum.NotDefined && Extra3 == AllowedEnum.NotDefined && Extra4 == AllowedEnum.NotDefined && Extra5 == AllowedEnum.NotDefined; }
            public AllowedRole() { }
            public AllowedRole(int id, AllowedEnum view = AllowedEnum.Yes, AllowedEnum edit = AllowedEnum.NotDefined, AllowedEnum remove = AllowedEnum.NotDefined, AllowedEnum extra1 = AllowedEnum.NotDefined, AllowedEnum extra2 = AllowedEnum.NotDefined, AllowedEnum extra3 = AllowedEnum.NotDefined, AllowedEnum extra4 = AllowedEnum.NotDefined, AllowedEnum extra5 = AllowedEnum.NotDefined) {
                RoleId = id; View = view; Edit = edit; Remove = remove; Extra1 = extra1; Extra2 = extra2; Extra3 = extra3; Extra4 = extra4; Extra5 = extra5;
            }
            public static AllowedRole Find(List<AllowedRole> list, int roleId) {
                if (list == null) return null;
                return (from l in list where roleId == l.RoleId select l).FirstOrDefault();
            }
            public bool __editable { get { return RoleId != Resource.ResourceAccess.GetSuperuserRoleId(); } }
        }
        public class AllowedUser {
            public int UserId { get; set; }
            public AllowedEnum View { get; set; }
            public AllowedEnum Edit { get; set; }
            public AllowedEnum Remove { get; set; }
            public AllowedEnum Extra1 { get; set; }
            public AllowedEnum Extra2 { get; set; }
            public AllowedEnum Extra3 { get; set; }
            public AllowedEnum Extra4 { get; set; }
            public AllowedEnum Extra5 { get; set; }
            public bool IsEmpty() { return View == AllowedEnum.NotDefined && Edit == AllowedEnum.NotDefined && Remove == AllowedEnum.NotDefined && Extra1 == AllowedEnum.NotDefined && Extra2 == AllowedEnum.NotDefined && Extra3 == AllowedEnum.NotDefined && Extra4 == AllowedEnum.NotDefined && Extra5 == AllowedEnum.NotDefined; }
            public AllowedUser() { }
            public AllowedUser(int id, AllowedEnum view = AllowedEnum.Yes, AllowedEnum edit = AllowedEnum.NotDefined, AllowedEnum remove = AllowedEnum.NotDefined, AllowedEnum extra1 = AllowedEnum.NotDefined, AllowedEnum extra2 = AllowedEnum.NotDefined, AllowedEnum extra3 = AllowedEnum.NotDefined, AllowedEnum extra4 = AllowedEnum.NotDefined, AllowedEnum extra5 = AllowedEnum.NotDefined) {
                UserId = id; View = view; Edit = edit; Remove = remove; Extra1 = extra1; Extra2 = extra2; Extra3 = extra3; Extra4 = extra4; Extra5 = extra5;
            }
            public static AllowedUser Find(List<AllowedUser> list, int userId) {
                if (list == null) return null;
                return (from l in list where userId == l.UserId select l).FirstOrDefault();
            }
        }

        protected bool IsAuthorized(Func<AllowedRole, AllowedEnum> testRole, Func<AllowedUser, AllowedEnum> testUser) {

            if (Resource.ResourceAccess.IsBackDoorWideOpen()) return true;

            // check if it's a superuser
            if (Manager.HasSuperUserRole)
                return true;

            if (Manager.HaveUser) {
                // we have a logged on user
                if (Manager.Need2FA) {
                    return IsAuthorized_Role(testRole, Resource.ResourceAccess.GetAnonymousRoleId()) || IsAuthorized_Role(testRole, Resource.ResourceAccess.GetUser2FARoleId());
                } else {
                    int superuserRole = Resource.ResourceAccess.GetSuperuserRoleId();
                    if (Manager.UserRoles != null && Manager.UserRoles.Contains(superuserRole))
                        return true;
                    // see if the user has a role that is explicitly forbidden to access this module
                    int userRole = Resource.ResourceAccess.GetUserRoleId();
                    foreach (AllowedRole allowedRole in AllowedRoles) {
                        if (Manager.UserRoles != null && Manager.UserRoles.Contains(allowedRole.RoleId)) {
                            if (testRole(allowedRole) == AllowedEnum.No)
                                return false;
                        }
                        if (allowedRole.RoleId == userRole) {// check if any logged on user is forbidden
                            if (testRole(allowedRole) == AllowedEnum.No)
                                return false;
                        }
                    }
                    // check if the user is explicitly forbidden
                    AllowedUser allowedUser = AllowedUser.Find(AllowedUsers, Manager.UserId);
                    if (allowedUser != null)
                        if (testUser(allowedUser) == AllowedEnum.No)
                            return false;
                    // see if the user has a role that is explicitly permitted to access this module
                    foreach (AllowedRole allowedRole in AllowedRoles) {
                        if (Manager.UserRoles != null && Manager.UserRoles.Contains(allowedRole.RoleId)) {
                            if (testRole(allowedRole) == AllowedEnum.Yes)
                                return true;
                        }
                        if (allowedRole.RoleId == userRole) {// check if any logged on user is permitted
                            if (testRole(allowedRole) == AllowedEnum.Yes)
                                return true;
                        }
                    }
                    // check if the user listed is explicitly allowed
                    if (allowedUser != null)
                        if (testUser(allowedUser) == AllowedEnum.Yes)
                            return true;
                }
            } else {
                // anonymous user
                return IsAuthorized_Role(testRole, Resource.ResourceAccess.GetAnonymousRoleId());
            }
            return false;
        }
        private bool IsAuthorized_Role(Func<AllowedRole, AllowedEnum> testRole, int role) {
            AllowedRole allowedRole = AllowedRole.Find(AllowedRoles, role);
            if (allowedRole != null) {
                // check if the role is explicitly forbidden
                if (testRole(allowedRole) == AllowedEnum.No)
                    return false;
                // check if the role is explicitly allowed
                if (testRole(allowedRole) == AllowedEnum.Yes)
                    return true;
            }
            return false;
        }
        public bool IsAuthorized(string level) {
            string internalName;
            if (string.IsNullOrWhiteSpace(level))
                internalName = level = Manager.EditMode ? RoleDefinition.Edit : RoleDefinition.View;
            else
                internalName = (from r in RolesDefinitions where r.Name == level select r.InternalName).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(internalName))
                throw new InternalError("Permission level {0} not found in Roles", level);

            // module specific authorization
            return IsAuthorized((allowedRole) => {
                PropertyInfo pi = ObjectSupport.TryGetProperty(allowedRole.GetType(), internalName);
                if (pi == null) throw new InternalError("Authorization role level {0} not found", level);
                return (AllowedEnum) pi.GetValue(allowedRole);
            }, (allowedUser) => {
                PropertyInfo pi = ObjectSupport.TryGetProperty(allowedUser.GetType(), internalName);
                if (pi == null) throw new InternalError("Authorization user level {0} not found", level);
                return (AllowedEnum) pi.GetValue(allowedUser);
            });
        }

        public bool IsAuthorized_View_Anonymous() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetAnonymousRoleId());
        }
        public bool IsAuthorized_View_AnyUser() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetUserRoleId());
        }

        // MODULE USAGE
        // MODULE USAGE
        // MODULE USAGE

        /// <remarks>
        /// This property is not populated. It must be explicitly set using __GetPagesAsync() if the data is needed (use
        /// ObjectSupport.HandlePropertyAsync).
        /// </remarks>
        [Category("Pages"), Caption("Pages"), Description("The pages where this module is used")]
        [UIHint("PageDefinitions"), ReadOnly]
        [DontSave][Data_DontSave]
        public List<PageDefinition> Pages { get; set; }

        public async Task<List<PageDefinition>> __GetPagesAsync() {
            if (Pages == null)
                Pages = await PageDefinition.GetPagesFromModuleAsync(ModuleGuid);
            return Pages;
        }


        // SEARCH
        // SEARCH
        // SEARCH

        public virtual void CustomSearch(ISearchWords searchWords) { }

        // VALIDATION
        // VALIDATION
        // VALIDATION

        public virtual void CustomValidation(ModelStateDictionary modelState, string modelPrefix) { }
    }
}
