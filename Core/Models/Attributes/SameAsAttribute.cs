/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SameAsAttribute : ValidationAttribute, YIClientValidation {

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
        public void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("SameAs", "The {0} field doesn't match", propData.GetCaption(container));
            tag.MergeAttribute("data-val-sameas", msg);
            tag.MergeAttribute("data-val-sameas-" + Forms.ConditionPropertyName, AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName));
            tag.MergeAttribute("data-val", "true");
        }
    }
}
