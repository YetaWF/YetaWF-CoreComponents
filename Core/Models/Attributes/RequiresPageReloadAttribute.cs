/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
#endif

namespace YetaWF.Core.Models.Attributes {

    /// <summary>
    /// When a property is modified that has this attribute, a page reload is required.
    /// </summary>
    /// <remarks>This is used by the Audit log in the YetaWF.Dashboard package and by page/module editing to force a page reload when necessary.</remarks>
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RequiresPageReloadAttribute : Attribute { }
}
