/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {

    public class CoreInfo : IAddOnSupport {

        public const string Resource_BuiltinCommands = "YetaWF_Core-BuiltinCommands";
        public const string Resource_UploadImages = "YetaWF_Core-UploadImages";
        public const string Resource_RemoveImages = "YetaWF_Core-RemoveImages";
        public const string Resource_SkinLists = "YetaWF_Core-SkinLists";
        public const string Resource_ViewOwnership = "YetaWF_Core-ViewOwnership";
        public const string Resource_PageSettings = "YetaWF_Core-PageSettings";
        public const string Resource_ModuleSettings = "YetaWF_Core-ModuleSettings";
        public const string Resource_ModuleLists = "YetaWF_Core-ModuleLists";
        public const string Resource_ModuleExport = "YetaWF_Core-ModuleExport";
        public const string Resource_ModuleImport = "YetaWF_Core-ModuleImport";
        public const string Resource_PageExport = "YetaWF_Core-PageExport";
        public const string Resource_PageImport = "YetaWF_Core-PageImport";
        public const string Resource_PageAdd = "YetaWF_Core-PageAdd";
        public const string Resource_ModuleExistingAdd = "YetaWF_Core-ModuleExistingAdd";
        public const string Resource_ModuleNewAdd = "YetaWF_Core-ModuleNewAdd";
        public const string Resource_SiteSkins = "YetaWF_Core-SiteSkins";
        public const string Resource_OtherUserLogin = "YetaWF_Core-OtherUserLogin";

        public const string Resource_SMTPServer_SendTestEmail = "YetaWF_Core-SMTPServer_SendTestEmail";

        public Task AddSupportAsync(YetaWFManager manager) {
            return Task.CompletedTask;
        }
    }
}