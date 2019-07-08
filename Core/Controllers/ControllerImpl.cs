/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Extensions;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using System.Linq;
using YetaWF.Core.Components;
using System.IO;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// The base class for any module-based controller. Modules within YetaWF that implement controllers derive from this class so controllers have access to module definitions.
    /// </summary>
    /// <typeparam name="TMod">The type of the module implementing the controller.</typeparam>
    public class ControllerImpl<TMod> : ControllerImpl where TMod : ModuleDefinition {

        // MODULE PROPERTIES
        // MODULE PROPERTIES
        // MODULE PROPERTIES

        /// <summary>
        /// Returns the module definitions YetaWF.Core.Modules.ModuleDefinition for the current module implementing the controller.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected TMod Module { get { return (TMod)CurrentModule; } }
    }

    /// <summary>
    /// Abstract base class for any module-based controller.
    /// </summary>
    public abstract class ControllerImpl : YetaWFController {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ControllerImpl), name, defaultValue, parms); }

        // MODULE INFO
        // MODULE INFO
        // MODULE INFO

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

        /// <summary>
        /// Returns a unique id usable for HTML.
        /// </summary>
        /// <param name="name">The prefix string used to generate the id.</param>
        protected string UniqueId(string name) {
            return Manager.UniqueId(name);
        }

        /// <summary>
        /// Returns the URL for the requested action with the specified arguments formatted as query string.
        /// </summary>
        /// <param name="actionName">The name of the action within the controller.</param>
        /// <param name="args">Optional anonymous object with arguments to be formatted as query string.</param>
        /// <returns>The URL.</returns>
        public string GetActionUrl(string actionName, object args = null) {
            string url = "/" + Area + "/" + ControllerName + "/" + actionName;
            QueryHelper query = QueryHelper.FromAnonymousObject(args);
            return query.ToUrl(url);
        }

        // CONTROLLER
        // CONTROLLER
        // CONTROLLER

        /// <summary>
        /// Called before the action result that is returned by an action method is executed.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action result.</param>
#if MVC6
#else
        protected override void OnResultExecuting(ResultExecutingContext filterContext) {
            // THIS SUPPRESSES CACHING
            // RESEARCH: Use OutputCache for actions that can be cached - first thought: that's how you're really going to mess up your site, need automatic solution
            // http://www.dotnet-tricks.com/Tutorial/mvc/4R5c050113-Understanding-Caching-in-Asp.Net-MVC-with-example.html
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
            filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();

            base.OnResultExecuting(filterContext);
        }
#endif
        /// <summary>
        /// Called when an unknown action is requested.
        /// </summary>
        /// <param name="actionName">The name of the unknown action.</param>
        /// <remarks>This results in a 404 Not Found HTTP error.</remarks>
#if MVC6
        // There doesn't appear to be any equivalent functionality in MVC6
        // We'll just say the page doesn't exist - this is only useful in development, otherwise who cares which action doesn't exist
#else
        protected override void HandleUnknownAction(string actionName) {
            //base.HandleUnknownAction(actionName);
            string error = __ResStr("errUnknownAction", "Unknown action {0} attempted in Controller {1}.", actionName, GetType().FullName);
            Logging.AddErrorLog(error);
            throw new HttpException(404, error);
        }
#endif
        /// <summary>
        /// Called when an action is about to be executed.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action.</param>
