﻿/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using Microsoft.Web.Infrastructure.DynamicValidationHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Routing;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views {

    public static class YetaWFViews {

        public static string RDVViewIndicator = "YetaWFView";

        public static async Task<string> ActionAsync(this HtmlHelper htmlHelper, string actionName, string controllerName, string areaName, RouteValueDictionary routeValues) {

            HttpContext currentContext = HttpContext.Current;
            if (currentContext != null) {
                bool? isRequestValidationEnabled = ValidationUtility.IsValidationEnabled(currentContext);
                if (isRequestValidationEnabled == true) {
                    ValidationUtility.EnableDynamicValidation(currentContext);
                }
            }

            routeValues["action"] = actionName;
            routeValues["controller"] = controllerName;
            routeValues["area"] = areaName;

            string html = null;

            VirtualPathData vpd = htmlHelper.RouteCollection.GetVirtualPathForArea(htmlHelper.ViewContext.RequestContext, routeValues);
            if (vpd == null)
                throw new InternalError($"No route found for {areaName}/{controllerName}/{actionName}");

            routeValues.Remove("area");

            RouteData routeData = CreateRouteData(vpd.Route, routeValues, vpd.DataTokens, htmlHelper.ViewContext);
            RequestContext requestContext = new RequestContext(htmlHelper.ViewContext.HttpContext, routeData);

            // Instantiate the controller and call Execute
            IControllerFactory factory = ControllerBuilder.Current.GetControllerFactory();
            IController controller = factory.CreateController(requestContext, controllerName);

            if (controller == null)
                throw new InternalError($"Controller {controllerName} not found");

            TextWriter oldOutput = YetaWFManager.Manager.CurrentContext.Response.Output;
            try {
                using (var sw = new StringWriter()) {

                    YetaWFManager.Manager.CurrentContext.Response.Output = sw;

                    // Invoke action
                    IAsyncController asyncController = controller as IAsyncController;
                    if (asyncController != null) {
                        await Task.Factory.FromAsync(asyncController.BeginExecute(requestContext, null, null), asyncController.EndExecute);
                    } else {
                        controller.Execute(requestContext);
                    }

                    html = sw.ToString();
                }
            } catch (Exception) {
                throw;
            } finally {

                YetaWFManager.Manager.CurrentContext.Response.Output = oldOutput;
            }
            return html;
        }

        private static RouteData CreateRouteData(RouteBase route, RouteValueDictionary routeValues, RouteValueDictionary dataTokens, ViewContext parentViewContext) {
            RouteData routeData = new RouteData();

            foreach (KeyValuePair<string, object> kvp in routeValues) {
                routeData.Values.Add(kvp.Key, kvp.Value);
            }

            foreach (KeyValuePair<string, object> kvp in dataTokens) {
                routeData.DataTokens.Add(kvp.Key, kvp.Value);
            }

            routeData.Route = route;
            // routeData.DataTokens[ControllerContext.ParentActionViewContextToken] = parentViewContext;

            return routeData;
        }
    }
}

#endif