/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.Controllers {

    // Exception handler for controllers (filter) - not currently used
    public class ControllerExceptionFilterAttribute : ExceptionFilterAttribute {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public override Task OnExceptionAsync(ExceptionContext context) {
            // log the error
            Exception exc = context.Exception;
            string msg = "(unknown)";
            if (exc != null) {
                msg = ErrorHandling.FormatExceptionMessage(exc);
                Logging.AddErrorLog(msg);
            }
            if (!YetaWFManager.HaveManager || !Manager.IsPostRequest)
                throw context.Exception;

            // for post/ajax requests, respond in a way we can display the error
            //context.ExceptionHandled = true;
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "application/json";
            string content = Utility.JsonSerialize(string.Format(Basics.AjaxJavascriptErrorReturn + "$YetaWF.error({0});", Utility.JsonSerialize(msg)));
            return context.HttpContext.Response.WriteAsync(content);
        }
    }
}
