/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AnchorValidationAttribute : DataTypeAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public AnchorValidationAttribute() : base(DataType.Text) {
            ErrorMessage = __ResStr("valAnchor", "The id is invalid - use only the letters a-z, A-Z, _, - and 0-9 for ids");
        }

        private static Regex _regex = new Regex(@"^\s*[_a-zA-Z][_a-zA-Z0-9-]*\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override bool IsValid(object value) {
            string valueAsString = value as string;
            if (string.IsNullOrWhiteSpace(valueAsString)) return true;
            return _regex.Match(valueAsString).Length > 0;
        }
    }
}
