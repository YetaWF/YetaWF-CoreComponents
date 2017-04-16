/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using System;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Legacy attribute - not used in MVC6
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class AllowHtmlAttribute : Attribute {
        public AllowHtmlAttribute() { }
    }
}

#else
#endif
