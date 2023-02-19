/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class StringLengthAttribute : System.ComponentModel.DataAnnotations.StringLengthAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public StringLengthAttribute(int maximumLength) : base(maximumLength) { }
        public new int MaximumLength { get { return base.MaximumLength;  } }
        public override bool IsValid(object? value) {
            if (MaximumLength == 0) return true;
            if (value is MultiString) {
                MultiString ms = (MultiString)value;
                foreach (var mse in ms) {
                    string s = mse.Value;
                    if (!base.IsValid(s))
                        return false;
                }
                return true;
            } else if (value is List<string>) {
                List<string> list = (List<string>)value;
                foreach (string s in list) {
                    if (!base.IsValid(s))
                        return false;
                }
                return true;
            } else {
                if (value != null) {
                    TypeConverter conv = TypeDescriptor.GetConverter(value.GetType());
                    string? val = conv.ConvertToString(value);
                    return base.IsValid(val);
                }
                return base.IsValid(value);
            }
        }
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
            if (MaximumLength == 0) return ValidationResult.Success;
            ValidationResult? result = base.IsValid(value, validationContext);
            if (result == ValidationResult.Success)
                return ValidationResult.Success;
            string errorMessage = GetErrorMessage(validationContext.DisplayName);
            return new ValidationResult(errorMessage);
        }
        private string GetErrorMessage(string? caption) {
            string errorMessage;
            if (MinimumLength == 0 && MaximumLength > 0)
                errorMessage = __ResStr("badStringLengthMax", "The length for '{0}' can't exceed {1} characters",
                    caption, MaximumLength);
            else
                errorMessage = __ResStr("badStringLengthMinMax", "The length for '{0}' must be between {1} and {2} characters",
                    caption, MinimumLength, MaximumLength);
            return errorMessage;
        }
        public class ValidationStringLength : ValidationBase {
            public int Min { get; set; }
            public int Max { get; set; }
        }
        public ValidationBase? AddValidation(object container, PropertyData propData, string caption) {
            if (MaximumLength == 0) return null;
            return new ValidationStringLength {
                Method = nameof(StringLengthAttribute),
                Message = GetErrorMessage(caption),
                Min = MinimumLength,
                Max = MaximumLength,
            };
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RangeAttribute : System.ComponentModel.DataAnnotations.RangeAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RangeAttribute(int minimum, int maximum) : base(minimum, maximum) { }
        public RangeAttribute(double minimum, double maximum) : base(minimum, maximum) { }
        public RangeAttribute(decimal minimum, decimal maximum) : base((double) minimum, (double) maximum) { }
        public RangeAttribute(Type type, string minimum, string maximum) : base(type, minimum, maximum) { }

        public class ValidationRange : ValidationBase {
            public object Min { get; set; } = null!;
            public object Max { get; set; } = null!;
        }
        public ValidationBase? AddValidation(object container, PropertyData propData, string caption) {
            return new ValidationRange {
                Method = nameof(RangeAttribute),
                Message = __ResStr("range", "The '{0}' value must be between {1} and {2}", caption, Minimum, Maximum),
                Min = Minimum,
                Max = Maximum,
            };
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SiteDomainValidationAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public SiteDomainValidationAttribute() : base(@"^\s*[A-Za-z0-9][A-Za-z0-9\.\-]*\.[A-Za-z0-9]+\s*$",
                __ResStr("errInvSite", "The site's domain name is invalid - It cannot use http:// or https:// and it can only contain letters, numbers and these characters: - ."),
                __ResStr("errInvSite2", "The site's domain name is invalid (field '{0}') - It cannot use http:// or https:// and it can only contain letters, numbers and these characters: - ."),
                __ResStr("errInvSite3", "The site's domain name '{0}' is invalid - It cannot use http:// or https:// and it can only contain letters, numbers and these characters: - .")
            ) { }
    }

    //sample: <meta name="google-site-verification" content="flC7VM4WGUt7vWo8iiP2-EQ60L4jC44BVOTjpPmH0hg" />
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GoogleVerificationExpressionAttribute : RegexValidationBaseAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public GoogleVerificationExpressionAttribute() : base(@"^(\s*<meta\s+name=""google\-site\-verification""\s+content=\""[^\""]+?\""\s*/>\s*)+$",
                __ResStr("errInvMeta", "The meta tag is invalid - It should be in the format <meta name=\"google-site-verification\" content=\"....your-code....\" />"),
                __ResStr("errInvMeta2", "The meta tag is invalid (field '{0}') - It should be in the format <meta name=\"google-site-verification\" content=\"....your-code....\" />"),
                __ResStr("errInvMeta3", "The meta tag '{0}' is invalid - It should be in the format <meta name=\"google-site-verification\" content=\"....your-code....\" />")
            ) { }
    }
}
