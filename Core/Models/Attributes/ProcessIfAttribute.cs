/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    /// <summary>
    /// Conditional processing/validation of properties within a property list.
    /// </summary>
    /// <remarks>Used to show/hide properties in a property list, dependent on a property's enum value (typically a dropdown list) or bool value.
    ///
    /// This is used both client-side and server-side to determine conditional property processing/validation.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ProcessIfAttribute : Attribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        /// <summary>
        /// The name of the other property used to determine conditional processing/validation of this property.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Values the other property can take in order for this property to be processed/validated.
        /// </summary>
        public object[] Objects { get; private set; }
        /// <summary>
        /// Defines whether the property is hidden or just disabled when it's not processed/validated.
        /// The default is to hide the property.
        /// </summary>
        public bool Disable { get; set; }

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
        public bool Processing(object model) {
            //TODO: This could be expanded to support other types, notably strings - don't have a use case yet
            int currVal = Convert.ToInt32(GetDependentPropertyValue(model));
            foreach (object obj in Objects) {
                if (currVal == Convert.ToInt32(obj)) // if this fails you're using something other than an enum (int) or bool as "other" property
                    return true; // we're processing this
            }
            return false;
        }
        private object GetDependentPropertyValue(object model) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, Name);
            return pi.GetValue(model, null);
        }
    }
}