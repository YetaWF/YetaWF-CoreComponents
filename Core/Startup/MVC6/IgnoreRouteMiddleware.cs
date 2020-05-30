/* Copyright �2020 Softel vdm, Inc.. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
        private List<string> Folders = new List<string> { @"/addons/_main/grids/", @"/addons/_main/localization/", @"/addons/_main/propertylists/", @"/addons/_sitetemplates/" };
        private List<string> AllowedFolders = new List<string> { @"/.well-known/" };

        public Task Invoke(HttpContext context) {
            if (context.Request.Path.HasValue) {
                string path = context.Request.Path.Value.ToLower();
                foreach (string folder in AllowedFolders) {
                    if (path.StartsWith(folder)) {
                        return next.Invoke(context);
                    }
                }
                foreach (string extension in Extensions) {
                    if (path.EndsWith(extension)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return Task.CompletedTask;
                    }
                }
                foreach (string folder in Folders) {
                    if (path.StartsWith(folder)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return Task.CompletedTask;
                    }
                }
            }
            return next.Invoke(context);
        }
    }
}

#else
#endif
