/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    public abstract class RequiredIfBase : RequiredAttribute {

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
            return GetValueOfPropertyValue(model, ((string)obj).Substring(RequiredIfBase.ValueOf.Length));
        }
        protected static bool IsValueOfEntry(object name) {
            if (name == null) return false;
            if (name.GetType() != typeof(string)) return false;
            return ((string)name).StartsWith(RequiredIfBase.ValueOf);
        }
        private static object GetValueOfPropertyValue(object model, string name) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, name);
            return pi.GetValue(model, null);
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfInRangeAttribute : RequiredIfBase, YIClientValidation {

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
            int val = (int)GetControllingPropertyValue(model);
            return val >= RequiredValueLow && val <= RequiredValueHigh;
        }
        public new void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            string msg = __ResStr("requiredIfNot", "The '{0}' field is required", propData.GetCaption(container));
            tag.MergeAttribute("data-val-requiredifinrange", msg);
            tag.MergeAttribute("data-val-requiredifinrange-" + Forms.ConditionPropertyName, AttributeHelper.GetDependentPropertyName(this.RequiredPropertyName));
            tag.MergeAttribute("data-val-requiredifinrange-" + Forms.ConditionPropertyValueLow, RequiredValueLow.ToString());
            tag.MergeAttribute("data-val-requiredifinrange-" + Forms.ConditionPropertyValueHigh, RequiredValueHigh.ToString());
            tag.MergeAttribute("data-val", "true");
        }
    }
}
