﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace YetaWF.Core.Endpoints.Filters {

    public static class AntiForgeryTokenFilterExtension  {

        public static TBuilder AntiForgeryToken<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder {
            builder.AddEndpointFilterFactory((filterFactoryContext, next) => {
                return async invocationContext => {
                    IAntiforgery antiforgery = filterFactoryContext.ApplicationServices.GetRequiredService<IAntiforgery>();
                    try {
                        await antiforgery.ValidateRequestAsync(invocationContext.HttpContext);
                    } catch (AntiforgeryValidationException) {
                        return Results.Unauthorized();
                    }
                    return await next(invocationContext);
                };
            });
            return builder;
        }
    }
}