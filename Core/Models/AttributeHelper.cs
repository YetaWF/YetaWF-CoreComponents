/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

    public interface YIClientValidation {
        ValidationBase AddValidation(object container, PropertyData propData, string caption, YTagBuilder tag);
    }
    public class ValidationBase {
        public string Method { get; set; }
        [JsonIgnore]
        public string Message { get; set; }
        public string M { get { return Message; } }// use a short name for serialization
    }

    public static class AttributeHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static string GetPropertyCaption(ValidationContext validationContext) {
            object instance = validationContext.ObjectInstance;
            Type type = validationContext.ObjectType;
            string propertyName = validationContext.DisplayName;
            PropertyData propData = ObjectSupport.GetPropertyData(type, propertyName);
            return propData.GetCaption(instance);
        }
        public static string GetDependentPropertyName(string PropertyName) {
            if (!string.IsNullOrWhiteSpace(Manager.NestedComponentPrefix))
                return Manager.NestedComponentPrefix + "." + PropertyName;
            else
                return PropertyName;
        }
    }
}
