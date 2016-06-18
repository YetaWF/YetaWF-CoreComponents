/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class FileUploadKendo : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {
            
            ScriptManager scripts = manager.ScriptManager;
            string areaName = "FileUpload";

            manager.ScriptManager.AddLocalization(areaName, "CancelButtonText", this.__ResStr("CancelButtonText", "Cancel"));
            manager.ScriptManager.AddLocalization(areaName, "HeaderStatusUploading", this.__ResStr("HeaderStatusUploading", "Uploading..."));
            manager.ScriptManager.AddLocalization(areaName, "HeaderStatusUploaded", this.__ResStr("HeaderStatusUploaded", "Done"));
            manager.ScriptManager.AddLocalization(areaName, "RemoveButtonText", this.__ResStr("RemoveButtonText", "Remove"));
            manager.ScriptManager.AddLocalization(areaName, "RetryButtonText", this.__ResStr("RetryButtonText", "Retry"));
            manager.ScriptManager.AddLocalization(areaName, "StatusFailedText", this.__ResStr("StatusFailedText", "Failed!"));
            manager.ScriptManager.AddLocalization(areaName, "StatusUploadedText", this.__ResStr("StatusUploadedText", "Done!"));
            manager.ScriptManager.AddLocalization(areaName, "StatusUploadingText", this.__ResStr("StatusUploadingText", "Uploading..."));
            manager.ScriptManager.AddLocalization(areaName, "UploadFilesButtonText", this.__ResStr("UploadFilesButtonText", "Upload Selected Files"));
            manager.ScriptManager.AddLocalization(areaName, "FileTooLarge", "The uploaded file is too large");
            manager.ScriptManager.AddLocalization(areaName, "UnexpectedStatus", "Unexpected return status from server - {0}");
        }
    }
}
