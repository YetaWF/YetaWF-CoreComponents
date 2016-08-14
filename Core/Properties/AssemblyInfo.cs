/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
[assembly: AssemblyCopyright("Copyright © 2016 - Softel vdm, Inc.")]
[assembly: AssemblyProduct("Core")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.4.0")]

[assembly: Package(PackageTypeEnum.Core, "YetaWF")]
[assembly: PackageInfo("http://YetaWF.com/UpdateServer",
    "http://yetawf.com/Documentation/YetaWF/Core",
    "http://YetaWF.com/Documentation/YetaWF/Support",
    "http://yetawf.com/Documentation/YetaWF/Core#Release%20Notice",
    "http://yetawf.com/Documentation/YetaWF/Core#License")]

[assembly: PublicPartialViews]

//TODO: Check for completeness before each release
[assembly: RequiresAddOnGlobal("bassistance.de", "jquery-validation")]
[assembly: RequiresAddOnGlobal("gist.github.com_remi_957732", "jquery_validate_hooks")]
[assembly: RequiresAddOnGlobal("github.com.danielm", "uploader")]
[assembly: RequiresAddOnGlobal("github.com.free-jqgrid", "jqgrid")]
[assembly: RequiresAddOnGlobal("jquery.com", "jquery")]
[assembly: RequiresAddOnGlobal("jquery.com", "jquery-color")]
[assembly: RequiresAddOnGlobal("jqueryui.com", "jqueryui")]
[assembly: RequiresAddOnGlobal("jqueryui.com", "jqueryui-themes")]
[assembly: RequiresAddOnGlobal("jstree.com", "jsTree")]
[assembly: RequiresAddOnGlobal("medialize.github.io", "uri.js")]
[assembly: RequiresAddOnGlobal("microsoft.com", "jquery_unobtrusive_validation")]
[assembly: RequiresAddOnGlobal("necolas.github.io", "normalize")]
[assembly: RequiresAddOnGlobal("no-margin-for-errors.com", "prettyLoader")]
//[assembly: RequiresAddOnGlobal("telerik.com", "Kendo_UI_Core")] // this is validated in code
//[assembly: RequiresAddOnGlobal("telerik.com", "Kendo_UI_Pro")]

[assembly: RequiresAddOn("YetaWF", "Core", "Basics")]
[assembly: RequiresAddOn("YetaWF", "Core", "Forms")]
[assembly: RequiresAddOn("YetaWF", "Core", "kendoMenu")]
[assembly: RequiresAddOn("YetaWF", "Core", "Modules")]
[assembly: RequiresAddOn("YetaWF", "Core", "Popups")]

[assembly: Resource(CoreInfo.Resource_BuiltinCommands, "Allow use of built-in commands", Superuser = true)]
[assembly: Resource(CoreInfo.Resource_UploadImages, "Allow uploading images", User = true, Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_RemoveImages, "Remove uploaded images", User = true, Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_SkinLists, "Retrieve page/module skin lists (Ajax)", Editor = true, Administrator = true, Superuser = true)]
[assembly: Resource(CoreInfo.Resource_SMTPServer_SendTestEmail, "Send test emails (using SMTPServer control)", Administrator = true, Superuser = true)]


// TODO: There are some templates that generate <label for=id> where id points to a <div> which is invalid html5
