/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Web.Mvc;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Used to indicate a disallowed request. Typically used as a return result in a controller.
    /// </summary>
    public class HttpForbiddenResult : ActionResult {
        public override void ExecuteResult(ControllerContext context) {
            if (context == null)
                throw new ArgumentNullException("context");
            context.HttpContext.Response.StatusCode = 403;
        }
    }
}
