/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
#endif

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RequiresPageReloadAttribute : Attribute { }
}
