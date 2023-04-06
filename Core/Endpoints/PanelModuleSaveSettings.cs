/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Endpoints.Filters;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Support.Repository;

namespace YetaWF.Modules.ComponentsHTML.Endpoints;

/// <summary>
/// Endpoints for the PanelModule template.
/// </summary>
public class PanelModuleSaveSettingsEndpoints : YetaWFEndpoints {

    internal const string SaveExpandCollapse = "SaveExpandCollapse";

    private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(PanelModuleSaveSettingsEndpoints), name, defaultValue, parms); }

    /// <summary>
    /// Registers endpoints for the PanelModuleSaveSettings template.
    /// </summary>
    public static void RegisterEndpoints(IEndpointRouteBuilder endpoints, Package package, string areaName) {

        RouteGroupBuilder group = endpoints.MapGroup(GetPackageApiRoute(package, typeof(PanelModuleSaveSettingsEndpoints)));

        // Saves an uploaded image file. Works in conjunction with the PanelModuleSaveSettings template and YetaWF.Core.Upload.FileUpload.
        group.MapPost(SaveExpandCollapse, (HttpContext context, Guid __ModuleGuid, bool expanded) => {
            SettingsDictionary modSettings = Manager.SessionSettings.GetModuleSettings(__ModuleGuid);
            modSettings.SetValue<bool>("PanelExpanded", expanded);
            modSettings.Save();
            return Done();
        })
            .AntiForgeryToken();
    }
}
