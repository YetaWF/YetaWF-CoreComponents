/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using YetaWF.Core.Models;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views {
    public static class HtmlHelperExtender {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static TYPE GetModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property) {
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.GetModelProperty<TYPE>(property);
        }
        public static bool TryGetModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property, out TYPE value) {
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.TryGetModelProperty<TYPE>(property, out value);
        }
        // Get a property from the model
        public static TYPE GetModelProperty<TYPE>(this HtmlHelper htmlHelper, string property) {
            TYPE obj;
            if (!htmlHelper.TryGetModelProperty<TYPE>(property, out obj))
                throw new InternalError("Required property {0} is missing.", property);
            return (TYPE) obj;
        }
        public static bool TryGetModelProperty<TYPE>(this HtmlHelper htmlHelper, string property, out TYPE value) {
            object model = Manager.GetCurrentModel();
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(model, property, out value))
                return false;
            return true;
        }

        public static TYPE GetParentModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property) {
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.GetParentModelProperty<TYPE>(property);
        }
        public static bool TryGetParentModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property, out TYPE value) {
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.TryGetParentModelProperty<TYPE>(property, out value);
        }
        // Get a property from the parent model
        public static TYPE GetParentModelProperty<TYPE>(this HtmlHelper htmlHelper, string property) {
            TYPE obj;
            if (!TryGetParentModelProperty<TYPE>(htmlHelper, property, out obj))
                throw new InternalError("Required property {0} is missing.", property);
            return (TYPE) obj;
        }
        private static bool TryGetParentModelProperty<TYPE>(this HtmlHelper htmlHelper, string property, out TYPE value) {
            value = default(TYPE);
            // get the last segment (the variable name)
            property = property.Split(new char[] { '.' }).Last();
            object parentModel = Manager.GetParentModel();
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(parentModel, property, out value))
                return false;
            return true;
        }

        // get a property from the model's metadata

        public static bool TryGetControlInfo<TYPE>(this HtmlHelper htmlHelper, string field, string name, out TYPE value) {
            Dictionary<string, object> additionalValues;
            if (Manager.ControlInfoOverrides != null) {
                additionalValues = Manager.ControlInfoOverrides;
            } else {
                ModelMetadata metadata = ModelMetadata.FromStringExpression(field, htmlHelper.ViewContext.ViewData);
                additionalValues = metadata.AdditionalValues;
            }
            value = default(TYPE);
            object obj;
            if (!additionalValues.TryGetValue(name, out obj))
                return false;
            value = (TYPE) obj;
            return true;
        }
        public static TYPE GetControlInfo<TYPE>(this HtmlHelper htmlHelper, string field, string name, TYPE defaultValue = default(TYPE)) {
            TYPE val = defaultValue;
            if (!htmlHelper.TryGetControlInfo<TYPE>(field, name, out val))
                return defaultValue;
            return val;
        }

        public class ControlInfoOverride : IDisposable {
            protected Dictionary<string, object> SavedValues { get; private set; }
            public ControlInfoOverride(Dictionary<string, object> additionalValues) {
                SavedValues = Manager.ControlInfoOverrides;
                Manager.ControlInfoOverrides = additionalValues;
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) { Manager.ControlInfoOverrides = SavedValues; }
            //~ControlInfoOverride() { Dispose(false); }
        }
    }
}
