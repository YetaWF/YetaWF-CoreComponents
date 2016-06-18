/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    public class RequiredIfSupplied : RequiredAttribute, IClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }

        public RequiredIfSupplied(String propertyName) {
            RequiredPropertyName = propertyName;
        }

        public new IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = string.Format(__ResStr("requiredIfSupplied", "The {0} field is required"), AttributeHelper.GetPropertyCaption(metadata)),
                ValidationType = "requiredifsupplied"
            };
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            yield return rule;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (IsSupplied(context.ObjectInstance)) {
                ValidationResult result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
        public bool IsSupplied(object model) {
            object propValue = GetDependentPropertyValue(model);
            return propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString());
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
    }
}
