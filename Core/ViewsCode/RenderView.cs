using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Web.Routing;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views {

    public static class YetaWFViews {

        public static string RDVViewIndicator = "YetaWFView";

        //$$$ activate this
        public static async Task<string> RenderViewAsync(
#if MVC6
            this IHtmlHelper htmlHelper,
#else
            this HtmlHelper htmlHelper,
#endif
                string actionName, string controllerName, string area, RouteValueDictionary routeValues) {

            //$$$ HttpContext currentContext = HttpContext.Current;
            //if (currentContext != null) {
            //    bool? isRequestValidationEnabled = ValidationUtility.IsValidationEnabled(currentContext);
            //    if (isRequestValidationEnabled == true) {
            //        ValidationUtility.EnableDynamicValidation(currentContext);
            //    }
            //}

            RouteData routeData = new RouteData();
            foreach (KeyValuePair<string, object> kvp in routeValues)
                routeData.Values.Add(kvp.Key, kvp.Value);
            routeData.Values.Add("action", actionName);
            routeData.Values.Add("controller", controllerName);
            routeData.Values.Add(RDVViewIndicator, true);

            RequestContext requestContext = new RequestContext(htmlHelper.ViewContext.HttpContext, routeData);

            // Instantiate the controller and call Execute
            IControllerFactory factory = ControllerBuilder.Current.GetControllerFactory();
            IController controller = factory.CreateController(requestContext, controllerName);
            if (controller == null)
                throw new InternalError($"Controller {controllerName} not found");

            // Invoke action
            IAsyncController asyncController = controller as IAsyncController;
            if (asyncController != null) {
                await Task.Factory.FromAsync(asyncController.BeginExecute(requestContext, null, null), asyncController.EndExecute);
            } else {
                controller.Execute(requestContext);
            }

            return null;
        }
    }
}
