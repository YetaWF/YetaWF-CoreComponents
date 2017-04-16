/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Collections.Generic;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfAttribute : RequiredAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }
        private Object RequiredValue { get; set; }

        public RequiredIfAttribute(String propertyName, Object value) {
            RequiredPropertyName = propertyName;
            RequiredValue = value;
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
#if MVC6
        public new void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = string.Format(__ResStr("requiredIf", "The {0} field is required"), AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredif", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredif-" + Forms.ConditionPropertyName, AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, context.ModelMetadata, (ViewContext)context.ActionContext));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredif-" + Forms.ConditionPropertyValue, RequiredValue.ToString());
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public new IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = string.Format(__ResStr("requiredIf", "The {0} field is required"), AttributeHelper.GetPropertyCaption(metadata)),
                ValidationType = "requiredif"
            };
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            rule.ValidationParameters[Forms.ConditionPropertyValue] = RequiredValue;
            yield return rule;
        }
#endif

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfNotAttribute : RequiredAttribute, YIClientValidatable {

        [CombinedResources]
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
#if MVC6
        public new void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = string.Format(__ResStr("requiredIfNot", "The {0} field is required"), AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifnot", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifnot-" + Forms.ConditionPropertyName, AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, context.ModelMetadata, (ViewContext)context.ActionContext));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifnot-" + Forms.ConditionPropertyValue, RequiredValue.ToString());
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public new IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = string.Format(__ResStr("requiredIfNot", "The {0} field is required"), AttributeHelper.GetPropertyCaption(metadata)),
                ValidationType = "requiredifnot"
            };
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            rule.ValidationParameters[Forms.ConditionPropertyValue] = RequiredValue;
            yield return rule;
        }
#endif

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfInRangeAttribute : RequiredAttribute, YIClientValidatable {

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
#if MVC6
        public new void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = string.Format(__ResStr("requiredIfNot", "The {0} field is required"), AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifinrange", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifinrange-" + Forms.ConditionPropertyName, AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, context.ModelMetadata, (ViewContext)context.ActionContext));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifinrange-" + Forms.ConditionPropertyValueLow, RequiredValueLow.ToString());
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifinrange-" + Forms.ConditionPropertyValueHigh, RequiredValueHigh.ToString());
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
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
#endif
    }
}
