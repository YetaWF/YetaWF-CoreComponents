/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
    public class YetaWFController :
#if MVC6
                                    Microsoft.AspNetCore.Mvc.Controller
#else
                                    Controller
#endif
    {
        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
#else
        /// <summary>
        /// Constructor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected YetaWFController() {
            // Don't perform html char validation (it's annoying) - This is the equivalent of adding [ValidateInput(false)] on every controller.
            // This also means we don't need AllowHtml attributes
            ValidateRequest = false;
            AllowJavascriptResult = true;
        }
#endif
        /// <summary>
        ///  Update an area's view name with the complete area specifier.
        /// </summary>
        public static string MakeFullViewName(string viewName, string area) {
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
        protected async Task<ModuleDefinition> GetModuleAsync() {
            if (_currentModule == null) {
                ModuleDefinition mod = null;
                mod = (ModuleDefinition)RouteData.Values[Globals.RVD_ModuleDefinition];
                if (mod == null) {
                    if (Manager.IsGetRequest) {
                        string moduleGuid = Manager.RequestQueryString[Basics.ModuleGuid];
                        if (string.IsNullOrWhiteSpace(moduleGuid))
                            return null;
                        Guid guid = new Guid(moduleGuid);
                        mod = await ModuleDefinition.LoadAsync(guid);
                    } else if (Manager.IsPostRequest) {
                        string moduleGuid = Manager.RequestForm[Basics.ModuleGuid];
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
        ModuleDefinition _currentModule = null;

        protected ActionResult Reload_Page(string popupText = null, string popupTitle = null) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadPage);
                return new YJsonResult { Data = sb.ToString() };
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? this.__ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("$YetaWF.alert({0}, {1}, function() {{ $YetaWF.reloadPage(true); }});", popupText, popupTitle);
                return new YJsonResult { Data = sb.ToString() };
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next) {

            Logging.AddTraceLog("Action Request - {0}", filterContext.Controller.GetType().FullName);
            await SetupActionContextAsync(filterContext);

            if (Manager.IsPostRequest) {
                // find the unique Id prefix info
                string uniqueIdCounters = null;
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
                MethodInfo mi = ctrlType.GetMethod(actionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                ExcludeDemoModeAttribute exclDemoAttr = (ExcludeDemoModeAttribute)Attribute.GetCustomAttribute(mi, typeof(ExcludeDemoModeAttribute));
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
            Manager.CurrentResponse.StatusCode = 404;
        }

        public static async Task SetupEnvironmentInfoAsync() {

            if (!Manager.LocalizationSupportEnabled) {// this only needs to be done once, so we gate on LocalizationSupportEnabled
                GetCharSize();
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
        internal static void GetCharSize() {
            string wh = null;
            try {
                wh = Manager.RequestForm[Globals.Link_CharInfo];
                if (wh == null)
                    wh = Manager.RequestQueryString[Globals.Link_CharInfo];
            } catch (Exception) { }
            int width = 0, height = 0;
            if (!string.IsNullOrWhiteSpace(wh)) {
                string[] parts = wh.Split(new char[] { ',' });
                width = Convert.ToInt32(parts[0]);
                height = Convert.ToInt32(parts[1]);
            }
            if (width > 0 && height > 0) {
                Manager.NewCharSize(width, height);
            }
        }
        internal static bool GoingToPopup() {
            string toPopup = null;
            try {
                toPopup = Manager.RequestForm[Globals.Link_ToPopup];
                if (toPopup == null)
                    toPopup = Manager.RequestQueryString[Globals.Link_ToPopup];
            } catch (Exception) { }
            return toPopup != null;
        }
        internal static bool InPopup() {
            string inPopup = null;
            try {
                inPopup = Manager.RequestForm[Globals.Link_InPopup];
                if (inPopup == null)
                    inPopup = Manager.RequestQueryString[Globals.Link_InPopup];
            } catch (Exception) { }
            return inPopup != null;
        }
        internal static bool PageControlShown() {
            string pageControlShown = null;
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
                string editMode = Manager.RequestQueryString[Globals.Link_EditMode];
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
            string originList = null;
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
            public string Data { get; set; }
            public string FieldPrefix { get; set; }
            public int Skip { get; set; }
            public int Take { get; set; }
            public List<DataProviderSortInfo> Sorts { get; set; }
            public List<DataProviderFilterInfo> Filters { get; set; }
        }

        /// <summary>
        /// Returns an action result that renders grid contents as a partial view.
        /// </summary>
        /// <remarks>Used for Ajax grids.</remarks>
        /// <returns>Returns an action result that renders a grid as a partial view.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync(GridDefinition gridModel, GridPartialViewData gridPVData) {
            DataSourceResult ds = await gridModel.DirectDataAsync(gridPVData.Skip, gridPVData.Take, gridPVData.Sorts?.ToList(), gridPVData.Filters?.ToList());// copy sort/filter in case changes are made (we save this later)
            return await GridPartialViewAsync(gridModel, ds, null, gridPVData.FieldPrefix, gridPVData.Skip, gridPVData.Take, gridPVData.Sorts, gridPVData.Filters);
        }

        /// <summary>
        /// Returns an action result that renders grid contents as a partial view.
        /// </summary>
        /// <remarks>Used for static grids.</remarks>
        /// <returns>Returns an action result that renders a grid as a partial view.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync<TYPE>(GridDefinition gridModel, GridPartialViewData gridPVData) {
            List<TYPE> list = Utility.JsonDeserialize<List<TYPE>>(gridPVData.Data);
            List<object> objList = (from l in list select (object)l).ToList();
            DataSourceResult ds = gridModel.SortFilterStaticData(objList, 0, int.MaxValue, gridPVData.Sorts?.ToList(), gridPVData.Filters?.ToList());// copy sort/filter in case changes are made (we save this later)
            return await GridPartialViewAsync(gridModel, ds, objList, gridPVData.FieldPrefix, gridPVData.Skip, gridPVData.Take, gridPVData.Sorts, gridPVData.Filters);
        }

        /// <summary>
        /// Returns an action result that renders grid contents as a partial view.
        /// </summary>
        /// <remarks>Returns an action result that renders a grid as a partial view.</remarks>
        private Task<PartialViewResult> GridPartialViewAsync(GridDefinition gridModel, DataSourceResult data, List<object> staticData, string fieldPrefix, int skip, int take, List<DataProviderSortInfo> sorts, List<DataProviderFilterInfo> filters) {
            GridPartialData gridPartialModel = new GridPartialData() {
                Data = data,
                StaticData = staticData,
                Skip = skip,
                Take = take,
                Sorts = sorts,
                Filters = filters,
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
        protected PartialViewResult PartialView(ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {
            return PartialView(null /* viewName */, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip);
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
        protected PartialViewResult PartialView(object model, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {
            return PartialView(null /* viewName */, model, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip);
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
        protected PartialViewResult PartialView(string viewName, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {
            return PartialView(viewName, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip);
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
        protected PartialViewResult PartialView(string viewName, object model, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {

            if (model != null)
                ViewData.Model = model;

            return new PartialViewResult {
                ViewName = viewName,
                ViewData = ViewData,
#if MVC6
#else
                ViewEngineCollection = ViewEngineCollection,
#endif
                Module = CurrentModule,
                Script = Script,
                ContentType = ContentType,
                PureContent = PureContent,
                AreaViewName = AreaViewName,
                Gzip = Gzip,
            };
        }
        /// <summary>
        /// An action result to render a partial view.
        /// </summary>
#if MVC6
        public class PartialViewResult : Microsoft.AspNetCore.Mvc.PartialViewResult {
#else
        public class PartialViewResult : System.Web.Mvc.PartialViewResult {
#endif
            /// <summary>
            /// The YetaWFManager instance for the current HTTP request.
            /// </summary>
            protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

            private const string DefaultContentType = "text/html";

            /// <summary>
            /// Constructor.
            /// </summary>
#if MVC6
            public PartialViewResult() { }
#else
            public PartialViewResult() { }
#endif
            /// <summary>
            /// The current module being rendered by this partial view.
            /// </summary>
            public ModuleDefinition Module { get; set; }
            /// <summary>
            /// The JavaScript to be executed client-side after the partial view has been rendered.
            /// </summary>
            public ScriptBuilder Script { get; set; }
#if MVC6
#else
            /// <summary>
            /// The content type. If not specified, the default is "text/html".
            /// </summary>
            public string ContentType { get; set; }
#endif
            public bool PureContent { get; set; }
            public bool AreaViewName { get; set; }
            public bool Gzip { get; set; }

            private static readonly Regex reEndDiv = new Regex(@"</div>\s*$"); // very last div

            /// <summary>
            /// Renders the view.
            /// </summary>
            /// <param name="context">The action context.</param>
#if MVC6
            public override async Task ExecuteResultAsync(ActionContext context) {
#else
            public override void ExecuteResult(ControllerContext context) {
#endif
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
#if MVC6
                        ViewName = (string)context.RouteData.Values["action"];
#else
                        ViewName = context.RouteData.GetRequiredString("action");
#endif
                        ViewName = YetaWFController.MakeFullViewName(ViewName, Module.AreaName);
                    }
                }
                if (string.IsNullOrWhiteSpace(ViewName))
                    throw new InternalError("Invalid action");
#if MVC6
                HttpResponse response = context.HttpContext.Response;
#else
                HttpResponseBase response = context.HttpContext.Response;
#endif
                if (!string.IsNullOrEmpty(ContentType))
                    response.ContentType = ContentType;

                ModuleDefinition oldMod = Manager.CurrentModule;
                Manager.CurrentModule = Module;

                string viewHtml;
                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb)) {

                    YHtmlHelper htmlHelper =
#if MVC6
                        new YHtmlHelper(context, context.ModelState);
#else
                        new YHtmlHelper(context.RequestContext, context.Controller.ViewData.ModelState);
#endif

                    context.RouteData.Values.Add(Globals.RVD_ModuleDefinition, Module);//$$ needed?

                    bool inPartialView = Manager.InPartialView;
                    Manager.InPartialView = true;
                    bool wantFocus = Manager.WantFocus;
                    Manager.WantFocus = Module.WantFocus;
                    try {
#if MVC6
                        viewHtml = await htmlHelper.ForViewAsync(base.ViewName, Module, Model);
#else
                        viewHtml = YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you here :-(
                            return await htmlHelper.ForViewAsync(base.ViewName, Module, Model);
                        });
#endif
                    } catch (Exception) {
                        throw;
                    } finally {
                        Manager.InPartialView = inPartialView;
                        Manager.WantFocus = wantFocus;
                    }
#if MVC6
                    viewHtml = await PostRenderAsync(htmlHelper, context, viewHtml);
#else
                    YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you :-(
                        viewHtml = await PostRenderAsync(htmlHelper, context, viewHtml);
                    });
#endif
                }
#if DEBUG
                if (sb.Length > 0)
                    throw new InternalError($"View {ViewName} wrote output using HtmlHelper, which is not supported - All output must be rendered using ForViewAsync and returned as a string - output rendered: \"{sb.ToString()}\"");
#endif

                Manager.CurrentModule = oldMod;

                if (Gzip) {
                    // if gzip was explicitly requested, return zipped (this is rarely used as most responses are compressed based on iis settings/middleware)
                    // we use this to explicitly return certain json responses compressed (not all, as small responses don't warrant compression).
#if MVC6
                    // gzip encoding is performed by middleware
#else
                    context.HttpContext.Response.AppendHeader("Content-encoding", "gzip");
                    context.HttpContext.Response.Filter = new GZipStream(context.HttpContext.Response.Filter, CompressionMode.Compress);
#endif
                }
#if MVC6
                byte[] btes = Encoding.ASCII.GetBytes(viewHtml);
                await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
                response.Output.Write(viewHtml);
#endif
            }

#if MVC6
            private async Task<string> PostRenderAsync(YHtmlHelper htmlHelper, ActionContext context, string viewHtml)
#else
            private async Task<string> PostRenderAsync(YHtmlHelper htmlHelper, ControllerContext context, string viewHtml)
#endif
            {
#if MVC6
                HttpResponse response = context.HttpContext.Response;
#else
                HttpResponseBase response = context.HttpContext.Response;
#endif
                // if the controller specified a content type, only return the exact response
                // if the controller didn't specify a content type and the content type is text/html, add all the other goodies
#if MVC6
                if (!PureContent && string.IsNullOrEmpty(ContentType) && (response.ContentType == null || response.ContentType == DefaultContentType))
#else
                if (!PureContent && string.IsNullOrEmpty(ContentType) && response.ContentType == DefaultContentType)
#endif
                {
                    if (Module == null) throw new InternalError("Must use PureContent when no module context is available");

                    Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);

                    if (Manager.CurrentPage != null) Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);
                    Manager.AddOnManager.AddExplicitlyInvokedModules(Module.ReferencedModules);

                    viewHtml = viewHtml + (await htmlHelper.RenderReferencedModule_AjaxAsync()).ToString();
                    viewHtml = await PostProcessView.ProcessAsync(htmlHelper, Module, viewHtml);

                    if (Script != null)
                        Manager.ScriptManager.AddLastDocumentReady(Script);

                    if (Manager.UniqueIdCounters.IsTracked)
                        Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdCounters", Manager.UniqueIdCounters);

                    // add generated scripts
                    string js = await Manager.ScriptManager.RenderVolatileChangesAsync() ?? "";
                    js += await Manager.ScriptManager.RenderAjaxAsync() ?? "";

                    viewHtml = reEndDiv.Replace(viewHtml, js + "</div>", 1);

                    // DEBUG: viewHtml is the complete response to the Ajax request

                    if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.Compression)
                        viewHtml = WhiteSpaceResponseFilter.Compress(Manager, viewHtml);
                }
                return viewHtml;
            }
        }
    }
}
