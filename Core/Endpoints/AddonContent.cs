/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Endpoints.Filters;
using YetaWF.Core.Log;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints {

    /// <summary>
    /// Endpoint for all page requests within YetaWF that only need addons rendered (used client-side to bootstrap additional missing addons, i.e., progressively populating JavaScript/CSS).
    /// </summary>
    /// <remarks>There is currently no use case so this is untested. This used to be used with components that require jQuery, which no longer exist.</remarks>
    public class AddonContentEndpoints : YetaWFEndpoints {

        public static void RegisterEndpoints(IEndpointRouteBuilder endpoints, Package package, string areaName) {
            endpoints.MapPost(GetPackageApiEndpoint(package, typeof(AddonContentEndpoints), nameof(Show)), async (HttpContext context, [FromBody] DataIn dataIn) => {
                return await Show(context, dataIn);
            })
                .AntiForgeryToken();
        }

        public class AddonDescription {
            public string AreaName { get; set; } = null!;
            public string ShortName { get; set; } = null!;
            public string? Argument1 { get; set; }
        }

        /// <summary>
        /// Data received from the client for the requested page.
        /// </summary>
        /// <remarks>An instance of this class is sent from the client to request a "Single Page Application" update for required addons.</remarks>
        public class DataIn {
            /// <summary>
            /// The requested addons.
            /// </summary>
            public List<AddonDescription> Addons { get; set; } = null!;
            /// <summary>
            /// A collection of all CSS files the client has already loaded.
            /// </summary>
            public List<string> KnownCss { get; set; } = null!;
            /// <summary>
            /// A collection of all JavaScript files the client has already loaded.
            /// </summary>
            public List<string> KnownScripts { get; set; } = null!;
        }

        /// <summary>
        /// Handles all addon content requests issued client-side.
        /// </summary>
        /// <param name="context">The HttpContext.</param>
        /// <param name="dataIn">Describes the data requested.</param>
        /// <returns></returns>
        private static async Task<IResult> Show(HttpContext context, DataIn dataIn) {
            if (!YetaWFManager.HaveManager || dataIn.Addons == null || Manager.CurrentRequest.Headers == null || Manager.CurrentRequest.Headers["X-Requested-With"] != "XMLHttpRequest")
                return Results.NotFound();

            // Process the requested addons
            return await GetAddonResultAsync(context, dataIn);
        }

        private static async Task<IResult> GetAddonResultAsync(HttpContext context, DataIn dataIn) {
            PageContentEndpoints.PageContentData cr = new PageContentEndpoints.PageContentData();
            try {

                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                foreach (AddonContentEndpoints.AddonDescription addon in dataIn.Addons) {
                    await Manager.AddOnManager.AddAddOnNamedAsync(addon.AreaName, addon.ShortName, addon.Argument1);
                }

                await Manager.CssManager.RenderAsync(cr, dataIn.KnownCss);
                await Manager.ScriptManager.RenderAsync(cr, dataIn.KnownScripts);
                Manager.ScriptManager.RenderEndofPageScripts(cr);

            } catch (Exception exc) {
                cr.Status = Logging.AddErrorLog(ErrorHandling.FormatExceptionMessage(exc));
            }
            return Results.Ok(cr);
        }
    }
}
