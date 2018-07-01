/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PackageDomainValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public PackageDomainValidationAttribute() : base(@"^\s*[a-zA-Z0-9]{1,50}\s*$",
                __ResStr("valPkgDomain", "The domain specified is invalid - Please use only letters and numbers"),
                __ResStr("valPkgDomain2", "The domain specified is invalid ({0}) - Please use only letters and numbers"),
                __ResStr("valPkgDomain3", "The domain '{0}' is invalid - Please use only letters and numbers")
            ) { }
    }
}
