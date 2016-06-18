/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfAttribute : RequiredAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }
        private Object RequiredValue { get; set; }

        public RequiredIfAttribute(String propertyName, Object value) {
            RequiredPropertyName = propertyName;
            RequiredValue = value;
        }
        public new IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = string.Format(__ResStr("requiredIf", "The {0} field is required"), AttributeHelper.GetPropertyCaption(metadata)),
                ValidationType = "requiredif"
            };
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            rule.ValidationParameters[Forms.ConditionPropertyValue] = RequiredValue;
            yield return rule;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
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
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfNotAttribute : RequiredAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }
        private Object RequiredValue { get; set; }

        public RequiredIfNotAttribute(String propertyName, Object value) {
            RequiredPropertyName = propertyName;
            RequiredValue = value;
        }
        public new IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = string.Format(__ResStr("requiredIfNot", "The {0} field is required"), AttributeHelper.GetPropertyCaption(metadata)),
                ValidationType = "requiredifnot"
            };
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            rule.ValidationParameters[Forms.ConditionPropertyValue] = RequiredValue;
            yield return rule;
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
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfInRangeAttribute : RequiredAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }
        private int RequiredValueLow { get; set; }
        private int RequiredValueHigh { get; set; }

        public RequiredIfInRangeAttribute(String propertyName, int low, int high) {
            RequiredPropertyName = propertyName;
            RequiredValueLow = low;
            RequiredValueHigh = high;
        }
        public new IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = string.Format(__ResStr("requiredIfInRange", "The {0} field is required"), AttributeHelper.GetPropertyCaption(metadata)),
                ValidationType = "requiredifinrange"
            };
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            rule.ValidationParameters[Forms.ConditionPropertyValueLow] = RequiredValueLow;
            rule.ValidationParameters[Forms.ConditionPropertyValueHigh] = RequiredValueHigh;
            yield return rule;
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
    }
}
