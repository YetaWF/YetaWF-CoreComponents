/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfAttribute : RequiredAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }
        private Object RequiredValue { get; set; }

        public RequiredIfAttribute(String propertyName, Object value) {
            RequiredPropertyName = propertyName;
            if (value.GetType().IsEnum)
                RequiredValue = (int)value;// save enums as int value
            else
                RequiredValue = value;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (Is(context.ObjectInstance)) {
                ValidationResult result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
        public bool Is(object model) {
            object propValue = GetDependentPropertyValue(model);
            return propValue.Equals(RequiredValue);
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
        public new void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("requiredIf", "The {0} field is required", propData.GetCaption(container));
            tag.MergeAttribute("data-val-requiredif", msg);
            tag.MergeAttribute("data-val-requiredif-" + Forms.ConditionPropertyName, AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName));
            tag.MergeAttribute("data-val-requiredif-" + Forms.ConditionPropertyValue, RequiredValue.ToString());
            tag.MergeAttribute("data-val", "true");
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfNotAttribute : RequiredAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }
        private Object RequiredValue { get; set; }

        public RequiredIfNotAttribute(String propertyName, Object value) {
            RequiredPropertyName = propertyName;
            RequiredValue = value;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (IsNot(context.ObjectInstance)) {
                ValidationResult result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
        public bool IsNot(object model) {
            object propValue = GetDependentPropertyValue(model);
            if (propValue == null)
                return RequiredValue != propValue;
            return !propValue.Equals(RequiredValue);
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
        public new void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("requiredIfNot", "The {0} field is required", propData.GetCaption(container));
            tag.MergeAttribute("data-val-requiredifnot", msg);
            tag.MergeAttribute("data-val-requiredifnot-" + Forms.ConditionPropertyName, AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName));
            tag.MergeAttribute("data-val-requiredifnot-" + Forms.ConditionPropertyValue, RequiredValue?.ToString());
            tag.MergeAttribute("data-val", "true");
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfInRangeAttribute : RequiredAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }
        private int RequiredValueLow { get; set; }
        private int RequiredValueHigh { get; set; }

        public RequiredIfInRangeAttribute(String propertyName, int low, int high) {
            RequiredPropertyName = propertyName;
            RequiredValueLow = low;
            RequiredValueHigh = high;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (InRange(context.ObjectInstance)) {
                ValidationResult result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
        public bool InRange(object model) {
            int val = (int) GetDependentPropertyValue(model);
            return val >= RequiredValueLow && val <= RequiredValueHigh;
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
        public new void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("requiredIfNot", "The {0} field is required", propData.GetCaption(container));
            tag.MergeAttribute("data-val-requiredifinrange", msg);
            tag.MergeAttribute("data-val-requiredifinrange-" + Forms.ConditionPropertyName, AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName));
            tag.MergeAttribute("data-val-requiredifinrange-" + Forms.ConditionPropertyValueLow, RequiredValueLow.ToString());
            tag.MergeAttribute("data-val-requiredifinrange-" + Forms.ConditionPropertyValueHigh, RequiredValueHigh.ToString());
            tag.MergeAttribute("data-val", "true");
        }
    }
}
