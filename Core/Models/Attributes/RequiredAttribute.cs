/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredAttribute : ValidationAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public RequiredAttribute() { }

        protected void SetMessage(ValidationContext valContext) {
            string caption = AttributeHelper.GetPropertyCaption(valContext);
            ErrorMessage = __ResStr("required", "The '{0}' field is required", caption);
        }
        public override bool IsValid(object value) {
            if (value is MultiString) {
                MultiString ms = (MultiString) value;
                string s = ms.ToString();
                if (s == null || s.Length == 0)
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
        public void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("required", "The '{0}' field is required", propData.GetCaption(container));
            tag.MergeAttribute("data-val-required", msg);
            tag.MergeAttribute("data-val", "true");
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SelectionRequiredAttribute : ValidationAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public SelectionRequiredAttribute() { }

        protected void SetMessage(ValidationContext context) {
            string caption = AttributeHelper.GetPropertyCaption(context);
            ErrorMessage = __ResStr("selectionRequired", "The '{0}' field is required - Please select one of the available options", caption);
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
        public void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("selectionRequired", "The '{0}' field is required - Please select one of the available options", propData.GetCaption(container));
            tag.MergeAttribute("data-val-selectionrequired", msg);
            tag.MergeAttribute("data-val", "true");
        }
    }
}
