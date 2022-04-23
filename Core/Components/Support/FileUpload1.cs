/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;

namespace YetaWF.Core.Components {

    public class FileUpload1 {

        public FileUpload1() {
            SelectButtonText = this.__ResStr("btnSelFile", "Select File...");
            SelectButtonTooltip = this.__ResStr("btnSelFileTT", "Click to browse for files");
            DropFilesText = this.__ResStr("txtDrop", "Drop files here to upload");
            SerializeForm = false;
        }

        public string SelectButtonText { get; set; }
        public string SelectButtonTooltip { get; set; }
        public string DropFilesText { get; set; }
        public bool SerializeForm { get; set; }// serialize all form data when uploading a file

        public string SaveURL { get; set; } = null!;
        public string RemoveURL { get; set; } = null!;
    }
}
