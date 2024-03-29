﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        public override void ExecuteResult(ActionContext context) {
            context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
    }
}
