/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

// Inspired by https://ppolyzos.com/2016/09/09/asp-net-core-render-view-to-string/

namespace YetaWF.Core.Pages {

    /// <summary>
    /// Error handling middleware is added during startup in the request pipeline.
    /// </summary>
    /// <remarks>The error hander logs any errors that occur. Errors are usually immediately saved to the log file in use, by flushing the log file. To avoid log spamming, the log is flushed at most every 10 seconds.
    ///
    /// Errors during (non-POST) requests are only handled if the error doesn't occur while processing a module. Modules handle errors themselves to display the errors in place of the module content. Otherwise, the request is redirected to an error page displaying a suitable error message.
    ///
    /// Errors during POST requests are returned as a JSON object with the YetaWF.Core.Addons.Basics.AjaxJavascriptErrorReturn prefix. This is unique to the YetaWF framework and its client side JavaScript handling which expects certain prefixes identifying the response.</remarks>
    public class ErrorHandlingMiddleware {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private readonly RequestDelegate next;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
        public ErrorHandlingMiddleware(RequestDelegate next) {
            this.next = next;
        }

        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        public async Task Invoke(HttpContext context /* other scoped dependencies */) {
            try {
                await next(context);
            } catch (Exception ex) {
                bool result = await HandleExceptionAsync(context, ex);
                if (!result)
                    throw;
            }
        }

        private async Task<bool> HandleExceptionAsync(HttpContext context, Exception exception) {
            // log the error
            string msg = Logging.AddErrorLog(ErrorHandling.FormatExceptionMessage(exception));

            // flush log on error, but avoid log spamming
            if (LastError == null || DateTime.Now > ((DateTime)LastError).AddSeconds(10)) {
                Logging.ForceFlush();// make sure this is recorded immediately so we can see it in the log
                LastError = DateTime.Now;
            }
            if (YetaWFManager.HaveManager) {
                var response = context.Response;
                if (!Manager.IsPostRequest) {
                    if (Manager.CurrentModule != null) { // we're rendering a module, let module handle its own error
                        return false;// not handled
                    } else { // this was a direct action GET so we need to show an error page
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                        context.Response.Headers[HeaderNames.Location] = MessageUrl(msg, StatusCodes.Status500InternalServerError);
                        return true;
                    }
                }

                // for post/ajax requests, respond in a way we can display the error
                //context.ExceptionHandled = true;
                response.StatusCode = 200;
                response.ContentType = "application/json";
                string content = Utility.JsonSerialize(string.Format(Basics.AjaxJavascriptErrorReturn + "$YetaWF.error({0});", Utility.JsonSerialize(msg)));
                await context.Response.WriteAsync(content);
                return true;// handled
            }
            return false;
        }
        private static DateTime? LastError = null;// use local time

        /// <summary>
        /// Redirect from error handling middleware with a message.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        /// <param name="statusCode">The HTTP status code to return.</param>
        private string MessageUrl(string message, int statusCode) {
            // we're in a GET request without module, so all we can do is redirect and show the message in the ShowMessage module
            // the ShowMessage module is in the Basics package and we reference it by permanent Guid
            string url = YetaWFManager.Manager.CurrentSite.MakeUrl(ModuleDefinition.GetModulePermanentUrl(new Guid("{b486cdfc-3726-4549-889e-1f833eb49865}")));
            QueryHelper query = QueryHelper.FromUrl(url, out url);
            query["Message"] = message;
            query["Code"] = statusCode.ToString();
            return query.ToUrl(url);
        }
    }
}
