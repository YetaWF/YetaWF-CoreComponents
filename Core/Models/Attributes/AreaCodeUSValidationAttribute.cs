/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class AreaCodeUSValidationAttribute : ValidationAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(AreaCodeUSValidationAttribute), name, defaultValue, parms); }

        public AreaCodeUSValidationAttribute() { }

        protected override ValidationResult? IsValid(object? value, ValidationContext context) {
            if (value != null) {
                string areaCode = (string)value;
                if (!AreaCodeUSValidationAttribute.Valid(areaCode))
                    return new ValidationResult(__ResStr("inv", "{0} is an invalid area code", areaCode));
            }
            return ValidationResult.Success;
        }

        public static bool Valid(string areaCode) {
            Regex areaRegex = new Regex(@"^[0-9]{3}$");
            return areaRegex.IsMatch(areaCode);
        }
    }
}