#if MVC6
        public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next) {
            Logging.AddTraceLog("Action Request - {0}", filterContext.Controller.GetType().FullName);
#else
        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            Logging.AddTraceLog("Action Request - {0}", filterContext.ActionDescriptor.ControllerDescriptor.ControllerType.FullName);
            YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you :-(
#endif

            await SetupActionContextAsync(filterContext);
            if (Manager.IsPostRequest) {
                // find the unique Id prefix (saved as hidden field in Form)
                string uniqueIdPrefix = null;
#if MVC6
                if (HttpContext.Request.HasFormContentType)
                    uniqueIdPrefix = HttpContext.Request.Form[Forms.UniqueIdPrefix];
#else
                    uniqueIdPrefix = HttpContext.Request.Form[Forms.UniqueIdPrefix];
#endif
                    if (string.IsNullOrEmpty(uniqueIdPrefix)) {
#if MVC6
                        uniqueIdPrefix = HttpContext.Request.Query[Forms.UniqueIdPrefix];
#else
                        uniqueIdPrefix = HttpContext.Request.QueryString[Forms.UniqueIdPrefix];
#endif
                    }
                    if (!string.IsNullOrEmpty(uniqueIdPrefix))
                        Manager.UniqueIdPrefix = uniqueIdPrefix;
                }
                Type ctrlType;
                string actionName;
#if MVC6
                ctrlType = filterContext.Controller.GetType();
                actionName = ((ControllerActionDescriptor)filterContext.ActionDescriptor).ActionName;
#else
                ctrlType = filterContext.ActionDescriptor.ControllerDescriptor.ControllerType;
                actionName = filterContext.ActionDescriptor.ActionName;
#endif
                MethodInfo mi = ctrlType.GetMethod(actionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                // check if the action is authorized by checking the module's authorization
                string level = null;
                PermissionAttribute permAttr = (PermissionAttribute)Attribute.GetCustomAttribute(mi, typeof(PermissionAttribute));
                if (permAttr != null)
                    level = permAttr.Level;

                ModuleDefinition mod = CurrentModule;
                if (!mod.IsAuthorized(level)) {
                    if (Manager.IsPostRequest) {
#if MVC6
                        filterContext.Result = new UnauthorizedResult();
#else
                        filterContext.Result = new HttpUnauthorizedResult();
#endif
                    } else {
                        // We get here if an action is attempted that the user is not authorized for
                        // we could attempt to capture and redirect to user login, whatevz
                        filterContext.Result = new EmptyResult();
                    }
                    return;
                }

                // action is about to start - if this is a postback or ajax request, we'll clean up parameters
                if (Manager.IsPostRequest) {
#if MVC6
                    IDictionary<string,object> parms = filterContext.ActionArguments;
#else
                    IDictionary<string, object> parms = filterContext.ActionParameters;
#endif
                    if (parms != null) {
                        // remove leading/trailing spaces based on TrimAttribute for properties
                        // and update ModelState for RequiredIfxxx attributes
#if MVC6
                        Controller controller = (Controller)filterContext.Controller;
                        ViewDataDictionary viewData = controller.ViewData;
#else
                        ViewDataDictionary viewData = filterContext.Controller.ViewData;
#endif
                        ModelStateDictionary modelState = viewData.ModelState;
                        foreach (var parm in parms) {
                            FixArgumentParmTrim(parm.Value);
                            FixArgumentParmCase(parm.Value);
                            await FixDataAsync(parm.Value);
                            CorrectModelState(parm.Value, modelState);
                        }

                        // translate any xxx.JSON properties to native objects
                        if (modelState.IsValid)
                            ReplaceJSONParms(parms);

                        // if we have a template action, search parameters for templates with actions and execute it
#if MVC6
                        if (HttpContext.Request.HasFormContentType) {
#else
#endif
                        string templateName = HttpContext.Request.Form[Basics.TemplateName];
                        if (!string.IsNullOrWhiteSpace(templateName)) {
                            string actionValStr = HttpContext.Request.Form[Basics.TemplateAction];
                            string actionExtraStr = HttpContext.Request.Form[Basics.TemplateExtraData];
                            foreach (var parm in parms) {
                                if (SearchTemplate(templateName, modelState.IsValid, actionValStr, actionExtraStr, parm)) {
                                    modelState.Clear();
                                    break;
                                }
                            }
                        }
#if MVC6
                        }
#else
#endif
                    }

                    // origin list (we already do this in global.asax.cs for GET, maybe move it there)
                    //string originList = (string) HttpContext.Request.Form[Globals.Link_OriginList];
                    //if (!string.IsNullOrWhiteSpace(originList))
                    //    Manager.OriginList = Utility.JsonDeserialize<List<Origin>>(originList);
                    //else
                    //    Manager.OriginList = new List<Origin>();

                    //string inPopup = HttpContext.Request.Form[Globals.Link_InPopup];
                    //if (!string.IsNullOrWhiteSpace(inPopup))
                    //    Manager.IsInPopup = true;

                    ViewData.Add(Globals.RVD_ModuleDefinition, CurrentModule);
                }
#if MVC6
                await base.OnActionExecutionAsync(filterContext, next);
#else
                base.OnActionExecuting(filterContext);
#endif
#if MVC6
#else
            }); // End of Syncify
#endif
        }

        internal static void CorrectModelState(object model, ModelStateDictionary ModelState, string prefix = "") {
            if (model == null) return;
            Type modelType = model.GetType();
            if (ModelState.Keys.Count() == 0) return;
            List<PropertyData> props = ObjectSupport.GetPropertyData(modelType);
            foreach (var prop in props) {
                if (!ModelState.Keys.Contains(prefix + prop.Name)) {
                    // check if the property name is for a class
                    string subPrefix = prefix + prop.Name + ".";
                    if ((from k in ModelState.Keys where k.StartsWith(subPrefix) select k).FirstOrDefault() != null) {
                        object subObject = prop.PropInfo.GetValue(model);
                        CorrectModelState(subObject, ModelState, subPrefix);
                    }
                    continue;
                }

                bool process = true;// overall whether we need to process this property
                bool hasAttribute = false;// has at least one attribute
                bool found = false;// found an enabling attribute
                if (!found) {
                    if (ExprAttribute.IsRequired(prop.ExprValidationAttributes, model)) {
                        found = true;
                        process = true;
                    }
                }
                if (!found) {
                    if (ExprAttribute.IsSelectionRequired(prop.ExprValidationAttributes, model)) {
                        found = true;
                        process = true;
                    }
                }
                if (!found) {
                    if (ExprAttribute.IsSuppressed(prop.ExprValidationAttributes, model)) {
                        found = true;
                        process = false;
                    }
                }
                if (process) {
                    if (hasAttribute && !found) {
                        // there was no attribute that made this required
                        // we don't process this property
                        ModelState.Remove(prefix + prop.Name);
                        continue;
                    } else {
                        // we process this property
                    }
                } else {
                    // we don't process this property
                    ModelState.Remove(prefix + prop.Name);
                }
            }
        }

        /// <summary>
        /// Returns whether the current form submission used the Apply button to submit the form.
        /// </summary>
        public bool IsApply {
            get {
                return (Manager.RequestForm[Globals.Link_SubmitIsApply] != null);
            }
        }
        /// <summary>
        /// Returns whether the current form submission is intended to reload the form without validation, essentially Apply without validation.
        /// </summary>
        public bool IsReload {
            get {
                return (Manager.RequestForm[Globals.Link_SubmitIsReload] != null);
            }
        }
        /// <summary>
        /// Returns whether the current form submission used the Submit button to submit the form.
        /// </summary>
        public bool IsSubmit {
            get {
                return !IsApply && !IsReload;
            }
        }

        // INPUT CLEANUP
        // INPUT CLEANUP
        // INPUT CLEANUP

        /// <summary>
        /// Handle all input fields and call template-specific pre-controller action.
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        private async Task<bool> FixDataAsync(object parm) {
            if (parm == null) return false;
            bool any = false;

            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                any = true;
                PropertyInfo pi = prop.PropInfo;
                if (pi.CanRead && pi.CanWrite) {

                    if (prop.UIHint != null && !prop.ReadOnly) {
                        // check template-specific processing
                        Dictionary<string, MethodInfo> meths = YetaWFComponentBaseStartup.GetComponentsWithControllerPreprocessAction();
                        MethodInfo meth;
                        if (meths.TryGetValue(prop.UIHint, out meth)) {
#if MVC6
                            ModelStateEntry modelStateEntry;
#else
                            ModelState modelStateEntry;
#endif
                            bool preprocess = false;
                            if (ModelState.TryGetValue(prop.UIHint, out modelStateEntry)) {
#if MVC6
                                if (modelStateEntry.ValidationState == ModelValidationState.Valid)
                                    preprocess = true;
#else
                                if (modelStateEntry.Errors.Count == 0)
                                    preprocess = true;
#endif
                            } else {
                                preprocess = true;
                            }
                            if (preprocess) { // don't call component if there already is an error
                                //string caption = prop.GetCaption(parm);
                                object obj = prop.GetPropertyValue<object>(parm);
                                Task methObjTask = (Task)meth.Invoke(null, new object[] { prop.Name, obj, ModelState });
                                await methObjTask.ConfigureAwait(false);
                                PropertyInfo resultProp = methObjTask.GetType().GetProperty("Result");
                                pi.SetValue(parm, resultProp.GetValue(methObjTask));
                            }
                        }
                    }

                    // Date/Time translation to UTC (independent of any templates)
                    // change all dates to utc - internally YetaWF ALWAYS uses utc
                    // incoming dates are sent from client in utc, but arrive in local time ("thanks" to ASP.NET translating them)
                    // so we translate them back to utc. There is probably a better way somewhere in ASP.NET, but haven't figured it out yet.
                    if (pi.PropertyType == typeof(DateTime) || pi.PropertyType == typeof(DateTime?)) {
                        DateTime? dt = prop.GetPropertyValue<DateTime?>(parm);
                        if (dt != null && ((DateTime)dt).Kind == DateTimeKind.Local) {
                            DateTime dl = (DateTime)dt;
                            if (prop.UIHint == "DateTime" || prop.UIHint == "Time" || prop.UIHint == "Date") {
                                // we're receiving date/time in the user's specified timezone so we now have to convert it to Utc
                                dt = Formatting.GetUtcDateTime(dl);
                                pi.SetValue(parm, dt, null);
                            }
                        }
                    } else {
                        ParameterInfo[] indexParms = pi.GetIndexParameters();
                        int indexParmsLen = indexParms.Length;
                        if (indexParmsLen == 0) {
                            try {
                                await FixDataAsync(prop.GetPropertyValue<object>(parm));  // try to handle nested types
                            } catch (Exception) { }
                        } else if (indexParmsLen == 1 && indexParms[0].ParameterType == typeof(int)) {
                            // enumerable types
                            IEnumerable<object> ienum = parm as IEnumerable<object>;
                            if (ienum != null) {
                                IEnumerator<object> ienumerator = ienum.GetEnumerator();
                                for (int i = 0; ienumerator.MoveNext(); i++) {
                                    await FixDataAsync(ienumerator.Current);
                                }
                            }
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
        private static bool SearchTemplate(string templateName, bool modelIsValid, string actionValStr, string actionExtraStr, KeyValuePair<string, object> pair) {
            return SearchTemplateArgument(templateName, modelIsValid, actionValStr, actionExtraStr, pair.Value);
        }
        private static bool SearchTemplateArgument(string templateName, bool modelIsValid, string actionValStr, string actionExtraStr, object parm) {
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
                                if (act.ExecuteAction(actionVal, modelIsValid, actionExtraStr))
                                    return true;
                                return false;
                            }
                        }
                        object o;
                        try {
                            o = prop.GetPropertyValue<object>(parm);
                        } catch (Exception) {
                            o = null;
                        }
                        if (o != null)
                            return SearchTemplateArgument(templateName, modelIsValid, actionValStr, actionExtraStr, o);  // handle nested types
                    }
                }
            }
            return false;
        }

        // Replace JSON parms
        private void ReplaceJSONParms(IDictionary<string, object> actionParms) {
#if MVC6
            if (HttpContext.Request.HasFormContentType) {
                foreach (var entry in HttpContext.Request.Form.Keys) {
                    if (entry != null && entry.EndsWith("-JSON")) {
                        string data = HttpContext.Request.Form[entry];
                        string parmName = entry.Substring(0, entry.Length - 5);
                        AddJSONParmData(actionParms, parmName, data);
                    }
                }
            }
#else
            foreach (var entry in HttpContext.Request.Form.AllKeys) {
                if (entry != null && entry.EndsWith("-JSON")) {
                    string data = HttpContext.Request.Form[entry];
                    string parmName = entry.Substring(0, entry.Length - 5);
                    AddJSONParmData(actionParms, parmName, data);
                }
            }
#endif
        }

        private void AddJSONParmData(IDictionary<string, object> actionParms, string parmName, string jsonData) {
            // search each direct parm
            // NOT SUPPORTED FOR DIRECT PARMS (MUST USE MODEL)
            //foreach (var parm in actionParms) {
            //    if (string.Compare(parm.Key, parmName, true) == 0) {
            //        object parmData = Utility.JsonDeserialize(jsonData, parm.Value.GetType());
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
                            object parmData = Utility.JsonDeserialize(jsonData, propInfo.PropertyType);
                            propInfo.SetValue(parm.Value, parmData);
                        }
                    }
                }
            }
        }

        // VIEW
        // VIEW
        // VIEW

        /// <summary>
        /// Invokes a view from a module controller.
        /// </summary>
        /// <returns>An action result.</returns>
        [Obsolete("This form of the View() method is not supported by YetaWF")]
        protected new YetaWFViewResult View() { throw new NotSupportedException(); }
