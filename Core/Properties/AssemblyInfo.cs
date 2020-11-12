/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Reflection;
using System.Runtime.InteropServices;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Packages;
using YetaWF.PackageAttributes;

[assembly: AssemblyTitle("Core Support")]
[assembly: AssemblyDescription("YetaWF Core Support")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Softel vdm, Inc.")]
[assembly: AssemblyCopyright("Copyright © 2020 - Softel vdm, Inc.")]
[assembly: AssemblyProduct("Core")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("5.3.0.0")]

[assembly: Package(PackageTypeEnum.Core, "YetaWF")]
[assembly: ServiceLevel(ServiceLevelEnum.Core)]

[assembly: PackageInfo("https://YetaWF.com/UpdateServer",
    "https://yetawf.com/Documentation/YetaWFCore",
    "https://YetaWF.com/Documentation/YetaWFCore#Support",
    "https://yetawf.com/Documentation/YetaWFCore#Release%20Notice",
    "https://yetawf.com/Documentation/YetaWFCore#License")]

[assembly: Resource(CoreInfo.Resource_BuiltinCommands, "Built-in commands", Superuser = true)]
[assembly: Resource(CoreInfo.Resource_UploadImages, "Uploading images", User = true, Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_RemoveImages, "Remove uploaded images", User = true, Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_SkinLists, "Retrieve page/module skin lists (Ajax)", Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_SMTPServer_SendTestEmail, "Send test emails (using SMTPServer control)", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ViewOwnership, "View module & page ownership", Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_PageSettings, "Edit page settings", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ModuleSettings, "Edit module settings", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ModuleLists, "View module lists", Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ModuleExport, "Export modules", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ModuleImport, "Import modules", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_PageExport, "Export pages", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_PageImport, "Import pages", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_PageAdd, "Add new pages", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ModuleExistingAdd, "Add existing modules", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ModuleNewAdd, "Add new modules", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_SiteSkins, "Change site skins", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_OtherUserLogin, "Log in as another user", Administrator = true, Superuser = true)]


// TODO: There are some templates that generate <label for=id> where id points to a <div> which is invalid html5
