/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.Extensions;
using YetaWF.Core.Localize;
using YetaWF.Core.Log;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

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
        protected TMod Module { get { return (TMod)CurrentModule; } }
    }

    /// <summary>
    /// Abstract base class for any module-based controller.
    /// </summary>
    public abstract class ControllerImpl : YetaWFController {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ControllerImpl), name, defaultValue, parms); }

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
        private string? _ClassName { get; set; }

        private const string CONTROLLER_NAMESPACE = "(xcompanyx.Modules.xproductx.Controllers)";

        private void GetModuleInfo() {
            if (string.IsNullOrEmpty(_ModuleName)) {
                Package package = Package.GetPackageFromAssembly(GetType().Assembly);
                string ns = GetType().Namespace !;

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
            }
        }
        private string _ModuleName { get; set; } = null!;
        private string _Product { get; set; } = null!;
        private string _Area { get; set; } = null!;
        private string _CompanyName { get; set; } = null!;
        private string _Domain { get; set; } = null!;
        private string _ControllerName { get; set; } = null!;

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
        public string GetActionUrl(string actionName, object? args = null) {
            string url = $"/{Area}/{ControllerName}/{actionName}";
            QueryHelper query = QueryHelper.FromAnonymousObject(args);
            return query.ToUrl(url);
        }

        // CONTROLLER
        // CONTROLLER
        // CONTROLLER


        /// <summary>
        /// Called when an action is about to be executed.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action.</param>
        public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next) {

            Logging.AddTraceLog("Action Request - {0}", filterContext.Controller.GetType().FullName!);
            await SetupActionContextAsync(filterContext);

            Type ctrlType = filterContext.Controller.GetType();
            string actionName = ((ControllerActionDescriptor)filterContext.ActionDescriptor).ActionName;

            MethodInfo? mi = ctrlType.GetMethod(actionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
            if (mi == null)
                throw new InternalError($"Action {actionName} not found on {filterContext.Controller.GetType().FullName}");
            // check if the action is authorized by checking the module's authorization
            string? level = null;
            PermissionAttribute? permAttr = (PermissionAttribute?)Attribute.GetCustomAttribute(mi, typeof(PermissionAttribute));
            if (permAttr != null)
                level = permAttr.Level;

            ModuleDefinition mod = CurrentModule;
            if (!mod.IsAuthorized(level)) {
                if (Manager.IsPostRequest) {
                    filterContext.Result = new UnauthorizedResult();
                } else {
                    // We get here if an action is attempted that the user is not authorized for
                    // we could attempt to capture and redirect to user login, whatevz
                    filterContext.Result = new EmptyResult();
                }
                return;
            }

            // action is about to start - if this is a postback or ajax request, we'll clean up parameters
            if (Manager.IsPostRequest) {
                IDictionary<string,object?> parms = filterContext.ActionArguments;
                if (parms != null) {
                    // remove leading/trailing spaces based on TrimAttribute for properties
                    // and update ModelState for RequiredIfxxx attributes
                    Controller controller = (Controller)filterContext.Controller;
                    ViewDataDictionary viewData = controller.ViewData;
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
                    if (HttpContext.Request.HasFormContentType) {
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
                    }
                }

                ViewData.Add(Globals.RVD_ModuleDefinition, CurrentModule);
            }

            await base.OnActionExecutionAsync(filterContext, next);
        }

        internal static void CorrectModelState(object? model, ModelStateDictionary modelState, string prefix = "") {
            // This is ugly, fighting .net model binding/validation all the way. I gave up. This works.
            if (model == null) return;
            Type modelType = model.GetType();
            if (!modelState.Keys.Any()) return;
            List<PropertyData> props = ObjectSupport.GetPropertyData(modelType);
            foreach (var prop in props) {

                if (!modelState.Keys.Contains(prefix + prop.Name)) {
                    // check if the property name is for a class object
                    string subPrefix = prefix + prop.Name + ".";
                    if ((from k in modelState.Keys where k.StartsWith(subPrefix) select k).FirstOrDefault() != null) {
                        if (ExprAttribute.IsProcessed(prop.ExprValidationAttributes, model) && !ExprAttribute.IsHide(prop.ExprValidationAttributes, model)) {
                            object? subObject = prop.PropInfo.GetValue(model);
                            CorrectModelState(subObject, modelState, subPrefix);
                        } else {
                            RemoveModelState(modelState, prefix + prop.Name);
                        }
                    }
                    continue;
                }

                bool process = true;// overall whether we need to process this property
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
                if (!process) {
                    // we don't process this property
                    RemoveModelState(modelState, prefix + prop.Name);
                }
            }
        }

        private static void RemoveModelState(ModelStateDictionary modelState, string name, bool RemoveChildren = true) {

            modelState.Remove(name);
            if (RemoveChildren) {
                string prefix = $"{name}.";
                List<string> keys = modelState.Keys.ToList();
                foreach (string key in keys) {
                    if (key.StartsWith(prefix))
                        modelState.Remove(key);
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
        private async Task<bool> FixDataAsync(object? parm) {
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
                        if (meths.TryGetValue(prop.UIHint, out MethodInfo? meth)) {
                            bool preprocess = false;
                            if (ModelState.TryGetValue(prop.UIHint, out ModelStateEntry? modelStateEntry)) {
                                if (modelStateEntry.ValidationState == ModelValidationState.Valid)
                                    preprocess = true;
                            } else {
                                preprocess = true;
                            }
                            if (preprocess) { // don't call component if there already is an error
                                //string caption = prop.GetCaption(parm);
                                object? obj = prop.GetPropertyValue<object?>(parm);
                                Task methObjTask = (Task)meth.Invoke(null, new object?[] { prop.Name, obj, ModelState }) !;
                                await methObjTask.ConfigureAwait(false);
                                PropertyInfo resultProp = methObjTask.GetType().GetProperty("Result") !;
                                pi.SetValue(parm, resultProp.GetValue(methObjTask));
                            }
                        }
                    }

                    ParameterInfo[] indexParms = pi.GetIndexParameters();
                    int indexParmsLen = indexParms.Length;
                    if (indexParmsLen == 0) {
                        try {
                            await FixDataAsync(prop.GetPropertyValue<object?>(parm));  // try to handle nested types
                        } catch (Exception) { }
                    } else if (indexParmsLen == 1 && indexParms[0].ParameterType == typeof(int)) {
                        // enumerable types
                        if (parm is IEnumerable<object> ienum) {
                            IEnumerator<object> ienumerator = ienum.GetEnumerator();
                            for (int i = 0; ienumerator.MoveNext(); i++) {
                                await FixDataAsync(ienumerator.Current);
                            }
                        }
                    }
                }
            }
            return any;
        }

        // update all model parameters and trim as requested
        private static bool FixArgumentParmTrim(object? parm) {
            if (parm == null) return false;
            bool any = false;

            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                any = true;
                TrimAttribute? trimAttr = prop.TryGetAttribute<TrimAttribute>();
                if (trimAttr != null) {
                    TrimAttribute.EnumStyle style = trimAttr.Value;
                    if (style != TrimAttribute.EnumStyle.None) {
                        PropertyInfo pi = prop.PropInfo;
                        if (pi.PropertyType == typeof(MultiString)) {
                            MultiString ms = prop.GetPropertyValue<MultiString>(parm);
                            ms.Trim();
                        } else if (pi.PropertyType == typeof(string)) {
                            if (pi.CanWrite) {
                                string? val = (string?) pi.GetValue(parm, null);
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
                            FixArgumentParmTrim(prop.GetPropertyValue<object?>(parm));  // handle nested types (but only if containing type has the Trim attribute)
                        }
                    }
                }
            }
            return any;
        }
        // update all model parameters and ucase/lcase as requested
        private static bool FixArgumentParmCase(object? parm) {
            if (parm == null) return false;
            bool any = false;

            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                any = true;
                CaseAttribute? caseAttr = prop.TryGetAttribute<CaseAttribute>();
                if (caseAttr != null) {
                    CaseAttribute.EnumStyle style = caseAttr.Value;
                    PropertyInfo pi = prop.PropInfo;
                    if (pi.PropertyType == typeof(MultiString)) {
                        MultiString ms = prop.GetPropertyValue<MultiString>(parm);
                        ms.Case(style);
                    } else if (pi.PropertyType == typeof(string)) {
                        if (pi.CanWrite) {
                            string? val = (string?)pi.GetValue(parm, null); ;
                            if (!string.IsNullOrEmpty(val)) {
                                val = style switch {
                                    CaseAttribute.EnumStyle.Lower => val.ToLower(),
                                    _ => val.ToUpper(),
                                };
                                pi.SetValue(parm, val, null);
                            }
                        }
                    } else {
                        FixArgumentParmCase(prop.GetPropertyValue<object?>(parm));  // handle nested types (but only if containing type has the Case attribute)
                    }
                }
            }
            return any;
        }

        // search for templates
        private static bool SearchTemplate(string templateName, bool modelIsValid, string actionValStr, string actionExtraStr, KeyValuePair<string, object?> pair) {
            return SearchTemplateArgument(templateName, modelIsValid, actionValStr, actionExtraStr, pair.Value);
        }
        private static bool SearchTemplateArgument(string templateName, bool modelIsValid, string actionValStr, string actionExtraStr, object? parm) {
            if (parm == null) return false;
            Type tpParm = parm.GetType();
            List<PropertyData> props = ObjectSupport.GetPropertyData(tpParm);
            foreach (var prop in props) {
                if (prop.PropInfo.PropertyType.IsClass && !prop.PropInfo.PropertyType.IsAbstract) {
                    if (!prop.ReadOnly && prop.PropInfo.CanRead && prop.PropInfo.CanWrite) {
                        ClassData classData = ObjectSupport.GetClassData(prop.PropInfo.PropertyType);
                        TemplateActionAttribute? actionAttr = classData.TryGetAttribute<TemplateActionAttribute>();
                        if (actionAttr != null) {
                            if (actionAttr.Value == templateName) {
                                object? objVal = prop.GetPropertyValue<object?>(parm);
                                if (objVal is not ITemplateAction act)
                                    throw new InternalError("ITemplateAction not implemented for {0}", prop.Name);
                                int actionVal = 0;
                                if (!string.IsNullOrWhiteSpace(actionValStr))
                                    actionVal = Convert.ToInt32(actionValStr);
                                if (act.ExecuteAction(actionVal, modelIsValid, actionExtraStr))
                                    return true;
                                return false;
                            }
                        }
                        object? o;
                        try {
                            o = prop.GetPropertyValue<object?>(parm);
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
        private void ReplaceJSONParms(IDictionary<string, object?> actionParms) {
            if (HttpContext.Request.HasFormContentType) {
                foreach (var entry in HttpContext.Request.Form.Keys) {
                    if (entry != null && entry.EndsWith("-JSON")) {
                        string data = HttpContext.Request.Form[entry];
                        string parmName = entry[0..^5];
                        AddJSONParmData(actionParms, parmName, data);
                    }
                }
            }
        }

        private void AddJSONParmData(IDictionary<string, object?> actionParms, string parmName, string jsonData) {
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
                        PropertyInfo? propInfo = ObjectSupport.TryGetProperty(tpParm, parmName);
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
        /// <summary>
        /// Renders the default view (defined using ModuleDefinition.DefaultView) using the provided model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A YetaWFViewResult.</returns>
        protected new YetaWFViewResult View(object? model) {
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
        protected YetaWFViewResult View(string? viewName, object? model, bool UseAreaViewName = true) {
            if (UseAreaViewName) {
                if (string.IsNullOrWhiteSpace(viewName))
                    viewName = CurrentModule.DefaultViewName;
                else
                    viewName = YetaWFController.MakeFullViewName(viewName, Area);
                if (string.IsNullOrWhiteSpace(viewName)) {
                    viewName = (string?)RouteData.Values["action"];
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
            private object? Model { get; set; }
            private string ViewName { get; set; }
            private YetaWFController RequestingController { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="requestingController">The controller requesting the view.</param>
            /// <param name="viewName">The name of the view.</param>
            /// <param name="module">The module on behalf of which to view is rendered.</param>
            /// <param name="model">The view's data model.</param>
            public YetaWFViewResult(YetaWFController requestingController, string viewName, ModuleDefinition module, object? model) {
                ViewName = viewName;
                Module = module;
                Model = model;
                RequestingController = requestingController;
            }

            public override async Task ExecuteResultAsync(ActionContext context) {

                using (var sw = new StringWriter()) {
                    YHtmlHelper htmlHelper = new YHtmlHelper(context, context.ModelState);
                    string data = await htmlHelper.ForViewAsync(ViewName, Module, Model);
#if DEBUG
                    if (sw.ToString().Length > 0)
                        throw new InternalError($"View {ViewName} wrote output which is not supported - All output must be rendered using ForViewAsync and returned as a string - output rendered: \"{sw.ToString()}\"");
#endif
                    if (!string.IsNullOrWhiteSpace(data)) {
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data.ToString());
                        Stream body = context.HttpContext.Response.Body;
                        await body.WriteAsync(buffer, 0, buffer.Length);
                    }
                }
            }
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
        protected ActionResult Reload(object? model = null, int dummy = 0, string? PopupText = null, string? PopupTitle = null, ReloadEnum Reload = ReloadEnum.Page) {
            if (Manager.IsPostRequest) {
                return Reload switch {
                    ReloadEnum.Module => Reload_Module(model, PopupText, PopupTitle),
                    ReloadEnum.ModuleParts => Reload_ModuleParts(model, PopupText, PopupTitle),
                    _ => Reload_Page(PopupText, PopupTitle),
                };
            } else {
                if (string.IsNullOrEmpty(PopupText))
                    throw new InternalError("We don't have a message to display - programmer error");
                return View("ShowMessage", PopupText, UseAreaViewName: false);
            }
        }
        private ActionResult Reload_Module(object? model, string? popupText, string? popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModule);
                return new YJsonResult { Data = sb.ToString() };
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("$YetaWF.message({0}, {1}, function() {{ $YetaWF.reloadModule(); }});", popupText, popupTitle);
                return new YJsonResult { Data = sb.ToString() };
            }
        }
        private ActionResult Reload_ModuleParts(object? model, string? popupText, string? popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                return new YJsonResult { Data = sb.ToString() };
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                sb.Append("$YetaWF.message({0}, {1});", popupText, popupTitle);
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
        protected ActionResult NotAuthorized(string? message) {

            message ??= __ResStr("notAuth", "Not Authorized");

            if (Manager.IsPostRequest) {
                return new UnauthorizedResult();
            } else {
                Manager.CurrentResponse.StatusCode = StatusCodes.Status403Forbidden;
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
        /// <param name="PreserveOriginList">Preserves the URL origin list. Only supported when <paramref name="NextPage"/> is used.</param>
        /// <param name="PreSaveJavaScript">Optional additional Javascript code that is returned as part of the ActionResult and runs before the form is saved.</param>
        /// <param name="PostSaveJavaScript">Optional additional Javascript code that is returned as part of the ActionResult and runs after the form is saved.</param>
        /// <param name="ForceRedirect">Force a real redirect bypassing Unified Page Set handling.</param>
        /// <param name="PopupOptions">TODO: This is not a good option, passes JavaScript/JSON to the client side for the popup window.</param>
        /// <param name="PageChanged">The new page changed status.</param>
        /// <param name="ForceApply">Force handling as Apply.</param>
        /// <param name="ExtraData">Additional data added to URL as _extraData argument. Length should be minimal, otherwise URL and Referer header may grow too large.</param>
        /// <param name="ForcePopup">The message is shown as a popup even if toasts are enabled.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        protected ActionResult FormProcessed(object? model, string? popupText = null, string? popupTitle = null,
                OnCloseEnum OnClose = OnCloseEnum.Return, OnPopupCloseEnum OnPopupClose = OnPopupCloseEnum.ReloadParentPage, OnApplyEnum OnApply = OnApplyEnum.ReloadModule,
                string? NextPage = null, bool PreserveOriginList = false, string? ExtraData = null,
                string? PreSaveJavaScript = null, string? PostSaveJavaScript = null, bool ForceRedirect = false, string? PopupOptions = null, bool ForceApply = false,
                bool? PageChanged = null,
                bool ForcePopup = false) {

            ScriptBuilder sb = new ScriptBuilder();

            if (PreSaveJavaScript != null)
                sb.Append(PreSaveJavaScript);

            popupText = string.IsNullOrWhiteSpace(popupText) ? null : Utility.JsonSerialize(popupText);
            popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
            PopupOptions ??= "null";

            if (PreserveOriginList && !string.IsNullOrWhiteSpace(NextPage)) {
                string url = NextPage;
                if (Manager.OriginList != null) {
                    QueryHelper qh = QueryHelper.FromUrl(url, out string urlOnly);
                    qh.Add(Globals.Link_OriginList, Utility.JsonSerialize(Manager.OriginList), Replace: true);
                    NextPage = qh.ToUrl(urlOnly);
                }
            }

            bool isApply = IsApply || IsReload || ForceApply;
            if (isApply) {
                NextPage = null;
                OnPopupClose = OnPopupCloseEnum.UpdateInPlace;
                OnClose = OnCloseEnum.UpdateInPlace;
            } else {
                if (Manager.IsInPopup) {
                    if (OnPopupClose == OnPopupCloseEnum.GotoNewPage) {
                        if (string.IsNullOrWhiteSpace(NextPage))
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

                string? url = NextPage;
                if (string.IsNullOrWhiteSpace(url))
                    url = Manager.CurrentSite.HomePageUrl;
                url = AddUrlPayload(url, false, ExtraData);
                if (ForceRedirect)
                    url = QueryHelper.AddRando(url);
                url = Utility.JsonSerialize(url);

                if (Manager.IsInPopup) {
                    if (ForceRedirect) {
                        if (string.IsNullOrWhiteSpace(popupText)) {
                            sb.Append("$YetaWF.setLoading();window.parent.location.assign({0});", url);
                        } else {
                            sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.parent.location.assign({url}); }}, {PopupOptions});");
                        }
                    } else if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append($@"
$YetaWF.setLoading();
if (window.parent.$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

}} else
    window.parent.location.assign({url});");

                    } else {
                        sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    $YetaWF.setLoading();
    if (window.parent.$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

    }} else
        window.parent.location.assign({url});
}});");
                    }
                } else {
                    if (ForceRedirect) {
                        if (isApply) {
                            sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.reload(true); }}, {PopupOptions});");
                        } else {
                            if (string.IsNullOrWhiteSpace(popupText)) {
                                sb.Append($@"
$YetaWF.setLoading();window.location.assign({url});");
                            } else {
                                sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.assign({url}); }}, {PopupOptions});");
                            }
                        }
                    } else if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append($@"
$YetaWF.setLoading();
if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

}} else
    window.location.assign({url});");
                    } else {
                        sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    $YetaWF.setLoading();
    if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

    }} else
        window.location.assign({url});
}});");
                    }
                }
            } else {
                if (Manager.IsInPopup) {
                    if (string.IsNullOrWhiteSpace(popupText)) {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                                throw new InternalError("No next page");
                            case OnPopupCloseEnum.Nothing:
                                sb.Append(PostSaveJavaScript);
                                break;
                            case OnPopupCloseEnum.ReloadNothing:
                                sb.Append($@"$YetaWF.closePopup(false);{PostSaveJavaScript}");
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                sb.Append("$YetaWF.closePopup(true);");
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                sb.Append($@"
window.parent.$YetaWF.refreshPage();
$YetaWF.closePopup(false);");
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    } else {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                                throw new InternalError("No next page");
                            case OnPopupCloseEnum.Nothing:
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ {PostSaveJavaScript} }}, {PopupOptions});");
                                break;
                            case OnPopupCloseEnum.ReloadNothing:
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.closePopup(false);{PostSaveJavaScript} }}, {PopupOptions});");
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.closePopup(true);{PostSaveJavaScript} }}, {PopupOptions});");
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ window.parent.$YetaWF.refreshPage(); $YetaWF.closePopup(false); }}, {PopupOptions});");
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    }
                } else {
                    switch (OnClose) {
                        case OnCloseEnum.GotoNewPage:
                            throw new InternalError("No next page");
                        case OnCloseEnum.Nothing:
                            if (!string.IsNullOrWhiteSpace(popupText)) {
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ {PostSaveJavaScript} }}, {PopupOptions});");
                            }
                            if (PageChanged != null)
                                sb.Append($@"$YetaWF.pageChanged = {((bool)PageChanged ? "true" : "false")} ;");
                            break;
                        case OnCloseEnum.UpdateInPlace:
                            isApply = true;
                            break;
                        case OnCloseEnum.Return:
                            if (Manager.OriginList == null || Manager.OriginList.Count == 0) {
                                if (string.IsNullOrWhiteSpace(popupText))
                                    sb.Append($@"window.close();{PostSaveJavaScript}");
                                else {
                                    if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ window.close();{PostSaveJavaScript} }}, {PopupOptions});");
                                }
                            } else {
                                string url = Utility.JsonSerialize(Manager.ReturnToUrl);
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"
if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

}} else
    window.location.assign({url});");
                                } else {
                                    sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

    }} else
        window.location.assign({PopupOptions});
}});");
                                }
                            }
                            break;
                        case OnCloseEnum.CloseWindow:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append($@"window.close();{PostSaveJavaScript}");
                            else {
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ window.close();{PostSaveJavaScript} }}, {PopupOptions});");
                            }
                            break;
                        case OnCloseEnum.ReloadPage:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append($@"$YetaWF.reloadPage(true);");
                            else {
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.reloadPage(true);{PostSaveJavaScript} }}, {PopupOptions});");
                            }
                            break;
                        default:
                            throw new InternalError("Invalid OnClose value {0}", OnClose);
                    }
                }
                if (isApply) {
                    if (OnApply == OnApplyEnum.ReloadPage) {
                        if (string.IsNullOrWhiteSpace(popupText))
                            sb.Append($@"$YetaWF.reloadPage(true);{PostSaveJavaScript}");
                        else {
                            if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                            sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.reloadPage(true);{PostSaveJavaScript} }}, {PopupOptions});");
                        }
                    } else {
                        if (!string.IsNullOrWhiteSpace(popupText)) {
                            if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                            sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ {PostSaveJavaScript} }}, {PopupOptions});");
                        } else
                            sb.Append(PostSaveJavaScript);
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
            Manager.CurrentResponse.StatusCode = StatusCodes.Status307TemporaryRedirect;
            Manager.CurrentResponse.Headers.Add("Location", url);
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
        /// <param name="ForceRedirect">true to force a page load (even in a Unified Page Set), false otherwise to use the default page or page content loading.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        /// <remarks>
        /// The Redirect method can be used for GET, PUT, Ajax requests and also within popups.
        /// This works in cooperation with client-side code to redirect popups, etc., which is normally not supported in MVC.
        /// </remarks>
        protected ActionResult Redirect(string? url, bool ForcePopup = false, bool SetCurrentEditMode = false, bool ForceRedirect = false, string? ExtraJavascript = null) {

            if (ForceRedirect && ForcePopup) throw new InternalError("Can't use ForceRedirect and ForcePopup at the same time");
            if (!string.IsNullOrWhiteSpace(ExtraJavascript) && !Manager.IsPostRequest) throw new InternalError("ExtraJavascript is only supported with POST requests");

            if (string.IsNullOrWhiteSpace(url))
                url = Manager.CurrentSite.HomePageUrl;

            url = AddUrlPayload(url, SetCurrentEditMode, null);
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
                    if (Manager.CurrentRequest.IsHttps) {
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

        private static string AddUrlPayload(string url, bool SetCurrentEditMode, string? ExtraData) {

            QueryHelper qhUrl = QueryHelper.FromUrl(url, out string urlOnly);
            // If we're coming from a referring page with edit/noedit, we need to propagate that to the redirect
            if (SetCurrentEditMode) { // forced set edit mode
                qhUrl.Remove(Globals.Link_EditMode);
                if (Manager.EditMode)
                    qhUrl.Add(Globals.Link_EditMode, "y");
            } else if (!qhUrl.HasEntry(Globals.Link_EditMode)) {
                // current url has no edit/noedit preference
                if (Manager.EditMode) {
                    // in edit mode, force edit again
                    qhUrl.Add(Globals.Link_EditMode, "y");
                } else {
                    // not in edit mode, use referrer mode
                    string referrer = Manager.ReferrerUrl;
                    if (!string.IsNullOrWhiteSpace(referrer)) {
                        QueryHelper qhRef = QueryHelper.FromUrl(referrer, out string refUrlOnly);
                        if (qhRef.HasEntry(Globals.Link_EditMode)) { // referrer is edit
                            qhUrl.Add(Globals.Link_EditMode, "y", Replace: true);
                        }
                    }
                }
            }
            qhUrl.Remove(Globals.Link_PageControl);
            if (Manager.PageControlShown)
                qhUrl.Add(Globals.Link_PageControl, "y");
            if (!string.IsNullOrWhiteSpace(ExtraData))
                qhUrl.Add("_ExtraData", ExtraData, Replace: true);

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
        /// <returns>The bound object of the specified type, or null if there are validation errors.</returns>
        protected async Task<object> GetObjectFromModelAsync(Type objType, string modelName) {

            object? obj = Activator.CreateInstance(objType);
            if (obj == null)
                throw new InternalError("Object with type {0} cannot be instantiated", objType.FullName);

            // update model with available data (even if there are validation errors)
            await TryUpdateModelAsync(obj, objType, modelName??"");

            FixArgumentParmTrim(obj);
            FixArgumentParmCase(obj);
            await FixDataAsync(obj);

            CorrectModelState(obj, ViewData.ModelState, (modelName != null) ? $"{modelName}." : "");

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
            return obj;
        }
    }
}
