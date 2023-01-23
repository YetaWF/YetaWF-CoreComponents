/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints.Filters {

    public static class ResourceAuthorizeFilterExtension  {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ResourceAuthorizeFilterExtension), name, defaultValue, parms); }

        public static RouteHandlerBuilder ResourceAuthorize(this RouteHandlerBuilder builder, string resource) {
            builder.AddEndpointFilterFactory((filterFactoryContext, next) => {
                return async invocationContext =>
                {
                    if (!await Resource.ResourceAccess.IsResourceAuthorizedAsync(resource))
                        throw new Error(__ResStr("notAuth", "Not Authorized"));
                    return await next(invocationContext);
                };
            });
            return builder;
        }
    }
}