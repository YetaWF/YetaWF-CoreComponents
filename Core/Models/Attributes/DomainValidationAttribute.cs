/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DomainValidationAttribute : RegexValidationBaseAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public DomainValidationAttribute() : base(@"^\s*\S+\s*$",
                __ResStr("valDomain", "The domain is invalid"),
                __ResStr("valDomain2", "The domain (field '{0}') is invalid"),
                __ResStr("valDomain3", "The domain '{0}' is invalid")
            ) { }
    }
}
