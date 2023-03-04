/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.DataProvider;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Endpoints.Support;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Search;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    // Interface to derived module type data provider
    public interface IModuleDefinitionIO : IDisposable, IAsyncDisposable {
        Task SaveModuleDefinitionAsync(ModuleDefinition mod);
    }

    public partial class ModuleDefinition {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition), name, defaultValue, parms); }

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        protected static bool HaveManager { get { return YetaWFManager.HaveManager; } }

        public virtual bool JSONModule {  get { return false; } }//$$$ eventually remove

        // MODULE INFO
        // MODULE INFO
        // MODULE INFO

        [Category("About"), Description("The internal, permanent module name"), Caption("Permanent Module Name")]
        [UIHint("String"), ReadOnly]
        public string PermanentModuleName {
            get {
                GetModuleInfo();
                return string.Format(Globals.PermanentModuleNameFormat, Domain, ClassName);
            }
        }

        [Category("About"), Description("The displayable module name"), Caption("Module Display Name")]
        [UIHint("String"), ReadOnly]
        public string ModuleDisplayName {
            get {
                GetModuleInfo();
                return ModuleName;
            }
        }
        [Category("About"), Description("The internal company name of the module's publisher"), Caption("Company Name")]
        [UIHint("String"), ReadOnly]
        public string CompanyName {
            get {
                GetModuleInfo();
                return _CompanyName;
            }
        }
        [Category("About"), Description("The displayable company name of the module's publisher"), Caption("Company Display Name")]
        [UIHint("String"), ReadOnly]
        public string CompanyDisplayName {
            get {
                GetModuleInfo();
                return _CompanyDisplayName;
            }
        }
        [Category("About"), Description("The domain name of the product or company publishing the module"), Caption("Domain")]
        [UIHint("String"), ReadOnly]
        public string Domain {
            get {
                GetModuleInfo();
                return _Domain;
            }
        }
        [Category("About"), Description("The MVC area name of the module"), Caption("Area")]
        [UIHint("String"), ReadOnly]
        public string AreaName {
            get {
                GetModuleInfo();
                return _Area;
            }
        }
        [Category("About"), Description("The module's product name"), Caption("Product")]
        [UIHint("String"), ReadOnly]
        public string Product {
            get {
                GetModuleInfo();
                return _Product;
            }
        }
        [Category("About"), Description("The module version"), Caption("Version")]
        [UIHint("String"), ReadOnly]
        public string Version {
            get {
                GetModuleInfo();
                return _Version;
            }
        }

        [Category("About"), Description("The module's class name"), Caption("Class Name")]
        [UIHint("String"), ReadOnly]
        public string ClassName {
            get {
                if (string.IsNullOrEmpty(_ClassName)) {
                    _ClassName = GetType().Name;
                }
                return _ClassName;
            }
        }
        private string? _ClassName { get; set; }

        [Category("About"), Description("The module's full class name"), Caption("Class Name (Full)")]
        [UIHint("String"), ReadOnly]
        public string FullClassName {
            get {
                return GetType().FullName!;
            }
        }

        [Category("About"), Description("The module name"), Caption("Module Name")]
        [UIHint("String"), ReadOnly]
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
                string ns = type.Namespace!;

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
        private string _Product { get; set; } = null!;
        private string _Area { get; set; } = null!;
        private string _CompanyName { get; set; } = null!;
        private string _Domain { get; set; } = null!;
        private string _ModuleName { get; set; } = null!;
        private string _Version { get; set; } = null!;
        private string _CompanyDisplayName { get; set; } = null!;

        [Category("Variables"), Description("Displays whether the module is a unique module"), Caption("IsModuleUnique")]
        [UIHint("Boolean"), ReadOnly]
        public bool IsModuleUnique {
            get {
                UniqueModuleAttribute? attr = (UniqueModuleAttribute?) Attribute.GetCustomAttribute(GetType(), typeof(UniqueModuleAttribute));
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
        public virtual string Action {
            get {
                if (string.IsNullOrEmpty(_Action)) {
                    string action = ClassName;
                    if (!action.EndsWith(Globals.ModuleClassSuffix)) {
                        if (GetType() == typeof(ModuleDefinition)) // don't throw an error for the base class (this happens during model binding before invoking controller action, it's unclear why MVC would retrieve read/only properties)
                            return string.Empty;
                        throw new InternalError("Module {0} is using an invalid class name - should end in \"...{1}\".", action, Globals.ModuleClassSuffix);
                    }
                    _Action = action.Substring(0, action.Length - Globals.ModuleClassSuffix.Length); // remove trailing Module
                }
                return _Action;
            }
        }
        private string? _Action { get; set; }

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
        public static async Task<ModuleDefinition?> FindDesignedModuleAsync(string url) {
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

        [Category("Variables"), Caption("Has Settings"), Description("Defines whether the module has settings that can be edited and saved")]
        [UIHint("Boolean"), ReadOnly]
        [DontSave]
        public virtual bool ModuleHasSettings { get { return true; } }

        // this is provided by a specific derived module type - its data provider reads/writes specific module types
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
        private IModuleDefinitionIO? _dataProvider;

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
        public static async Task<ModuleDefinition?> LoadAsync(Guid moduleGuid, bool AllowNone = false) {
            // load it as an already saved module
            ModuleDefinition? mod = null;
            try {
                mod = await YetaWF.Core.IO.Module.LoadModuleDefinitionAsync(moduleGuid);
            } catch (Exception) {
                mod = null;
                if (!AllowNone)
                    throw;
            }
            if (mod == null) {
                // if it hasn't been saved yet, check if this is a permanent module guid for a unique module
                Type? type = InstalledModules.TryFindModule(moduleGuid);
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
        public static async Task<ModuleDefinition?> LoadByUrlAsync(string url) {
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
            await YetaWF.Core.IO.Module.SaveModuleDefinitionAsync(this, DataProvider);
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
        public static ModuleDefinition CreateNewDesignedModule(Guid permanentGuid, string? name, MultiString? title) {
            Type? type = InstalledModules.TryFindModule(permanentGuid);
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

            ModuleDefinition? existingMod = await ModuleDefinition.LoadAsync(mod.PermanentGuid, AllowNone: true);
            if (existingMod != null)
                return existingMod;

            mod.Temporary = false;
            return mod;
        }

        /// <summary>
        /// Create a unique module.
        /// </summary>
        /// <param name="modType"></param>
        /// <returns></returns>
        public static async Task<TYPE> CreateRequiredUniqueModuleAsync<TYPE>() {
            Type modType = typeof(TYPE);
            ModuleDefinition mod = await CreateUniqueModuleAsync(modType) ?? throw new InternalError($"Unique module of type {modType.Name} not found");
            return (TYPE)(object) mod;
        }

        /// <summary>
        /// Removes a module definition.
        /// </summary>
        /// <param name="moduleGuid"></param>
        public static async Task<bool> TryRemoveAsync(Guid moduleGuid) {
            return await YetaWF.Core.IO.Module.RemoveModuleDefinitionAsync(moduleGuid);
        }

        /// <summary>
        /// Creates a new module definition.
        /// This creates an instance of a module from a known assembly and module type.
        /// Applications should not create members using this method. It is reserved for internal functions.
        /// </summary>
        public static ModuleDefinition Create(string assembly, string type) {
            // load the assembly/type to create a new module
            Type? tp;
            try {
                Assembly? asm = Assemblies.Load(assembly);
                tp = asm!.GetType(type);
            } catch (Exception) {
                throw new InternalError("Can't create module {0}, {1}", assembly, type);
            }
            return Create(tp!);
        }

        private static ModuleDefinition Create(Type type, Guid? moduleGuid = null) {
            object? obj = Activator.CreateInstance(type);
            if (obj == null)
                throw new InternalError("Can't create module {0}", type.Name);
            ModuleDefinition? module = obj as ModuleDefinition;
            if (module == null)
                throw new InternalError("Type {0} is not a module", type.Name);
            if (moduleGuid != null)
                module.ModuleGuid = (Guid) moduleGuid;
            return module;
        }

        public static string GetModuleDataFolder(Guid modGuid) {
            return Path.Combine(Manager.SiteFolder, ModuleDefinition.BaseFolderName, modGuid.ToString()) + "_Data";
        }
        [Category("Variables"), Description("The module's data folder used to store additional data"), Caption("Data Folder")]
        [UIHint("String"), ReadOnly]
        public string ModuleDataFolder {
            get {
                return ModuleDefinition.GetModuleDataFolder(ModuleGuid);
            }
        }
        public static string BaseFolderName { get { return "YetaWF_Modules"; } }

        // ACTIONS
        // ACTIONS
        // ACTIONS

        public virtual async Task<List<ModuleAction>> RetrieveModuleActionsAsync() {
            if (_moduleActions == null)
                _moduleActions = await GetAllModuleActionsAsync();
            return (from a in _moduleActions select a).ToList();// return a copy
        }
        private List<ModuleAction>? _moduleActions;

        /// <summary>
        /// Retrieve a known module action with parameters.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="parms">Parameters (action dependent).</param>
        /// <returns>An action. May be null if not authorized.</returns>
        public async Task<ModuleAction?> GetModuleActionAsync(string name, params object?[] parms) {
            if (string.IsNullOrWhiteSpace(name))
                throw new InternalError("Missing action name");
            MethodInfo? mi = GetType().GetMethod($"GetAction_{name}");
            ModuleAction? action = null;
            if (mi != null) {
                action = (ModuleAction?)mi.Invoke(this, parms);
                if (action == null)
                    return null;
            }
            if (action == null) {
                mi = GetType().GetMethod($"GetAction_{name}Async");
                if (mi == null)
                    throw new InternalError("Action name {0} doesn't exist", "GetAction_" + name);
                action = await (Task<ModuleAction?>)mi.Invoke(this, parms) ! ;
                if (action == null)
                    return null;
            }
            if (string.IsNullOrWhiteSpace(action.Url))
                action.Url = $"/{AreaName}/{Controller}/{name}";
            return action;
        }

        /// <summary>
        /// Retrieve a known module action with parameters.
        /// </summary>
        /// <param name="name">The name of the action.</param>
        /// <param name="parms">Parameters (action dependent).</param>
        /// <returns>A list of actions.</returns>
        /// <returns>An action. May be null if not authorized.</returns>
        public async Task<List<ModuleAction>?> GetModuleActionsAsync(string name, params object?[] parms) {
            if (string.IsNullOrWhiteSpace(name))
                throw new InternalError("Missing action name");
            MethodInfo? mi = GetType().GetMethod($"GetAction_{name}");
            List<ModuleAction>? actions = null;
            if (mi != null) {
                actions = (List<ModuleAction>?)mi.Invoke(this, parms);
                if (actions == null)
                    return null;
            }
            if (actions == null) {
                mi = GetType().GetMethod($"GetAction_{name}Async");
                if (mi == null)
                    throw new InternalError("Action name {0} doesn't exist", "GetAction_" + name);
                actions = await (Task<List<ModuleAction>?>)mi.Invoke(this, parms) !;
                if (actions == null)
                    return null;
            }
            foreach (ModuleAction action in actions) {
                if (string.IsNullOrWhiteSpace(action.Url))
                    action.Url = $"/{AreaName}/{Controller}/{name}";
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

                ModuleAction? action = null;
                if (m.ReturnType == typeof(ModuleAction)) {
                    action = (ModuleAction?)m.Invoke(this, new object[] { });
                } else if (m.ReturnType == typeof(Task<ModuleAction>)) {
                    object? ret = m.Invoke(this, new object[] { });
                    if (ret != null)
                        action = await (Task<ModuleAction?>)m.Invoke(this, new object[] { }) !;
                }
                if (action != null) {
                    if (string.IsNullOrWhiteSpace(action.Url))
                        action.Url = $"/{AreaName}/{Controller}/{name}";
                    moduleActions.Add(action);
                }
            }
            return moduleActions;
        }

        protected async Task<string> CustomIconAsync(string iconName) {
            SkinImages skinImg = new SkinImages();
            return await skinImg.FindIcon_PackageAsync(iconName, Package.GetCurrentPackage(this));
        }

        // DIRECT RENDERING
        // DIRECT RENDERING
        // DIRECT RENDERING

        /// <summary>
        /// Renders the module contents (without container) based on the provided model.
        /// </summary>
        /// <param name="model">The data model.</param>
        /// <returns>A <cref="ActionInfo"/> containing HTML and success information.</returns>
        public async Task<ActionInfo> RenderAsync(object model, string? ViewName = null, bool UseAreaViewName = true) {

            if (string.IsNullOrEmpty(ViewName)) {
                if (!string.IsNullOrEmpty(DefaultViewName)) {
                    ViewName = DefaultViewName;
                    UseAreaViewName = false;
                }
            }
            if (string.IsNullOrWhiteSpace(ViewName))
                ViewName = ModuleName;
            if (UseAreaViewName)
                ViewName = MakeFullViewName(ViewName, AreaName);

            YHtmlHelper htmlHelper = new YHtmlHelper(new Microsoft.AspNetCore.Mvc.ActionContext(), (this as ModuleDefinition2)?.ModelState);//$$$$$ remove this garbage
            string html = await htmlHelper.ForViewAsync(ViewName, this, model);

            return new ActionInfo { HTML = html, Failed = false };
        }

        protected static string MakeFullViewName(string? viewName, string area) {
            if (string.IsNullOrWhiteSpace(viewName))
                throw new InternalError("Missing view name");
            viewName = area + "_" + viewName;
            return viewName;
        }

        /// <summary>
        /// Current request is marked 404 (Not Found).
        /// </summary>
        /// <remarks>The page and all modules are still rendered and processed.</remarks>
        protected void MarkNotFound() {
            Manager.CurrentResponse.StatusCode = StatusCodes.Status404NotFound;
        }

        // CONTROLLER RENDERING
        // CONTROLLER RENDERING
        // CONTROLLER RENDERING

        /// <summary>
        /// Renders a module including container in view mode, overriding edit mode.
        /// </summary>
        /// <param name="htmlHelper">An instance of the HtmlHelper class.</param>
        /// <param name="Args">Optional parameters passed to the action rendering the module.</param>
        /// <returns>Returns HTML.</returns>
        public async Task<string> RenderModuleViewAsync(YHtmlHelper htmlHelper, object? Args = null) {

            bool oldEditMode = Manager.EditMode;
            try {
                Manager.EditMode = false;
                return await RenderModuleWithContainerAsync(htmlHelper, Args);
            } catch (Exception) {
                throw;
            } finally {
                Manager.EditMode = oldEditMode;
            }
        }

        /// <summary>
        /// Renders a module including container.
        /// </summary>
        /// <param name="htmlHelper">An instance of the HtmlHelper class.</param>
        /// <param name="Args">Optional parameters passed to the action rendering the module.</param>
        /// <returns>Returns HTML.</returns>
        public async Task<string> RenderModuleWithContainerAsync(YHtmlHelper htmlHelper, object? Args = null) {

            if (!Visible && !Manager.EditMode) return string.Empty;

            // If a module is authorized for anonymous but not users, we suppress it if we're Editor, Admin, Superuser, etc. to avoid cases where we
            // have 2 of the same modules, one for anonymous users, the other for logged on users.
            if (Manager.HaveUser && !Manager.EditMode && IsAuthorized_View_Anonymous() && !IsAuthorized_View_AnyUser())
                return string.Empty;

            // execute actionalert
            ModuleDefinition? oldMod = Manager.CurrentModule;
            Manager.CurrentModule = this;
            Manager.WantFocus = this.WantFocus;

            bool tempEditOverride = false;
            if (Manager.EditMode) {
                if (!IsAuthorized(RoleDefinition.Edit)) {
                    if (IsAuthorized(RoleDefinition.View)) {
                        // can't edit, but view is OK
                        Manager.EditMode = false;
                        tempEditOverride = true;
                    }
                }
            }

            await Manager.AddOnManager.AddModuleAsync(this);

            ActionInfo info;
            try {
                info = await htmlHelper.ActionAsync(this, Action, Controller, AreaName, parameters: Args);
                // module script initialization
                if (!info.Failed && !string.IsNullOrWhiteSpace(info.HTML)) {
                    if (await Manager.AddOnManager.TryAddAddOnNamedAsync(AreaName, ClassName)) // add supporting files
                        Manager.ScriptManager.AddLast($@"typeof {AreaName}==='undefined'||!{AreaName}.{ClassName}||new {AreaName}.{ClassName}('{ModuleHtmlId}');");
                }

            } catch (Exception exc) {
                // Only mvc5 catches all exceptions here. Some Mvc6 errors are handled in HtmlHelper.Action() because of their async nature.
                HtmlBuilder hb = ProcessModuleError(exc, ModuleName);
                info = new ActionInfo() { HTML = hb.ToString(), Failed = true };
            }

            if (tempEditOverride)
                Manager.EditMode = true;

            Manager.WantFocus = false;
            Manager.CurrentModule = oldMod;
            if (string.IsNullOrEmpty(info.HTML) && !Manager.EditMode && !Manager.RenderingUniqueModuleAddons)
                return string.Empty; // if the module contents are empty, we bail

            bool showTitle = ShowTitle;
            if (Manager.IsInPopup) {
                if (Manager.CurrentPage.Temporary) {
                    // a temporary page only has one module so we'll use the module title as the page title.
                    showTitle = false;
                } else if (GetModulesInMainPane() == 1) {
                    // a permanent page can have one or more modules in the Main pane - if there is just one module, we'll use the module title as page title
                    showTitle = false;
                } else {
                    ; // a page with multiple modules is expected to have a valid page title
                }
            }
            if (Manager.CurrentPage.Temporary) {
                // add the module's temporary page css class
                if (!string.IsNullOrWhiteSpace(this.TempPageCssClass)) {
                    string tempCss = Utility.JserEncode(this.TempPageCssClass);
                    Manager.ScriptManager.AddLast(
$"$YetaWF.elementRemoveClass(document.body, 'data-pagecss');" + // remove existing page specific classes
$"$YetaWF.elementAddClass(document.body, '{tempCss}');" + // add our new class(es)
$"document.body.setAttribute('data-pagecss', '{tempCss}');"// remember so we can remove them for the next page
                    );
                }
            }

            SkinAccess skinAccess = new SkinAccess();
            string containerHtml = await skinAccess.MakeModuleContainerAsync(this, info.HTML, ShowTitle: showTitle);

            if (!Manager.RenderingUniqueModuleAddons) {
                string? title = Manager.PageTitle;
                if (string.IsNullOrWhiteSpace(title)) {
                    // if a page has no title, use the title of the first module in the Main pane
                    PageDefinition.ModuleList mods = Manager.CurrentPage.ModuleDefinitions.GetModulesForPane(Globals.MainPane);
                    if (mods.Count > 0) {
                        ModuleDefinition? mod = await mods[0].GetModuleAsync();
                        if (mod != null)
                            title = mod.Title;
                    }
                    // if the title is still not available, simply use the very first module (any pane)
                    if (string.IsNullOrWhiteSpace(title)) {
                        if (Manager.CurrentPage.ModuleDefinitions.Count > 1) {
                            ModuleDefinition? mod = await Manager.CurrentPage.ModuleDefinitions[0].GetModuleAsync();
                            if (this == mod)
                                title = Title;
                        }
                    }
                    Manager.PageTitle = title;
                }
            }

            Manager.LastUpdated = this.DateUpdated;

            Manager.AddOnManager.AddExplicitlyInvokedModules(ReferencedModules);

            //DEBUG:  containerHtml has entire module
            return containerHtml;
        }

        private int GetModulesInMainPane() {
            return (from m in Manager.CurrentPage.ModuleDefinitions where m.Pane == Globals.MainPane select m).Count();
        }

        /// <summary>
        /// Ajax invoked modules - used to render REFERENCED modules during ajax calls
        /// </summary>
        public async Task<string> RenderReferencedModule_AjaxAsync(YHtmlHelper htmlHelper) {
            // execute action
            ModuleDefinition? oldMod = Manager.CurrentModule;
            Manager.CurrentModule = this;

            ActionInfo info = await htmlHelper.ActionAsync(this, Action, Controller, AreaName);
            Manager.CurrentModule = oldMod;
            if (string.IsNullOrEmpty(info.HTML) && !Manager.EditMode)
                return string.Empty; // if the module contents are empty, we bail

            await Manager.AddOnManager.AddModuleAsync(this);

            return info.HTML;
        }

        public static HtmlBuilder ProcessModuleError(Exception? exc, string name, string? details = null) {
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("<div class='{0}'>", Globals.CssDivAlert);
//#if DEBUG
//            hb.Append(__ResStr("modErr", "An error occurred in module {0}:<br/>", Utility.HE(name)));
//#endif
            if (details != null) {
                hb.Append($"{Utility.HE(details)}");
                if (exc != null)
                    hb.Append("<br/>");
            }
            if (exc != null) {
                // skip first exception (because it's not user friendly)
                if (!string.IsNullOrWhiteSpace(ErrorHandling.FormatExceptionMessage(exc)) && exc.InnerException != null) exc = exc.InnerException;
                hb.Append(Utility.HE(ErrorHandling.FormatExceptionMessage(exc)));
            }
            hb.Append("</div>");
            if (Manager.CurrentResponse.StatusCode == 200)
                Manager.CurrentResponse.StatusCode = 500; // mark as error if we don't already have an error code (usually from MarkNotFound)
            return hb;
        }

        [Category("Variables"), Caption("Show Module Menu"), Description("Displays whether the module menu is shown for this module")]
        [UIHint("Boolean")]
        public virtual bool ShowModuleMenu { get { return true; } }

        [Category("Variables"), Description("Displays whether the action menu is shown for this module"), Caption("Show Action Menu")]
        [UIHint("Boolean")]
        public virtual bool ShowActionMenu { get { return true; } }

        // CONFIGURATION (only used for Configuration modules)
        // CONFIGURATION (only used for Configuration modules)
        // CONFIGURATION (only used for Configuration modules)

        public virtual DataProviderImpl? GetConfigDataProvider() {
            if (configDPthrowError)
                throw new InternalError("Module {0} is not a configuration module", GetType().FullName);
            else
                return null;
        }
        private bool configDPthrowError = true;

        public DataProviderImpl? TryGetConfigDataProvider() {
            configDPthrowError = false;// avoid exception spam
            DataProviderImpl? dpImpl = GetConfigDataProvider();
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
                DataProviderImpl? dataProvider = TryGetConfigDataProvider();
                if (dataProvider == null) return new { };
                using (dataProvider) {
                    Type typeDP = dataProvider.GetType();
                    // get the config data
                    MethodInfo? mi = typeDP.GetMethod("GetConfigAsync");
                    if (mi == null) throw new InternalError($"Data provider {typeDP.FullName} doesn't implement a GetConfigAsync method for a configuration module");
                    dynamic configRetVal = mi.Invoke(dataProvider, null) !;
                    object config = configRetVal.Result; // only used in site templates so don't care about using Result
                    return config;
                }
           }
        }

        // Method used to save initial settings from site templates
        public void UpdateConfigProperty(string name, object value) {
            using (DataProviderImpl dataProvider = GetConfigDataProvider() ! ) {
                Type typeDP = dataProvider.GetType();
                // get the config data
                MethodInfo? mi = typeDP.GetMethod("GetConfigAsync");
                if (mi == null) throw new InternalError("Data provider {0} doesn't implement a GetConfigAsync method for a configuration module", typeDP.FullName);
                dynamic configRetVal = mi.Invoke(dataProvider, null) !;
                object config = configRetVal.Result; // only used in site templates so don't care about using Result
                // update the property
                Type configType = config.GetType();
                PropertyInfo? pi = ObjectSupport.TryGetProperty(configType, name);
                if (pi == null) throw new InternalError($"Configuration {configType.FullName} doesn't offer a {name} property");
                pi.SetValue(config, value);

                mi = typeDP.GetMethod("UpdateConfigAsync");
                if (mi == null) throw new InternalError("Data provider {0} doesn't implement a UpdateConfigAsync method for a configuration module", typeDP.FullName);
                Task retVal = (Task) mi.Invoke(dataProvider, new object[] { config }) !;
                retVal.Wait();// only used in site templates so don't care about using Wait
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
            public static AllowedRole? Find(List<AllowedRole> list, int roleId) {
                if (list == null) return null;
                return (from l in list where roleId == l.RoleId select l).FirstOrDefault();
            }
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
            public static AllowedUser? Find(List<AllowedUser> list, int userId) {
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
                    AllowedUser? allowedUser = AllowedUser.Find(AllowedUsers, Manager.UserId);
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
            AllowedRole? allowedRole = AllowedRole.Find(AllowedRoles, role);
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
        public bool IsAuthorized(string? level = null) {
            string? internalName;
            if (string.IsNullOrWhiteSpace(level))
                internalName = level = Manager.EditMode ? RoleDefinition.Edit : RoleDefinition.View;
            else
                internalName = (from r in RolesDefinitions where r.Name == level select r.InternalName).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(internalName))
                throw new InternalError("Permission level {0} not found in Roles", level);

            // module specific authorization
            return IsAuthorized((allowedRole) => {
                PropertyInfo? pi = ObjectSupport.TryGetProperty(allowedRole.GetType(), internalName);
                if (pi == null) throw new InternalError("Authorization role level {0} not found", level);
                return (AllowedEnum) pi.GetValue(allowedRole) !;
            }, (allowedUser) => {
                PropertyInfo? pi = ObjectSupport.TryGetProperty(allowedUser.GetType(), internalName);
                if (pi == null) throw new InternalError("Authorization user level {0} not found", level);
                return (AllowedEnum) pi.GetValue(allowedUser) !;
            });
        }

        public bool IsAuthorized_View_Anonymous() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetAnonymousRoleId());
        }
        public bool IsAuthorized_View_AnyUser() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetUserRoleId());
        }
        public bool IsAuthorized_View_Editor() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetEditorRoleId());
        }
        public bool IsAuthorized_View_Administrator() {
            return IsAuthorized_Role((allowedRole) => allowedRole.View, Resource.ResourceAccess.GetAdministratorRoleId());
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
        public List<PageDefinition> Pages { get; set; } = null!;

        public Task<List<PageDefinition>> __GetPagesAsync() {
            return PageDefinition.GetPagesFromModuleAsync(ModuleGuid);
        }


        // SEARCH
        // SEARCH
        // SEARCH

        public virtual void CustomSearch(ISearchWords searchWords) { }

        // VALIDATION
        // VALIDATION
        // VALIDATION

        /// <summary>
        /// Custom module settings validation used during Module Settings Edit/Save.
        /// </summary>
        /// <param name="modelState"></param>
        /// <param name="modelPrefix"></param>
        public virtual void CustomValidation(ModelState modelState, string modelPrefix) { }
    }
}
