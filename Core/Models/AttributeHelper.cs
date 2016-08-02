/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {
    public static class AttributeHelper {

        public static string GetPropertyCaption(ValidationContext validationContext) {
            object instance = validationContext.ObjectInstance;
            Type type = validationContext.ObjectType;
            string propertyName = validationContext.DisplayName;
            PropertyData propData = ObjectSupport.GetPropertyData(type, propertyName);
            return propData.GetCaption(instance);
        }
        public static string GetPropertyCaption(ModelMetadata metadata) {
            object instance = metadata.Container;
            Type type = metadata.ContainerType;
            string propertyName = metadata.PropertyName;
            PropertyData propData = ObjectSupport.GetPropertyData(type, propertyName);
            return propData.GetCaption(instance);
        }
        public static string GetPropertyCaption(object obj) {
            if (obj is ModelMetadata) return GetPropertyCaption((ModelMetadata) obj);
            else if (obj is ValidationContext) return GetPropertyCaption((ValidationContext) obj);
            else throw new InternalError("Must provide ModelMetadata or ValidationContext to retrieve caption");
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
    }
}
