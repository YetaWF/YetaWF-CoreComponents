/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using YetaWF.Core.Addons;
using YetaWF.Core.Extensions;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Controllers {

    public class ControllerImpl<TMod> : ControllerImpl where TMod : ModuleDefinition {

        // MODULE PROPERTIES
        // MODULE PROPERTIES
        // MODULE PROPERTIES

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected TMod Module {
            get {
                if (Equals(_module, default(TMod))) {
                    if (Manager.IsGetRequest) {
                        _module = (TMod)RouteData.Values[Globals.RVD_ModuleDefinition];
                        if (_module == null) {
                            string moduleGuid = Manager.RequestQueryString[Basics.ModuleGuid];
                            if (string.IsNullOrWhiteSpace(moduleGuid))
                                throw new InternalError("Missing QueryString[{0}] value in controller for module {1} - {2}", Basics.ModuleGuid, ModuleName, GetType().Namespace);
                            Guid guid = new Guid(moduleGuid);
                            _module = (TMod)ModuleDefinition.Load(guid);
                        }
                    } else if (Manager.IsPostRequest) {
                        _module = (TMod)RouteData.Values[Globals.RVD_ModuleDefinition];
                        if (_module == null) {
                            string moduleGuid = Manager.RequestForm[Basics.ModuleGuid];
                            if (string.IsNullOrWhiteSpace(moduleGuid))
                                moduleGuid = Manager.RequestQueryString[Basics.ModuleGuid];
                            if (string.IsNullOrWhiteSpace(moduleGuid))
                                throw new InternalError("Missing {0} value in controller for module {1} - {2}", Basics.ModuleGuid, ModuleName, GetType().Namespace);
                            Guid guid = new Guid(moduleGuid);
                            _module = (TMod)ModuleDefinition.Load(guid);
                        }
                    }
                    if (_module == default(TMod))
                        throw new InternalError("No ModuleDefinition available in controller {0} {1}.", GetType().Namespace, ClassName);
                }
                return _module;
            }
        }
        private TMod _module;

        protected override ModuleDefinition GetModule() { return Module; }
    }

    public abstract class ControllerImpl : YetaWFController {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ControllerImpl), name, defaultValue, parms); }

        // MODULE INFO
        // MODULE INFO
        // MODULE INFO

        protected abstract ModuleDefinition GetModule();

        /// <summary>
        /// Returns the module name.
        /// </summary>
        public string ModuleName {
            get {
                GetModuleInfo();
                return _ModuleName;
            }
        }
        /// <summary>
        /// Returns the controller name.
        /// </summary>
        public string ControllerName {
            get {
                GetModuleInfo();
                return _ControllerName;
            }
        }
        /// <summary>
        /// Return the area name.
        /// </summary>
        public string Area {
            get {
                GetModuleInfo();
                return _Area;
            }
        }

        /// <summary>
        /// Return the company name.
        /// </summary>
        public string CompanyName {
            get {
                GetModuleInfo();
                return _CompanyName;
            }
        }

        /// <summary>
        /// Returns the company's domain name.
        /// </summary>
        public string Domain {
            get {
                GetModuleInfo();
                return _Domain;
            }
        }

        /// <summary>
        /// Returns the class name.
        /// </summary>
        public string ClassName {
            get {
                if (string.IsNullOrEmpty(_ClassName)) {
                    _ClassName = GetType().Name;
                }
                return _ClassName;
            }
        }
        private string _ClassName { get; set; }

        private const string CONTROLLER_NAMESPACE = "(xcompanyx.Modules.xproductx.Controllers)";

        private void GetModuleInfo() {
            if (string.IsNullOrEmpty(_ModuleName)) {
                Package package = Package.GetPackageFromAssembly(GetType().Assembly);
                string ns = GetType().Namespace;

                string[] s = ns.Split(new char[] { '.' }, 4);
                if (s.Length != 4)
                    throw new InternalError("Controller namespace '{0}' must have 4 components - {1}", ns, CONTROLLER_NAMESPACE);
                _CompanyName = s[0];
                if (s[1] != "Modules")
                    throw new InternalError("Controller namespace '{0}' must have 'Modules' as second component", ns);
                if (s[2] != package.Product)
                    throw new InternalError("Controller namespace '{0}' must have the product name as third component", ns);
                _Product = s[2];
                if (s[3] != "Controllers")
                    throw new InternalError("Module namespace '{0}' must have 'Controllers' as fourth component", ns);

                if (!ClassName.EndsWith("ModuleController"))
                    throw new InternalError("The controller class name doesn't end in \"ModuleController\" - {0}", ClassName);
                _ModuleName = ClassName.Substring(0, ClassName.Length - "ModuleController".Length);
                _ControllerName = ClassName.Substring(0, ClassName.Length - "Controller".Length);

                _Area = package.AreaName;
                _Domain = package.Domain;
                _Version = package.Version;
            }
        }
        private string _Product { get; set; }
        private string _Area { get; set; }
        private string _CompanyName { get; set; }
        private string _Domain { get; set; }
        private string _ModuleName { get; set; }
        private string _ControllerName { get; set; }
        private string _Version { get; set; }

        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT

        protected string UniqueId(string name) {
            return Manager.UniqueId(name);
        }

        public string GetActionUrl(string actionName, object args = null) {
            string url = "/" + Area + "/" + ControllerName + "/" + actionName;
            if (args != null) {
                string qs = YetaWFManager.GetQueryStringFromAnonymousObject(args);
                if (qs != null)
                    url = url + "?" + qs.ToString();
            }
            return url;
        }

        // CONTROLLER
        // CONTROLLER
        // CONTROLLER

        protected override void OnResultExecuting(ResultExecutingContext filterContext) {
            // THIS SUPPRESSES CACHING
            // RESEARCH: Use OutputCache for actions that can be cached
            // http://www.dotnet-tricks.com/Tutorial/mvc/4R5c050113-Understanding-Caching-in-Asp.Net-MVC-with-example.html
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
            filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();

            base.OnResultExecuting(filterContext);
        }

        protected override void HandleUnknownAction(string actionName) {
            //base.HandleUnknownAction(actionName);
            string error = __ResStr("errUnknownAction", "Unknown action {0} attempted in Controller {1}.", actionName, GetType().FullName);
            Logging.AddErrorLog(error);
            throw new HttpException(404, error);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            base.OnActionExecuting(filterContext);

            string url = HttpContext.Request.Url.ToString();
            if (Manager.IsPostRequest) {
                // Request for a module
                // Make sure we have all necessary information
                // otherwise, we'll try to invoke a controller directly
                // find the module handling this request (saved as hidden field in Form)
                object moduleGuid = HttpContext.Request.Form[Basics.ModuleGuid];
                if (moduleGuid == null) {
                    moduleGuid = HttpContext.Request.QueryString[Basics.ModuleGuid];
                    if (moduleGuid == null)
                        throw new InternalError("Missing {0} hidden field for POST request Url {1}", Basics.ModuleGuid, url);
                }
                // find the unique Id prefix (saved as hidden field in Form)
                string uniqueIdPrefix = HttpContext.Request.Form[Forms.UniqueIdPrefix];
                if (string.IsNullOrEmpty(uniqueIdPrefix))
                    uniqueIdPrefix = HttpContext.Request.QueryString[Forms.UniqueIdPrefix];
                if (!string.IsNullOrEmpty(uniqueIdPrefix))
                    Manager.UniqueIdPrefix = uniqueIdPrefix;
            }

            MethodInfo mi = filterContext.ActionDescriptor.ControllerDescriptor.ControllerType.GetMethod(filterContext.ActionDescriptor.ActionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
            // check if the action is authorized by checking the module's authorization
            string level = null;
            PermissionAttribute permAttr = (PermissionAttribute) Attribute.GetCustomAttribute(mi, typeof(PermissionAttribute));
            if (permAttr != null)
                level = permAttr.Level;
            ModuleDefinition mod = GetModule();
            if (!mod.IsAuthorized(level)) {
                if (Manager.IsAjaxRequest || Manager.IsPostRequest) {
                    filterContext.Result = new HttpUnauthorizedResult();
                } else {
                    // We get here if an action is attempted that the user is not authorized for
                    // we could attempt to capture and redirect to user login, whatevz
                    filterContext.Result = new EmptyResult();
                }
                return;
            }

            // action is about to start - if this is a postback or ajax request, we'll clean up parameters
            if (Manager.IsAjaxRequest || Manager.IsPostRequest) {
                if (filterContext.ActionParameters != null) {
                    // remove leading/trailing spaces based on TrimAttribute for properties
                    // and update ModelState for RequiredIfxxx attributes
                    ViewDataDictionary viewData = filterContext.Controller.ViewData;
                    ModelStateDictionary modelState = viewData.ModelState;
                    foreach (var parm in filterContext.ActionParameters) {
                        FixArgumentParmTrim(parm.Value);
                        FixArgumentParmCase(parm.Value);
                        FixDates(parm.Value);
                        PropertyListSupport.CorrectModelState(parm.Value, modelState);
                    }

                    // translate any xxx.JSON properties to native objects
                    if (modelState.IsValid)
                        ReplaceJSONParms(filterContext.ActionParameters);

                    // if we have a template action, search parameters for templates with actions and execute it
                    if (modelState.IsValid) {
                        string templateName = HttpContext.Request.Form[Basics.TemplateName];
                        if (!string.IsNullOrWhiteSpace(templateName)) {
                            string actionValStr = HttpContext.Request.Form[Basics.TemplateAction];
                            string actionExtraStr = HttpContext.Request.Form[Basics.TemplateExtraData];
                            foreach (var parm in filterContext.ActionParameters) {
                                if (SearchTemplate(templateName, actionValStr, actionExtraStr, parm))
                                    break;
                            }
                        }
                    }
                }
            }

            // origin list (we already do this in global.asax.cs for GET, maybe move it there)
            //string originList = (string) HttpContext.Request.Form[Globals.Link_OriginList];
            //if (!string.IsNullOrWhiteSpace(originList))
            //    Manager.OriginList = YetaWFManager.Jser.Deserialize<List<Origin>>(originList);
            //else
            //    Manager.OriginList = new List<Origin>();

            //string inPopup = HttpContext.Request.Form[Globals.Link_InPopup];
            //if (!string.IsNullOrWhiteSpace(inPopup))
            //    Manager.IsInPopup = true;

            ViewData.Add(Globals.RVD_ModuleDefinition, GetModule());
        }

        // INPUT CLEANUP
        // INPUT CLEANUP
        // INPUT CLEANUP

        // change all dates to utc - internally YetaWF ALWAYS uses utc
        // incoming dates are sent from client in local time (the user defined timezone - defined in user settings on the server)
        // we totally bypass client side date/time handling, but incoming date/times have to be translated into utc
        private bool FixDates(object parm) {
            if (parm == null) return false;
            bool any = false;

            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                any = true;
                PropertyInfo pi = prop.PropInfo;
                if (pi.CanRead && pi.CanWrite) {
                    if (pi.PropertyType == typeof(DateTime) || pi.PropertyType == typeof(DateTime?)) {
                        DateTime? dt = prop.GetPropertyValue<DateTime?>(parm);
                        if (dt != null && ((DateTime)dt).Kind == DateTimeKind.Local && (prop.UIHint == "DateTime" || prop.UIHint == "Date")) {
                            // we're receiving date/time in the user's specified timezone (server side), so we now have to convert it to Utc
                            dt = Formatting.GetUtcDateTime((DateTime)dt);
                            pi.SetValue(parm, dt, null);
                        }
                    } else {
                        if (pi.GetIndexParameters().Length == 0) {
                            try {
                                FixDates(prop.GetPropertyValue<object>(parm));  // try to handle nested types
                            } catch (Exception) { }
                        }
                    }
                }
            }
            return any;
        }

        // update all model parameters and trim as requested
        private static bool FixArgumentParmTrim(object parm) {
            if (parm == null) return false;
            bool any = false;

            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                any = true;
                TrimAttribute trimAttr = prop.TryGetAttribute<TrimAttribute>();
                if (trimAttr != null) {
                    TrimAttribute.EnumStyle style = trimAttr.Value;
                    if (style != TrimAttribute.EnumStyle.None) {
                        PropertyInfo pi = prop.PropInfo;
                        if (pi.PropertyType == typeof(MultiString)) {
                            MultiString ms = prop.GetPropertyValue<MultiString>(parm);
                            ms.Trim();
                        } else if (pi.PropertyType == typeof(string)) {
                            if (pi.CanWrite) {
                                string val = (string) pi.GetValue(parm, null); ;
                                if (!string.IsNullOrEmpty(val)) {
                                    switch (style) {
                                        default:
                                        case TrimAttribute.EnumStyle.None:
                                            break;
                                        case TrimAttribute.EnumStyle.Both:
                                            val = val.Trim();
                                            break;
                                        case TrimAttribute.EnumStyle.Left:
                                            val = val.TrimEnd();
                                            break;
                                        case TrimAttribute.EnumStyle.Right:
                                            val = val.TrimStart();
                                            break;
                                    }
                                    pi.SetValue(parm, val, null);
                                }
                            }
                        } else {
                            FixArgumentParmTrim(prop.GetPropertyValue<object>(parm));  // handle nested types (but only if containing type has the Trim attribute)
                        }
                    }
                }
            }
            return any;
        }
        // update all model parameters and ucase/lcase as requested
        private static bool FixArgumentParmCase(object parm) {
            if (parm == null) return false;
            bool any = false;

            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                any = true;
                CaseAttribute caseAttr = prop.TryGetAttribute<CaseAttribute>();
                if (caseAttr != null) {
                    CaseAttribute.EnumStyle style = caseAttr.Value;
                    PropertyInfo pi = prop.PropInfo;
                    if (pi.PropertyType == typeof(MultiString)) {
                        MultiString ms = prop.GetPropertyValue<MultiString>(parm);
                        ms.Case(style);
                    } else if (pi.PropertyType == typeof(string)) {
                        if (pi.CanWrite) {
                            string val = (string)pi.GetValue(parm, null); ;
                            if (!string.IsNullOrEmpty(val)) {
                                switch (style) {
                                    default:
                                    case CaseAttribute.EnumStyle.Upper:
                                        val = val.ToUpper();
                                        break;
                                    case CaseAttribute.EnumStyle.Lower:
                                        val = val.ToLower();
                                        break;
                                }
                                pi.SetValue(parm, val, null);
                            }
                        }
                    } else {
                        FixArgumentParmCase(prop.GetPropertyValue<object>(parm));  // handle nested types (but only if containing type has the Case attribute)
                    }
                }
            }
            return any;
        }

        // search for templates
        private static bool SearchTemplate(string templateName, string actionValStr, string actionExtraStr, KeyValuePair<string, object> pair) {
            return SearchTemplateArgument(templateName, actionValStr, actionExtraStr, pair.Value);
        }
        private static bool SearchTemplateArgument(string templateName, string actionValStr, string actionExtraStr, object parm) {
            if (parm == null) return false;
            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                if (prop.PropInfo.PropertyType.IsClass && !prop.PropInfo.PropertyType.IsAbstract) {
                    if (!prop.ReadOnly && prop.PropInfo.CanRead && prop.PropInfo.CanWrite) {
                        ClassData classData = ObjectSupport.GetClassData(prop.PropInfo.PropertyType);
                        TemplateActionAttribute actionAttr = classData.TryGetAttribute<TemplateActionAttribute>();
                        if (actionAttr != null) {
                            if (actionAttr.Value == templateName) {
                                object objVal = prop.GetPropertyValue<object>(parm);
                                ITemplateAction act = objVal as ITemplateAction;
                                if (act == null) throw new InternalError("ITemplateAction not implemented for {0}", prop.Name);
                                int actionVal = 0;
                                if (!string.IsNullOrWhiteSpace(actionValStr))
                                    actionVal = Convert.ToInt32(actionValStr);
                                act.ExecuteAction(actionVal, actionExtraStr);
                                return true;
                            }
                        }
                        object o;
                        try {
                            o = prop.GetPropertyValue<object>(parm);
                        } catch (Exception) {
                            o = null;
                        }
                        if (o != null)
                            SearchTemplateArgument(templateName, actionValStr, actionExtraStr, o);  // handle nested types
                    }
                }
            }
            return false;
        }

        // Replace JSON parms
        private void ReplaceJSONParms(IDictionary<string, object> actionParms) {
            foreach (var entry in HttpContext.Request.Form.AllKeys) {
                if (entry != null && entry.EndsWith("-JSON")) {
                    string data = HttpContext.Request.Form[entry];
                    string parmName = entry.Substring(0, entry.Length - 5);
                    AddJSONParmData(actionParms, parmName, data);
                }
            }
        }

        private void AddJSONParmData(IDictionary<string, object> actionParms, string parmName, string jsonData) {
            // search each direct parm
            // NOT SUPPORTED FOR DIRECT PARMS (MUST USE MODEL)
            //foreach (var parm in actionParms) {
            //    if (string.Compare(parm.Key, parmName, true) == 0) {
            //        object parmData = YetaWFManager.Jser.Deserialize(jsonData, parm.Value.GetType());
            //        parm.Value = parmData;
            //        return;
            //    }
            //}
            // search each parm's members (usually Model.xxx)
            foreach (var parm in actionParms) {
                if (parm.Value != null) {
                    Type tpParm = parm.Value.GetType();
                    if (tpParm.IsClass && !tpParm.IsAbstract) {
                        PropertyInfo propInfo = ObjectSupport.TryGetProperty(tpParm, parmName);
                        if (propInfo != null) {
                            // if fails if we found a xx.JSON form arg and a matching model property, but the JSON data isn't valid
                            object parmData = YetaWFManager.Jser.Deserialize(jsonData, propInfo.PropertyType);
                            propInfo.SetValue(parm.Value, parmData);
                        }
                    }
                }
            }
        }

        // VIEW

        // Invoke a view from a module controller
        protected new ViewResult View() { throw new NotSupportedException(); }
        protected new ViewResult View(IView view) { throw new NotSupportedException(); }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Justification = "This is deliberate so the base class implementation isn't used accidentally")]
        protected new ViewResult View(string viewName) {
            return View(viewName, UseAreaViewName: true);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Justification = "This is deliberate so the base class implementation isn't used accidentally")]
        protected new ViewResult View(object model) {
            return View(model, UseAreaViewName: true);
        }
        protected new ViewResult View(IView view, object model) { throw new NotSupportedException(); }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Justification = "This is deliberate so the base class implementation isn't used accidentally")]
        protected new ViewResult View(string viewName, object model) {
            return View(viewName, model, UseAreaViewName: true);
        }
        protected new ViewResult View(string viewName, string masterName) { throw new NotSupportedException(); }
        protected new virtual ViewResult View(string viewName, string masterName, object model) { throw new NotSupportedException(); }


        protected ViewResult View(string viewName, bool UseAreaViewName) {
            if (UseAreaViewName)
                viewName = YetaWFController.MakeFullViewName(viewName, Area);
            return base.View(viewName);
        }
        protected ViewResult View(object model, bool UseAreaViewName) {
            string viewName = ControllerContext.RouteData.GetRequiredString("action");
            if (UseAreaViewName)
                viewName = YetaWFController.MakeFullViewName(viewName, Area);
            return base.View(viewName, model);
        }
        protected ViewResult View(string viewName, object model, bool UseAreaViewName) {
            if (UseAreaViewName)
                viewName = YetaWFController.MakeFullViewName(viewName, Area);
            return base.View(viewName, model);
        }

        // PARTIAL VIEW
        // PARTIAL VIEW
        // PARTIAL VIEW

        protected PartialViewResult PartialView(ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true) {
            return PartialView(null /* viewName */, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName);
        }

        protected PartialViewResult PartialView(object model, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true) {
            return PartialView(null /* viewName */, model, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName);
        }

        protected PartialViewResult PartialView(string viewName, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true) {
            return PartialView(viewName, null /* model */, Script, ContentType: ContentType, PureContent: PureContent, AreaViewName: AreaViewName);
        }

        protected PartialViewResult PartialView(string viewName, object model, ScriptBuilder Script = null, string ContentType = null, bool PureContent = false, bool AreaViewName = true) {

            if (model != null) {
                ViewData.Model = model;
            }

            return new PartialViewResult {
                ViewName = viewName,
                ViewData = ViewData,
                TempData = TempData,
                ViewEngineCollection = ViewEngineCollection,
                Module = GetModule(),
                Script = Script,
                ContentType = ContentType,
                PureContent = PureContent,
                AreaViewName = AreaViewName,
            };
        }
        public class PartialViewResult : System.Web.Mvc.PartialViewResult {

            protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

            private const string DefaultContentType = "text/html";

            public PartialViewResult() { }

            public ModuleDefinition Module { get; set; }
            public ScriptBuilder Script { get; set; }
            public string ContentType { get; set; }
            public bool PureContent { get; set; }
            public bool AreaViewName { get; set; }

            private static readonly Regex reEndDiv = new Regex(@"</\s*div\s*>\s*$"); // very last div

            public override void ExecuteResult(ControllerContext context) {

                Manager.Verify_AjaxRequest();

                if (context == null)
                    throw new ArgumentNullException("context");
                if (String.IsNullOrEmpty(ViewName))
                    ViewName = context.RouteData.GetRequiredString("action");

                if (AreaViewName)
                    ViewName = YetaWFController.MakeFullViewName(ViewName, Module.Area);

                ViewEngineResult viewEngine = null;
                string viewHtml = "";

                if (View == null) {
                    viewEngine = FindView(context);
                    View = viewEngine.View;
                }
                IView view = FindView(context).View;

                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);
                ViewContext vc = new ViewContext(context, view, context.Controller.ViewData, context.Controller.TempData, sw);
                IViewDataContainer vdc = new ViewDataContainer() { ViewData = context.Controller.ViewData };
                HtmlHelper helper = new HtmlHelper(vc, vdc);

                var response = context.HttpContext.Response;
                if (!string.IsNullOrEmpty(ContentType))
                    response.ContentType = ContentType;

                bool inPartialView = Manager.InPartialView;
                Manager.InPartialView = true;
                try {
                    viewHtml = helper.Partial(base.ViewName, Model).ToString();
                } catch (Exception) {
                    Manager.InPartialView = inPartialView;
                    throw;
                }
                Manager.InPartialView = inPartialView;

                // if the controller specified a content type, only return the exact response
                // if the controller didn't specify a content type and the content type is text/html, add all the other goodies
                if (!PureContent && string.IsNullOrEmpty(ContentType) && response.ContentType == DefaultContentType) {

                    Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
                    if (Manager.CurrentPage!= null) Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);
                    Manager.AddOnManager.AddExplicitlyInvokedModules(Module.ReferencedModules);
                    viewHtml = viewHtml + helper.RenderReferencedModule_Ajax().ToString();

                    viewHtml = YetaWF.Core.Views.RazorView.PostProcessViewHtml(helper, Module, viewHtml);

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
                response.Output.Write(viewHtml);

                if (viewEngine != null)
                    viewEngine.ViewEngine.ReleaseView(context, View);
            }

            private class ViewDataContainer : IViewDataContainer {
                public ViewDataDictionary ViewData { get; set; }
            }
        }

        // GRID PARTIALVIEW
        // GRID PARTIALVIEW
        // GRID PARTIALVIEW

        protected PartialViewResult GridPartialView(DataSourceResult dataSrc) {
            string partialView = "GridData";
            return PartialView(partialView, dataSrc, ContentType: "application/json", PureContent: true, AreaViewName: false);
        }

        protected PartialViewResult GridPartialView(GridDefinition.GridEntryDefinition entryDef) {
            string partialView = "GridEntry";
            return PartialView(partialView, entryDef, ContentType: "application/json", PureContent: true, AreaViewName: false);
        }


        // PAGE/FORM SAVE
        // PAGE/FORM SAVE
        // PAGE/FORM SAVE

        protected enum ReloadEnum {
            Page = 1,
            Module = 2, // TODO: The entire module is not currently supported - use page reload instead
            ModuleParts = 3
        }

        /// <summary>
        /// The page/form was successfully saved. This handles returning to a parent page or displaying a popup if a return page is not available.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="PopupText"></param>
        /// <param name="PopupTitle"></param>
        /// <param name="dummy"></param>
        /// <param name="Reload"></param>
        /// <returns></returns>
        protected ActionResult Reload(object model = null, string dummy = null, string PopupText = null, string PopupTitle = null, ReloadEnum Reload = ReloadEnum.Page)
        {
            if (Manager.IsAjaxRequest) {
                switch (Reload) {
                    default:
                    case ReloadEnum.Page:
                        return Reload_Page(model, PopupText, PopupTitle);
                    case ReloadEnum.Module:
                        return Reload_Module(model, PopupText, PopupTitle);
                    case ReloadEnum.ModuleParts:
                        return Reload_ModuleParts(model, PopupText, PopupTitle);
                }
            } else {
                if (string.IsNullOrEmpty(PopupText))
                    throw new InternalError("We don't have a message to display - programmer error");
                return View("ShowMessage", PopupText, UseAreaViewName: false);
            }
        }
        private ActionResult Reload_Page(object model, string popupText, string popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadPage);
                return new JsonResult { Data = sb.ToString() };
            } else {
                popupText = YetaWFManager.Jser.Serialize(popupText);
                popupTitle = YetaWFManager.Jser.Serialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("Y_Alert({0}, {1}, function() {{ Y_ReloadPage(true); }});", popupText, popupTitle);
                return new JsonResult { Data = sb.ToString() };
            }
        }
        private ActionResult Reload_Module(object model, string popupText, string popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModule);
                return new JsonResult { Data = sb.ToString() };
            } else {
                popupText = YetaWFManager.Jser.Serialize(popupText);
                popupTitle = YetaWFManager.Jser.Serialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("Y_Alert({0}, {1}, function() {{ Y_ReloadModule(); }});", popupText, popupTitle);
                return new JsonResult { Data = sb.ToString() };
            }
        }
        private ActionResult Reload_ModuleParts(object model, string popupText, string popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                return new JsonResult { Data = sb.ToString() };
            } else {
                popupText = YetaWFManager.Jser.Serialize(popupText);
                popupTitle = YetaWFManager.Jser.Serialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                sb.Append("Y_Alert({0}, {1});", popupText, popupTitle);
                return new JsonResult { Data = sb.ToString() };
            }
        }

        /// <summary>
        /// Not authorized for this type of access
        /// </summary>
        /// <returns></returns>
        protected ActionResult NotAuthorized() {
            if (Manager.IsAjaxRequest || Manager.IsPostRequest)
                return new HttpUnauthorizedResult();
            else
                return View("ShowMessage", __ResStr("nothAuth", "You are not authorized to access this module - {0}", GetType().FullName), UseAreaViewName: false);
        }

        protected enum OnPopupCloseEnum {
            Nothing = 0,
            ReloadNothing = 1,
            ReloadParentPage = 2,
            ReloadModule = 3,
            GotoNewPage = 4,
            UpdateInPlace = 5,
        }
        protected enum OnCloseEnum {
            Nothing = 0,
            Return = 1,
            GotoNewPage = 2,
            UpdateInPlace = 3,
            ReloadPage = 4,
            CloseWindow = 9,
        }
        protected enum OnApplyEnum {
            ReloadModule = 1,
            ReloadPage = 2,
        }

        /// <summary>
        /// The page/form was successfully processed. This handles returning to a parent page or displaying a popup if a return page is not available.
        /// </summary>
        /// <param name="model">The model to display.</param>
        /// <param name="popupText">A message displayed in a popup. Specify null to suppress the popup.</param>
        /// <param name="popupTitle">The title for the popup if a message (popupText) is specified. If null is specified, a default title indicating success is supplied.</param>
        /// <param name="OnClose">The action to take when the page is closed. This is only used if a page is closed (as opposed to a popup or when the Apply button was processed).</param>
        /// <param name="OnPopupClose">The action to take when a popup is closed. This is only used if a popup is closed (as opposed to a page or when the Apply button was processed).</param>
        /// <param name="OnApply">The action to take when the Apply button was processed.</param>
        /// <param name="NextPage">The Url where the page is redirected (OnClose or OnPopupClose must request a matching action, otherwise this is ignored).</param>
        /// <param name="ExtraJavaScript">Optional additional Javascript code that is returned as part of the ActionResult.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        protected ActionResult FormProcessed(object model, string popupText = null, string popupTitle = null, OnCloseEnum OnClose = OnCloseEnum.Return, OnPopupCloseEnum OnPopupClose = OnPopupCloseEnum.ReloadParentPage, OnApplyEnum OnApply = OnApplyEnum.ReloadModule, string NextPage = null, string ExtraJavaScript = null) {
            ScriptBuilder sb = new ScriptBuilder();

            sb.Append(Basics.AjaxJavascriptReturn);

            if (ExtraJavaScript != null)
                sb.Append(ExtraJavaScript);

            popupText = popupText != null ? YetaWFManager.Jser.Serialize(popupText) : null;
            popupTitle = YetaWFManager.Jser.Serialize(popupTitle ?? __ResStr("completeTitle", "Success"));

            bool isApply = Manager.RequestForm[Globals.Link_SubmitIsApply] != null;
            if (isApply) {
                NextPage = null;
                OnPopupClose = OnPopupCloseEnum.UpdateInPlace;
                OnClose = OnCloseEnum.UpdateInPlace;
            } else {
                if (Manager.IsInPopup) {
                    if (OnPopupClose == OnPopupCloseEnum.ReloadParentPage || OnPopupClose == OnPopupCloseEnum.GotoNewPage) {
                        if (OnPopupClose == OnPopupCloseEnum.ReloadParentPage && string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.ReturnToUrl;
                        if (string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.CurrentSite.HomePageUrl;
                    } else
                        NextPage = null;
                } else { //if (!Manager.IsInPopup)
                    if (OnClose == OnCloseEnum.Return || OnClose == OnCloseEnum.GotoNewPage) {
                        if (OnClose == OnCloseEnum.Return && string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.ReturnToUrl;
                        if (string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.CurrentSite.HomePageUrl;
                    } else
                        NextPage = null;
                }
            }

            // handle NextPage (if any)
            if (!string.IsNullOrWhiteSpace(NextPage)) {
                string url = NextPage;
                if (string.IsNullOrWhiteSpace(url))
                    url = Manager.CurrentSite.HomePageUrl;
                url = YetaWFManager.Jser.Serialize(url);

                if (Manager.IsInPopup) {
                    if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append("Y_Loading();");
                        sb.Append("window.parent.location.assign({0});", url);
                    } else {
                        sb.Append("Y_Alert({0}, {1}, function() {{ Y_Loading();window.parent.location.assign({2}); }});", popupText, popupTitle, url);
                    }
                } else {
                    if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append("Y_Loading();");
                        sb.Append("window.location.assign({0});", url);
                    } else {
                        sb.Append("Y_Alert({0}, {1}, function() {{ Y_Loading();window.location.assign({2}); }});", popupText, popupTitle, url);
                    }
                }
            } else {
                if (Manager.IsInPopup) {
                    if (string.IsNullOrWhiteSpace(popupText)) {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                                throw new InternalError("No next page");
                            case OnPopupCloseEnum.Nothing:
                                break;
                            case OnPopupCloseEnum.ReloadNothing:
                                sb.Append("Y_ClosePopup(false);");
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                sb.Append("Y_ClosePopup(true);");
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                sb.Append("window.parent.YetaWF_Basics.refreshPage();");
                                sb.Append("Y_ClosePopup(false);");
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    } else {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                                throw new InternalError("No next page");
                            case OnPopupCloseEnum.Nothing:
                                sb.Append("Y_Alert({0}, {1});", popupText, popupTitle);
                                break;
                            case OnPopupCloseEnum.ReloadNothing:
                                sb.Append("Y_Alert({0}, {1}, function() {{ Y_ClosePopup(false); }});", popupText, popupTitle);
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                sb.Append("Y_Alert({0}, {1}, function() {{ Y_ClosePopup(true); }});", popupText, popupTitle);
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                sb.Append("Y_Alert({0}, {1});", popupText, popupTitle);
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                sb.Append("Y_Alert({0}, {1}, function() {{ window.parent.YetaWF_Basics.refreshPage(); Y_ClosePopup(false); }});", popupText, popupTitle);
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    }
                } else {
                    string url = null;
                    switch (OnClose) {
                        case OnCloseEnum.GotoNewPage:
                            throw new InternalError("No next page");
                        case OnCloseEnum.Nothing:
                            if (!string.IsNullOrWhiteSpace(popupText))
                                sb.Append("Y_Alert({0}, {1});", popupText, popupTitle);
                            break;
                        case OnCloseEnum.UpdateInPlace:
                            if (!isApply && !string.IsNullOrWhiteSpace(popupText)) {
                                sb.Append("Y_Alert({0}, {1});", popupText, popupTitle);
                                OnApply = OnApplyEnum.ReloadModule;
                            }
                            isApply = true;
                            break;
                        case OnCloseEnum.Return:
                            if (Manager.OriginList == null || Manager.OriginList.Count == 0) {
                                if (string.IsNullOrWhiteSpace(popupText))
                                    sb.Append("window.close();");
                                else
                                    sb.Append("Y_Alert({0}, {1}, function() {{ window.close(); }});", popupText, popupTitle);
                            } else {
                                url = YetaWFManager.Jser.Serialize(Manager.ReturnToUrl);
                                if (string.IsNullOrWhiteSpace(popupText))
                                    sb.Append("window.location.assign({0});", url);
                                else
                                    sb.Append("Y_Alert({0}, {1}, function() {{ window.location.assign({2}); }});", popupText, popupTitle, url);
                            }
                            break;
                        case OnCloseEnum.CloseWindow:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append("window.close();");
                            else
                                sb.Append("Y_Alert({0}, {1}, function() {{ window.close(); }});", popupText, popupTitle);
                            break;
                        case OnCloseEnum.ReloadPage:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append("Y_ReloadPage(true);");
                            else
                                sb.Append("Y_Alert({0}, {1}, function() {{ Y_ReloadPage(true); }});", popupText, popupTitle);
                            break;
                        default:
                            throw new InternalError("Invalid OnClose value {0}", OnClose);
                    }
                }
                if (isApply) {
                    if (OnApply == OnApplyEnum.ReloadPage) {
                        if (string.IsNullOrWhiteSpace(popupText))
                            sb.Append("Y_ReloadPage(true);");
                        else
                            sb.Append("Y_Alert({0}, {1}, function() {{ Y_ReloadPage(true); }});", popupText, popupTitle);
                    } else {
                        if (sb.Length == Basics.AjaxJavascriptReturn.Length)
                            return PartialView(model);// no javascript after all
                        else
                            return PartialView(model, sb);
                    }
                }
            }
            return new JsonResult { Data = sb.ToString() };
        }

        // REDIRECT
        // REDIRECT
        // REDIRECT

        /// <summary>
        /// Redirect to the specified target defined by the supplied action.
        /// </summary>
        /// <param name="action">The ModuleAction defining the target where the page is redirected.</param>
        /// <returns>An ActionResult to be returned by the controller.
        ///
        /// The Redirect method can be used for GET, PUT, Ajax requests and also within popups.
        /// This works on cooperation with client-side code to redirect popups, etc., which is normally not supported in MVC.</returns>
        protected ActionResult Redirect(ModuleAction action) {
            if (action == null)
                return Redirect("");
            Manager.Verify_AjaxRequest();
            return Redirect(action.GetCompleteUrl(), ForcePopup: action.Style == ModuleAction.ActionStyleEnum.Popup || action.Style == ModuleAction.ActionStyleEnum.ForcePopup);
        }

        /// <summary>
        /// Redirect to the specified target Url.
        /// </summary>
        /// <param name="url">The Urk defining the target where the page is redirected. If null is specified, the site's Home page is used instead.</param>
        /// <returns>An ActionResult to be returned by the controller.
        ///
        /// The Redirect method can be used for GET, PUT, Ajax requests and also within popups.
        /// This works on cooperation with client-side code to redirect popups, etc., which is normally not supported in MVC.</returns>
        protected ActionResult Redirect(string url, bool ForcePopup = false) {

            if (string.IsNullOrWhiteSpace(url))
                url = Manager.CurrentSite.HomePageUrl;

            if (Manager.IsAjaxRequest) {
                // for ajax requests we return javascript to redirect
                ScriptBuilder sb = new ScriptBuilder();
                sb.Append(Basics.AjaxJavascriptReturn);

                if (string.IsNullOrWhiteSpace(url))
                    url = "/";
                if (Manager.IsInPopup && ForcePopup)
                    url += (url.Contains("?") ? "&" : "?") + Globals.Link_ToPopup + "=y";

                if (ForcePopup) {
                    // We need to redirect to a popup (in a postback)
                    // we're not be in a popup and want to become a popup on top of whatever page there is
                    // send code to activate the new popup
                    // the assumption is we're in a postback and we have to find out whether to use http or https for the popup
                    // if we're on a page with https: the popup must also be https: otherwise the browser will have a fit
                    if (Manager.CurrentRequest.IsSecureConnection) {
                        if (url.StartsWith("//") || url.IsHttps()) {
                            // good
                        } else if (url.IsHttp()) {
                            url = url.Substring("http:".Length);// remove http: and leave just //
                        } else if (url.StartsWith("/")) {
                            url = Manager.CurrentSite.MakeUrl(url, PagePageSecurity: Pages.PageDefinition.PageSecurityType.httpsOnly);
                        } else {
                            // who knows
                        }
                    }
                    url = YetaWFManager.Jser.Serialize(url);
                    if (Manager.IsInPopup) {
                        // simply replace the current popup with the new popup
                        sb.Append("window.location.assign({0});", url);
                    } else {
                        // create the popup client-side
                        sb.Append("YetaWF_Popup.openPopup({0});", url);
                    }
                } else {
                    url = YetaWFManager.Jser.Serialize(url);
                    if (Manager.IsInPopup)
                        sb.Append("window.parent.location.assign({0});", url);
                    else
                        sb.Append("window.location.assign({0});", url);
                }
                return new JsonResult { Data = sb.ToString() };
            } else {
                return base.Redirect(url);
            }
        }

        /// <summary>
        /// Return a JSON object indicating success.
        /// </summary>
        /// <returns>This is used with client-side code when a JSON object is expected.</returns>
        protected ActionResult ReturnSuccess() {
            Manager.Verify_AjaxRequest();

            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(Basics.AjaxJavascriptReturn);
            return new JsonResult { Data = sb.ToString() };
        }

        // DATA BINDING
        // DATA BINDING
        // DATA BINDING

        /// <summary>
        /// Used by ModuleEdit controller only - ModuleEdit edits a generic ModuleDefinition so we need to bind it to the correct type from the controller.
        /// </summary>
        /// <param name="objType">The module type (the derived type).</param>
        /// <param name="modelName">The model name (always "Module").</param>
        /// <returns>The bound object of the specified type.</returns>
        protected object GetObjectFromModel(Type objType, string modelName) {
            Type parameterType = objType;
            IModelBinder binder = Binders.GetBinder(parameterType);
            IValueProvider valueProvider = ValueProvider;
            const Predicate<string> propertyFilter = null;

            ModelBindingContext bindingContext = new ModelBindingContext {
                FallbackToEmptyPrefix = true,
                ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, parameterType),
                ModelName = modelName,
                ModelState = ViewData.ModelState,
                PropertyFilter = propertyFilter,
                ValueProvider = valueProvider
            };

            object o = binder.BindModel(ControllerContext, bindingContext);
            if (o != null) {
                FixArgumentParmTrim(o);
                FixArgumentParmCase(o);
                FixDates(o);

                PropertyListSupport.CorrectModelState(o, ViewData.ModelState, modelName + ".");

                // translate any xxx.JSON properties to native objects (There is no use case for this)
                //if (ViewData.ModelState.IsValid)
                //    ReplaceJSONParms(filterContext.ActionParameters);

                // if we have a template action, search parameters for templates with actions and execute it
                if (ViewData.ModelState.IsValid) {
                    string templateName = HttpContext.Request.Form[Basics.TemplateName];
                    if (!string.IsNullOrWhiteSpace(templateName)) {
                        string actionValStr = HttpContext.Request.Form[Basics.TemplateAction];
                        string actionExtraStr = HttpContext.Request.Form[Basics.TemplateExtraData];
                        SearchTemplateArgument(templateName, actionValStr, actionExtraStr, o);
                    }
                }
            }
            return o;
        }
    }
}
