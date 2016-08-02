/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Models {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SuppressIfEqualAttribute : MoreMetadataAttribute {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public string RequiredPropertyName { get; private set; }

        public SuppressIfEqualAttribute(string property, object value)
            : base("SuppressIf", value) {
            RequiredPropertyName = property;
        }
        public bool IsEqual(object model) {
            object propValue = GetDependentPropertyValue(model);
            if (propValue == null)
                return (Value == null);
            return propValue.Equals(Value);
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
    }
}
