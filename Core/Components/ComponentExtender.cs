/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Support;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using System.Collections.Generic;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using System.Linq;
#if MVC6
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    /// <summary>
    /// This static class implements extension methods for YetaWF components.
    /// </summary>
    public static class YetaWFComponentExtender {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(YetaWFComponentExtender), name, defaultValue, parms); }

        internal class LabelInfo {
            [UIHint("Label")]
            public string LabelContents { get; set; }
            public string LabelContents_HelpLink { get; set; }
        }

        /// <summary>
        /// Returns a collection of integer/enum values suitable for rendering in a DropDownList component.
        /// </summary>
        /// <param name="uiHint">The component name found in a UIHintAttribute.</param>
        /// <returns>Returns a collection of values suitable for rendering in a DropDownList component.</returns>
        /// <remarks>
        /// This method can be used with enum types to obtain a collection of values suitable for rendering in a DropDownList component.
        /// Components can implement the GetSelectionListIntAsync method to support retrieval of this collection.
        /// </remarks>
        public static async Task<List<SelectionItem<int?>>> GetSelectionListIntFromUIHintAsync(string uiHint) {
            Type compType;
            if (!YetaWFComponentBaseStartup.GetComponentsDisplay().TryGetValue(uiHint, out compType))
                return null;
            YetaWFComponentBase component = (YetaWFComponentBase)Activator.CreateInstance(compType);

            ISelectionListInt iSelList = component as ISelectionListInt;
            if (iSelList != null) {
                List<SelectionItem<int>> list = await iSelList.GetSelectionListIntAsync(false);
                return (from l in list select new SelectionItem<int?> { Text = l.Text, Tooltip = l.Tooltip, Value = l.Value }).ToList();
            }

            ISelectionListIntNull iSelNullList = component as ISelectionListIntNull;
            if (iSelNullList != null)
                return await iSelNullList.GetSelectionListIntNullAsync(false);

            return null;
        }

        /// <summary>
        /// Returns a collection of string values suitable for rendering in a DropDownList component.
        /// </summary>
        /// <param name="uiHint">The component name found in a UIHintAttribute.</param>
        /// <returns>Returns a collection of string values suitable for rendering in a DropDownList component.</returns>
        /// <remarks>
        /// This method can be used with string types to obtain a collection of values suitable for rendering in a DropDownList component.
        /// Components can implement the GetSelectionListStringAsync method to support retrieval of this collection.
        /// </remarks>
        public static async Task<List<SelectionItem<string>>> GetSelectionListStringFromUIHintAsync(string uiHint) {
            Type compType;
            if (!YetaWFComponentBaseStartup.GetComponentsDisplay().TryGetValue(uiHint, out compType))
                return null;
            YetaWFComponentBase component = (YetaWFComponentBase)Activator.CreateInstance(compType);
            ISelectionListString iSelList = component as ISelectionListString;
            if (iSelList == null)
                return null;
            return await iSelList.GetSelectionListStringAsync(false);
        }

        /// <summary>
        /// Tests whether a valid component can be found for a container's property.
        /// </summary>
        /// <param name="container">The container model.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="UIHint">A component name. May be null to extract the component name from the container's property.</param>
        /// <returns>Returns true if a valid component can be found.</returns>
        /// <remarks>This is used by the framework for debugging/testing purposes only.</remarks>
#if MVC6
        public static bool IsSupported(object container, string propertyName, string UIHint = null)
#else
        public static bool IsSupported(object container, string propertyName, string UIHint = null)
