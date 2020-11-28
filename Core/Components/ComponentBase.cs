/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// This interface is implemented by components.
    /// </summary>
    /// <typeparam name="TYPE">The type of the model rendered by the component.</typeparam>
    public interface IYetaWFComponent<TYPE> {
        /// <summary>
        /// Called by the framework when the component is used so the component can add component specific addons.
        /// </summary>
        Task IncludeAsync();
        /// <summary>
        /// Called by the framework when the component needs to be rendered as HTML.
        /// </summary>
        /// <param name="model">The model being rendered by the component.</param>
        /// <returns>The component rendered as HTML.</returns>
        Task<string> RenderAsync(TYPE model);
    }
    /// <summary>
    /// This interface is implemented by components which act as containers for other components.
    /// </summary>
    /// <typeparam name="TYPE">The type of the model rendered by the component.</typeparam>
    public interface IYetaWFContainer<TYPE> {
        /// <summary>
        /// Called by the framework when the component is used so the component can add component specific addons.
        /// </summary>
        Task IncludeAsync();
        /// <summary>
        /// Called by the framework when the component needs to be rendered as HTML.
        /// </summary>
        /// <param name="model">The model being rendered by the component.</param>
        /// <returns>The component rendered as HTML.</returns>
        Task<string> RenderContainerAsync(TYPE model);
    }

    /// <summary>
    /// This interface is implemented by components that can return a collection of integer/enum values suitable for rendering in a DropDownList component.
    /// </summary>
    public interface ISelectionListInt {
        /// <summary>
        /// Returns a collection of integer/enum values suitable for rendering in a DropDownList component.
        /// </summary>
        /// <param name="showDefault">Set to true to add a "(select)" entry at the top of the list, false otherwise.</param>
        /// <returns>Returns a collection of integer/enum values suitable for rendering in a DropDownList component.</returns>
        Task<List<SelectionItem<int>>> GetSelectionListIntAsync(bool showDefault);
    }
    /// <summary>
        /// This interface is implemented by components that can return a collection of integer/enum values suitable for rendering in a DropDownList component.
        /// </summary>
    public interface ISelectionListIntNull {
        /// <summary>
        /// Returns a collection of integer/enum values suitable for rendering in a DropDownList component.
        /// </summary>
        /// <param name="showDefault">Set to true to add a "(select)" entry at the top of the list, false otherwise.</param>
        /// <returns>Returns a collection of integer/enum values suitable for rendering in a DropDownList component.</returns>
        Task<List<SelectionItem<int?>>> GetSelectionListIntNullAsync(bool showDefault);
    }
    /// <summary>
    /// This interface is implemented by components that can return a collection of string values suitable for rendering in a DropDownList component.
    /// </summary>
    public interface ISelectionListString {
        /// <summary>
        /// Returns a collection of string values suitable for rendering in a DropDownList component.
        /// </summary>
        /// <param name="showDefault">Set to true to add a "(select)" entry at the top of the list, false otherwise.</param>
        /// <returns>Returns a collection of string values suitable for rendering in a DropDownList component.</returns>
        Task<List<SelectionItem<string?>>> GetSelectionListStringAsync(bool showDefault);
    }

    /// <summary>
    /// Describes a complex filter.
    /// </summary>
    public class ComplexFilter {
        /// <summary>
        /// The popup URL used to edit the filter.
        /// </summary>
        public string Url { get; set; } = null!;
        /// <summary>
        /// The component name handling this filter.
        /// </summary>
        public string UIHint { get; set; } = null!;
    }
    /// <summary>
    /// The base class for all JSON data used by filters.
    /// </summary>
    public class ComplexFilterJSONBase {
        /// <summary>
        /// The component name handling this filter.
        /// </summary>
        public string UIHint { get; set; } = null!;
    }

    /// <summary>
    /// This interface is implemented by components that offer a complex grid filter.
    /// </summary>
    public interface IComplexFilter {
        /// <summary>
        /// Returns information about the available filter.
        /// </summary>
        /// <param name="uiHint">The component name handling this filter.</param>
        /// <returns>Returns a ComplexFilter instance with information about the available filter.</returns>
        Task<ComplexFilter> GetComplexFilterAsync(string uiHint);
        /// <summary>
        /// Returns DataProviderFilterInfo given the filter's JSON data.
        /// </summary>
        /// <param name="jsonData">The JSON data representing the current filter setting.</param>
        /// <param name="name">Defines the property name.</param>
        /// <returns>Returns DataProviderFilterInfo given the filter's JSON data.</returns>
        DataProviderFilterInfo GetDataProviderFilterInfo(string jsonData, string name);
    }

    /// <summary>
    /// The base class for all components used in YetaWF.
    /// </summary>
    public abstract class YetaWFComponentBase {

        /// <summary>
        /// The YetaWF.Core.Support.Manager instance of current HTTP request.
        /// </summary>
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// The component type. YetaWF supports edit components which are used to input data and are modifiable and display components which are used to display data and cannot be modified.
        /// </summary>
        public enum ComponentType {
            /// <summary>
            /// Display component.
            /// </summary>
            Display = 0,
            /// <summary>
            /// Edit component.
            /// </summary>
            Edit = 1,
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public YetaWFComponentBase() {
            Package = GetPackage();
        }

        /// <summary>
        /// Defines the package implementing this component.
        /// </summary>
        public readonly Package Package;

        internal void SetRenderInfo(YHtmlHelper htmlHelper,
             object container, string? propertyName, string? fieldName, PropertyData? propData, object? htmlAttributes, bool validation)
        {
            HtmlHelper = htmlHelper;
            Container = container;
            PropertyName = propertyName;
            if (propData != null)
                PropData = propData;
            FieldNamePrefix = Manager.NestedComponentPrefix;
            if (string.IsNullOrWhiteSpace(fieldName)) {
                FieldName = propertyName ?? "";
                if (!string.IsNullOrWhiteSpace(FieldNamePrefix) && propertyName != null)
                    FieldName = FieldNamePrefix + "." + propertyName;
            } else {
                FieldName = fieldName;
            }
            HtmlAttributes = YHtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            Validation = validation;
        }

        /// <summary>
        /// A component can opt-in to use the HTML id provided by HtmlAttributes if one is available.
        /// </summary>
        /// <remarks>TODO: This method is a poor idea and will be reviewed/changed.</remarks>
        public void UseSuppliedIdAsControlId() {
            if (HtmlAttributes.ContainsKey("id")) {
                ControlId = (string)HtmlAttributes["id"]!;
                HtmlAttributes.Remove("id");
            }
        }

        /// <summary>
        /// Returns the HtmlHelper instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public YHtmlHelper HtmlHelper
        {
            get {
                if (_htmlHelper == null) throw new InternalError("No htmlHelper available");
                return _htmlHelper;
            }
            set {
                _htmlHelper = value;
            }
        }
        private YHtmlHelper? _htmlHelper;

        /// <summary>
        /// The container model for which this component is used/rendered.
        /// </summary>
        public object Container { get; private set; } = null!;

        /// <summary>
        /// Returns whether the component is a container component, i.e., can contain other components.
        /// </summary>
        public bool IsContainerComponent {
            get { return _propertyName == null; }
        }

        /// <summary>
        /// Defines the name of the property in the container that this components represents.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string? PropertyName {
            get {
                if (IsContainerComponent) throw new InternalError($"{this.GetType().FullName} was invoked as a container");
                return _propertyName;
            }
            private set {
                _propertyName = value;
            }
        }
        string? _propertyName;

        /// <summary>
        /// Defines the YetaWF.Core.Models.PropertyData instance of the property in the container that this components represents.
        /// </summary>
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
        PropertyData _propData = null!;

        /// <summary>
        /// Defines the prefix used when generating an HTML field name for the component.
        /// A prefix is typically present with nested components.
        /// </summary>
        public string? FieldNamePrefix { get; private set; }

        /// <summary>
        /// The HTML field name of the components.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public string FieldName {
            get {
                if (IsContainerComponent) throw new InternalError($"{this.GetType().FullName} was invoked as a container");
                if (_fieldName == null) throw new InternalError($"{nameof(FieldName)} not defined");
                return _fieldName;
            }
            private set {
                _fieldName = value;
            }
        }
        string? _fieldName;

        /// <summary>
        /// HTML attributes that were provided to render the component.
        /// </summary>
        public IDictionary<string, object?> HtmlAttributes { get; private set; } = null!;

        /// <summary>
        /// Defines whether the component requires client-side validation.
        /// </summary>
        public bool Validation { get; private set; }

        /// <summary>
        /// The HTML id of this component.
        /// </summary>
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
        private string? _controlId;

        /// <summary>
        /// The HTML id used for a &lt;div&gt; tag.
        /// </summary>
        /// <remarks>This is a convenience property, so a component can reference one of its &lt;div&gt; tags by id.
        ///</remarks>
        public string DivId {
            get {
                if (string.IsNullOrEmpty(_divId))
                    _divId = Manager.UniqueId("div");
                return _divId;
            }
        }
        private string? _divId;

        /// <summary>
        /// Returns a unique HTML id.
        /// </summary>
        /// <param name="name">A string prefix prepended to the generated id.</param>
        /// <returns>A unique HTML id.</returns>
        /// <remarks>Every call to the Unique() method returns a new, unique id.</remarks>
        public string UniqueId(string name = "b") {
            return Manager.UniqueId(name);
        }

        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as an HTML attribute data value.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns an encoded HTML attribute data value.</returns>
        public static string HAE(string? text) {
            return Utility.HtmlAttributeEncode(text);
        }
        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as HTML.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns encoded HTML.</returns>
        public static string HE(string? text) {
            return Utility.HtmlEncode(text);
        }
        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as a JavaScript string.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns encoded JavaScript string.
        /// The string to encode should not use surrounding quotes.
        /// These must be added after encoding.
        /// </returns>
        public static string JE(string? text) {
            return Utility.JserEncode(text);
        }
        /// <summary>
        /// Encodes the provided <paramref name="val"/> a JavaScript true/false string.
        /// </summary>
        /// <param name="val">The value to encode.</param>
        /// <returns>Returns a JavaScript true/false string.</returns>
        public static string JE(bool val) {
            return val ? "true" : "false";
        }

        /// <summary>
        /// Returns the package implementing the component.
        /// </summary>
        /// <returns>Returns the package implementing the component.</returns>
        public abstract Package GetPackage();
        /// <summary>
        /// Returns the component name.
        /// </summary>
        /// <returns>Returns the component name.</returns>
        /// <remarks>Components in packages whose product name starts with "Component" use the exact name returned by GetTemplateName when used in UIHint attributes. These are considered core components.
        /// Components in other packages use the package's area name as a prefix. E.g., the UserId component in the YetaWF.Identity package is named "YetaWF_Identity_UserId" when used in UIHint attributes.
        ///
        /// The GetTemplateName method returns the component name without area name prefix in all cases.</remarks>
        public abstract string GetTemplateName();
        /// <summary>
        /// Returns the component type (edit/display).
        /// </summary>
        /// <returns>Returns the component type.</returns>
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
        public bool TryGetSiblingProperty<TYPE>(string property, [MaybeNullWhen(false)] out TYPE? value) {
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(Container, property, out value))
                return false;
            return true;
        }
        /// <summary>
        /// Retrieves a sibling property. Used to extract related properties from container, which typically are used for additional component customization.
        /// </summary>
        public TYPE? GetSiblingProperty<TYPE>(string property) {
            if (!ObjectSupport.TryGetPropertyValue<TYPE>(Container, property, out TYPE value))
                throw new InternalError($"No sibling property {property} found");
            return value;
        }
    }
}
