/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredAttribute : ValidationAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RequiredAttribute() { }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            SetMessage(metadata);
            var rule = new ModelClientValidationRule {
                ErrorMessage = ErrorMessage,
                ValidationType = "customrequired"
            };
            yield return rule;
        }
        protected void SetMessage(object obj) {
            string caption = AttributeHelper.GetPropertyCaption(obj);
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
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SelectionRequiredAttribute : ValidationAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public SelectionRequiredAttribute() { }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            SetMessage(metadata);
            var rule = new ModelClientValidationRule {
                ErrorMessage = ErrorMessage,
                ValidationType = "selectionrequired"
            };
            yield return rule;
        }
        protected void SetMessage(object obj) {
            string caption = AttributeHelper.GetPropertyCaption(obj);
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
    }

}
