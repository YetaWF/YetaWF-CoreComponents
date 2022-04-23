/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System;

namespace YetaWF2.Middleware {

    public static class YetaWFForwardedHeadersExtensions {

        private const string ForwardedHeadersAdded = "ForwardedHeadersAdded";

        /// <summary>
        /// Forwards proxied headers onto current request
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseYetaWFForwardedHeaders(this IApplicationBuilder builder) {

            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Don't add more than one instance of this middleware to the pipeline using the options from the DI container.
            // Doing so could cause a request to be processed multiple times and the ForwardLimit to be exceeded.
            if (!builder.Properties.ContainsKey(ForwardedHeadersAdded)) {
                builder.Properties[ForwardedHeadersAdded] = true;
                return builder.UseMiddleware<YetaWFForwardedHeadersMiddleware>();
            }

            return builder;
        }

        /// <summary>
        /// Forwards proxied headers onto current request
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Enables the different forwarding options.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseYetaWFForwardedHeaders(this IApplicationBuilder builder, ForwardedHeadersOptions options) {

            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return builder.UseMiddleware<YetaWFForwardedHeadersMiddleware>(Options.Create(options));
        }
    }
}
