/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    public abstract class ProcessIfBase : Attribute {

        protected static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        /// <summary>
        /// The name of the other property used to determine conditional processing/validation of this property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Defines whether the property is hidden or just disabled when it's not processed/validated.
        /// The default is to hide the property.
        /// </summary>
        public bool Disable { get; set; }

        public abstract bool Processing(object model);

        protected object GetControllingPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, Name);
            return pi.GetValue(model, null);
        }
    }

    /// <summary>
    /// Conditional processing/validation of properties within a property list.
    /// </summary>
    /// <remarks>Used to show/hide properties in a property list, dependent on a property's enum value (typically a dropdown list) or bool value.
    ///
    /// This is used both client-side and server-side to determine conditional property processing/validation.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ProcessIfAttribute : ProcessIfBase {

        /// <summary>
        /// Values the other property can take in order for this property to be processed/validated.
        /// </summary>
        public object[] Objects { get; private set; }

        /// <summary>
        /// Processing/validation of the property is dependent on the value of another property.
        /// </summary>
        /// <param name="name">The name of the other property this property depends on.</param>
        /// <param name="parms">Values the other property can take in order for this property to be processed/validated.
        /// If the other property doesn't have any of these values, this property is not processed/validated.</param>
        /// <remarks>This is typically used in with an enum as the other property, with the specified Name.
        /// Enums are rendered as dropdown lists. When the other property's value changes (i.e., the dropdown list is changed)
        /// all properties decorated with a matching ProcessIf attribute will be shown/hidden (client-side), depending on whether
        /// the enum property (the "other" property) has one of the values that are defined in the ProcessIf parms argument.
        ///
        /// Any hidden properties are not validated client-side and server-side.
        ///
        /// The ProcessIf attribute supports simple and complex properties.
        ///
        /// ** This is currently only supported for enums and bool as the "other" property (used with the Enum template, UIHint("Enum") or bool, UIHint("Boolean")). **
        /// </remarks>
        public ProcessIfAttribute(string name, params object[] parms) {
            Name = name;
            Objects = parms ?? new object[] { null };
        }
        /// <summary>
        /// Returns whether processing/validation is required for the property decorated with this attribute.
        /// </summary>
        /// <param name="model">The model containing the property decorated with this attribute.</param>
        /// <returns>true if processing/validation is required, false otherwise.</returns>
        public override bool Processing(object model) {
            //TODO: This could be expanded to support other types
            object val = GetControllingPropertyValue(model);
            if (val.GetType() == typeof(string)) {
                string currVal = (string)val;
                foreach (object obj in Objects) {
                    if (currVal == (string)obj)
                        return true; // we're processing this
                }
            } else {
                int currVal = Convert.ToInt32(val);
                foreach (object obj in Objects) {
                    if (currVal == Convert.ToInt32(obj)) // if this fails you're using something other than an enum (int) or bool as "other" property
                        return true; // we're processing this
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Conditional processing/validation of properties within a property list.
    /// </summary>
    /// <remarks>Used to show/hide properties in a property list, dependent on a property's non-null value.
    ///
    /// This is used both client-side and server-side to determine conditional property processing/validation.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ProcessIfSuppliedAttribute : ProcessIfBase {

        /// <summary>
        /// Processing/validation of the property is dependent on the value/presence another property.
        /// </summary>
        /// <param name="name">The name of the other property this property depends on.</param>
        /// </remarks>
        public ProcessIfSuppliedAttribute(string name, params object[] parms) {
            Name = name;
        }
        /// <summary>
        /// Returns whether processing/validation is required for the property decorated with this attribute.
        /// </summary>
        /// <param name="model">The model containing the property decorated with this attribute.</param>
        /// <returns>true if processing/validation is required, false otherwise.</returns>
        public override bool Processing(object model) {
            string currVal = (string)GetControllingPropertyValue(model);
            if (!string.IsNullOrWhiteSpace(currVal)) {
                return true; // we're processing this
            }
            return false;
        }
    }

    /// <summary>
    /// Conditional processing/validation of properties within a property list.
    /// </summary>
    /// <remarks>Used to show/hide properties in a property list, dependent on a property's null value.
    ///
    /// This is used both client-side and server-side to determine conditional property processing/validation.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ProcessIfNotSuppliedAttribute : ProcessIfBase {

        /// <summary>
        /// Processing/validation of the property is dependent on the value/presence another property.
        /// </summary>
        /// <param name="name">The name of the other property this property depends on.</param>
        /// </remarks>
        public ProcessIfNotSuppliedAttribute(string name, params object[] parms) {
            Name = name;
        }
        /// <summary>
        /// Returns whether processing/validation is required for the property decorated with this attribute.
        /// </summary>
        /// <param name="model">The model containing the property decorated with this attribute.</param>
        /// <returns>true if processing/validation is required, false otherwise.</returns>
        public override bool Processing(object model) {
            string currVal = (string)GetControllingPropertyValue(model);
            if (string.IsNullOrWhiteSpace(currVal)) {
                return true; // we're processing this
            }
            return false;
        }
    }
}
