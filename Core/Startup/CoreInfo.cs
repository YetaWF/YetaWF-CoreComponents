/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;

namespace YetaWF.Core.Addons {

    public class CoreInfo : IAddOnSupport {

        public const string Resource_BuiltinCommands = "YetaWF_Core-BuiltinCommands";
        public const string Resource_UploadImages = "YetaWF_Core-UploadImages";
        public const string Resource_RemoveImages = "YetaWF_Core-RemoveImages";
        public const string Resource_SkinLists = "YetaWF_Core-SkinLists";

        public const string Resource_SMTPServer_SendTestEmail = "YetaWF_Core-SMTPServer_SendTestEmail";

        public void AddSupport(YetaWFManager manager) { }
    }
}