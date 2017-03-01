/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using System;

namespace YetaWF.Core.Support {

    // FromBody attribute so we can be source compatible on MVC5 - It does nothing
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromBodyAttribute : Attribute
    {
    }
}

#endif
