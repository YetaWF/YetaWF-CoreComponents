/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AreaValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public AreaValidationAttribute() : base(@"^\s*[a-zA-Z0-9]{1,50}_[a-zA-Z0-9]{1,50}\s*$",
                __ResStr("valArea", "The area specified is invalid - Please use only letters and numbers in the format domain_product"),
                __ResStr("valArea2", "The area specified is invalid ('{0}' field) - Please use only letters and numbers in the format domain_product"),
                __ResStr("valArea3", "The area '{0}' is invalid - Please use only letters and numbers in the format domain_product")
            ) { }
    }
}
