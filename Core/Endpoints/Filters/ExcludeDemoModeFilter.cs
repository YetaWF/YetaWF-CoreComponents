/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints.Filters {

    public class ExcludeDemoModeFilter : IEndpointFilter {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ExcludeDemoModeFilter), name, defaultValue, parms); }

        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            if (YetaWFManager.IsDemo || Manager.IsDemoUser)
                throw new Error(__ResStr("demoMode", "This action is not available in Demo mode."));
            return next(context);
        }
    }

    public static class ExcludeDemoModeFilterExtension  {

        public static RouteHandlerBuilder ExcludeDemoMode(this RouteHandlerBuilder builder) {
            builder.AddEndpointFilter<RouteHandlerBuilder, ExcludeDemoModeFilter>();
            return builder;
        }
    }
}