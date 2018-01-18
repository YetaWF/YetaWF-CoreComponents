/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class FileUpload : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            string areaName = "FileUpload";

            scripts.AddLocalization(areaName, "StatusUploadNoResp", "Upload failed - The file is too large or the server did not respond");
            scripts.AddLocalization(areaName, "StatusUploadFailed", "Upload failed - {0}");
            scripts.AddLocalization(areaName, "FileTypeError", "The file type is invalid and can't be uploaded");
            scripts.AddLocalization(areaName, "FileSizeError", "The file is too large and can't be uploaded");
            scripts.AddLocalization(areaName, "FallbackMode", "Your browser doesn't support file uploading");
        }
    }
}
