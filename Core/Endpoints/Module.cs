/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Endpoints {

    /// <summary>
    /// Endpoint for module requests within YetaWF.
    /// </summary>
    public class ModuleEndpoints : YetaWFEndpoints {

        public const string Update = "Update";
        public const string DynamicProperty = "Dynamic";

        public static void RegisterEndpoints(IEndpointRouteBuilder endpoints, Package package, string areaName) {

            RouteGroupBuilder group = endpoints.MapGroup(GetPackageApiRoute(package, typeof(ModuleEndpoints)));

            group.MapPost($"Update/{{ModuleGuid}}", async (HttpContext context,
                    [FromBody] ModuleSubmitData dataIn, [FromRoute] Guid moduleGuid, [FromQuery] string? action) => {
                return await UpdateAsync(context, dataIn, moduleGuid, action);
            });
        }

        /// <summary>
        /// Data received from the client for the requested module.
        /// </summary>
        /// <remarks>An instance of this class is sent from the client to request a module update.</remarks>
        public class ModuleSubmitData {

            /// <summary>
            /// The module's data model.
            /// </summary>
            public object Model { get; set; } = null!;

            /// <summary>
            /// Apply button was clicked.
            /// </summary>
            public bool __Apply { get; set; }
            /// <summary>
            /// A submit button was clicked and should be handled as a form reload
            /// </summary>
            public bool __Reload { get; set; }

            /// <summary>
            /// Navigation history.
            /// </summary>
            public List<Origin> __OriginList { get; set; } = new List<Origin>();
            /// <summary>
            /// The unique id prefix counter used by the current page. This value is used to prevent collisions when generating unique HTML tag ids.
            /// </summary>
            public YetaWFManager.UniqueIdInfo UniqueIdCounters { get; set; } = null!;
            /// <summary>
            /// Page control module shown.
            /// </summary>
            public bool __Pagectl { get; set; }
            /// <summary>
            /// Defines whether we're in a popup.
            /// </summary>
            public bool __InPopup { get; set; }

            // Templates
            public string? __TemplateName { get; set; }
            public int? __TemplateAction { get; set; }
            public string? __TemplateExtraData { get; set; }
        }

        /// <summary>
        /// Handles all module requests issued client-side, typically a form submit request.
        /// </summary>
        /// <param name="context">The HttpContext.</param>
        /// <param name="dataIn">Describes the data requested.</param>
        /// <returns></returns>
        private static async Task<IResult> UpdateAsync(HttpContext context, ModuleSubmitData dataIn, Guid moduleGuid, string? action) {

            // save environmental data
            Manager.UniqueIdCounters = dataIn.UniqueIdCounters;
            ModuleDefinition module = await GetModuleAsync(moduleGuid);
            ModuleDefinition2? module2 = module as ModuleDefinition2;
            if (module2 is null)//$$$CHANGE
                throw new InternalError("Invalid module type");

            module2.IsApply = dataIn.__Apply;
            module2.IsReload = dataIn.__Reload;

            Manager.IsInPopup = dataIn.__InPopup;
            Manager.PageControlShown = dataIn.__Pagectl;

            // Find the module action
            string actionUpdate;
            if (action != null) 
                actionUpdate = $"Update{action}Async";
            else
                actionUpdate = ModuleDefinition2.MethodUpdateModuleAsync;

            Type moduleType = module.GetType();
            MethodInfo? miAsync = moduleType.GetMethod(actionUpdate);
            if (miAsync == null)
                throw new InternalError($"{moduleType.FullName} doesn't have a method named {actionUpdate}");
            ParameterInfo[] parms = miAsync.GetParameters();
            if (parms.Length == 1) {
                ; // default, pass the model
            } else if (parms.Length == 2) {
                if (parms[1].Name != DynamicProperty) throw new InternalError($"{moduleType.FullName} has a method named {actionUpdate} which accepts 2 parameters but the second parameter is not named {DynamicProperty}");
            } else
                throw new InternalError($"{moduleType.FullName} doesn't have a method named {actionUpdate} which accepts 1 model parameter");

            ParameterInfo parm = parms[0];
            object? model = Utility.JsonDeserialize(dataIn.Model.ToString()!, parm.ParameterType);
            if (model is null)
                throw new InternalError($"Model data missing for module {moduleType.FullName} method {actionUpdate}");

            //$$$$ resource authorize attribute

            // Authorization
            string? level = null;
            PermissionAttribute? permAttr = (PermissionAttribute?)Attribute.GetCustomAttribute(miAsync, typeof(PermissionAttribute));
            if (permAttr != null)
                level = permAttr.Level;
            if (!module.IsAuthorized(level)) {
                if (Manager.IsPostRequest) {
                    return Results.Unauthorized();
                } else {
                    // We get here if an action is attempted that the user is not authorized for
                    // we could attempt to capture and redirect to user login, whatevz
                    return Results.Empty;
                }
            }

            if (YetaWFManager.IsDemo || Manager.IsDemoUser) {
                // if this is a demo user and the action is marked with the ExcludeDemoMode Attribute, reject
                // ExcludeDemoMode
                ExcludeDemoModeAttribute? exclDemoAttr = (ExcludeDemoModeAttribute?)Attribute.GetCustomAttribute(miAsync!, typeof(ExcludeDemoModeAttribute));
                if (exclDemoAttr != null)
                    throw new Error("This action is not available in Demo mode.");
            }

            await module2.ModelState.ValidateModel(model, dataIn.__TemplateName, dataIn.__TemplateAction, dataIn.__TemplateExtraData);

            Task<IResult> methStringTask;
            if (parms.Length == 2) {
                string dynamicString = ((JsonElement)dataIn.Model).GetProperty(DynamicProperty).ToString();
                methStringTask = (Task<IResult>)miAsync.Invoke(module, new object?[] { model, dynamicString })!;
            } else {
                methStringTask = (Task<IResult>)miAsync.Invoke(module, new object?[] { model })!;
            }

            return await methStringTask;
        }
    }
}
