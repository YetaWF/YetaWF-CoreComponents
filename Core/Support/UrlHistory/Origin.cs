/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core.Support.UrlHistory {
    public class Origin {
        public string Url { get; set; } = null!;
        public bool EditMode { get; set; }
        public bool InPopup { get; set; }
    }
}
