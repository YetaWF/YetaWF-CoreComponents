﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class StringLengthAttribute : System.ComponentModel.DataAnnotations.StringLengthAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public StringLengthAttribute(int maximumLength) : base(maximumLength) { }
        public new int MaximumLength { get { return base.MaximumLength;  } }
        public override bool IsValid(object value) {
            if (MaximumLength == 0) return true;
            if (value is MultiString) {
                MultiString ms = (MultiString) value;
                foreach (var mse in ms) {
                    string s = mse.Value;
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

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            string errorMessage = GetErrorMessage(AttributeHelper.GetPropertyCaption(metadata));
            yield return new ModelClientValidationStringLengthRule(errorMessage, MinimumLength, MaximumLength);
        }
        private string GetErrorMessage(string caption) {
            string errorMessage;
            if (MinimumLength == 0 && MaximumLength > 0)
                errorMessage = string.Format(__ResStr("badStringLengthMax", "The length for '{0}' can't exceed {1} characters"),
                    caption, MaximumLength);
            else
                errorMessage = string.Format(__ResStr("badStringLengthMinMax", "The length for '{0}' must be between {1} and {2} characters"),
                    caption, MinimumLength, MaximumLength);
            return errorMessage;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RangeAttribute : System.ComponentModel.DataAnnotations.RangeAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RangeAttribute(int minimum, int maximum) : base(minimum, maximum) { }
        public RangeAttribute(double minimum, double maximum) : base(minimum, maximum) { }
        public RangeAttribute(decimal minimum, decimal maximum) : base((double) minimum, (double) maximum) { }
        public RangeAttribute(Type type, string minimum, string maximum) : base(type, minimum, maximum) { }
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            string errorMessage = ErrorMessage;
            if (string.IsNullOrWhiteSpace(errorMessage))
            ErrorMessage = string.Format(__ResStr("range", "The '{0}' value must be between {1} and {2}"),
                    AttributeHelper.GetPropertyCaption(metadata), Minimum, Maximum);
            yield return new ModelClientValidationRangeRule(ErrorMessage, base.Minimum, base.Maximum); // string.Format(base.ErrorMessageString, metadata.DisplayName), base.Minimum, base.Maximum);
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RegularExpressionAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute {
        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RegularExpressionAttribute(string pattern) : base(pattern) { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SiteDomainValidationAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        // An empty string is valid (otherwise add RequiredAttribute)
        public override bool IsValid(Object value) {
            if (string.IsNullOrWhiteSpace((string) value))
                return true;
            return base.IsValid(value);
        }
        public SiteDomainValidationAttribute()
            : base(@"^\s*[A-Za-z0-9][A-Za-z0-9\.\-]*[A-Za-z0-9]\s*$") {
            ErrorMessage = __ResStr("errInvSite", "The site's domain name is invalid - It cannot use http:// or https:// and it can only contain letters, numbers and these characters: - .");
        }
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            return new[] { new ModelClientValidationRegexRule(ErrorMessage, this.Pattern) };
        }
    }

    //sample: <meta name="google-site-verification" content="flC7VM4WGUt7vWo8iiP2-EQ60L4jC44BVOTjpPmH0hg" />
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GoogleVerificationExpressionAttribute : System.ComponentModel.DataAnnotations.RegularExpressionAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        // An empty string is valid (otherwise add RequiredAttribute)
        public override bool IsValid(Object value) {
            if (string.IsNullOrWhiteSpace((string) value))
                return true;
            return base.IsValid(value);
        }
        public GoogleVerificationExpressionAttribute()
            : base(@"^(\s*<meta\s+name=""google\-site\-verification""\s+content=\""[^\""]+?\""\s*/>\s*)+$") {
            ErrorMessage = __ResStr("errInvMeta", "The meta tag is invalid - It should be in the format <meta name=\"google-site-verification\" content=\"....your-code....\" />");
        }
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            return new[] { new ModelClientValidationRegexRule(ErrorMessage, this.Pattern) };
        }
    }
}
