/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
using YetaWF.Core.Views.Shared;
using YetaWF.Core.Models;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Addons;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using YetaWF.Core.Pages;
#else
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using System.Web.Mvc.Html;
using YetaWF.Core.Addons;
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
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
#else
        /// <summary>
        /// Constructor.
        /// </summary>
        protected YetaWFController() {
            // Don't perform html char validation (it's annoying) - This is the equivalent of adding [ValidateInput(false)] on every controller.
            // This also means we don't need AllowHtml attributes
            ValidateRequest = false;
            AllowJavascriptResult = true;
        }
#endif
        /// <summary>
        /// Defines whether the action can return a Javascript result to be executed client-side.
        /// </summary>
        /// <remarks>Most actions can accept a Javascript result which is executed client-side.
        /// For some "plain old" MVC controllers, a Javascript result is not acceptable, so these need to override and return false.</remarks>
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
                if (Manager.IsGetRequest) {
                    mod = (ModuleDefinition)RouteData.Values[Globals.RVD_ModuleDefinition];
                    if (mod == null) {
                        string moduleGuid = Manager.RequestQueryString[Basics.ModuleGuid];
                        if (string.IsNullOrWhiteSpace(moduleGuid))
                            return null;
                        Guid guid = new Guid(moduleGuid);
                        mod = await ModuleDefinition.LoadAsync(guid);
                    }
                } else if (Manager.IsPostRequest) {
                    mod = (ModuleDefinition)RouteData.Values[Globals.RVD_ModuleDefinition];
                    if (mod == null) {
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
                // show inner exception
                if (exc.Message != null && !string.IsNullOrWhiteSpace(exc.Message))
                    msg = exc.Message;
                while (exc.InnerException != null) {
                    exc = exc.InnerException;
                    if (exc.Message != null && !string.IsNullOrWhiteSpace(exc.Message))
                        msg += " " + exc.Message;
                }
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
            ContentResult cr = Content(
                string.Format(Basics.AjaxJavascriptErrorReturn + "Y_Error({0});", YetaWFManager.JsonSerialize(msg)));
            cr.ExecuteResult(filterContext);
        }
        /// <summary>
        /// Redirect with a message - THIS ONLY WORKS FOR A GET REQUEST.
        /// </summary>
        /// <param name="Message">Error message to display.</param>
        /// <returns></returns>
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
        protected async Task SetupActionContextAsync(ActionExecutingContext filterContext) {
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
            YetaWFManager.Syncify(async () => {
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
                Manager.EditMode = GetTempEditMode();

                // determine user identity - authentication provider updates Manager with user information
                await Resource.ResourceAccess.ResolveUserAsync();
                // get user's default language
                Manager.GetUserLanguage();
                // only now can we enable resource loading
                Manager.LocalizationSupportEnabled = true;
            }
        }
        protected static void GetCharSize() {
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
        public static bool GoingToPopup() {
            string toPopup = null;
            try {
                toPopup = Manager.RequestForm[Globals.Link_ToPopup];
                if (toPopup == null)
                    toPopup = Manager.RequestQueryString[Globals.Link_ToPopup];
            } catch (Exception) { }
            return toPopup != null;
        }
        protected static bool InPopup() {
            string inPopup = null;
            try {
                inPopup = Manager.RequestForm[Globals.Link_InPopup];
                if (inPopup == null)
                    inPopup = Manager.RequestQueryString[Globals.Link_InPopup];
            } catch (Exception) { }
            return inPopup != null;
        }
        protected static bool PageControlShown() {
            string pageControlShown = null;
            try {
                pageControlShown = Manager.RequestForm[Globals.Link_PageControl];
                if (pageControlShown == null)
                    pageControlShown = Manager.RequestQueryString[Globals.Link_PageControl];
            } catch (Exception) { }
            return pageControlShown != null;
        }
        protected static bool GetTempEditMode() {
            try {
                string editMode = Manager.RequestQueryString[Globals.Link_EditMode];
                if (editMode != null)
                    return true;
            } catch (Exception) { }
            return false;
        }
        protected static List<Origin> GetOriginList() {

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
        /// An action result that renders a grid as a partial view.
        /// </summary>
        /// <param name="dataSrc">The data source.</param>
        /// <returns>Used in conjunction with the Grid template.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync(DataSourceResult dataSrc) {
            await HandlePropertiesAsync(dataSrc.Data);
            string partialView = "GridData";
            return PartialView(partialView, dataSrc, ContentType: "application/json", PureContent: true, AreaViewName: false, Gzip: true);
        }
        /// <summary>
        /// An action result that renders a single grid record as a partial view.
        /// </summary>
        /// <param name="entryDef">The definition of the grid record.</param>
        /// <returns>Used in conjunction with the Grid template.</returns>
        protected async Task<PartialViewResult> GridPartialViewAsync(GridDefinition.GridEntryDefinition entryDef) {
            await HandlePropertiesAsync(entryDef.Model);
            string partialView = "GridEntry";
            return PartialView(partialView, entryDef, ContentType: "application/json", PureContent: true, AreaViewName: false);
        }
        private async Task HandlePropertiesAsync(List<object> data) {
            foreach (object item in data)
                await HandlePropertiesAsync(item);
        }
        private async Task HandlePropertiesAsync(object data) {
            await ObjectSupport.HandlePropertyAsync<Menus.MenuList>("Commands", "__GetCommandsAsync", data);
        }

        // PARTIAL VIEW
        // PARTIAL VIEW
        // PARTIAL VIEW

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="Script">Optional Javascript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, Javascript, etc.</param>
        /// <param name="UseAreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <returns></returns>
        protected PartialViewResult PartialView(ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {
            return PartialView(null /* viewName */, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip);
        }

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="Script">Optional Javascript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, Javascript, etc.</param>
        /// <param name="UseAreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <returns></returns>
        protected PartialViewResult PartialView(object model, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {
            return PartialView(null /* viewName */, model, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip);
        }

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="viewName">The name of the partial view.</param>
        /// <param name="Script">Optional Javascript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, Javascript, etc.</param>
        /// <param name="UseAreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <returns></returns>
        protected PartialViewResult PartialView(string viewName, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true, bool Gzip = false) {
            return PartialView(viewName, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName, Gzip: Gzip);
        }

        /// <summary>
        /// Returns an action to render a partial view.
        /// </summary>
        /// <param name="viewName">The name of the partial view.</param>
        /// <param name="model">The model.</param>
        /// <param name="Script">Optional Javascript executed client-side when the view is rendered.</param>
        /// <param name="ContentType">The optional content type. Default is text/html.</param>
        /// <param name="PureContent">Set to false to process the partial view a regular response to a view (including any processing YetaWF adds). If true is specified, only the rendered view is returned, without YetaWF processing, Javascript, etc.</param>
        /// <param name="UseAreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <returns></returns>
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
            /// The Javascript to be executed client-side after the partial view has been rendered.
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
                            ViewName = Module.DefaultViewName + "_Partial";
                    } else {
                        ViewName = YetaWFController.MakeFullViewName(ViewName, Module.Area);
                    }
                    if (string.IsNullOrWhiteSpace(ViewName)) {
#if MVC6
                        ViewName = (string)context.RouteData.Values["action"];
#else
                        ViewName = context.RouteData.GetRequiredString("action");
#endif
                        ViewName = YetaWFController.MakeFullViewName(ViewName, Module.Area);
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

                string viewHtml;
#if MVC6
                bool inPartialView = Manager.InPartialView;
                Manager.InPartialView = true;
                try {
                    IViewRenderService _viewRenderService = (IViewRenderService)YetaWFManager.ServiceProvider.GetService(typeof(IViewRenderService));
                    context.RouteData.Values.Add(Globals.RVD_ModuleDefinition, Module);
                    viewHtml = await _viewRenderService.RenderToStringAsync(context, ViewName, ViewData, PostRenderAsync);
                } catch (Exception) {
                    throw;
                } finally {
                    Manager.InPartialView = inPartialView;
                }
#else
                ViewEngineResult viewEngine = null;

                if (View == null) {
                    viewEngine = FindView(context);
                    View = viewEngine.View;
                }
                IView view = FindView(context).View;

                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb)) {
                    ViewContext vc = new ViewContext(context, view, context.Controller.ViewData, context.Controller.TempData, sw);
                    IViewDataContainer vdc = new ViewDataContainer() { ViewData = context.Controller.ViewData };
                    HtmlHelper htmlHelper = new HtmlHelper(vc, vdc);
                    context.RouteData.Values.Add(Globals.RVD_ModuleDefinition, Module);

                    bool inPartialView = Manager.InPartialView;
                    Manager.InPartialView = true;
                    try {
                        viewHtml = htmlHelper.Partial(base.ViewName, Model).ToString();
                    } catch (Exception) {
                        throw;
                    } finally {
                        Manager.InPartialView = inPartialView;
                    }
                    YetaWFManager.Syncify(async () => {
                        viewHtml = await PostRenderAsync(htmlHelper, context, viewHtml);
                    });
                }
#endif
                if (Gzip) {
                    // if gzip was explicitly requested, return zipped (this is rarely used as most responses are compressed based on iis settings/middleware)
                    // we use this to explicitly return certain json responses compressed (not all, as small responses don't warrant compression).
#if MVC6
#else
                    context.HttpContext.Response.AppendHeader("Content-encoding", "gzip");
                    context.HttpContext.Response.Filter = new GZipStream(context.HttpContext.Response.Filter, CompressionMode.Compress);
#endif
                }
#if MVC6
                // gzip encoding is performed by middleware
                byte[] btes = Encoding.ASCII.GetBytes(viewHtml);
                await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
                response.Output.Write(viewHtml);
                if (viewEngine != null)
                    viewEngine.ViewEngine.ReleaseView(context, View);
#endif
            }

#if MVC6
#else
            private class ViewDataContainer : IViewDataContainer {
                public ViewDataDictionary ViewData { get; set; }
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
                    viewHtml = YetaWF.Core.Views.RazorViewExtensions.PostProcessViewHtml(htmlHelper, Module, viewHtml);

                    Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
                    viewHtml = vars.ReplaceVariables(viewHtml);// variable substitution

                    if (Script != null)
                        Manager.ScriptManager.AddLastDocumentReady(Script);

                    // add generated scripts
                    string js = Manager.ScriptManager.RenderAjax().ToString();
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
