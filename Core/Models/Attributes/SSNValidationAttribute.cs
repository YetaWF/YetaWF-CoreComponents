/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SSNValidationAttribute : RegexValidationBaseAttribute {

        public const int MaxSSN = 9;

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public SSNValidationAttribute() : base(@"^\s*([0-9]{9})\s*$",
                __ResStr("valSSN", "The social security number is invalid"),
                __ResStr("valSSN2", "The social security number is invalid (field '{0}')"),
                __ResStr("valSSN3", "The social security number '{0}' is invalid")
            ) { }

        /// <summary>
        /// Returns a formatted user-displayable social security number (including spaces, parentheses, etc.)
        /// </summary>
        /// <param name="ssn">The social security number to format.</param>
        /// <returns>Returns a formatted user-displayable social security number.</returns>
        public static string? GetDisplay(string ssn) {
            if (string.IsNullOrWhiteSpace(ssn) || ssn.Length != MaxSSN)
                return null;
            return $"{ssn.Substring(0, 3)}-{ssn.Substring(3, 2)}-{ssn.Substring(5, 4)}";
        }
    }
}



