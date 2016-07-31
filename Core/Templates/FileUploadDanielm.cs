/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class FileUploadDanielm : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            string areaName = "FileUpload";

            manager.ScriptManager.AddLocalization(areaName, "StatusUploadNoResp", "Upload failed - The file is too large or the server did not respond");
            manager.ScriptManager.AddLocalization(areaName, "StatusUploadFailed", "Upload failed - {0}");
            manager.ScriptManager.AddLocalization(areaName, "FileTypeError", "The file type is invalid and can't be uploaded");
            manager.ScriptManager.AddLocalization(areaName, "FileSizeError", "The file is too large and can't be uploaded");
            manager.ScriptManager.AddLocalization(areaName, "FallbackMode", "Your browser doesn't support file uploading");
        }
    }
}
