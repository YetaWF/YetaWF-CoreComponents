/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YetaWF2.Middleware {

    // Ignore files/folders that should never be accessible
    public class IgnoreRouteMiddleware {

        private readonly RequestDelegate next;

        public IgnoreRouteMiddleware(RequestDelegate next) {
            this.next = next;
        }

        private List<string> Extensions = new List<string> { ".cs", ".cshtml", ".json" };
        private List<string> Folders = new List<string> { @"\addons\_main\grids\", @"\addons\_main\localization\", @"\addons\_sitetemplates\" };

        public async Task Invoke(HttpContext context) {
            if (context.Request.Path.HasValue) {
                string path = context.Request.Path.Value.ToLower();
                foreach (string extension in Extensions) {
                    if (path.EndsWith(extension)) {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }
                foreach (string folder in Folders) {
                    if (path.Contains(folder)) {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }
            }
            await next.Invoke(context);
        }
    }
}

#else
#endif
