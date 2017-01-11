/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Drawing;
using System.Web;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Image;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using YetaWF.Core.Upload;

namespace YetaWF.Core.Controllers.Shared
{
    public class ImageHelperController : YetaWFController
    {
        [HttpPost]
        [ResourceAuthorize(CoreInfo.Resource_UploadImages)]
        public ActionResult SaveImage(HttpPostedFileBase __filename, string __lastInternalName) {
            FileUpload upload = new FileUpload();
            string tempName = upload.StoreTempImageFile(__filename);

            if (!string.IsNullOrWhiteSpace(__lastInternalName)) // delete the previous file we had open
                upload.RemoveTempFile(__lastInternalName);

            Size size = ImageSupport.GetImageSize(tempName);

            ScriptBuilder sb = new ScriptBuilder();
            // Upload control considers Json result a success. result has a function to execute, newName has the file name
            sb.Append("{\n");
            sb.Append("  \"result\":");
            sb.Append("      \"Y_Confirm(\\\"{0}\\\");\",", this.__ResStr("saveImageOK", "Image \\\\\\\"{0}\\\\\\\" successfully uploaded", YetaWFManager.JserEncode(__filename.FileName)));
            sb.Append("  \"filename\": \"{0}\",\n", YetaWFManager.JserEncode(tempName));
            sb.Append("  \"realFilename\": \"{0}\",\n", YetaWFManager.JserEncode(__filename.FileName));
            sb.Append("  \"attributes\": \"{0}\"\n", this.__ResStr("imgAttr", "{0} x {1} (w x h)", size.Width, size.Height));
            sb.Append("}");
            return new JsonResult { Data = sb.ToString() };
        }

        [HttpPost]
        [ResourceAuthorize(CoreInfo.Resource_RemoveImages)]
        public ActionResult RemoveImage(string __filename, string __internalName) {
            FileUpload upload = new FileUpload();
            upload.RemoveTempFile(__internalName);
            return new EmptyResult();
        }
    }
}