#endif
        {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            if (UIHint == null) {
                UIHintAttribute uiAttr = propData.TryGetAttribute<UIHintAttribute>();
                if (uiAttr == null)
                    throw new InternalError($"No UIHintAttribute found for property {propertyName}");
                UIHint = uiAttr.UIHint;
            }
            Type compType;
            if (!YetaWFComponentBaseStartup.GetComponentsDisplay().TryGetValue(UIHint, out compType) &&
                    !YetaWFComponentBaseStartup.GetComponentsEdit().TryGetValue(UIHint, out compType))
                return false;
            return true;
        }

        /// <summary>
        /// Renders an HTML &lt;label&gt; with the specified attributes.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model where the specified property <paramref name="propertyName"/> is located.</param>
        /// <param name="propertyName">The property name for which a label is rendered.</param>
        /// <param name="ShowVariable">Defines whether the property name is shown as part of the description (tooltip).</param>
        /// <param name="SuppressIfEmpty">Defines whether an empty string should be generated if the label caption is empty (true), otherwise the label is always generated even if there is no caption (false).</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <returns>Returns HTML with the rendered &lt;label&gt;.</returns>
        /// <remarks>
        /// The <paramref name="HtmlAttributes"/> may contain a "Description" entry which is used as the label's description (tooltip).
        /// If none is provided, the property's DescriptionAttribute is used instead. If this isn't found, the label does not have a description or tooltip.
        ///
        /// The <paramref name="HtmlAttributes"/> may contain a "Caption" entry which is used as the label's caption (text).
        /// If none is provided, the container model's "<paramref name="propertyName"/>_Label" property is retrieved and used as label caption.
        /// If none is provided, the property's CaptionAttribute is used instead.
        /// If this isn't found, the label does not have a caption (text).
        ///
        /// If the container model's property <paramref name="propertyName"/> has a HelpLinkAttribute, the label has a clickable help icon.
        /// </remarks>
        public static async Task<string> ForLabelAsync(this YHtmlHelper htmlHelper,
                object container, string propertyName, bool ShowVariable = false, bool SuppressIfEmpty = true, object HtmlAttributes = null) {
            Type containerType = container.GetType();
            PropertyData propData = ObjectSupport.GetPropertyData(containerType, propertyName);

            IDictionary<string, object> htmlAttributes = HtmlAttributes != null ? YHtmlHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes) : new Dictionary<string, object>();

            string description;
            if (htmlAttributes.ContainsKey("Description")) {
                description = (string)htmlAttributes["Description"];
                htmlAttributes.Remove("Description");
            } else {
                description = propData.GetDescription(container);
            }
            if (!string.IsNullOrWhiteSpace(description)) {
                if (ShowVariable)
                    description = __ResStr("showVarFmt", "{0} (Variable {1})", description, propertyName);
                htmlAttributes.Add(Basics.CssTooltip, description);
            }

            string caption;
            if (htmlAttributes.ContainsKey("Caption")) {
                caption = (string)htmlAttributes["Caption"];
                htmlAttributes.Remove("Caption");
            } else {
                caption = propData.GetCaption(container);
                if (string.IsNullOrEmpty(caption)) {
                    PropertyData propDataLabel = ObjectSupport.TryGetPropertyData(containerType, $"{propertyName}_Label");
                    if (propDataLabel != null)
                        caption = propDataLabel.GetPropertyValue<string>(container);
                }
            }
            if (string.IsNullOrEmpty(caption)) { // we're distinguishing between "" and " "
                if (SuppressIfEmpty)
                    return null;
            }

            string helpLink = propData.GetHelpLink(container);
            LabelInfo info = new LabelInfo() {
                LabelContents = caption,
                LabelContents_HelpLink = helpLink,
            };
            return await htmlHelper.ForDisplayAsync(info, nameof(LabelInfo.LabelContents), HtmlAttributes: htmlAttributes);
        }
        /// <summary>
        /// Renders a display component.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model where the specified property <paramref name="propertyName"/> is located.</param>
        /// <param name="propertyName">The property name for which a component is rendered.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <param name="UIHint">The component name used for rendering which identities the component. This may be null, in which case the property's UIHintAttribute is used instead.</param>
        /// <returns>Returns HTML with the rendered component.</returns>
        public static async Task<string> ForDisplayAsync(this YHtmlHelper htmlHelper,
            object container, string propertyName, object HtmlAttributes = null, string UIHint = null)
        {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, propertyName, null, HtmlAttributes, false, UIHint);
        }
        /// <summary>
        /// Renders an edit component.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model where the specified property <paramref name="propertyName"/> is located.</param>
        /// <param name="propertyName">The property name for which a component is rendered.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <param name="Validation">Defines whether client-side validation is used.</param>
        /// <param name="UIHint">The component name used for rendering which identities the component. This may be null, in which case the property's UIHintAttribute is used instead.</param>
        /// <returns>Returns HTML with the rendered component.</returns>
        public static async Task<string> ForEditAsync(this YHtmlHelper htmlHelper,
            object container, string propertyName, object HtmlAttributes = null, bool Validation = true, string UIHint = null)
        {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Edit, htmlHelper, container, propertyName, null, HtmlAttributes, Validation, UIHint);
        }
        private static async Task<string> RenderComponentAsync(Dictionary<string, Type> components, YetaWFComponentBase.ComponentType renderType, YHtmlHelper htmlHelper,
            object container, string propertyName, string fieldName, object htmlAttributes, bool validation, string uiHint = null)
        {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            object model = propData.PropInfo.GetValue(container, null);
            if (uiHint == null) {
                UIHintAttribute uiAttr = propData.TryGetAttribute<UIHintAttribute>();
                if (uiAttr == null)
                    throw new InternalError($"No UIHintAttribute found for property {propertyName}");
                uiHint = uiAttr.UIHint;
            }
            return await RenderComponentAsync(components, renderType, htmlHelper, container, propertyName, fieldName, propData, model, uiHint, htmlAttributes, validation);
        }
        /// <summary>
        /// Renders a display component.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model where the specified property <paramref name="propertyName"/> is located.</param>
        /// <param name="propertyName">The property name for which a component is rendered.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <param name="uiHint">The component name used for rendering which identities the component. This may be null, in which case the property's UIHintAttribute is used instead.</param>
        /// <returns>Returns HTML with the rendered component.</returns>
        public static async Task<string> ForDisplayComponentAsync(this YHtmlHelper htmlHelper,
                object container, string propertyName, object propertyValue, string uiHint, object HtmlAttributes = null) {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, propertyName, null, propData, propertyValue, uiHint, HtmlAttributes, false);
        }
        /// <summary>
        /// Renders a edit component.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model where the specified property <paramref name="propertyName"/> is located.</param>
        /// <param name="propertyName">The property name for which a component is rendered.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <param name="Validation">Defines whether client-side validation is used.</param>
        /// <param name="uiHint">The component name used for rendering which identities the component. This may be null, in which case the property's UIHintAttribute is used instead.</param>
        /// <returns>Returns HTML with the rendered component.</returns>
        public static async Task<string> ForEditComponentAsync(this YHtmlHelper htmlHelper,
                object container, string propertyName, object propertyValue, string uiHint, object HtmlAttributes = null, bool Validation = true) {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Edit, htmlHelper, container, propertyName, null, propData, propertyValue, uiHint, HtmlAttributes, Validation);
        }
        /// <summary>
        /// Renders a display container, a component containing other components.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <param name="uiHint">The component name used for rendering which identities the component.</param>
        /// <returns>Returns HTML with the rendered components.</returns>
        public static async Task<string> ForDisplayContainerAsync(this YHtmlHelper htmlHelper,
                object container, string uiHint, object HtmlAttributes = null) {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, null, null, null, null, uiHint, HtmlAttributes, false);
        }
        /// <summary>
        /// Renders an edit container, a component containing other components.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <param name="uiHint">The component name used for rendering which identities the component.</param>
        /// <returns>Returns HTML with the rendered components.</returns>
        public static async Task<string> ForEditContainerAsync(this YHtmlHelper htmlHelper,
                object container, string uiHint, object HtmlAttributes = null) {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Edit, htmlHelper, container, null, null, null, null, uiHint, HtmlAttributes, true);
        }
        /// <summary>
        /// Renders a display component.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model where the specified property <paramref name="propertyName"/> is located.</param>
        /// <param name="propertyName">The property name for which a component is rendered.</param>
        /// <param name="fieldName">The HTML field name of the component.</param>
        /// <param name="realPropertyContainer">Instead of using the container <paramref name="container"/>, this container model is used for rendering.</param>
        /// <param name="realProperty">Instead of using the property <paramref name="propertyName"/>, this container model is used for rendering.</param>
        /// <param name="model">The container model.</param>
        /// <param name="uiHint">The component name used for rendering which identities the component.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <returns>Returns HTML with the rendered components.</returns>
        /// <remarks>This is used to render <paramref name="realProperty"/> where <paramref name="propertyName"/> should be rendered.
        /// This is typically used to render complex components in place of one simple property.</remarks>
        public static async Task<string> ForDisplayAsAsync(this YHtmlHelper htmlHelper,
                object container, string propertyName, string fieldName, object realPropertyContainer, string realProperty, object model, string uiHint, object HtmlAttributes = null) {
            PropertyData realPropData = ObjectSupport.GetPropertyData(realPropertyContainer.GetType(), realProperty);
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, propertyName, fieldName, realPropData, model, uiHint, HtmlAttributes, false);
        }
        /// <summary>
        /// Renders an edit component.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="container">The container model where the specified property <paramref name="propertyName"/> is located.</param>
        /// <param name="propertyName">The property name for which a component is rendered.</param>
        /// <param name="fieldName">The HTML field name of the component.</param>
        /// <param name="realPropertyContainer">Instead of using the container <paramref name="container"/>, this container model is used for rendering.</param>
        /// <param name="realProperty">Instead of using the property <paramref name="propertyName"/>, this container model is used for rendering.</param>
        /// <param name="model">The container model.</param>
        /// <param name="uiHint">The component name used for rendering which identities the component.</param>
        /// <param name="HtmlAttributes">A collection of attributes.</param>
        /// <returns>Returns HTML with the rendered components.</returns>
        /// <remarks>This is used to render <paramref name="realProperty"/> where <paramref name="propertyName"/> should be rendered.
        /// This is typically used to render complex components in place of one simple property.</remarks>
        public static async Task<string> ForEditAsAsync(this YHtmlHelper htmlHelper,
                object container, string propertyName, string fieldName, object realPropertyContainer, string realProperty, object model, string uiHint, object HtmlAttributes = null) {
            PropertyData realPropData = ObjectSupport.GetPropertyData(realPropertyContainer.GetType(), realProperty);
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, propertyName, fieldName, realPropData, model, uiHint, HtmlAttributes, true);
        }

        private static async Task<string> RenderComponentAsync(Dictionary<string,Type> components, YetaWFComponentBase.ComponentType renderType, YHtmlHelper htmlHelper,
             object container, string propertyName, string fieldName, PropertyData propData, object model, string uiHint, object htmlAttributes, bool validation)
        {
#if MVC6
#else
            if (!YetaWFManager.IsSync())
                throw new InternalError("Rendering on MVC5 cannot be async");
#endif
            if (string.IsNullOrWhiteSpace(uiHint))
                throw new InternalError($"No UIHint found for {(propertyName ?? "(Container)")} in {container.GetType().FullName}");

            Type compType;
            if (!components.TryGetValue(uiHint, out compType))
                throw new InternalError($"Component {uiHint} ({renderType}) not found");
            YetaWFComponentBase component = (YetaWFComponentBase)Activator.CreateInstance(compType);

            // Get standard support for this package
            if (!Manager.ComponentPackagesSeen.Contains(component.Package)) {
                if (renderType == YetaWFComponentBase.ComponentType.Edit)
                    await component.IncludeStandardEditAsync();
                else
                    await component.IncludeStandardDisplayAsync();
                try {
                    Manager.ComponentPackagesSeen.Add(component.Package);
                } catch (Exception) { }
            }

            Type containerType = container.GetType();
            if (propertyName == null) {
                // Container
                component.SetRenderInfo(htmlHelper, container, null, null, null, htmlAttributes, validation);
                // Invoke IncludeAsync
                MethodInfo miAsync = compType.GetMethod(nameof(IYetaWFContainer<object>.IncludeAsync), new Type[] { });
                if (miAsync == null)
                    throw new InternalError($"{compType.FullName} doesn't have an {nameof(IYetaWFContainer<object>.IncludeAsync)} method for {containerType.FullName}");
                Task methRetvalTask = (Task)miAsync.Invoke(component, null);
                await methRetvalTask;

                // Invoke RenderAsync
                miAsync = compType.GetMethod(nameof(IYetaWFContainer<object>.RenderContainerAsync), new Type[] { containerType });
                if (miAsync == null)
                    throw new InternalError($"{compType.FullName} doesn't have a {nameof(IYetaWFContainer<object>.RenderContainerAsync)} method accepting a model type {typeof(object).FullName} for {containerType.FullName}");
                Task<string> methStringTask = (Task<string>)miAsync.Invoke(component, new object[] { container });
                return await methStringTask;
            } else {
                // Component
                if (propData == null)
                    propData = ObjectSupport.GetPropertyData(containerType, propertyName);
                component.SetRenderInfo(htmlHelper, container, propertyName, fieldName, propData, htmlAttributes, validation);
                // Invoke IncludeAsync
                MethodInfo miAsync = compType.GetMethod(nameof(IYetaWFComponent<object>.IncludeAsync), new Type[] { });
                if (miAsync == null)
                    throw new InternalError($"{compType.FullName} doesn't have an {nameof(IYetaWFComponent<object>.IncludeAsync)} method for {containerType.FullName}, {propertyName}");
                Task methRetvalTask = (Task)miAsync.Invoke(component, null);
                await methRetvalTask;

                // Invoke RenderAsync
                miAsync = compType.GetMethod(nameof(IYetaWFComponent<object>.RenderAsync), new Type[] { propData.PropInfo.PropertyType });
                if (miAsync == null)
                    throw new InternalError($"{compType.FullName} doesn't have a {nameof(IYetaWFComponent<object>.RenderAsync)} method accepting a model type {propData.PropInfo.PropertyType.FullName} for {containerType.FullName}, {propertyName}");
                Task<string> methStringTask = (Task<string>)miAsync.Invoke(component, new object[] { model });

                string yhtml = await methStringTask;
#if DEBUG
                if (!string.IsNullOrWhiteSpace(yhtml)) {
                    if (yhtml.Contains("System.Threading.Tasks.Task"))
                        throw new InternalError($"Component {uiHint} contains System.Threading.Tasks.Task - check for missing \"await\" - generated HTML: \"{yhtml}\"");
                    if (yhtml.Contains("Microsoft.AspNetCore.Mvc.Rendering"))
                        throw new InternalError($"Component {uiHint} contains Microsoft.AspNetCore.Mvc.Rendering - check for missing \"ToString()\" - generated HTML: \"{yhtml}\"");
                }
#endif
                return yhtml;
            }
        }

        /// <summary>
        /// Extracts all UIHint information from the specified <paramref name="type"/> and adds all required addons for the components used.
        /// </summary>
        /// <param name="type">The model type.</param>
        /// <remarks>Addons can only be added during an initial HTTP Get request. Some components (Grid component, Tree component) may
        /// need to render an empty component first and record data is added later during HTTP Post requests.
        ///
        /// The AddComponentForType method is used during the initial HTTP Get request to add all required addons for all components used so they are later available when record data is added to the component.
        /// </remarks>
        public static async Task AddComponentForType(Type type) {
            List<PropertyData> propData = ObjectSupport.GetPropertyData(type);
            foreach (PropertyData prop in propData) {
                if (prop.UIHint != null) {
                    if (prop.ReadOnly)
                        await YetaWFComponentExtender.MarkUsedDisplayAsync(prop.UIHint);
                    else
                        await YetaWFComponentExtender.MarkUsedEditAsync(prop.UIHint);
                    if (prop.PropInfo.PropertyType.IsClass)
                        await AddComponentForType(prop.PropInfo.PropertyType);
                }
            }
        }
        internal static async Task MarkUsedDisplayAsync(string UIHint) {
            await MarkUsedAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, UIHint);
        }
        internal static async Task MarkUsedEditAsync(string UIHint = null) {
            await MarkUsedAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Edit, UIHint);
        }
        private static async Task MarkUsedAsync(Dictionary<string, Type> components, YetaWFComponentBase.ComponentType renderType, string uIHint) {
            if (string.IsNullOrWhiteSpace(uIHint))
                throw new InternalError($"UIHint missing");
            Type compType;
            if (!components.TryGetValue(uIHint, out compType))
                throw new InternalError($"Component {uIHint} ({renderType}) not found");
            YetaWFComponentBase component = (YetaWFComponentBase)Activator.CreateInstance(compType);

            // Get standard support for this package
            if (!Manager.ComponentPackagesSeen.Contains(component.Package)) {
                if (renderType == YetaWFComponentBase.ComponentType.Edit)
                    await component.IncludeStandardEditAsync();
                else
                    await component.IncludeStandardDisplayAsync();
                try {
                    Manager.ComponentPackagesSeen.Add(component.Package);
                } catch (Exception) { }
            }

            // Invoke IncludeAsync
            MethodInfo miAsync = compType.GetMethod(nameof(IYetaWFContainer<object>.IncludeAsync), new Type[] { });
            if (miAsync == null)
                throw new InternalError($"{compType.FullName} doesn't have an {nameof(IYetaWFContainer<object>.IncludeAsync)} method");
            Task methRetvalTask = (Task)miAsync.Invoke(component, null);
            await methRetvalTask;
        }
    }
}
