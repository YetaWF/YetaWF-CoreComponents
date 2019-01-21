/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Used to indicate a forbidden request ({link "HTTP response 403" https://en.wikipedia.org/wiki/HTTP_403}).
    /// </summary>
    /// <remarks> Typically used as a return result in a controller.
    /// </remarks>
    public class HttpForbiddenResult : ActionResult {
        /// <summary>
        /// Processes the result of an action method and returns an {link "HTTP response 403" https://en.wikipedia.org/wiki/HTTP_403} (Forbidden).
        /// </summary>
        /// <param name="context">The context in which the result is executed. The context information includes the controller, HTTP content, request context, and route data.</param>
#if MVC6
        public override void ExecuteResult(ActionContext context) {
#else
        public override void ExecuteResult(ControllerContext context) {
#endif
            if (context == null)
                throw new ArgumentNullException("context");
            context.HttpContext.Response.StatusCode = 403;
        }
    }
}
