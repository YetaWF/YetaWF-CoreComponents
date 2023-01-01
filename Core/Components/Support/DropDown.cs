/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Models;

namespace YetaWF.Core.Components {

    /// <summary>
    /// An instance of the SelectionItem class represents an entry suitable for use with the DropDownList template.
    /// </summary>
    /// <typeparam name="TYPE">Defines the type of the Value property, used as value in the dropdownlist.</typeparam>
    public class SelectionItem<TYPE> {
        /// <summary>
        /// The text displayed in the dropdownlist for the entry.
        /// </summary>
        public MultiString Text { get; set; } = null!;
        /// <summary>
        /// The value in the dropdownlist for the entry.
        /// </summary>
        public TYPE Value { get; set; } = default!;
        /// <summary>
        /// The tooltip displayed in the dropdownlist for the entry.
        /// </summary>
        public MultiString? Tooltip { get; set; }
    }
}
