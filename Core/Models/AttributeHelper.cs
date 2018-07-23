/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
#else
#endif

namespace YetaWF.Core.Models {

    public interface YIClientValidation {
        void AddValidation(object container, PropertyData propData, YTagBuilder tag);
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
