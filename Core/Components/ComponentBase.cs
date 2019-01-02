/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Support;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;
using System.Collections.Generic;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
#else
using System.Web.Mvc;
using System.Web.Routing;
#endif

namespace YetaWF.Core.Components {

    public interface IYetaWFComponent<TYPE> {
        Task IncludeAsync();
        Task<YHtmlString> RenderAsync(TYPE model);
    }
    public interface IYetaWFContainer<TYPE> {
        Task IncludeAsync();
        Task<YHtmlString> RenderContainerAsync(TYPE model);
    }

    public abstract class YetaWFComponentBase {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public enum ComponentType {
            Display = 0,
            Edit = 1,
        }

#if MVC6
        public void SetRenderInfo(IHtmlHelper htmlHelper,
#else
        public void SetRenderInfo(HtmlHelper htmlHelper,
#endif
             object container, string propertyName, PropertyData propData, object htmlAttributes, bool validation)
        {
            HtmlHelper = htmlHelper;
            Container = container;
            PropertyName = propertyName;
            PropData = propData;
            FieldNamePrefix = Manager.NestedComponentPrefix;
            FieldName = propertyName;
            if (!string.IsNullOrWhiteSpace(FieldNamePrefix) && propertyName != null)
                FieldName = FieldNamePrefix + "." + FieldName;
            HtmlAttributes = htmlAttributes != null ? AnonymousObjectToHtmlAttributes(htmlAttributes) : new Dictionary<string, object>();
            Validation = validation;
        }
        protected void UseSuppliedIdAsControlId() {
            if (HtmlAttributes.ContainsKey("id")) {
                ControlId = (string)HtmlAttributes["id"];
                HtmlAttributes.Remove("id");
            }
        }

        public static IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes) {
            if (htmlAttributes as RouteValueDictionary != null) return (RouteValueDictionary)htmlAttributes;
            if (htmlAttributes as Dictionary<string, object> != null) return (Dictionary<string, object>)htmlAttributes;
#if MVC6
            return Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
#else
            return HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
#endif
        }

#if MVC6
        public IHtmlHelper HtmlHelper
#else
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public HtmlHelper HtmlHelper
#endif
        {
            get {
                if (_htmlHelper == null) throw new InternalError("No htmlHelper available");
                return _htmlHelper;
            }
            set {
                _htmlHelper = value;
            }
        }
#if MVC6
        private IHtmlHelper _htmlHelper;
#else
        private HtmlHelper _htmlHelper;
#endif
        public object Container { get; private set; }

        public bool IsContainerComponent {
            get { return _propertyName == null; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string PropertyName {
            get {
                if (IsContainerComponent) throw new InternalError($"{this.GetType().FullName} was invoked as a container");
                return _propertyName;
            }
            private set {
                _propertyName = value;
            }
        }
        string _propertyName;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public PropertyData PropData {
            get {
                if (IsContainerComponent) throw new InternalError($"{this.GetType().FullName} was invoked as a container");
                return _propData;
            }
            private set {
                _propData = value;
            }
        }
        PropertyData _propData;

        public string FieldNamePrefix { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string FieldName {
            get {
                if (IsContainerComponent) throw new InternalError($"{this.GetType().FullName} was invoked as a container");
                return _fieldName;
            }
            private set {
                _fieldName = value;
            }
        }
        string _fieldName;

        public IDictionary<string, object> HtmlAttributes { get; private set; }
        public bool Validation { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public YetaWFComponentBase() {
            Package = GetPackage();
        }
        public readonly Package Package;

        public string ControlId {
            get {
                if (string.IsNullOrEmpty(_controlId))
                    _controlId = Manager.UniqueId("ctrl");
                return _controlId;
            }
            private set {
                _controlId = value;
            }
        }
        private string _controlId;

        public string DivId {
            get {
                if (string.IsNullOrEmpty(_divId))
                    _divId = Manager.UniqueId("div");
                return _divId;
            }
        }
        private string _divId;

        public string UniqueId(string name = "b") {
            return Manager.UniqueId(name);
        }

        public static string HAE(string text) {
            return YetaWFManager.HtmlAttributeEncode(text);
        }
        public static string HE(string text) {
            return YetaWFManager.HtmlEncode(text);
        }
        public static string JE(string text) {
            return YetaWFManager.JserEncode(text);
        }
        public static string JE(bool val) {
            return val ? "true" : "false";
        }

        public abstract Package GetPackage();
        public abstract string GetTemplateName();
        public abstract ComponentType GetComponentType();

        /// <summary>
        /// Include required JavaScript, Css files when displaying a component, for all components in this package.
        /// </summary>
        public virtual Task IncludeStandardDisplayAsync() { return Task.CompletedTask; }
        /// <summary>
        /// Include required JavaScript, Css files when editing a component, for all components in this package.
        /// </summary>
        public virtual Task IncludeStandardEditAsync() { return Task.CompletedTask; }

        /// <summary>
        /// Retrieves a sibling property. Used to extract related properties from container, which typically are used for additional component customization.
        /// </summary>
        public bool TryGetSiblingProperty<TYPE>(string property, out TYPE value) {
            value = default(TYPE);
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(Container, property, out value))
                return false;
            return true;
        }
        /// <summary>
        /// Retrieves a sibling property. Used to extract related properties from container, which typically are used for additional component customization.
        /// </summary>
        public TYPE GetSiblingProperty<TYPE>(string property, TYPE dflt = default(TYPE)) {
            TYPE value = dflt;
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(Container, property, out value))
                throw new InternalError($"No sibling property {property} found");
            return value;
        }
    }
}
