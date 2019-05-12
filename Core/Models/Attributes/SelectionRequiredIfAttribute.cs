/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    public abstract class SelectionRequiredIfBase : RequiredAttribute {

        protected static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public const string ValueOf = "ValOf+";

        public String RequiredPropertyName { get; set; }

        protected object GetControllingPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
        public static object GetValueOfEntry(object model, object obj) {
            if (!IsValueOfEntry(obj)) return obj;
            return GetValueOfPropertyValue(model, ((string)obj).Substring(SelectionRequiredIfBase.ValueOf.Length));
        }
        protected static bool IsValueOfEntry(object name) {
            if (name == null) return false;
            if (name.GetType() != typeof(string)) return false;
            return ((string)name).StartsWith(SelectionRequiredIfBase.ValueOf);
        }
        private static object GetValueOfPropertyValue(object model, string name) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, name);
            return pi.GetValue(model, null);
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredIfAttribute : SelectionRequiredIfBase, YIClientValidation {

        private Object RequiredValue { get; set; }

        public SelectionRequiredIfAttribute(String propertyName, Object value) {
            RequiredPropertyName = propertyName;
            if (value.GetType().IsEnum)
                RequiredValue = (int)value;// save enums as int value
            else
                RequiredValue = value;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            SetMessage(context);
            if (IsEqual(context.ObjectInstance)) {
                ValidationResult result = base.IsValid(value, context);
                return result;
            }
            return ValidationResult.Success;
        }
        public bool IsEqual(object model) {
            object propValue = GetControllingPropertyValue(model);
            Type propType = propValue.GetType();
            object reqVal = GetValueOfEntry(model, RequiredValue);
            if (propType.IsEnum)
                return (int)propValue == (int)reqVal;
            else
                return propValue.Equals(reqVal);
        }
        protected new void SetMessage(ValidationContext context) {
            string caption = AttributeHelper.GetPropertyCaption(context);
            ErrorMessage = __ResStr("selectionRequired", "The '{0}' field is required - Please select one of the available options", caption);
        }
        public override bool IsValid(object value) {
            if (value is MultiString) {
                MultiString ms = (MultiString)value;
                if (ms.ToString().Length == 0)
                    return false;
                return true;
            } else if (value is Guid) {
                if ((Guid)value == Guid.Empty)
                    return false;
                return true;
            } else if (value != null && !string.IsNullOrWhiteSpace(value.ToString()) && value.ToString().Trim() != "0")
                return true;
            return false;
        }

        public new void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("SelectionRequiredIf", "The '{0}' field is required - Please select one of the available options", propData.GetCaption(container));
            object reqVal = GetValueOfEntry(container, RequiredValue);
            tag.MergeAttribute("data-val-selectionrequiredif", msg);
            tag.MergeAttribute("data-val-selectionrequiredif-" + Forms.ConditionPropertyName, AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName));
            tag.MergeAttribute("data-val-selectionrequiredif-" + Forms.ConditionPropertyValue, reqVal?.ToString());
            tag.MergeAttribute("data-val", "true");
        }
    }
}
