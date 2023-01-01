/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
using YetaWF.Core.Views;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Base class for all controllers used by YetaWF (including "plain old" MVC controllers).
    /// </summary>
    [AreaConvention]
    public class YetaWFController : Microsoft.AspNetCore.Mvc.Controller {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(YetaWFController), name, defaultValue, parms); }

        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        ///  Update an area's view name with the complete area specifier.
        /// </summary>
        public static string MakeFullViewName(string? viewName, string area) {
            if (string.IsNullOrWhiteSpace(viewName))
                throw new InternalError("Missing view name");
            viewName = area + "_" + viewName;
            return viewName;
        }

        /// <summary>
        /// Returns the module definitions YetaWF.Core.Modules.ModuleDefinition for the current module implementing the controller (if any). Can be used with a base class to get the derived module's module definitions.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected virtual ModuleDefinition CurrentModule {
            get {
                if (_currentModule == null) throw new InternalError("No saved module");
                return _currentModule;
            }
            set {
                _currentModule = value;
            }

        }

        /// <summary>
        /// Returns the module definitions YetaWF.Core.Modules.ModuleDefinition for the current module implementing the controller. Can be used with a base class to get the derived module's module definitions.
        /// </summary>
        protected async Task<ModuleDefinition?> GetModuleAsync() {
            if (_currentModule == null) {
                ModuleDefinition? mod = null;
                mod = (ModuleDefinition?)RouteData.Values[Globals.RVD_ModuleDefinition];
                if (mod == null) {
                    if (Manager.IsGetRequest) {
                        string? moduleGuid = Manager.RequestQueryString[Basics.ModuleGuid];
                        if (string.IsNullOrWhiteSpace(moduleGuid))
                            return null;
                        Guid guid = new Guid(moduleGuid);
                        mod = await ModuleDefinition.LoadAsync(guid);
                    } else if (Manager.IsPostRequest) {
                        string? moduleGuid = Manager.RequestForm[Basics.ModuleGuid];
                        if (string.IsNullOrWhiteSpace(moduleGuid))
                            moduleGuid = Manager.RequestQueryString[Basics.ModuleGuid];
                        if (string.IsNullOrWhiteSpace(moduleGuid))
                            return null;
                        Guid guid = new Guid(moduleGuid);
                        mod = await ModuleDefinition.LoadAsync(guid);
                    }
                }
                if (mod == null)
                    throw new InternalError("No ModuleDefinition available in controller {0}", GetType().Namespace);
                CurrentModule = mod;
            }
            return _currentModule;
        }
        ModuleDefinition? _currentModule = null;

        protected ActionResult Reload_Page(string? popupText = null, string? popupTitle = null) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadPage);
                return new YJsonResult { Data = sb.ToString() };
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("$YetaWF.message({0}, {1}, function() {{ $YetaWF.reloadPage(true); }});", popupText, popupTitle);
                return new YJsonResult { Data = sb.ToString() };
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next) {

            Logging.AddTraceLog("Action Request - {0}", filterContext.Controller.GetType().FullName!);
            await SetupActionContextAsync(filterContext);

            if (Manager.IsPostRequest) {
                // find the unique Id prefix info
                string? uniqueIdCounters = null;
                if (HttpContext.Request.HasFormContentType)
                    uniqueIdCounters = HttpContext.Request.Form[Forms.UniqueIdCounters];
                if (string.IsNullOrEmpty(uniqueIdCounters))
                    uniqueIdCounters = HttpContext.Request.Query[Forms.UniqueIdCounters];
                if (!string.IsNullOrEmpty(uniqueIdCounters))
                    Manager.UniqueIdCounters = Utility.JsonDeserialize<YetaWFManager.UniqueIdInfo>(uniqueIdCounters);
            }

            await base.OnActionExecutionAsync(filterContext, next);
        }

        internal async Task SetupActionContextAsync(ActionExecutingContext filterContext) {

            await SetupEnvironmentInfoAsync();
            await GetModuleAsync();

            if (YetaWFManager.IsDemo || Manager.IsDemoUser) {
                // if this is a demo user and the action is marked with the ExcludeDemoMode Attribute, reject
                Type ctrlType = filterContext.Controller.GetType();
                string actionName = ((ControllerActionDescriptor)filterContext.ActionDescriptor).ActionName;
                MethodInfo? mi = ctrlType.GetMethod(actionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                ExcludeDemoModeAttribute? exclDemoAttr = (ExcludeDemoModeAttribute?)Attribute.GetCustomAttribute(mi!, typeof(ExcludeDemoModeAttribute));
                if (exclDemoAttr != null)
                    throw new Error("This action is not available in Demo mode.");
            }
        }

        /// <summary>
        /// Not authorized for this type of access.
        /// </summary>
        /// <returns></returns>
        protected ActionResult NotAuthorized() {
            Logging.AddErrorLog("Not Authorized");
            return new UnauthorizedResult();
        }
        /// <summary>
        /// Current request is marked 404 (Not Found).
        /// </summary>
        /// <remarks>The page and all modules are still rendered and processed.</remarks>
        protected void MarkNotFound() {
            Logging.AddErrorLog("404 Not Found");
            Manager.CurrentResponse.StatusCode = StatusCodes.Status404NotFound;
        }

        public static async Task SetupEnvironmentInfoAsync() {

            if (!Manager.LocalizationSupportEnabled) {// this only needs to be done once, so we gate on LocalizationSupportEnabled
                Manager.IsInPopup = InPopup();
                Manager.OriginList = GetOriginList();
                Manager.PageControlShown = PageControlShown();

                // determine user identity - authentication provider updates Manager with user information
                await Resource.ResourceAccess.ResolveUserAsync();

                // get user's default language
                Manager.GetUserLanguage();
                // only now can we enable resource loading
                Manager.LocalizationSupportEnabled = true;
            }
        }
        internal static bool GoingToPopup() {
            string? toPopup = null;
            try {
                toPopup = Manager.RequestForm[Globals.Link_ToPopup];
                if (toPopup == null)
                    toPopup = Manager.RequestQueryString[Globals.Link_ToPopup];
            } catch (Exception) { }
            return toPopup != null;
        }
        internal static bool InPopup() {
            string? inPopup = null;
            try {
                inPopup = Manager.RequestForm[Globals.Link_InPopup];
                if (inPopup == null)
                    inPopup = Manager.RequestQueryString[Globals.Link_InPopup];
            } catch (Exception) { }
            return inPopup != null;
        }
        internal static bool PageControlShown() {
            string? pageControlShown = null;
            try {
                pageControlShown = Manager.RequestForm[Globals.Link_PageControl];
                if (pageControlShown == null)
                    pageControlShown = Manager.RequestQueryString[Globals.Link_PageControl];
            } catch (Exception) { }
            return pageControlShown != null;
        }
        internal static bool GetTempEditMode() {
            if (!Manager.HaveUser)
                return false;
            try {
                string? editMode = Manager.RequestQueryString[Globals.Link_EditMode];
                if (editMode != null)
                    return true;
            } catch (Exception) { }
            return false;
        }
        internal static List<Origin> GetOriginList() {

            // Get info where we came from for return handling. We append the originlist when we
            // use links within our site. (We don't use UrlReferrer or the browser's history).
            // We're saving the origin list so we can return there once a form is completed (saved)
            // Because it relies on our own information it only works if we're navigating within our site.
            // If the user enters a direct Url or we can't determine where we're coming from, we usually use
            // the home page to return to.
            string? originList = null;
            try {
                originList = Manager.RequestForm[Globals.Link_OriginList];
                if (originList == null)
                    originList = Manager.RequestQueryString[Globals.Link_OriginList];
            } catch (Exception) { }
            if (!string.IsNullOrWhiteSpace(originList)) {
                try {
                    return Utility.JsonDeserialize<List<Origin>>(originList);
                } catch (Exception) {
                    throw new InternalError("Invalid Url arguments");
                }
            } else
                return new List<Origin>();
        }

        // GRID PARTIALVIEW
        // GRID PARTIALVIEW
        // GRID PARTIALVIEW

        public class GridPartialViewData {
            public string Data { get; set; } = null!;
            public string FieldPrefix { get; set; } = null!;
            public int Skip { get; set; }
            public int Take { get; set; }
            public bool Search { get; set; }
            public List<DataProviderSortInfo>? Sorts { get; set; }
            public List<DataProviderFilterInfo>? Filters { get; set; }

            /// <summary>
            /// Changes filter logic for string search.
            /// </summary>
            internal void UpdateSearchLogic() {
                if (Search && Filters != null) {
                    foreach (DataProviderFilterInfo filter in Filters)
                        filter.Logic = "||";
                }
            }
        }

        /// <summary>
        /// Returns an action result that renders grid contents as a partial view.
        /// </summary>
        /// <remarks>Used for Ajax grids.</remarks>
        /// <returns>Returns an action result that renders a grid as a partial view.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync(GridDefinition gridModel, GridPartialViewData gridPVData) {
            gridPVData.UpdateSearchLogic();
            DataSourceResult ds = await gridModel.DirectDataAsync(gridPVData.Skip, gridPVData.Take, gridPVData.Sorts?.ToList(), gridPVData.Filters?.ToList());// copy sort/filter in case changes are made (we save this later)
            return await GridPartialViewAsync(gridModel, ds, null, gridPVData.FieldPrefix, gridPVData.Skip, gridPVData.Take, gridPVData.Sorts, gridPVData.Filters, gridPVData.Search);
        }

        /// <summary>
        /// Returns an action result that renders grid contents as a partial view.
        /// </summary>
        /// <remarks>Used for static grids.</remarks>
        /// <returns>Returns an action result that renders a grid as a partial view.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync<TYPE>(GridDefinition gridModel, GridPartialViewData gridPVData) {
            List<TYPE> list = Utility.JsonDeserialize<List<TYPE>>(gridPVData.Data);
            List<object> objList = (from l in list select (object)l).ToList();
            gridPVData.UpdateSearchLogic();
            DataSourceResult ds = gridModel.SortFilterStaticData!(objList, 0, int.MaxValue, gridPVData.Sorts?.ToList(), gridPVData.Filters?.ToList());// copy sort/filter in case changes are made (we save this later)
            return await GridPartialViewAsync(gridModel, ds, objList, gridPVData.FieldPrefix, gridPVData.Skip, gridPVData.Take, gridPVData.Sorts, gridPVData.Filters, gridPVData.Search);
        }

        /// <summary>
        /// Returns an action result that renders grid contents as a partial view.
        /// </summary>
        /// <remarks>Returns an action result that renders a grid as a partial view.</remarks>
        private Task<PartialViewResult> GridPartialViewAsync(GridDefinition gridModel, DataSourceResult data, List<object>? staticData, string fieldPrefix, int skip, int take, List<DataProviderSortInfo>? sorts, List<DataProviderFilterInfo>? filters, bool search) {
            GridPartialData gridPartialModel = new GridPartialData() {
                Data = data,
                StaticData = staticData,
                Skip = skip,
                Take = take,
                Sorts = sorts,
                Filters = filters,
                Search = search,
                FieldPrefix = fieldPrefix,
                GridDef = gridModel,
            };
            return Task.FromResult(PartialView("GridPartialDataView", gridPartialModel, ContentType: "application/json", PureContent: true, AreaViewName: false, Gzip: true));
        }
        protected Task<PartialViewResult> GridRecordViewAsync(GridRecordData model) {
            return Task.FromResult(PartialView("GridRecord", model, ContentType: "application/json", PureContent: true, AreaViewName: false, Gzip: true));
        }

        // TREE PARTIALVIEW
        // TREE PARTIALVIEW
        // TREE PARTIALVIEW

        /// <summary>
        /// Returns an action result that renders tree contents as a partial view.
        /// </summary>
        /// <remarks>Used for tree components.</remarks>
        /// <returns>Returns an action result that renders tree contents as a partial view.</returns>
        protected Task<PartialViewResult> TreePartialViewAsync<TYPE>(TreeDefinition treeModel, List<TYPE> list) {
            List<object> data = (from l in list select (object)l).ToList<object>();
            DataSourceResult ds = new DataSourceResult() {
                Data = data,
                Total = data.Count,
            };
            TreePartialData treePartial = new TreePartialData {
                TreeDef = treeModel,
                Data = ds,
            };
            return Task.FromResult(PartialView("TreePartialDataView", treePartial, ContentType: "application/json", PureContent: true, AreaViewName: false, Gzip: true));
        }

        // PARTIAL VIEW
        // PARTIAL VIEW
        // PARTIAL VIEW

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="Script">Optional JavaScript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view as a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
        /// <param name="AreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <param name="Gzip">Defines whether the returned content is GZIPed.</param>
        /// <returns>Returns an action to render a partial view.</returns>
        protected PartialViewResult PartialView(ScriptBuilder? Script = null, string? ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false, bool ForcePopup = false) {
            return PartialView(null /* viewName */, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip, ForcePopup: ForcePopup);
        }

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="Script">Optional JavaScript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view as a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
        /// <param name="AreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <param name="Gzip">Defines whether the returned content is GZIPed.</param>
        /// <returns>Returns an action to render a partial view.</returns>
        protected PartialViewResult PartialView(object? model, ScriptBuilder? Script = null, string? ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false, bool ForcePopup = false) {
            return PartialView(null /* viewName */, model, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip, ForcePopup: ForcePopup);
        }

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="viewName">The name of the partial view.</param>
        /// <param name="Script">Optional JavaScript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view as a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
        /// <param name="AreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <param name="Gzip">Defines whether the returned content is GZIPed.</param>
        /// <returns>Returns an action to render a partial view.</returns>
        protected PartialViewResult PartialView(string viewName, ScriptBuilder? Script = null, string? ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false, bool ForcePopup = false) {
            return PartialView(viewName, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip, ForcePopup: ForcePopup);
        }

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="viewName">The name of the partial view.</param>
        /// <param name="model">The model.</param>
        /// <param name="Script">Optional JavaScript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view as a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
        /// <param name="AreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <param name="Gzip">Defines whether the returned content is GZIPed.</param>
        /// <returns>Returns an action to render a partial view.</returns>
        protected PartialViewResult PartialView(string? viewName, object? model, ScriptBuilder? Script = null, string? ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false, bool ForcePopup = false) {

            if (model != null)
                ViewData.Model = model;

            return new PartialViewResult {
                ViewName = viewName,
                ViewData = ViewData,
                Module = CurrentModule,
                Script = Script,
                ContentType = ContentType,
                PureContent = PureContent,
                AreaViewName = AreaViewName,
                Gzip = Gzip,
                ForcePopup = ForcePopup,
            };
        }
        /// <summary>
        /// An action result to render a partial view.
        /// </summary>
        public class PartialViewResult : Microsoft.AspNetCore.Mvc.PartialViewResult {
            /// <summary>
            /// The YetaWFManager instance for the current HTTP request.
            /// </summary>
            protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

            private const string DefaultContentType = "text/html";

            /// <summary>
            /// Constructor.
            /// </summary>
            public PartialViewResult() { }

            /// <summary>
            /// The current module being rendered by this partial view.
            /// </summary>
            public ModuleDefinition Module { get; set; } = null!;
            /// <summary>
            /// The JavaScript to be executed client-side after the partial view has been rendered.
            /// </summary>
            public ScriptBuilder? Script { get; set; }

            public bool PureContent { get; set; }
            public bool AreaViewName { get; set; }
            public bool Gzip { get; set; }
            public bool ForcePopup { get; set; }

            private static readonly Regex reEndDiv = new Regex(@"</div>\s*$"); // very last div

            /// <summary>
            /// Renders the view.
            /// </summary>
            /// <param name="context">The action context.</param>
            public override async Task ExecuteResultAsync(ActionContext context) {
                Manager.Verify_PostRequest();
                Manager.NextUniqueIdPrefix();// get the next unique id prefix (so we don't have any conflicts when replacing modules)

                if (context == null)
                    throw new ArgumentNullException("context");
                if (AreaViewName) {
                    if (Module == null) throw new InternalError("Can't use AreaViewName without module context");
                    if (String.IsNullOrEmpty(ViewName)) {
                        if (!string.IsNullOrWhiteSpace(Module.DefaultViewName))
                            ViewName = Module.DefaultViewName + YetaWFViewExtender.PartialSuffix;
                    } else {
                        ViewName = YetaWFController.MakeFullViewName(ViewName, Module.AreaName);
                    }
                    if (string.IsNullOrWhiteSpace(ViewName)) {
                        ViewName = (string?)context.RouteData.Values["action"];
                        ViewName = YetaWFController.MakeFullViewName(ViewName, Module.AreaName);
                    }
                }
                if (string.IsNullOrWhiteSpace(ViewName))
                    throw new InternalError("Invalid action");

                HttpResponse response = context.HttpContext.Response;
                if (!string.IsNullOrEmpty(ContentType))
                    response.ContentType = ContentType;

                ModuleDefinition? oldMod = Manager.CurrentModule;
                Manager.CurrentModule = Module;

                string viewHtml;
                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb)) {

                    YHtmlHelper htmlHelper = new YHtmlHelper(context, context.ModelState);

                    context.RouteData.Values.Add(Globals.RVD_ModuleDefinition, Module);//$$ needed?

                    bool inPartialView = Manager.InPartialView;
                    Manager.InPartialView = true;
                    bool wantFocus = Manager.WantFocus;
                    Manager.WantFocus = Module.WantFocus;
                    try {
                        viewHtml = await htmlHelper.ForViewAsync(base.ViewName, Module, Model);
                    } catch (Exception) {
                        throw;
                    } finally {
                        Manager.InPartialView = inPartialView;
                        Manager.WantFocus = wantFocus;
                    }

                    viewHtml = await PostRenderAsync(htmlHelper, context, viewHtml);
                }