#if MVC6
#else
        /// <summary>
        /// Returns a view action result. Not supported in YetaWF.
        /// </summary>
        /// <param name="view">IView interface.</param>
        /// <returns>An action result.</returns>
        [Obsolete("This form of the View() method is not supported by YetaWF")]
        protected new YetaWFViewResult View(IView view) { throw new NotSupportedException(); }
        /// <summary>
        /// Returns a view action result. Not supported in YetaWF.
        /// </summary>
        /// <param name="view">IView interface.</param>
        /// <param name="model">The data model.</param>
        /// <returns>An action result.</returns>
        [Obsolete("This form of the View() method is not supported by YetaWF")]
        protected new YetaWFViewResult View(IView view, object model) { throw new NotSupportedException(); }
        /// <summary>
        /// Returns a view action result. Not supported in YetaWF.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="masterName">The master page name.</param>
        /// <returns>An action result.</returns>
        [Obsolete("This form of the View() method is not supported by YetaWF")]
        protected new YetaWFViewResult View(string viewName, string masterName) { throw new NotSupportedException(); }
        /// <summary>
        /// Returns a view action result. Not supported in YetaWF.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="masterName">The master page name.</param>
        /// <param name="model">The data model.</param>
        /// <returns>An action result.</returns>
        [Obsolete("This form of the View() method is not supported by YetaWF")]
        protected new virtual YetaWFViewResult View(string viewName, string masterName, object model) { throw new NotSupportedException(); }
