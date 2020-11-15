/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Components {

    public class StringTT {
        [UIHint("String"), ReadOnly]
        public string Text { get; set; } = null!;
        [UIHint("String"), ReadOnly]
        public string? Tooltip { get; set; }
    }
}
