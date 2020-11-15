/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;

namespace YetaWF.Core.Models.Attributes {

    /// <summary>
    /// Represents the extent of the required restart. Values can be combined.
    /// </summary>
    [Flags]
    public enum RestartEnum {
        /// <summary>
        /// Only a multi-instance (web-farm/web-garden) needs to be restarted, i.e., all instances of the site.
        /// </summary>
        MultiInstance = 1,
        /// <summary>
        /// Only a single-instance (not a web-farm/web-garden) needs to be restarted.
        /// </summary>
        SingleInstance = 2,
        /// <summary>
        /// Both a multi-instance and a single-instance need to be restarted.
        /// </summary>
        All = 3,
    }

    /// <summary>
    /// When a property is modified that has this attribute, a site restart is required.
    /// </summary>
    /// <remarks>This is used by the Audit log in the YetaWF.Dashboard package to collect information whether a site restart is pending.</remarks>
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RequiresRestartAttribute : Attribute {
        /// <summary>
        /// Defines the extent of the required restart.
        /// </summary>
        public RestartEnum Restart { get; set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="restartFlags">Defines the extent of the required restart.</param>
        public RequiresRestartAttribute(RestartEnum restartFlags) {
            Restart = restartFlags;
        }
    }
}
