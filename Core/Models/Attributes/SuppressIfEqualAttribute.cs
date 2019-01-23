/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Models.Attributes {

    /// <summary>
    /// Conditional display of properties within a property list.
    /// </summary>
    /// <remarks>Used to suppress properties in a property list, dependent on another property's value.
    ///
    /// This is used server-side only to determine conditional property display. It does not affect validation as the properties are not present.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SuppressIfEqualAttribute : Attribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        /// <summary>
        /// The name of the other property used to determine whether this property is suppressed.
        /// </summary>
        public string RequiredPropertyName { get; private set; }
        /// <summary>
        /// Values the other property can take in order for this property not to be displayed.
        /// </summary>
        public object[] Objects { get; private set; }

        /// <summary>
        /// Suppression of the property is dependent on the value of another property.
        /// </summary>
        /// <param name="name">The name of the other property this property depends on.</param>
        /// <param name="parms">Values the other property can take in order for this property not to be displayed.
        /// If the other property doesn't have any of these values, this property is displayed.</param>
        /// <remarks>All properties decorated with a matching SuppressIfEqual attribute will not be shown (determined server-side), depending on whether
        /// the "other" property has one of the values that are defined in the SuppressIfEqual parms argument.
        ///
        /// Suppressed properties are not validated client-side or server-side.
        ///
        /// The SuppressIfEqual attribute supports simple and complex properties.
        /// </remarks>
        public SuppressIfEqualAttribute(string property, params object[] parms) {
            RequiredPropertyName = property;
            Objects = parms ?? new object[] { null };
        }
        /// <summary>
        /// Returns whether the other property matches one the values that are defined in the SuppressIfEqual parms argument for the property decorated with this attribute.
        /// </summary>
        /// <param name="model">The model containing the property decorated with this attribute.</param>
        /// <returns>true if the other property matches one of the values, in which case the property is suppressed, false otherwise.</returns>
        public bool IsEqual(object model) {
            object propValue = GetDependentPropertyValue(model);
            foreach (object obj in Objects) {
                if (propValue == null) {
                    if (obj == null)
                        return true;// it's equal
                } else {
                    if (propValue.Equals(obj))
                        return true; // it's equal
                }
            }
            return false;// not equal
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, RequiredPropertyName);
            return pi.GetValue(model, null);
        }
    }
}
