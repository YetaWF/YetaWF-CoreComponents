﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            response.StatusCode = 200;
            response.ContentType = "application/text";
            string content = string.Format(Basics.AjaxJavascriptErrorReturn + "$YetaWF.error({0});", YetaWFManager.JsonSerialize(msg));
            return context.HttpContext.Response.WriteAsync(content);
        }
    }
}
#else
#endif