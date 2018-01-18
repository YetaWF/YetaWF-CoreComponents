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
        public static string BuildDependentPropertyName(string PropertyName, ModelMetadata metadata, ViewContext viewContext) {
            // build the id of the property
            string depProp = viewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(PropertyName);
            // unfortunately this will have the name of the current field appended to the beginning,
            // because the TemplateInfo's context has had this fieldname appended to it. Instead, we
            // want to get the context as though it was one level higher (i.e. outside the current property,
            // which is the containing object (our Person), and hence the same level as the dependent property.
            string thisField = "." + metadata.PropertyName + ".";
            if (depProp.Contains(thisField))
                depProp = depProp.Replace(thisField, ".");
            else {
                thisField = metadata.PropertyName + ".";
                if (depProp.StartsWith(thisField)) // strip it off again
                    depProp = depProp.Substring(thisField.Length);
            }
            return depProp;
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
