/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints.Filters {

    public static class ExcludeDemoModeFilterExtension  {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ExcludeDemoModeFilterExtension), name, defaultValue, parms); }

        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static TBuilder ExcludeDemoMode<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder {
            builder.AddEndpointFilterFactory((filterFactoryContext, next) => {
                return async invocationContext => {
                    if (YetaWFManager.IsDemo || Manager.IsDemoUser)
                        throw new Error(__ResStr("demoMode", "This action is not available in Demo mode."));
                    return await next(invocationContext);
                };
            });
            return builder;
        }
    }
}