#endif
        /// <summary>
        /// Renders the default view (defined using ModuleDefinition.DefaultView) using the provided model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A YetaWFViewResult.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Justification = "This is deliberate so the base class implementation isn't used accidentally")]
        protected new YetaWFViewResult View(object model) {
            return View(null, model, UseAreaViewName: true);
        }
        /// <summary>
        /// Renders the specified view.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="UseAreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <returns>A YetaWFViewResult.</returns>
        protected YetaWFViewResult View(string viewName, bool UseAreaViewName = true) {
            return View(viewName, null, UseAreaViewName: UseAreaViewName);
        }
        /// <summary>
        /// Renders the specified view.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="model">The model.</param>
        /// <param name="UseAreaViewName">true if the view name is the name of a standard view, otherwise the area specific view by that name is used.</param>
        /// <returns>A YetaWFViewResult.</returns>
        protected YetaWFViewResult View(string viewName, object model, bool UseAreaViewName = true) {
            if (UseAreaViewName) {
                if (string.IsNullOrWhiteSpace(viewName))
                    viewName = CurrentModule.DefaultViewName;
                else
                    viewName = YetaWFController.MakeFullViewName(viewName, Area);
                if (string.IsNullOrWhiteSpace(viewName)) {
#if MVC6
                    viewName = (string)RouteData.Values["action"];
#else
                    viewName = ControllerContext.RouteData.GetRequiredString("action");
#endif
                    viewName = YetaWFController.MakeFullViewName(viewName, Area);
                }
            }
            if (string.IsNullOrWhiteSpace(viewName))
                throw new InternalError("Missing view name");

            ViewData.Model = model;
            return new YetaWFViewResult(this, viewName, CurrentModule, model);
        }

        /// <summary>
        /// An instance of the YetaWFViewResult is returned from action methods when requesting views.
        /// </summary>
        public class YetaWFViewResult : ActionResult {

            private ModuleDefinition Module { get; set; }
            private object Model { get; set; }
            private string ViewName { get; set; }
            private YetaWFController RequestingController { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="requestingController">The controller requesting the view.</param>
            /// <param name="viewName">The name of the view.</param>
            /// <param name="module">The module on behalf of which to view is rendered.</param>
            /// <param name="model">The view's data model.</param>
            public YetaWFViewResult(YetaWFController requestingController, string viewName, ModuleDefinition module, object model) {
                ViewName = viewName;
                Module = module;
                Model = model;
                RequestingController = requestingController;
            }
#if MVC6
            public override async Task ExecuteResultAsync(ActionContext context) {

                using (var sw = new StringWriter()) {
                    YHtmlHelper htmlHelper = new YHtmlHelper(context, context.ModelState);
                    string data = await htmlHelper.ForViewAsync(ViewName, Module, Model);
#if DEBUG
                    if (sw.ToString().Length > 0)
                        throw new InternalError($"View {ViewName} wrote output which is not supported - All output must be rendered using ForViewAsync and returned as a string - output rendered: \"{sw.ToString()}\"");
#endif
                    if (!string.IsNullOrWhiteSpace(data)) {
                        byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data.ToString());
                        Stream body = context.HttpContext.Response.Body;
                        body.Write(buffer, 0, buffer.Length);
                    }
                }
            }
#else
            /// <summary>
            /// Enables processing of the result of an action method by a custom type that inherits from the ActionResult class.
            /// </summary>
            /// <param name="context">The context in which the result is executed. The context information includes the controller, HTTP content, request context, and route data.</param>
            public override void ExecuteResult(ControllerContext context) {

                TextWriter sw = context.HttpContext.Response.Output;
                YHtmlHelper htmlHelper = new YHtmlHelper(context.RequestContext, context.Controller.ViewData.ModelState);

                try {
                    YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you here :-(
                        string data = await htmlHelper.ForViewAsync(ViewName, Module, Model);
#if DEBUG
                        if (sw.ToString().Length > 0)
                            throw new InternalError($"View {ViewName} wrote output which is not supported - All output must be rendered using ForViewAsync and returned as a string - output rendered: \"{sw.ToString()}\"");
#endif
                        if (!string.IsNullOrWhiteSpace(data))
                            sw.Write(data.ToString());
                    });
                } catch (Exception) {
                    throw;
                } finally { }
            }
