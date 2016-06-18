/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    public class SameAsAttribute : ValidationAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        private string RequiredPropertyName { get; set; }
        private new string ErrorMessage { get; set; }

        public SameAsAttribute(string propertyName, string message) : base(message) {
            RequiredPropertyName = propertyName;
            ErrorMessage = message;
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ValidationType = "sameas"
            };
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                rule.ErrorMessage = ErrorMessage;
            else
                rule.ErrorMessage = string.Format(__ResStr("SameAs", "The {0} field doesn't match."), AttributeHelper.GetPropertyCaption(metadata));
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            yield return rule;
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
    }
}
