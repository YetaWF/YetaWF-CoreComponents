/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text.Json.Serialization;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Definitions for the HelpInfo component used to render a help file.
    /// The help file contains HTML and is rendered as-is.
    /// </summary>
    public class HelpInfoDefinition {
        /// <summary>
        /// The package owning the help file.
        /// </summary>
        [JsonIgnore]
        public Package Package { get; set; } = null!;
        /// <summary>
        /// The name of the help file (without path or file extension.
        /// </summary>
        public string Name { get; set; } = null!;
        /// <summary>
        /// Defines whether the small object cache is used to cache the help file. Use for small help files only.
        /// Any help files over 1K bytes of data are not cached.
        /// </summary>
        public bool UseCache { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public HelpInfoDefinition() {
            UseCache = true;
        }
    }
}
