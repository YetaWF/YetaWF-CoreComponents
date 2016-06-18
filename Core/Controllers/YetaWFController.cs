/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Controllers {

    // Base class for all controllers used by YetaWF
    public class YetaWFController : Controller {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        ///  Update an area's view name with the complete area specifier.
        /// </summary>
        public static string MakeFullViewName(string viewName, string area) {
            viewName = area + "_" + viewName;
            return viewName;
        }

        // Handle exceptions and return suitable error info
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
        protected override void OnActionExecuting(ActionExecutingContext filterContext) {
            SetupEnvironmentInfo();
            Logging.AddLog("Action Request");
            base.OnActionExecuting(filterContext);
        }
        protected override void OnAuthentication(AuthenticationContext filterContext) {
            SetupEnvironmentInfo();
            base.OnAuthentication(filterContext);
        }

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
                if (toPopup == null)
                    toPopup = Manager.RequestParams[Globals.Link_ToPopup];
            } catch (Exception) { }
            return toPopup != null;
        }
        protected static bool InPopup() {
            string inPopup = null;
            try {
                inPopup = Manager.RequestForm[Globals.Link_InPopup];
                if (inPopup == null)
                    inPopup = Manager.RequestQueryString[Globals.Link_InPopup];
                if (inPopup == null)
                    inPopup = Manager.RequestParams[Globals.Link_InPopup];
            } catch (Exception) { }
            return inPopup != null;
        }
        protected static bool PageControlShown() {
            string pageControlShown = null;
            try {
                pageControlShown = Manager.RequestForm[Globals.Link_ShowPageControlKey];
                if (pageControlShown == null)
                    pageControlShown = Manager.RequestQueryString[Globals.Link_ShowPageControlKey];
                if (pageControlShown == null)
                    pageControlShown = Manager.RequestParams[Globals.Link_ShowPageControlKey];
            } catch (Exception) { }
            return pageControlShown != null;
        }
        protected static void GetCharSize() {
            string wh = null;
            try {
                wh = Manager.RequestForm[Globals.Link_CharInfo];
                if (wh == null)
                    wh = Manager.RequestQueryString[Globals.Link_CharInfo];
                if (wh == null)
                    wh = Manager.RequestParams[Globals.Link_CharInfo];
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
                if (originList == null)
                    originList = Manager.RequestParams[Globals.Link_OriginList];
            } catch (Exception) { }
            if (!string.IsNullOrWhiteSpace(originList))
                return YetaWFManager.Jser.Deserialize<List<Origin>>(originList);
            else
                return new List<Origin>();
        }
    }
}
