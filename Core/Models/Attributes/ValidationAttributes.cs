/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class StringLengthAttribute : System.ComponentModel.DataAnnotations.StringLengthAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public StringLengthAttribute(int maximumLength) : base(maximumLength) { }
        public new int MaximumLength { get { return base.MaximumLength;  } }
        public override bool IsValid(object value) {
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
            } else
                return base.IsValid(value);
        }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
            if (MaximumLength == 0) return ValidationResult.Success;
            ValidationResult result = base.IsValid(value, validationContext);
            if (result == ValidationResult.Success)
                return ValidationResult.Success;
            string errorMessage = GetErrorMessage(AttributeHelper.GetPropertyCaption(validationContext));
            return new ValidationResult(errorMessage);
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = GetErrorMessage(AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-length", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-length-max", MaximumLength.ToString());
            if (MinimumLength > 0)
                AttributeHelper.MergeAttribute(context.Attributes, "data-val-length-min", MinimumLength.ToString());
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            string errorMessage = GetErrorMessage(AttributeHelper.GetPropertyCaption(metadata));
            yield return new ModelClientValidationStringLengthRule(errorMessage, MinimumLength, MaximumLength);
        }
#endif
        private string GetErrorMessage(string caption) {
            string errorMessage;
            if (MinimumLength == 0 && MaximumLength > 0)
                errorMessage = __ResStr("badStringLengthMax", "The length for '{0}' can't exceed {1} characters",
                    caption, MaximumLength);
            else
                errorMessage = __ResStr("badStringLengthMinMax", "The length for '{0}' must be between {1} and {2} characters",
                    caption, MinimumLength, MaximumLength);
            return errorMessage;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RangeAttribute : System.ComponentModel.DataAnnotations.RangeAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RangeAttribute(int minimum, int maximum) : base(minimum, maximum) { }
        public RangeAttribute(double minimum, double maximum) : base(minimum, maximum) { }
        public RangeAttribute(decimal minimum, decimal maximum) : base((double) minimum, (double) maximum) { }
        public RangeAttribute(Type type, string minimum, string maximum) : base(type, minimum, maximum) { }

#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = __ResStr("range", "The '{0}' value must be between {1} and {2}",
                    AttributeHelper.GetPropertyCaption(context.ModelMetadata), Minimum, Maximum);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-range", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-range-min", base.Minimum.ToString());
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-range-max", base.Maximum.ToString());
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            string errorMessage = ErrorMessage;
            if (string.IsNullOrWhiteSpace(errorMessage))
                ErrorMessage = __ResStr("range", "The '{0}' value must be between {1} and {2}",
                    AttributeHelper.GetPropertyCaption(metadata), Minimum, Maximum);
            yield return new ModelClientValidationRangeRule(ErrorMessage, base.Minimum, base.Maximum); // string.Format(base.ErrorMessageString, metadata.DisplayName), base.Minimum, base.Maximum);
        }
#endif
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SiteDomainValidationAttribute : RegexValidationBaseAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public SiteDomainValidationAttribute() : base(@"^\s*[A-Za-z0-9][A-Za-z0-9\.\-]*\.[A-Za-z0-9]+\s*$",
                __ResStr("errInvSite", "The site's domain name is invalid - It cannot use http:// or https:// and it can only contain letters, numbers and these characters: - ."),
                __ResStr("errInvSite2", "The site's domain name is invalid (field '{0}') - It cannot use http:// or https:// and it can only contain letters, numbers and these characters: - ."),
                __ResStr("errInvSite3", "The site's domain name '{0}' is invalid - It cannot use http:// or https:// and it can only contain letters, numbers and these characters: - .")
            ) { }
    }

    //sample: <meta name="google-site-verification" content="flC7VM4WGUt7vWo8iiP2-EQ60L4jC44BVOTjpPmH0hg" />
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GoogleVerificationExpressionAttribute : RegexValidationBaseAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public GoogleVerificationExpressionAttribute() : base(@"^(\s*<meta\s+name=""google\-site\-verification""\s+content=\""[^\""]+?\""\s*/>\s*)+$",
                __ResStr("errInvMeta", "The meta tag is invalid - It should be in the format <meta name=\"google-site-verification\" content=\"....your-code....\" />"),
                __ResStr("errInvMeta2", "The meta tag is invalid (field '{0}') - It should be in the format <meta name=\"google-site-verification\" content=\"....your-code....\" />"),
                __ResStr("errInvMeta3", "The meta tag '{0}' is invalid - It should be in the format <meta name=\"google-site-verification\" content=\"....your-code....\" />")
            ) { }
    }
}
