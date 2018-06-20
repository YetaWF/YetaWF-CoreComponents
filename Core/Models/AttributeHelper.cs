/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

#if MVC6
    public interface YIClientValidatable : IClientModelValidator { }
#else
    public interface YIClientValidatable : IClientValidatable { }
#endif

    public static class AttributeHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static string GetPropertyCaption(ValidationContext validationContext) {
            object instance = validationContext.ObjectInstance;
            Type type = validationContext.ObjectType;
            string propertyName = validationContext.DisplayName;
            PropertyData propData = ObjectSupport.GetPropertyData(type, propertyName);
            return propData.GetCaption(instance);
        }
        /// <summary>
        /// Retrieve the caption.
        /// </summary>
        /// <param name="metadata">Model metadata.</param>
        /// <returns></returns>
        /// <remarks>This is only used while during AddValidation() calls so redirection is not required (or supported).</remarks>
        public static string GetPropertyCaption(ModelMetadata metadata) {
            Type type = metadata.ContainerType;
            string propertyName = metadata.PropertyName;
            PropertyData propData = ObjectSupport.GetPropertyData(type, propertyName);
            return propData.GetCaption(type);
        }
        public static string BuildDependentPropertyName(string PropertyName) {
            if (!string.IsNullOrWhiteSpace(Manager.NestedComponentPrefix))
                return Manager.NestedComponentPrefix + "." + PropertyName;
            else
                return PropertyName;
        }

        public static TYPE GetAttributeValue<TYPE>(ValidationContext validationContext, string attrName, TYPE dflt) {
            Type type = validationContext.ObjectType;
            string propertyName = validationContext.DisplayName;
            PropertyData propData = ObjectSupport.GetPropertyData(type, propertyName);
            return propData.GetAdditionalAttributeValue<TYPE>(attrName, dflt);
        }
#if MVC6
        public static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value) {
            if (attributes.ContainsKey(key)) return false;
            attributes.Add(key, value);
            return true;
        }
#else
#endif
    }
}
