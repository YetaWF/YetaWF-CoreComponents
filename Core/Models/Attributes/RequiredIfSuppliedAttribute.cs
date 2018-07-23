/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class RequiredIfSupplied : RequiredAttribute, YIClientValidation {

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
        public new void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("requiredIfSupplied", "The {0} field is required", propData.GetCaption(container));
            tag.MergeAttribute("data-val-requiredifsupplied", msg);
            tag.MergeAttribute("data-val-requiredifsupplied-" + Forms.ConditionPropertyName, AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName));
            tag.MergeAttribute("data-val", "true");
        }
    }
}
