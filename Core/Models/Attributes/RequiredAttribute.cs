/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredAttribute : ValidationAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RequiredAttribute() { }

        protected void SetMessage(ValidationContext valContext) {
            string caption = AttributeHelper.GetPropertyCaption(valContext);
            ErrorMessage = string.Format(__ResStr("required", "The '{0}' field is required"), caption);
        }
        protected void SetMessage(ModelMetadata metadata) {
            string caption = AttributeHelper.GetPropertyCaption(metadata);
            ErrorMessage = string.Format(__ResStr("required", "The '{0}' field is required"), caption);
        }
        public override bool IsValid(object value) {
            if (value is MultiString) {
                MultiString ms = (MultiString) value;
                if (ms.ToString().Length == 0)
                    return false;
                return true;
            } else if (value is Guid) {
                if ((Guid) value == Guid.Empty)
                    return false;
                return true;
            } else if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                return true;
            return false;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            SetMessage(context);
            if (IsValid(context.ObjectInstance)) {
                ValidationResult result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            SetMessage(context.ModelMetadata);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-customrequired-" + Forms.ConditionPropertyName, ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            SetMessage(metadata);
            var rule = new ModelClientValidationRule {
                ErrorMessage = ErrorMessage,
                ValidationType = "customrequired"
            };
            yield return rule;
        }
#endif
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SelectionRequiredAttribute : ValidationAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public SelectionRequiredAttribute() { }

        protected void SetMessage(ValidationContext context) {
            string caption = AttributeHelper.GetPropertyCaption(context);
            ErrorMessage = string.Format(__ResStr("selectionRequired", "The '{0}' field is required - Please select one of the available options"), caption);
        }
        protected void SetMessage(ModelMetadata metadata) {
            string caption = AttributeHelper.GetPropertyCaption(metadata);
            ErrorMessage = string.Format(__ResStr("selectionRequired", "The '{0}' field is required - Please select one of the available options"), caption);
        }
        public override bool IsValid(object value) {
            if (value is MultiString) {
                MultiString ms = (MultiString) value;
                if (ms.ToString().Length == 0)
                    return false;
                return true;
            } else if (value is Guid) {
                if ((Guid) value == Guid.Empty)
                    return false;
                return true;
            } else if (value != null && !string.IsNullOrWhiteSpace(value.ToString()) && value.ToString().Trim() != "0")
                return true;
            return false;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            SetMessage(context);
            if (IsValid(context.ObjectInstance)) {
                ValidationResult result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            SetMessage(context.ModelMetadata);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-selectionrequired-" + Forms.ConditionPropertyName, ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            SetMessage(metadata);
            var rule = new ModelClientValidationRule {
                ErrorMessage = ErrorMessage,
                ValidationType = "selectionrequired"
            };
            yield return rule;
        }
#endif
    }
}
