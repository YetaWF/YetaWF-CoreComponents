/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

// Inspired by https://ppolyzos.com/2016/09/09/asp-net-core-render-view-to-string/

namespace YetaWF.Core.Pages {

    public class ErrorHandlingMiddleware {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next) {
            this.next = next;
        }

        public async Task Invoke(HttpContext context /* other scoped dependencies */) {
            try {
                await next(context);
            } catch (Exception ex) {
                bool result = await HandleExceptionAsync(context, ex);
                if (!result)
                    throw ex;
            }
        }

        private async Task<bool> HandleExceptionAsync(HttpContext context, Exception exception) {
            // log the error
            string msg = "(unknown)";
            if (exception != null) {
                // show inner exception
                if (exception.Message != null && !string.IsNullOrWhiteSpace(exception.Message))
                    msg = exception.Message;
                while (exception.InnerException != null) {
                    exception = exception.InnerException;
                    if (exception.Message != null && !string.IsNullOrWhiteSpace(exception.Message))
                        msg += " " + exception.Message;
                }
                Logging.AddErrorLog(msg);
            }
            if (!YetaWFManager.HaveManager || (!Manager.IsAjaxRequest && !Manager.IsPostRequest))
                return false;// not handled

            // for post/ajax requests, respond in a way we can display the error
            //context.ExceptionHandled = true;
            var response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "application/text";
            string content = string.Format(Basics.AjaxJavascriptErrorReturn + "Y_Error({0});", YetaWFManager.Jser.Serialize(msg));
            await context.Response.WriteAsync(content);
            return true;// handled
        }
    }
}
#else
#endif