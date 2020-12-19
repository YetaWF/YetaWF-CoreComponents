/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ZipCodeValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ZipCodeValidationAttribute), name, defaultValue, parms); }

        public ZipCodeValidationAttribute() : base(@"^\s*[0-9]{5}(\-[0-9]{4}){0,1}\s*$",
                __ResStr("valZipCode", "The specified ZIP code is invalid - Use ZIP code format 00000 or 00000-0000"),
                __ResStr("valZipCode2", "The specified ZIP code is invalid ('{0}' field) - Use ZIP code format 00000 or 00000-0000"),
                __ResStr("valZipCode3", "The ZIP code '{0}' is invalid - Use ZIP code format 00000 or 00000-0000")
            ) { }
    }
}
