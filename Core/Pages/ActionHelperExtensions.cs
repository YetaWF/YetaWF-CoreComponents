/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    internal static class ActionHelperExtensions {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ActionHelperExtensions), name, defaultValue, parms); }

        public static async Task<ActionInfo> RenderActionAsync(this YHtmlHelper htmlHelper, ModuleDefinition module, object? parameters = null) {

            ActionInfo info = new ActionInfo();

            // Check direct module invocation (for now)
            Type moduleType = module.GetType();
            MethodInfo? miAsync = moduleType.GetMethod(ModuleDefinition.MethodRenderModuleAsync);
            if (miAsync == null)
                throw new InternalError($"Method {ModuleDefinition.MethodRenderModuleAsync} not found for module {module.FullClassName}");

            // verify resource authorize attribute
            ResourceAuthorizeAttribute? resAttr = (ResourceAuthorizeAttribute?)Attribute.GetCustomAttribute(miAsync, typeof(ResourceAuthorizeAttribute));
            if (resAttr != null) {
                if (!await Resource.ResourceAccess.IsResourceAuthorizedAsync(resAttr.Name))
                    throw new Error(__ResStr("notAuth", "Not Authorized"));
            }

            // check if the action is authorized by checking the module's authorization
            string? level = null;
            PermissionAttribute? permAttr = (PermissionAttribute?)Attribute.GetCustomAttribute(miAsync, typeof(PermissionAttribute));
            if (permAttr != null)
                level = permAttr.Level;

            if (!module.IsAuthorized(level)) {
                // We get here if an action is attempted that the user is not authorized for
                // we could attempt to capture and redirect to user login, whatevz
                return ActionInfo.Empty;
            }
            // we use the module and the action to invoke direct rendering
            Task<ActionInfo> result = ModuleEndpoints.InvokeRenderMethod<ActionInfo>(module, null, miAsync, miAsync.GetParameters(), null);
            info = await result;
            return info;
        }
    }
}
