/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
#endif

namespace YetaWF.Core.Models.Attributes {

    [Flags]
    public enum RestartEnum {
        MultiInstance = 1,
        SingleInstance = 2,
        All = 3,
    }

    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RequiresRestartAttribute : Attribute {
        public RestartEnum Restart { get; set; }
        public RequiresRestartAttribute(RestartEnum restartFlags) {
            Restart = restartFlags;
        }
    }
}
