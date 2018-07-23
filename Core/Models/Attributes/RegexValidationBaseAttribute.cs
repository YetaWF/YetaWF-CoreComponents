/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    public class RegexValidationBaseAttribute : DataTypeAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RegexValidationBaseAttribute(string pattern, string message, string errorMessageWithFieldFormat = null, string errorMessageWithDataFormat = null) : base(DataType.Text) {
            ErrorMessage = ErrorMessageWithFieldFormat = message;
            if (!string.IsNullOrWhiteSpace(errorMessageWithFieldFormat))
                ErrorMessageWithFieldFormat = errorMessageWithFieldFormat;
            if (!string.IsNullOrWhiteSpace(errorMessageWithDataFormat))
                ErrorMessageWithDataFormat = errorMessageWithDataFormat;
            Pattern = pattern;
        }

        public string ErrorMessageWithFieldFormat { get; protected set; }
        public string ErrorMessageWithDataFormat { get; protected set; }

        public string Pattern {
            get {
                return _Pattern;
            }
            protected set {
                _Pattern = value;
                _regex = new Regex(_Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            }
        }
        protected Regex _regex { get; set; }
        private string _Pattern;

        public override bool IsValid(object value) {
            if (value == null) return true;
            if (value is string) {
                string valueAsString = (string)value;
                if (string.IsNullOrWhiteSpace(valueAsString)) return true;
                if (_regex.Match(valueAsString).Length == 0) {
                    ErrorMessage = string.Format(ErrorMessageWithDataFormat, valueAsString);
                    return false;
                }
                return true;
            } else if (value is List<string>) {
                List<string> list = (List<string>)value;
                foreach (string l in list) {
                    if (!IsValid(l)) return false;
                }
                return true;
            } else if (value is System.Guid || value is System.Guid?) {
                // any guid is valid as it was already converted into a guid, so no checking needed
                return true;
            } else
                throw new InternalError("Invalid type used for RegexValidationBaseAttribute - {0}", value.GetType().FullName);
        }
        public void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = string.Format(ErrorMessageWithFieldFormat, propData.GetCaption(container));
            tag.MergeAttribute("data-val-regex", msg);
            tag.MergeAttribute("data-val-regex-pattern", Pattern);
            tag.MergeAttribute("data-val", "true");
        }
    }
}
