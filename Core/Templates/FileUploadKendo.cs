/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class FileUploadKendo : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            string areaName = "FileUpload";

            scripts.AddLocalization(areaName, "CancelButtonText", this.__ResStr("CancelButtonText", "Cancel"));
            scripts.AddLocalization(areaName, "HeaderStatusUploading", this.__ResStr("HeaderStatusUploading", "Uploading..."));
            scripts.AddLocalization(areaName, "HeaderStatusUploaded", this.__ResStr("HeaderStatusUploaded", "Done"));
            scripts.AddLocalization(areaName, "RemoveButtonText", this.__ResStr("RemoveButtonText", "Remove"));
            scripts.AddLocalization(areaName, "RetryButtonText", this.__ResStr("RetryButtonText", "Retry"));
            scripts.AddLocalization(areaName, "StatusFailedText", this.__ResStr("StatusFailedText", "Failed!"));
            scripts.AddLocalization(areaName, "StatusUploadedText", this.__ResStr("StatusUploadedText", "Done!"));
            scripts.AddLocalization(areaName, "StatusUploadingText", this.__ResStr("StatusUploadingText", "Uploading..."));
            scripts.AddLocalization(areaName, "UploadFilesButtonText", this.__ResStr("UploadFilesButtonText", "Upload Selected Files"));
            scripts.AddLocalization(areaName, "FileTooLarge", "The uploaded file is too large");
            scripts.AddLocalization(areaName, "UnexpectedStatus", "Unexpected return status from server - {0}");
        }
    }
}
