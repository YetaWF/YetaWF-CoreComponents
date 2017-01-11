/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CssClassesValidationAttribute : DataTypeAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public CssClassesValidationAttribute() : base(DataType.Text) {
            ErrorMessage = __ResStr("valCssClasses", "The classes listed are invalid - Please separate classes using spaces (not commas) and use only the letters a-z, A-Z, _ and 0-9 for class names - Class names can't start with a digit");
        }

        private static Regex _regex = new Regex(@"^\s*(([_a-zA-Z][_a-zA-Z0-9-]*)\s*)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override bool IsValid(object value) {
            string valueAsString = value as string;
            if (string.IsNullOrWhiteSpace(valueAsString)) return true;
            return _regex.Match(valueAsString).Length > 0;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CssClassValidationAttribute : DataTypeAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public CssClassValidationAttribute() : base(DataType.Text) {
            ErrorMessage = __ResStr("valCssClass", "The class listed are invalid - Use only the letters a-z, A-Z, _ and 0-9 for a class name - The class name can't start with a digit");
        }

        private static Regex _regex = new Regex(@"^\s*(([_a-zA-Z][_a-zA-Z0-9-]*)\s*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override bool IsValid(object value) {
            string valueAsString = value as string;
            if (string.IsNullOrWhiteSpace(valueAsString)) return true;
            return _regex.Match(valueAsString).Length > 0;
        }
    }
}
