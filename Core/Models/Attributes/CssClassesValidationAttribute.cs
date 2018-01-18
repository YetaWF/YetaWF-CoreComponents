/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CssClassesValidationAttribute : RegexValidationBaseAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public CssClassesValidationAttribute() : base(@"^\s*(([_a-zA-Z][_a-zA-Z0-9-]*)\s*)*$",
                __ResStr("valCssClasses", "The classes listed are invalid - Please separate classes using spaces (not commas) and use only the letters a-z, A-Z, _ and 0-9 for class names - Class names can't start with a digit"),
                __ResStr("valCssClasses2", "The classes listed are invalid (field '{0}') - Please separate classes using spaces (not commas) and use only the letters a-z, A-Z, _ and 0-9 for class names - Class names can't start with a digit"),
                __ResStr("valCssClasses3", "The classes '{0}' are invalid - Please separate classes using spaces (not commas) and use only the letters a-z, A-Z, _ and 0-9 for class names - Class names can't start with a digit")
            ) { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CssClassValidationAttribute : RegexValidationBaseAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public CssClassValidationAttribute() : base(@"^\s*[_a-zA-Z][_a-zA-Z0-9-]*\s*$",
                __ResStr("valCssClass", "The class listed is invalid - Use only the letters a-z, A-Z, _ and 0-9 for a class name - The class name can't start with a digit"),
                __ResStr("valCssClass2", "The class listed is invalid (field '{0}') - Use only the letters a-z, A-Z, _ and 0-9 for a class name - The class name can't start with a digit"),
                __ResStr("valCssClass3", "The class '{0}' is invalid - Use only the letters a-z, A-Z, _ and 0-9 for a class name - The class name can't start with a digit")
            ) { }
    }
}
