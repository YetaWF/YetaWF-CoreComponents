/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    internal static class ActionHelperExtensions {

        public static async Task<ActionInfo> RenderActionAsync(this YHtmlHelper htmlHelper, ModuleDefinition module, object? parameters = null) {

            ActionInfo info = new ActionInfo();

            // Check direct module invocation (for now)
            Type moduleType = module.GetType();
            MethodInfo? miAsync = moduleType.GetMethod(ModuleDefinition2.MethodRenderModuleAsync);
            if (miAsync == null)
                throw new InternalError($"Method {ModuleDefinition2.MethodRenderModuleAsync} not found for module {module.FullClassName}");

            //$$$$ resource authorize attribute

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