#if DEBUG
                if (sb.Length > 0)
                    throw new InternalError($"View {ViewName} wrote output using HtmlHelper, which is not supported - All output must be rendered using ForViewAsync and returned as a string - output rendered: \"{sb.ToString()}\"");
#endif

                Manager.CurrentModule = oldMod;

                byte[] btes = Encoding.UTF8.GetBytes(viewHtml);
                await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
            }

            private async Task<string> PostRenderAsync(YHtmlHelper htmlHelper, ActionContext context, string viewHtml) {

                HttpResponse response = context.HttpContext.Response;

                // if the controller specified a content type, only return the exact response
                // if the controller didn't specify a content type and the content type is text/html, add all the other goodies
                if (!PureContent && string.IsNullOrEmpty(ContentType) && (response.ContentType == null || response.ContentType == DefaultContentType)) {

                    if (Module == null) throw new InternalError("Must use PureContent when no module context is available");

                    if (response.ContentType == null)
                        response.ContentType = DefaultContentType;// otherwise we get complaints from FireFox

                    Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);

                    if (Manager.CurrentPage != null) Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);
                    Manager.AddOnManager.AddExplicitlyInvokedModules(Module.ReferencedModules);

                    if (ForcePopup)
                        viewHtml += "<script>YVolatile.Basics.ForcePopup=true;</script>";

                    viewHtml += (await htmlHelper.RenderReferencedModule_AjaxAsync()).ToString();
                    viewHtml = await PostProcessView.ProcessAsync(htmlHelper, Module, viewHtml);

                    if (Script != null)
                        Manager.ScriptManager.AddLastWhenReadyOnce(Script);

                    if (Manager.UniqueIdCounters.IsTracked)
                        Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdCounters", Manager.UniqueIdCounters);

                    // add generated scripts
                    string js = await Manager.ScriptManager.RenderVolatileChangesAsync() ?? "";
                    js += await Manager.ScriptManager.RenderAjaxAsync() ?? "";

                    viewHtml = reEndDiv.Replace(viewHtml, js + "</div>", 1);

                    // DEBUG: viewHtml is the complete response to the Ajax request

                    viewHtml = WhiteSpaceResponseFilter.Compress(viewHtml);
                }
                return viewHtml;
            }
        }
    }
}
