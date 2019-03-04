/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
using YetaWF.Core.Modules;
using YetaWF.Core.Models;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using System.IO;
using YetaWF.Core.Views;
using YetaWF.Core.DataProvider;
using System.Linq;
#if MVC6
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using YetaWF.Core.Pages;
#else
using System.IO.Compression;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
#endif

namespace YetaWF.Core.Controllers
{

    /// <summary>
    /// Base class for all controllers used by YetaWF (including "plain old" MVC controllers).
    /// </summary>
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
        /// Defines whether the action can return a JavaScript result to be executed client-side.
        /// </summary>
        /// <remarks>Most actions can accept a JavaScript result which is executed client-side.
        /// For some "plain old" MVC controllers, a JavaScript result is not acceptable, so these need to override and return false.</remarks>
        public virtual bool AllowJavascriptResult { get; set; }

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

#if MVC6
        // Handled identically in ErrorHandlingMiddleware
#else
        /// <summary>
        /// Handles exceptions and returns suitable error info.
        /// </summary>
        /// <param name="filterContext">The exception context.</param>
        protected override void OnException(ExceptionContext filterContext) {

            // log the error
            Exception exc = filterContext.Exception;
            string msg = "(unknown)";
            if (exc != null) {
                msg = ErrorHandling.FormatExceptionMessage(exc);
                Logging.AddErrorLog(msg);
            }
            if (!YetaWFManager.HaveManager || !Manager.IsPostRequest || !AllowJavascriptResult) {
                if (Manager.CurrentModule != null) { // we're rendering a module, let module handle its own error
                    throw filterContext.Exception;
                } else { // this was a direct action GET so we need to show an error page
                    Server.ClearError(); // this clears the current 500 error (if customErrors is on in web config we would get a 500 - Internal Server Error at this point
                    filterContext.HttpContext.Response.Clear(); // this prob doesn't do much
                    filterContext.HttpContext.Response.StatusCode = 200;
                    filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                    filterContext.ExceptionHandled = true;
                    RedirectResult redir = Redirect(MessageUrl(msg, 500));
                    redir.ExecuteResult(filterContext);
                    return;
                }
            }

            // for post/ajax requests, respond in a way we can display the error
            Server.ClearError(); // this clears the current 500 error (if customErrors is on in web config we would get a 500 - Internal Server Error at this point
            filterContext.HttpContext.Response.Clear(); // this prob doesn't do much
            filterContext.HttpContext.Response.StatusCode = 200;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
            filterContext.ExceptionHandled = true;
            YJsonResult cr = new YJsonResult { Data = Basics.AjaxJavascriptErrorReturn + $"$YetaWF.error({YetaWFManager.JsonSerialize(msg)});" };
            cr.ExecuteResult(filterContext);
        }
        /// <summary>
        /// Redirects to a page that displaus a message - THIS ONLY WORKS FOR A GET REQUEST.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>The URL to redirect to.</returns>
        private string MessageUrl(string message, int statusCode) {
            // we're in a GET request without module, so all we can do is redirect and show the message in the ShowMessage module
            // the ShowMessage module is in the Basics package and we reference it by permanent Guid
            string url = YetaWFManager.Manager.CurrentSite.MakeUrl(ModuleDefinition.GetModulePermanentUrl(new Guid("{b486cdfc-3726-4549-889e-1f833eb49865}")));
            QueryHelper query = QueryHelper.FromUrl(url, out url);
            query["Message"] = message;
            query["Code"] = statusCode.ToString();
            return query.ToUrl(url);
        }
#endif

#if MVC6
        public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next) {
            Logging.AddTraceLog("Action Request - {0}", filterContext.Controller.GetType().FullName);
            await SetupActionContextAsync(filterContext);
            await base.OnActionExecutionAsync(filterContext, next);
        }
#else
        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            Logging.AddTraceLog("Action Request - {0}", filterContext.ActionDescriptor.ControllerDescriptor.ControllerType.FullName);
            YetaWFManager.Syncify(async () => { // Sorry MVC5 no async for you
                await SetupActionContextAsync(filterContext);
            });
            base.OnActionExecuting(filterContext);
        }
