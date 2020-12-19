/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YetaWF.Core.Support.Middleware {

    /// <summary>
    /// Used to add middleware that can process requests before the request is handled by regular routing.
    /// </summary>
    public class DynamicPreRoutingMiddleware {

        private readonly RequestDelegate _nextMiddleware;

        private static List<Func<HttpContext, Task<bool>>> Middlewares = new List<Func<HttpContext, Task<bool>>>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nextMiddleware">The delegate representing the remaining middleware in the request pipeline.</param>
        public DynamicPreRoutingMiddleware(RequestDelegate nextMiddleware) {
            _nextMiddleware = nextMiddleware;
        }

        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="context">The HttpContext for the current request.</param>
        public async Task Invoke(HttpContext context) {
            foreach (Func<HttpContext, Task<bool>> middleware in Middlewares) {
                if (await middleware(context))
                    return;
            }
            await _nextMiddleware(context);
        }

        /// <summary>
        /// Add a callback invoked to handle the current request.
        /// </summary>
        /// <param name="func">Defines the callback invoked to handle the request. The callback returns true if the request was processed, false otherwide.</param>
        public static void Add(Func<HttpContext, Task<bool>> func) {
            Middlewares.Add(func);
        }
    }
}
