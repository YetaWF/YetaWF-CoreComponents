/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using System;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    [Obsolete("Do not use Area() - This is automatically defined by YetaWF")]
    public class AreaAttribute : System.Attribute {
        public AreaAttribute(string areaName) { }
    }

}

#else
#endif
