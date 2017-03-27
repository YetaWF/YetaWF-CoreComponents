﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Reflection;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
#else
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using YetaWF.Core.Addons;
#endif

namespace YetaWF.Core.Controllers {

    // Base class for all controllers used by YetaWF
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
        protected YetaWFController() {
            // Don't perform html char validation (it's annoying) - This is the equivalent of adding [ValidateInput(false)] on every controller.
            // TODO: this also means we can remove all AllowHtml attributes
            ValidateRequest = false;
        }
#endif
        /// <summary>
        ///  Update an area's view name with the complete area specifier.
        /// </summary>
        public static string MakeFullViewName(string viewName, string area) {
            viewName = area + "_" + viewName;
            return viewName;
        }

        // Handle exceptions and return suitable error info
#if MVC6
        // Handled identically in ErrorHandlingMiddleware
#else
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
            if (!YetaWFManager.HaveManager || (!Manager.IsAjaxRequest && !Manager.IsPostRequest))
                throw filterContext.Exception;

            // for post/ajax requests, respond in a way we can display the error
            Server.ClearError(); // this clears the current 500 error (if customErrors is on in web config we would get a 500 - Internal Server Error at this point
            filterContext.HttpContext.Response.Clear(); // this prob doesn't do much
            filterContext.HttpContext.Response.StatusCode = 200;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
            filterContext.ExceptionHandled = true;
            ContentResult cr = Content(
                string.Format(Basics.AjaxJavascriptErrorReturn + "Y_Error({0});", YetaWFManager.Jser.Serialize(msg)));
            cr.ExecuteResult(filterContext);
        }
#endif

#if MVC6
        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            Logging.AddTraceLog("Action Request - {0}", filterContext.Controller.GetType().FullName);
            SetupEnvironmentInfo();
            // if this is a demo and the action is marked with the ExcludeDemoMode Attribute, reject
            if (Manager.IsDemo) {
                Type ctrlType;
                string actionName;
#if MVC6
                ctrlType = filterContext.Controller.GetType();
                actionName = ((ControllerActionDescriptor)filterContext.ActionDescriptor).ActionName;
#else
                filterContext.ActionDescriptor.ControllerDescriptor.ControllerType;
                actionName = filterContext.ActionDescriptor.ActionName;
#endif
                MethodInfo mi = ctrlType.GetMethod(actionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                ExcludeDemoModeAttribute exclDemoAttr = (ExcludeDemoModeAttribute)Attribute.GetCustomAttribute(mi, typeof(ExcludeDemoModeAttribute));
                if (exclDemoAttr != null)
                    throw new Error("This action is not available in Demo mode.");
            }
            base.OnActionExecuting(filterContext);
        }
#else
        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            Logging.AddTraceLog("Action Request - {0}", filterContext.ActionDescriptor.ControllerDescriptor.ControllerType.FullName);
            SetupEnvironmentInfo();
            // if this is a demo and the action is marked with the ExcludeDemoMode Attribute, reject
            if (Manager.IsDemo) {
                MethodInfo mi = filterContext.ActionDescriptor.ControllerDescriptor.ControllerType.GetMethod(filterContext.ActionDescriptor.ActionName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                ExcludeDemoModeAttribute exclDemoAttr = (ExcludeDemoModeAttribute)Attribute.GetCustomAttribute(mi, typeof(ExcludeDemoModeAttribute));
                if (exclDemoAttr != null)
                    throw new Error("This action is not available in Demo mode.");
            }
            base.OnActionExecuting(filterContext);
        }
#endif
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

#if MVC6
        // This is handled in ResourceAuthorizeHandler
#else
        protected override void OnAuthentication(AuthenticationContext filterContext) {
            SetupEnvironmentInfo();
            base.OnAuthentication(filterContext);
        }
#endif
        public static void SetupEnvironmentInfo() {

            if (!Manager.LocalizationSupportEnabled) {// this only needs to be done once, so we gate on LocalizationSupportEnabled
                GetCharSize();
                Manager.IsInPopup = InPopup();
                Manager.OriginList = GetOriginList();
                Manager.PageControlShown = PageControlShown();
                bool? tempEditMode = GetTempEditMode();
                if (tempEditMode != null)
                    Manager.ForcedEditMode = (bool)tempEditMode;

                // determine user identity - authentication provider updates Manager with user information
                Resource.ResourceAccess.ResolveUser();
                // get user's default language
                Manager.GetUserLanguage();
                // only now can we enable resource loading
                Manager.LocalizationSupportEnabled = true;
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
                pageControlShown = Manager.RequestForm[Globals.Link_ShowPageControlKey];
                if (pageControlShown == null)
                    pageControlShown = Manager.RequestQueryString[Globals.Link_ShowPageControlKey];
            } catch (Exception) { }
            return pageControlShown != null;
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
        protected static bool? GetTempEditMode() {
            try {
                string editMode = Manager.RequestQueryString[Globals.Link_TempEditMode];
                if (editMode != null)
                    return true;
                editMode = Manager.RequestQueryString[Globals.Link_TempNoEditMode];
                if (editMode != null)
                    return false;
            } catch (Exception) { }
            return null;
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
                    return YetaWFManager.Jser.Deserialize<List<Origin>>(originList);
                } catch (Exception) {
                    throw new InternalError("Invalid Url arguments");
                }
            }  else
                return new List<Origin>();
        }
    }
}
