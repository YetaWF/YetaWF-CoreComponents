/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Models;

namespace YetaWF.Core.Components {

    public class SelectionItem<TYPE> {
        public MultiString Text { get; set; }
        public TYPE Value { get; set; }
        public MultiString Tooltip { get; set; }
    }
}
