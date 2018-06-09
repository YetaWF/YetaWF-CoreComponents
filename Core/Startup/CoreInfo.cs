/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {

    public class CoreInfo : IAddOnSupport {

        public const string Resource_BuiltinCommands = "YetaWF_Core-BuiltinCommands";
        public const string Resource_UploadImages = "YetaWF_Core-UploadImages";
        public const string Resource_RemoveImages = "YetaWF_Core-RemoveImages";
        public const string Resource_SkinLists = "YetaWF_Core-SkinLists";
        public const string Resource_ViewOwnership = "YetaWF_Core-ViewOwnership";
        public const string Resource_ModuleLists = "YetaWF_Core-ModuleLists";

        public const string Resource_SMTPServer_SendTestEmail = "YetaWF_Core-SMTPServer_SendTestEmail";//$$$ THIS is probably in the wrong place

        public Task AddSupportAsync(YetaWFManager manager) {
            return Task.CompletedTask;
        }
    }
}