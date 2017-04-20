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

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SameAsAttribute : ValidationAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        private string RequiredPropertyName { get; set; }
        private new string ErrorMessage { get; set; }

        public SameAsAttribute(string propertyName, string message) : base(message) {
            RequiredPropertyName = propertyName;
            ErrorMessage = message;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (IsSame(context.ObjectInstance, value))
                return ValidationResult.Success;
            return new ValidationResult(ErrorMessage);
        }
        public bool IsSame(object model, object value) {
            object propValue = GetDependentPropertyValue(model);
            if (propValue == null)
                return (value == null);
            return propValue.Equals(value);
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
#if MVC6
        public void AddValidation(ClientModelValidationContext context) {
            if (string.IsNullOrWhiteSpace(ErrorMessage))
                ErrorMessage = __ResStr("SameAs", "The {0} field doesn't match", AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-sameas", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-sameas-" + Forms.ConditionPropertyName, AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, context.ModelMetadata, (ViewContext)context.ActionContext));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ValidationType = "sameas"
            };
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                rule.ErrorMessage = ErrorMessage;
            else
                rule.ErrorMessage = __ResStr("SameAs", "The {0} field doesn't match", AttributeHelper.GetPropertyCaption(metadata));
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            yield return rule;
        }
#endif

    }
}
