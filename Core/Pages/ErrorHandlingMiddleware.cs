/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;
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
            // flush log on error, but avoid log spamming
            if (LastError == null || DateTime.Now > ((DateTime)LastError).AddSeconds(10)) {
                Logging.ForceFlush();// make sure this is recorded immediately so we can see it in the log
                LastError = DateTime.Now;
            }
            var response = context.Response;
            if (!YetaWFManager.HaveManager || !Manager.IsPostRequest) {
                if (Manager.CurrentModule != null) { // we're rendering a module, let module handle its own error
                    return false;// not handled
                } else { // this was a direct action GET so we need to show an error page
                    ActionContext actionContext = new ActionContext(context, new Microsoft.AspNetCore.Routing.RouteData() { }, new ActionDescriptor());
                    RedirectResult redir = new RedirectResult(MessageUrl(msg, 500));
                    redir.ExecuteResult(actionContext);
                    return true;
                }
            }

            // for post/ajax requests, respond in a way we can display the error
            //context.ExceptionHandled = true;
            response.StatusCode = 200;
            response.ContentType = "application/text";
            string content = string.Format(Basics.AjaxJavascriptErrorReturn + "YetaWF_Basics.Y_Error({0});", YetaWFManager.JsonSerialize(msg));
            await context.Response.WriteAsync(content);
            return true;// handled
        }
        private static DateTime? LastError = null;// use local time

        /// <summary>
        /// Redirect from error handling middleware with a message.
        /// </summary>
        /// <param name="Message">Error message to display.</param>
        private string MessageUrl(string message, int statusCode) {
            // we're in a get request without module, so all we can do is redirect and show the message in the ShowMessage module
            // the ShowMessage module is in the Basics package and we reference it by permanent Guid
            string url = YetaWFManager.Manager.CurrentSite.MakeUrl(ModuleDefinition.GetModulePermanentUrl(new Guid("{b486cdfc-3726-4549-889e-1f833eb49865}")));
            QueryHelper query = QueryHelper.FromUrl(url, out url);
            query["Message"] = message;
            query["Code"] = statusCode.ToString();
            return query.ToUrl(url);
        }
    }
}
#else
#endif