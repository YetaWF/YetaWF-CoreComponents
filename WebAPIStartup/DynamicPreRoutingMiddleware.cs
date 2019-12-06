/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YetaWF.Core.WebAPIStartup {

    public class DynamicPreRoutingMiddleware {

        private readonly RequestDelegate _nextMiddleware;

        private static List<Func<HttpContext, Task<bool>>> Middlewares = new List<Func<HttpContext, Task<bool>>>();

        public DynamicPreRoutingMiddleware(RequestDelegate nextMiddleware) {
            _nextMiddleware = nextMiddleware;
        }

        public async Task Invoke(HttpContext context) {
            foreach (Func<HttpContext, Task<bool>> middleware in Middlewares) {
                if (await middleware(context))
                    return;
            }
            await _nextMiddleware(context);
        }

        public static void Add(Func<HttpContext, Task<bool>> func) {
            Middlewares.Add(func);
        }
    }
}