#endif
        }

        // PAGE/FORM SAVE
        // PAGE/FORM SAVE
        // PAGE/FORM SAVE

        /// <summary>
        /// The type of form reload used with the Reload method.
        /// </summary>
        protected enum ReloadEnum {
            /// <summary>
            /// The entire page is reloaded.
            /// </summary>
            Page = 1,
            /// <summary>
            /// The entire module is reloaded. Not currently supported. Use Page to reload the entire page instead.
            /// </summary>
            Module = 2, // TODO: The entire module is not currently supported - use page reload instead
            /// <summary>
            /// Parts of the module are reloaded. E.g., in a grid control the data is reloaded.
            /// </summary>
            ModuleParts = 3
        }

        /// <summary>
        /// Returns an action result, indicating that the submission was successfully processed, causing a page or module reload, optionally with a popup message.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="dummy">Dummy variable to separate positional arguments from named arguments.</param>
        /// <param name="PopupText">The optional text of the popup message to be displayed. If not specified, no popup will be shown.</param>
        /// <param name="PopupTitle">The optional title of the popup message to be displayed. If not specified, the default is "Success".</param>
        /// <param name="Reload">The method with which the current page or module is processed, i.e., by reloading the page or module.</param>
        /// <returns></returns>
        protected ActionResult Reload(object model = null, int dummy = 0, string PopupText = null, string PopupTitle = null, ReloadEnum Reload = ReloadEnum.Page) {
            if (Manager.IsPostRequest) {
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
                return new YJsonResult { Data = sb.ToString() };
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("$YetaWF.alert({0}, {1}, function() {{ $YetaWF.reloadPage(true); }});", popupText, popupTitle);
                return new YJsonResult { Data = sb.ToString() };
            }
        }
        private ActionResult Reload_Module(object model, string popupText, string popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModule);
                return new YJsonResult { Data = sb.ToString() };
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("$YetaWF.alert({0}, {1}, function() {{ $YetaWF.reloadModule(); }});", popupText, popupTitle);
                return new YJsonResult { Data = sb.ToString() };
            }
        }
        private ActionResult Reload_ModuleParts(object model, string popupText, string popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                return new YJsonResult { Data = sb.ToString() };
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                sb.Append("$YetaWF.alert({0}, {1});", popupText, popupTitle);
                return new YJsonResult { Data = sb.ToString() };
            }
        }

        /// <summary>
        /// An action result that results in a 403 Not Authorized exception.
        /// </summary>
        /// <returns>An action result.</returns>
        protected new ActionResult NotAuthorized() {
            return NotAuthorized(null);
        }
        /// <summary>
        /// An action result that results in a 403 Not Authorized exception.
        /// </summary>
        /// <param name="message">The message text to be shown on an error page (GET requests only) along with the 403 exception.</param>
        /// <returns>An action result.</returns>
        protected ActionResult NotAuthorized(string message) {

            message = message ?? __ResStr("notAuth", "Not Authorized");

            if (Manager.IsPostRequest) {
#if MVC6
                return new UnauthorizedResult();
#else
                return new HttpUnauthorizedResult();
#endif
            } else {
#if MVC6
#else
                Manager.CurrentResponse.Status = "403 Not Authorized";
#endif
                Manager.CurrentResponse.StatusCode = 403;
                return View("ShowMessage", message, UseAreaViewName: false);
            }
        }

        /// <summary>
        /// The type of processing used when closing a popup window, used with the FormProcessed method.
        /// </summary>
        protected enum OnPopupCloseEnum {
            /// <summary>
            /// No processing. The popup is not closed.
            /// </summary>
            Nothing = 0,
            /// <summary>
            /// No processing. The popup is closed.
            /// </summary>
            ReloadNothing = 1,
            /// <summary>
            /// The popup is closed and the parent page is reloaded.
            /// </summary>
            ReloadParentPage = 2,
            /// <summary>
            /// The popup is closed and the module is reloaded.
            /// </summary>
            ReloadModule = 3,
            /// <summary>
            /// The popup is closed and a new page is loaded.
            /// </summary>
            GotoNewPage = 4,
            /// <summary>
            /// The popup is not closed and the module is updated in place with the new model.
            /// </summary>
            UpdateInPlace = 5,
        }
        /// <summary>
        /// The type of processing used when closing a page, used with the FormProcessed method.
        /// </summary>
        protected enum OnCloseEnum {
            /// <summary>
            /// No processing. The page is not closed.
            /// </summary>
            Nothing = 0,
            /// <summary>
            /// The page is reloaded with the previous page save in the OriginList. If none is available, the Home page is loaded.
            /// </summary>
            Return = 1,
            /// <summary>
            /// A new page is loaded.
            /// </summary>
            GotoNewPage = 2,
            /// <summary>
            /// The page/module is updated in place with the new model.
            /// </summary>
            UpdateInPlace = 3,
            /// <summary>
            /// The current page is reloaded.
            /// </summary>
            ReloadPage = 4,
            /// <summary>
            /// The current page is closed, which will close the browser window.
            /// </summary>
            CloseWindow = 9,
        }
        /// <summary>
        /// The type of processing used when processing the Apply action for a form, used with the FormProcessed method.
        /// </summary>
        protected enum OnApplyEnum {
            /// <summary>
            /// Reload the current module.
            /// </summary>
            ReloadModule = 1,
            /// <summary>
            /// Reload the current page.
            /// </summary>
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
        /// <param name="NextPage">The URL where the page is redirected (OnClose or OnPopupClose must request a matching action, otherwise this is ignored).</param>
        /// <param name="ExtraJavaScript">Optional additional Javascript code that is returned as part of the ActionResult.</param>
        /// <param name="ForceRedirect">Force a real redirect bypassing Unified Page Set handling.</param>
        /// <param name="PopupOptions">TODO: This is not a good option, passes JavaScript/JSON to the client side for the popup window.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        protected ActionResult FormProcessed(object model, string popupText = null, string popupTitle = null,
                OnCloseEnum OnClose = OnCloseEnum.Return, OnPopupCloseEnum OnPopupClose = OnPopupCloseEnum.ReloadParentPage, OnApplyEnum OnApply = OnApplyEnum.ReloadModule,
                string NextPage = null, string ExtraJavaScript = null, bool ForceRedirect = false, string PopupOptions = null) {

            ScriptBuilder sb = new ScriptBuilder();

            if (ExtraJavaScript != null)
                sb.Append(ExtraJavaScript);

            popupText = string.IsNullOrWhiteSpace(popupText) ? null : Utility.JsonSerialize(popupText);
            popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
            PopupOptions = PopupOptions ?? "null";

            bool isApply = IsApply || IsReload;
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
            if (ForceRedirect || !string.IsNullOrWhiteSpace(NextPage)) {
                string url = NextPage;
                if (string.IsNullOrWhiteSpace(url))
                    url = Manager.CurrentSite.HomePageUrl;
                url = AddUrlPayload(url, false, false);
                if (ForceRedirect)
                    url = QueryHelper.AddRando(url);
                url = Utility.JsonSerialize(url);

                if (Manager.IsInPopup) {
                    if (ForceRedirect) {
                        if (string.IsNullOrWhiteSpace(popupText)) {
                            sb.Append("$YetaWF.setLoading();window.parent.location.assign({0});", url);
                        } else {
                            sb.Append(
                               "$YetaWF.alert({0}, {1}, function() {{ $YetaWF.setLoading(); window.parent.location.assign({2}); }}, {3});", popupText, popupTitle, url, PopupOptions);
                        }
                    } else if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append(
                            "$YetaWF.setLoading();" +
                            "if (!window.parent.$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({0}), true))" +
                                "window.parent.location.assign({0});",
                                url);
                    } else {
                        sb.Append(
                            "$YetaWF.alert({0}, {1}, function() {{" +
                                "$YetaWF.setLoading();" +
                                "if (!window.parent.$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({2}), true))" +
                                "window.parent.location.assign({2});" +
                            "}}, {3});", popupText, popupTitle, url, PopupOptions);
                    }
                } else {
                    if (ForceRedirect) {
                        if (string.IsNullOrWhiteSpace(popupText)) {
                            sb.Append("$YetaWF.setLoading();window.location.assign({0});", url);
                        } else {
                            sb.Append(
                               "$YetaWF.alert({0}, {1}, function() {{ $YetaWF.setLoading(); window.location.assign({2}); }}, {3});", popupText, popupTitle, url, PopupOptions);
                        }
                    } else if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append(
                            "$YetaWF.setLoading();" +
                            "if (!$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({0}), true))" +
                              "window.location.assign({0});",
                                url);
                    } else {
                        sb.Append(
                           "$YetaWF.alert({0}, {1}, function() {{" +
                             "$YetaWF.setLoading();" +
                             "if (!$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({2}), true))" +
                               "window.location.assign({2});" +
                           "}}, {3});", popupText, popupTitle, url, PopupOptions);
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
                                sb.Append("$YetaWF.closePopup(false);");
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                sb.Append("$YetaWF.closePopup(true);");
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                sb.Append("window.parent.$YetaWF.refreshPage();");
                                sb.Append("$YetaWF.closePopup(false);");
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    } else {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                                throw new InternalError("No next page");
                            case OnPopupCloseEnum.Nothing:
                                sb.Append("$YetaWF.alert({0}, {1}, null, {2});", popupText, popupTitle, PopupOptions);
                                break;
                            case OnPopupCloseEnum.ReloadNothing:
                                sb.Append("$YetaWF.alert({0}, {1}, function() {{ $YetaWF.closePopup(false); }}, {2});", popupText, popupTitle, PopupOptions);
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                sb.Append("$YetaWF.alert({0}, {1}, function() {{ $YetaWF.closePopup(true); }}, {2});", popupText, popupTitle, PopupOptions);
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                sb.Append("$YetaWF.alert({0}, {1}, null, {2});", popupText, popupTitle, PopupOptions);
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                sb.Append("$YetaWF.alert({0}, {1}, function() {{ window.parent.$YetaWF.refreshPage(); $YetaWF.closePopup(false); }}, {2});", popupText, popupTitle, PopupOptions);
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
                                sb.Append("$YetaWF.alert({0}, {1}, {2});", popupText, popupTitle, PopupOptions);
                            break;
                        case OnCloseEnum.UpdateInPlace:
                            if (!string.IsNullOrWhiteSpace(popupText)) {
                                sb.Append("$YetaWF.alert({0}, {1}, {2});", popupText, popupTitle, PopupOptions);
                            }
                            isApply = true;
                            break;
                        case OnCloseEnum.Return:
                            if (Manager.OriginList == null || Manager.OriginList.Count == 0) {
                                if (string.IsNullOrWhiteSpace(popupText))
                                    sb.Append("window.close();");
                                else
                                    sb.Append("$YetaWF.alert({0}, {1}, function() {{ window.close(); }}, {2});", popupText, popupTitle, PopupOptions);
                            } else {
                                url = Utility.JsonSerialize(Manager.ReturnToUrl);
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append("if (!$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({0}), true))" +
                                            "window.location.assign({0});", url);
                                } else {
                                    sb.Append("$YetaWF.alert({0}, {1}, function() {{" +
                                        "if (!$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({0}), true))" +
                                          "window.location.assign({2});" +
                                      "}}, {3});", popupText, popupTitle, url, PopupOptions);
                                }
                            }
                            break;
                        case OnCloseEnum.CloseWindow:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append("window.close();");
                            else
                                sb.Append("$YetaWF.alert({0}, {1}, function() {{ window.close(); }}, {2});", popupText, popupTitle, PopupOptions);
                            break;
                        case OnCloseEnum.ReloadPage:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append("$YetaWF.reloadPage(true);");
                            else
                                sb.Append("$YetaWF.alert({0}, {1}, function() {{ $YetaWF.reloadPage(true); }}, {2});", popupText, popupTitle, PopupOptions);
                            break;
                        default:
                            throw new InternalError("Invalid OnClose value {0}", OnClose);
                    }
                }
                if (isApply) {
                    if (OnApply == OnApplyEnum.ReloadPage) {
                        if (string.IsNullOrWhiteSpace(popupText))
                            sb.Append("$YetaWF.reloadPage(true);");
                        else
                            sb.Append("$YetaWF.alert({0}, {1}, function() {{ $YetaWF.reloadPage(true); }}, {2});", popupText, popupTitle, PopupOptions);
                    } else {
                        return PartialView(model, sb);
                    }
                }
            }
            return new YJsonResult { Data = $"{Basics.AjaxJavascriptReturn}{sb.ToString()}" };
        }

        // REDIRECT
        // REDIRECT
        // REDIRECT


        /// <summary>
        /// Redirects to the specified URL, aborting page rendering. Can be used with UPS.
        /// </summary>
        /// <param name="url">The URL where the page is redirected.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        /// <remarks>
        /// The Redirect method can be used for GET and also within content rendering (UPS).
        /// </remarks>
        protected ActionResult RedirectToUrl(string url) {
#if MVC6
            Manager.CurrentResponse.StatusCode = 307; // Temporary redirect
            Manager.CurrentResponse.Headers.Add("Location", url);
#else
            Manager.CurrentResponse.Status = "307 - Redirect";
            Manager.CurrentResponse.AddHeader("Location", url);
#endif
            if (Manager.RenderContentOnly) {
                // nothing
            } else {
#if MVC6
#else
                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
            }
            return new EmptyResult();
        }

        /// <summary>
        /// Redirect to the specified target defined by the supplied action.
        /// </summary>
        /// <param name="action">The ModuleAction defining the target where the page is redirected.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        /// <remarks>
        /// The Redirect method can be used for PUT, Ajax requests and also within popups.
        /// This works in cooperation with client-side code to redirect popups, etc., which is normally not supported in MVC.
        /// </remarks>
        protected ActionResult Redirect(ModuleAction action) {
            if (action == null)
                return Redirect("");
            return Redirect(action.GetCompleteUrl(), ForcePopup: action.Style == ModuleAction.ActionStyleEnum.Popup || action.Style == ModuleAction.ActionStyleEnum.ForcePopup);
        }

        /// <summary>
        /// Redirects to the specified target URL.
        /// </summary>
        /// <param name="url">The URL defining the target where the page is redirected. If null is specified, the site's Home page is used instead.</param>
        /// <param name="ForcePopup">true if the redirect should occur in a popup window, false otherwise for a redirect within the browser window.</param>
        /// <param name="SetCurrentEditMode">true if the new page should be shown using the current Site Edit/Display Mode, false otherwise.</param>
        /// <param name="ExtraJavascript">Optional Javascript code executed when redirecting to another URL within a Unified Page Set.</param>
        /// <param name="SetCurrentControlPanelMode">Sets the current control panel mode (visibility).</param>
        /// <param name="ForceRedirect">true to force a page load (even in a Unified Page Set), false otherwise to use the default page or page content loading.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        /// <remarks>
        /// The Redirect method can be used for GET, PUT, Ajax requests and also within popups.
        /// This works in cooperation with client-side code to redirect popups, etc., which is normally not supported in MVC.
        /// </remarks>
        protected ActionResult Redirect(string url, bool ForcePopup = false, bool SetCurrentEditMode = false, bool SetCurrentControlPanelMode = false, bool ForceRedirect = false, string ExtraJavascript = null) {

            if (ForceRedirect && ForcePopup) throw new InternalError("Can't use ForceRedirect and ForcePopup at the same time");
            if (!string.IsNullOrWhiteSpace(ExtraJavascript) && !Manager.IsPostRequest) throw new InternalError("ExtraJavascript is only supported with POST requests");

            if (string.IsNullOrWhiteSpace(url))
                url = Manager.CurrentSite.HomePageUrl;

            url = AddUrlPayload(url, SetCurrentEditMode, SetCurrentControlPanelMode);
            if (ForceRedirect)
                url = QueryHelper.AddRando(url);

            if (Manager.IsPostRequest) {
                // for post requests we return javascript to redirect
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
#if MVC6
                    if (Manager.CurrentRequest.IsHttps) {
#else
                    if (Manager.CurrentRequest.IsSecureConnection) {
#endif
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
                    url = Utility.JsonSerialize(url);
                    if (Manager.IsInPopup) {
                        // simply replace the current popup with the new popup
                        sb.Append("window.parent.$YetaWF.Popups.openPopup({0}, false);", url);
                    } else {
                        // create the popup client-side
                        sb.Append("$YetaWF.Popups.openPopup({0}, false);", url);
                    }
                } else {
                    url = Utility.JsonSerialize(url);
                    if (ForceRedirect) {
                        sb.Append("$YetaWF.setLoading(); window.location.assign({0});", url);
                    } else if (Manager.IsInPopup) {
                        sb.Append("$YetaWF.setLoading();" +
                            "window.parent.location.assign({0});", url);
                    } else {
                        sb.Append(
                            "$YetaWF.setLoading();" +
                            "{1}" +
                            "if (!$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({0}), true))" +
                              "window.location.assign({0});",
                                url, (string.IsNullOrWhiteSpace(ExtraJavascript) ? "" : ExtraJavascript));
                    }
                }
                return new YJsonResult { Data = sb.ToString() };
            } else {
                return base.Redirect(url);
            }
        }

        private static string AddUrlPayload(string url, bool SetCurrentEditMode, bool SetCurrentControlPanelMode) {

            string urlOnly;
            QueryHelper qhUrl = QueryHelper.FromUrl(url, out urlOnly);
            // If we're coming from a referring page with edit/noedit, we need to propagate that to the redirect
            if (SetCurrentEditMode) { // forced set edit mode
                qhUrl.Remove(Globals.Link_EditMode);
                qhUrl.Remove(Globals.Link_NoEditMode);
                if (Manager.EditMode)
                    qhUrl.Add(Globals.Link_EditMode, "y");
            } else if (!qhUrl.HasEntry(Globals.Link_EditMode) && !qhUrl.HasEntry(Globals.Link_NoEditMode)) {
                // current url has no edit/noedit preference
                if (Manager.EditMode) {
                    // in edit mode, force edit again
                    qhUrl.Add(Globals.Link_EditMode, "y");
                } else {
                    // not in edit mode, use referrer mode
                    string referrer = Manager.ReferrerUrl;
                    if (!string.IsNullOrWhiteSpace(referrer)) {
                        string refUrlOnly;
                        QueryHelper qhRef = QueryHelper.FromUrl(referrer, out refUrlOnly);
                        if (qhRef.HasEntry(Globals.Link_EditMode)) { // referrer is edit
                            qhUrl.Add(Globals.Link_EditMode, "y", Replace: true);
                        }
                    }
                }
            }
            if (SetCurrentControlPanelMode) {
                qhUrl.Remove(Globals.Link_PageControl);
                qhUrl.Remove(Globals.Link_NoPageControl);
                if (Manager.PageControlShown)
                    qhUrl.Add(Globals.Link_PageControl, "y");
                else
                    qhUrl.Add(Globals.Link_NoPageControl, "y");
            } else {
                // check whether control panel should be open
                if (!qhUrl.HasEntry(Globals.Link_PageControl) && !qhUrl.HasEntry(Globals.Link_NoPageControl)) {
                    if (Manager.PageControlShown)
                        qhUrl.Add(Globals.Link_PageControl, "y");
                }
            }
            url = qhUrl.ToUrl(urlOnly);
            return url;
        }

        /// <summary>
        /// Return a JSON object indicating success.
        /// </summary>
        /// <returns>This is used with client-side code when a JSON object is expected.</returns>
        protected ActionResult ReturnSuccess() {
            Manager.Verify_PostRequest();

            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(Basics.AjaxJavascriptReturn);
            return new YJsonResult { Data = sb.ToString() };
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
#if MVC6
        protected async Task<object> GetObjectFromModelAsync(Type objType, string modelName)
#else
        protected async Task<object> GetObjectFromModelAsync(Type objType, string modelName)
#endif
        {
            object obj;
#if MVC6
            obj = Activator.CreateInstance(objType);
            if (obj == null)
                throw new InternalError("Object with type {0} cannot be instantiated", objType.FullName);
            bool result = await TryUpdateModelAsync(obj, objType, modelName);
            if (!result)
                throw new InternalError("Model with type {0} cannot be updated", objType.FullName);
#else
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
            obj = binder.BindModel(ControllerContext, bindingContext);
#endif
            if (obj != null) {
                FixArgumentParmTrim(obj);
                FixArgumentParmCase(obj);
                await FixDataAsync(obj);

                CorrectModelState(obj, ViewData.ModelState, modelName + ".");

                // translate any xxx.JSON properties to native objects (There is no use case for this)
                //if (ViewData.ModelState.IsValid)
                //    ReplaceJSONParms(filterContext.ActionParameters);

                // Search parameters for templates with actions and execute the action
                string templateName = HttpContext.Request.Form[Basics.TemplateName];
                if (!string.IsNullOrWhiteSpace(templateName)) {
                    string actionValStr = HttpContext.Request.Form[Basics.TemplateAction];
                    string actionExtraStr = HttpContext.Request.Form[Basics.TemplateExtraData];
                    if (SearchTemplateArgument(templateName, ViewData.ModelState.IsValid, actionValStr, actionExtraStr, obj))
                        ViewData.ModelState.Clear();
                }
            }
#if MVC6
            return obj;
#else
            return Task.FromResult<object>(obj);
#endif
        }
    }
}
