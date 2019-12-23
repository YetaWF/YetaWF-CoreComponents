/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core.Components {

    public class UploadResponse {
        public string Result { get; set; }
        public string FileName { get; set; }
        public string FileNamePlain { get; set; }
        public string RealFileName { get; set; }
        public string Attributes { get; set; }
        public string List { get; set; }
    }

    public class UploadRemoveResponse {
        public string Result { get; set; }
        public string List { get; set; }
    }

}
