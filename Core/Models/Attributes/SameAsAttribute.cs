/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SameAsAttribute : ValidationAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        private string RequiredPropertyName { get; set; }
        private new string ErrorMessage { get; set; }

        public SameAsAttribute(string propertyName, string message) : base(message) {
            RequiredPropertyName = propertyName;
            ErrorMessage = message;
        }
        protected override ValidationResult? IsValid(object? value, ValidationContext context) {
            if (IsSame(context.ObjectInstance, value))
                return ValidationResult.Success;
            return new ValidationResult(ErrorMessage);
        }
        public bool IsSame(object model, object? value) {
            object? propValue = GetDependentPropertyValue(model);
            if (propValue == null)
                return (value == null);
            return propValue.Equals(value);
        }
        private object? GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
        public class ValidationSameAs : ValidationBase {
            public string CondProp { get; set; } = null!;
        }
        public ValidationBase? AddValidation(object container, PropertyData propData, string caption, YTagBuilder tag) {
            return new ValidationSameAs {
                Method = nameof(SameAsAttribute),
                Message = __ResStr("sameas", "The {0} field doesn't match", caption),
                CondProp = AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName),
            };
        }
    }
}
