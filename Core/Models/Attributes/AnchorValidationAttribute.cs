/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AnchorValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public AnchorValidationAttribute() : base(@"^\s*[_a-zA-Z][_a-zA-Z0-9-]*\s*$",
                __ResStr("valAnchor", "The anchor id is invalid - use only the letters a-z, A-Z, _, - and 0-9 for ids"),
                __ResStr("valAnchor2", "The anchor id is invalid ('{0}' property) - use only the letters a-z, A-Z, _, - and 0-9 for ids"),
                __ResStr("valAnchor3", "The anchor id '{0}' is invalid - use only the letters a-z, A-Z, _, - and 0-9 for ids")
            ) { }
    }
}
