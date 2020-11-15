﻿/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

namespace YetaWF.Core.Support.UrlHistory {
    public class Origin {
        public string Url { get; set; } = null!;
        public bool EditMode { get; set; }
        public bool InPopup { get; set; }
    }
}
