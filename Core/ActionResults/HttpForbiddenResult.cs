/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Web.Mvc;

namespace YetaWF.Core.Support {

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
        public override void ExecuteResult(ControllerContext context) {
            if (context == null)
                throw new ArgumentNullException("context");
            context.HttpContext.Response.StatusCode = 403;
        }
    }
}
