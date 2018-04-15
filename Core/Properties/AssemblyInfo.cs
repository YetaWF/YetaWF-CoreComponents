/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
[assembly: AssemblyCopyright("Copyright © 2018 - Softel vdm, Inc.")]
[assembly: AssemblyProduct("Core")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("3.0.0.0")]

[assembly: Package(PackageTypeEnum.Core, "YetaWF")]
[assembly: ServiceLevel(ServiceLevelEnum.Core)]

[assembly: PackageInfo("https://YetaWF.com/UpdateServer",
    "https://yetawf.com/Documentation/YetaWF/Core",
    "https://YetaWF.com/Documentation/YetaWF/Support",
    "https://yetawf.com/Documentation/YetaWF/Core#Release%20Notice",
    "https://yetawf.com/Documentation/YetaWF/Core#License")]

[assembly: PublicPartialViews]

[assembly: Resource(CoreInfo.Resource_BuiltinCommands, "Allow use of built-in commands", Superuser = true)]
[assembly: Resource(CoreInfo.Resource_UploadImages, "Allow uploading images", User = true, Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_RemoveImages, "Remove uploaded images", User = true, Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_SkinLists, "Retrieve page/module skin lists (Ajax)", Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_SMTPServer_SendTestEmail, "Send test emails (using SMTPServer control)", Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_ViewOwnership, "View module & page ownership", Editor = true, Administrator = true, Superuser = true)]

// TODO: There are some templates that generate <label for=id> where id points to a <div> which is invalid html5
