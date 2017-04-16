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

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class RequiredIfSupplied : RequiredAttribute, YIClientValidatable {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public String RequiredPropertyName { get; private set; }

        public RequiredIfSupplied(String propertyName) {
            RequiredPropertyName = propertyName;
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
#if MVC6
        public new void AddValidation(ClientModelValidationContext context) {
            ErrorMessage = string.Format(__ResStr("requiredIfSupplied", "The {0} field is required"), AttributeHelper.GetPropertyCaption(context.ModelMetadata));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifsupplied", ErrorMessage);
            AttributeHelper.MergeAttribute(context.Attributes, "data-val-requiredifsupplied-" + Forms.ConditionPropertyName, AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, context.ModelMetadata, (ViewContext)context.ActionContext));
            AttributeHelper.MergeAttribute(context.Attributes, "data-val", "true");
        }
#else
        public new IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            var rule = new ModelClientValidationRule {
                ErrorMessage = string.Format(__ResStr("requiredIfSupplied", "The {0} field is required"), AttributeHelper.GetPropertyCaption(metadata)),
                ValidationType = "requiredifsupplied"
            };
            rule.ValidationParameters[Forms.ConditionPropertyName] = AttributeHelper.BuildDependentPropertyName(this.RequiredPropertyName, metadata, context as ViewContext);
            yield return rule;
        }
#endif

    }
}
