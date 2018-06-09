/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Models;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif

//$$$remove
namespace YetaWF.Core.Views {
    public static class HtmlHelperExtender {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static TYPE GetModelSupportProperty<TYPE>(this IHtmlHelper htmlHelper, string name, string property) {
#else
        public static TYPE GetModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property) {
#endif
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.GetModelProperty<TYPE>(property);
        }
#if MVC6
        public static bool TryGetModelSupportProperty<TYPE>(this IHtmlHelper htmlHelper, string name, string property, out TYPE value) {
#else
        public static bool TryGetModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property, out TYPE value) {
#endif
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.TryGetModelProperty<TYPE>(property, out value);
        }
        // Get a property from the model
#if MVC6
        public static TYPE GetModelProperty<TYPE>(this IHtmlHelper htmlHelper, string property) {
#else
        public static TYPE GetModelProperty<TYPE>(this HtmlHelper htmlHelper, string property) {
#endif
            TYPE obj;
            if (!htmlHelper.TryGetModelProperty<TYPE>(property, out obj))
                throw new InternalError("Required property {0} is missing.", property);
            return (TYPE) obj;
        }
#if MVC6
        public static bool TryGetModelProperty<TYPE>(this IHtmlHelper htmlHelper, string property, out TYPE value) {
#else
        public static bool TryGetModelProperty<TYPE>(this HtmlHelper htmlHelper, string property, out TYPE value) {
#endif
            object model = Manager.GetCurrentModel();
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(model, property, out value))
                return false;
            return true;
        }
#if MVC6
        public static TYPE GetParentModelSupportProperty<TYPE>(this IHtmlHelper htmlHelper, string name, string property) {
#else
        public static TYPE GetParentModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property) {
#endif
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.GetParentModelProperty<TYPE>(property);
        }
#if MVC6
        public static bool TryGetParentModelSupportProperty<TYPE>(this IHtmlHelper htmlHelper, string name, string property, out TYPE value) {
#else
        public static bool TryGetParentModelSupportProperty<TYPE>(this HtmlHelper htmlHelper, string name, string property, out TYPE value) {
#endif
            string fieldName = htmlHelper.FieldName(name);
            property = string.Format("{0}_{1}", fieldName, property);
            return htmlHelper.TryGetParentModelProperty<TYPE>(property, out value);
        }
        // Get a property from the parent model
#if MVC6
        public static TYPE GetParentModelProperty<TYPE>(this IHtmlHelper htmlHelper, string property) {
#else
        public static TYPE GetParentModelProperty<TYPE>(this HtmlHelper htmlHelper, string property) {
#endif
            TYPE obj;
            if (!TryGetParentModelProperty<TYPE>(htmlHelper, property, out obj))
                throw new InternalError("Required property {0} is missing.", property);
            return (TYPE) obj;
        }
#if MVC6
        private static bool TryGetParentModelProperty<TYPE>(this IHtmlHelper htmlHelper, string property, out TYPE value) {
#else
        private static bool TryGetParentModelProperty<TYPE>(this HtmlHelper htmlHelper, string property, out TYPE value) {
#endif
            value = default(TYPE);
            // get the last segment (the variable name)
            property = property.Split(new char[] { '.' }).Last();
            object parentModel = Manager.GetParentModel();
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(parentModel, property, out value))
                return false;
            return true;
        }

        // get a property from the model's metadata
#if MVC6
        public static bool TryGetControlInfo<TYPE>(this IHtmlHelper htmlHelper, string field, string name, out TYPE value) {
            IReadOnlyDictionary<object, object> additionalValues;
#else
        public static bool TryGetControlInfo<TYPE>(this HtmlHelper htmlHelper, string field, string name, out TYPE value) {
            Dictionary<string, object> additionalValues;
#endif
            if (Manager.ControlInfoOverrides != null) {
                additionalValues = Manager.ControlInfoOverrides;
            } else {
#if MVC6
                ModelExplorer modelExplorer = ExpressionMetadataProvider.FromStringExpression(field, htmlHelper.ViewData, htmlHelper.MetadataProvider);
                ModelMetadata metadata = modelExplorer.Metadata;
#else
                ModelMetadata metadata = ModelMetadata.FromStringExpression(field, htmlHelper.ViewContext.ViewData);
#endif
                additionalValues = metadata.AdditionalValues;
            }
            value = default(TYPE);
            object obj;
            if (!additionalValues.TryGetValue(name, out obj))
                return false;
            value = (TYPE) obj;
            return true;
        }
#if MVC6
        public static TYPE GetControlInfo<TYPE>(this IHtmlHelper htmlHelper, string field, string name, TYPE defaultValue = default(TYPE)) {
#else
        public static TYPE GetControlInfo<TYPE>(this HtmlHelper htmlHelper, string field, string name, TYPE defaultValue = default(TYPE)) {
#endif
            TYPE val = defaultValue;
            if (!htmlHelper.TryGetControlInfo<TYPE>(field, name, out val))
                return defaultValue;
            return val;
        }

        public class ControlInfoOverride : IDisposable {
#if MVC6
            protected IReadOnlyDictionary<object, object> SavedValues { get; private set; }
#else
            protected Dictionary<string, object> SavedValues { get; private set; }
#endif
#if MVC6
            public ControlInfoOverride(IReadOnlyDictionary<object, object> additionalValues) {
#else
            public ControlInfoOverride(Dictionary<string, object> additionalValues) {
#endif
                SavedValues = Manager.ControlInfoOverrides;
                Manager.ControlInfoOverrides = additionalValues;
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                if (disposing) DisposableTracker.RemoveObject(this);
                Manager.ControlInfoOverrides = SavedValues;
            }
            //~ControlInfoOverride() { Dispose(false); }
        }
    }
}