#endif
        internal async Task SetupActionContextAsync(ActionExecutingContext filterContext) {
            await SetupEnvironmentInfoAsync();
            await GetModuleAsync();
            // if this is a demo and the action is marked with the ExcludeDemoMode Attribute, reject
            if (Manager.IsDemo) {
#if MVC6
                Type ctrlType = filterContext.Controller.GetType();
                string actionName = ((ControllerActionDescriptor)filterContext.ActionDescriptor).ActionName;
                MethodInfo mi = ctrlType.GetMethod(actionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
#else
                MethodInfo mi = filterContext.ActionDescriptor.ControllerDescriptor.ControllerType.GetMethod(filterContext.ActionDescriptor.ActionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
#endif
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
#if MVC6
            return new UnauthorizedResult();
#else
            return new HttpUnauthorizedResult();
#endif
        }
        /// <summary>
        /// Current request is marked 404 (Not Found).
        /// </summary>
        /// <remarks>The page and all modules are still rendered and processed.</remarks>
        protected void MarkNotFound() {
#if MVC6
            Logging.AddErrorLog("404 Not Found");
#else
            Manager.CurrentResponse.Status = Logging.AddErrorLog("404 Not Found");
#endif
            Manager.CurrentResponse.StatusCode = 404;
        }

#if MVC6
        // This is handled in ResourceAuthorizeHandler
#else
        protected override void OnAuthentication(AuthenticationContext filterContext) {
            YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you :-(
                await YetaWFController.SetupEnvironmentInfoAsync();
            });
            base.OnAuthentication(filterContext);
        }
#endif
        public static async Task SetupEnvironmentInfoAsync() {

            if (!Manager.LocalizationSupportEnabled) {// this only needs to be done once, so we gate on LocalizationSupportEnabled
                GetCharSize();
                Manager.IsInPopup = InPopup();
                Manager.OriginList = GetOriginList();
                Manager.PageControlShown = PageControlShown();

                // determine user identity - authentication provider updates Manager with user information
                await Resource.ResourceAccess.ResolveUserAsync();

                Manager.EditMode = GetTempEditMode();

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
                    return YetaWFManager.JsonDeserialize<List<Origin>>(originList);
                } catch (Exception) {
                    throw new InternalError("Invalid Url arguments");
                }
            } else
                return new List<Origin>();
        }

        // GRID PARTIALVIEW
        // GRID PARTIALVIEW
        // GRID PARTIALVIEW

        /// <summary>
        /// Returns an action result that renders a grid as a partial view.
        /// </summary>
        /// <remarks>Used for static grids.</remarks>
        /// <returns>Returns an action result that renders a grid as a partial view.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync<TYPE>(GridDefinition gridModel, string data, string fieldPrefix, int skip, int take, List<DataProviderSortInfo> sorts, List<DataProviderFilterInfo> filters) {
            // save settings
            YetaWF.Core.Components.Grid.SaveSettings(skip, take, sorts, filters, gridModel.SettingsModuleGuid);
            List<TYPE> list = YetaWFManager.JsonDeserialize<List<TYPE>>(data);
            List<object> objList = (from l in list select (object)l).ToList();
            DataSourceResult ds = gridModel.SortFilterStaticData(objList, 0, int.MaxValue, sorts, filters);
            return await GridPartialViewAsync(gridModel, ds, objList, fieldPrefix, skip, take, sorts, filters);
        }
        /// <summary>
        /// Returns an action result that renders a grid as a partial view.
        /// </summary>
        /// <remarks>Used for Ajax grids.</remarks>
        /// <returns>Returns an action result that renders a grid as a partial view.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync(GridDefinition gridModel, string fieldPrefix, int skip, int take, List<DataProviderSortInfo> sorts, List<DataProviderFilterInfo> filters) {
            // save settings
            YetaWF.Core.Components.Grid.SaveSettings(skip, take, sorts, filters, gridModel.SettingsModuleGuid);
            DataSourceResult data = await gridModel.DirectDataAsync(skip, take, sorts, filters);
            return await GridPartialViewAsync(gridModel, data, null, fieldPrefix, skip, take, sorts, filters);
        }
        /// <summary>
        /// Returns an action result that renders a grid as a partial view.
        /// </summary>
        /// <remarks>Returns an action result that renders a grid as a partial view.</remarks>
        protected async Task<PartialViewResult> GridPartialViewAsync(GridDefinition gridModel, DataSourceResult data, List<object> staticData, string fieldPrefix, int skip, int take, List<DataProviderSortInfo> sorts, List<DataProviderFilterInfo> filters) {
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
            // handle async properties
            await HandlePropertiesAsync(gridPartialModel.Data.Data);
            // render
            return PartialView("GridPartialData", gridPartialModel, ContentType: "application/json", PureContent: true, AreaViewName: false, Gzip: true);
        }
        protected Task<PartialViewResult> GridRecordViewAsync(GridRecordData model) {
            return Task.FromResult(PartialView("GridRecord", model, ContentType: "application/json", PureContent: true, AreaViewName: false, Gzip: true));
        }

        public static async Task HandlePropertiesAsync(List<object> data) {
            foreach (object item in data)
                await HandlePropertiesAsync(item);
        }
        public static async Task HandlePropertiesAsync(object data) {
            await ObjectSupport.HandlePropertyAsync<MenuList>("Commands", "__GetCommandsAsync", data);
        }

        // PARTIAL VIEW
        // PARTIAL VIEW
        // PARTIAL VIEW

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="Script">Optional JavaScript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
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
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
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
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
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
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, JavaScript, etc.</param>
        /// <param name="AreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <param name="Gzip">Defines whether the returned content is GZIPed.</param>
        /// <returns>Returns an action to render a partial view.</returns>
        protected PartialViewResult PartialView(string viewName, object model, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {

            if (model != null) {
                ViewData.Model = model;
            }

            return new PartialViewResult {
                ViewName = viewName,
                ViewData = ViewData,
                TempData = TempData,
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

            private static readonly Regex reEndDiv = new Regex(@"</\s*div\s*>\s*$"); // very last div

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

#if MVC6
                    IHtmlHelper htmlHelper = context.HttpContext.RequestServices.GetRequiredService<IHtmlHelper>();
                    if (htmlHelper is IViewContextAware contextable) {
                        ViewContext viewContext = new ViewContext(context, NullView.Instance, this.ViewData, this.TempData, sw, new HtmlHelperOptions());
                        contextable.Contextualize(viewContext);
                    }
#else
                    ViewContext vc = new ViewContext(context, new ViewImpl(), context.Controller.ViewData, context.Controller.TempData, sw);
                    IViewDataContainer vdc = new ViewDataContainer() { ViewData = context.Controller.ViewData };
                    HtmlHelper htmlHelper = new HtmlHelper(vc, vdc);
#endif
                    context.RouteData.Values.Add(Globals.RVD_ModuleDefinition, Module);

                    bool inPartialView = Manager.InPartialView;
                    Manager.InPartialView = true;
                    try {
#if MVC6
                        viewHtml = (await htmlHelper.ForViewAsync(base.ViewName, Module, Model)).ToString();
#else
                        viewHtml = YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you :-(
                            return (await htmlHelper.ForViewAsync(base.ViewName, Module, Model)).ToString();
                        });
#endif
                    } catch (Exception) {
                        throw;
                    } finally {
                        Manager.InPartialView = inPartialView;
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
                    throw new InternalError($"View {ViewName} wrote output using HtmlHelper, which is not supported - All output must be rendered using ForViewAsync and returned as a {nameof(YHtmlString)} - output rendered: \"{sb.ToString()}\"");
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
#else
            private class ViewDataContainer : IViewDataContainer {
                public ViewDataDictionary ViewData { get; set; }
            }
            private class ViewImpl : IView {
                public void Render(ViewContext viewContext, TextWriter writer) {
                    throw new NotImplementedException();
                }
            }
#endif

#if MVC6
            private async Task<string> PostRenderAsync(IHtmlHelper htmlHelper, ActionContext context, string viewHtml)
#else
            private async Task<string> PostRenderAsync(HtmlHelper htmlHelper, ControllerContext context, string viewHtml)
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

                    // add generated scripts
                    string js = (await Manager.ScriptManager.RenderAjaxAsync()).ToString();
                    if (string.IsNullOrWhiteSpace(js))
                        js = "";

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
