using System;
using System.Threading.Tasks;
using YetaWF.Core.Support;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using System.Collections.Generic;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
#if MVC6
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    public static class YetaWFComponentExtender {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(YetaWFComponentExtender), name, defaultValue, parms); }

        public class LabelInfo {
            [UIHint("Label")]
            public string LabelContents { get; set; }
            public string LabelContents_HelpLink { get; set; }
        }

#if MVC6
        public static bool IsSupported(this IHtmlHelper htmlHelper, object container, string propertyName, string UIHint = null) 
#else
        public static bool IsSupported(this HtmlHelper htmlHelper, object container, string propertyName, string UIHint = null)
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
#if MVC6
        public static async Task<HtmlString> ForLabelAsync(this IHtmlHelper htmlHelper, 
#else
        public static async Task<HtmlString> ForLabelAsync(this HtmlHelper htmlHelper,
#endif
            object container, string propertyName, bool ShowVariable = false, bool SuppressIfEmpty = true)
        {
            Type containerType = container.GetType();
            PropertyData propData = ObjectSupport.GetPropertyData(containerType, propertyName);
            Dictionary<string, object> htmlAttributes = new Dictionary<string, object>();
            string description = propData.GetDescription(containerType);

            if (!string.IsNullOrWhiteSpace(description)) {
                if (ShowVariable)
                    description = __ResStr("showVarFmt", "{0} (Variable {1})", description, propertyName);
                htmlAttributes.Add(Basics.CssTooltip, description);
            }
            string label = propData.GetCaption(containerType);
            if (string.IsNullOrEmpty(label)) {
                PropertyData propDataLabel = ObjectSupport.GetPropertyData(containerType, $"{propertyName}_Label");
                label = propDataLabel.GetPropertyValue<string>(container);
            }
            if (string.IsNullOrEmpty(label)) { // we're distinguishing between "" and " "
                if (SuppressIfEmpty)
                    return HtmlStringExtender.Empty;
            }
            string helpLink = propData.GetHelpLink(containerType);
            LabelInfo info = new LabelInfo() {
                LabelContents = label,
                LabelContents_HelpLink = helpLink,
            };
            return await htmlHelper.ForDisplayAsync(info, nameof(LabelInfo.LabelContents), HtmlAttributes: htmlAttributes);
        }
#if MVC6
        public static async Task<HtmlString> ForDisplayAsync(this IHtmlHelper htmlHelper, 
#else
        public static async Task<HtmlString> ForDisplayAsync(this HtmlHelper htmlHelper,
#endif
            object container, string propertyName, object HtmlAttributes = null, string UIHint = null) 
        {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, propertyName, HtmlAttributes, false, UIHint);
        }
#if MVC6
        public static async Task<HtmlString> ForEditAsync(this IHtmlHelper htmlHelper, 
#else
        public static async Task<HtmlString> ForEditAsync(this HtmlHelper htmlHelper,
#endif
            object container, string propertyName, object HtmlAttributes = null, bool Validation = true, string UIHint = null)
        {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Edit, htmlHelper, container, propertyName, HtmlAttributes, Validation, UIHint);
        }
#if MVC6
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string,Type> components, string renderType, IHtmlHelper htmlHelper, 
#else
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string, Type> components, YetaWFComponentBase.ComponentType renderType, HtmlHelper htmlHelper,
#endif
            object container, string propertyName, object htmlAttributes, bool validation, string uiHint = null)
        {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            object model = propData.PropInfo.GetValue(container, null);
            if (uiHint == null) {
                UIHintAttribute uiAttr = propData.TryGetAttribute<UIHintAttribute>();
                if (uiAttr == null)
                    throw new InternalError($"No UIHintAttribute found for property {propertyName}");
                uiHint = uiAttr.UIHint;
            }
            return await RenderComponentAsync(components, renderType, htmlHelper, container, propertyName, propData, model, uiHint, htmlAttributes, validation);
        }
#if MVC6
        public static async Task<HtmlString> ForDisplayComponentAsync(this IHtmlHelper htmlHelper, 
#else
        public static async Task<HtmlString> ForDisplayComponentAsync(this HtmlHelper htmlHelper,
#endif
                object container, string propertyName, object propertyValue, string uiHint, object HtmlAttributes = null) {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, propertyName, propData, propertyValue, uiHint, HtmlAttributes, false);
        }
#if MVC6
        public static async Task<HtmlString> ForEditComponentAsync(this IHtmlHelper htmlHelper, 
#else
        public static async Task<HtmlString> ForEditComponentAsync(this HtmlHelper htmlHelper,
#endif
                object container, string propertyName, object propertyValue, string uiHint, object HtmlAttributes = null, bool Validation = true) {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Edit, htmlHelper, container, propertyName, propData, propertyValue, uiHint, HtmlAttributes, Validation);
        }
#if MVC6
        public static async Task<HtmlString> ForDisplayContainerAsync(this IHtmlHelper htmlHelper, 
#else
        public static async Task<HtmlString> ForDisplayContainerAsync(this HtmlHelper htmlHelper,
#endif
                object container, string uiHint, object HtmlAttributes = null) {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), YetaWFComponentBase.ComponentType.Display, htmlHelper, container, null, null, null, uiHint, HtmlAttributes, false);
        }
#if MVC6
        public static async Task<HtmlString> ForEditContainerAsync(this IHtmlHelper htmlHelper, 
#else
        public static async Task<HtmlString> ForEditContainerAsync(this HtmlHelper htmlHelper,
#endif
                object container, string uiHint, object HtmlAttributes = null) {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), YetaWFComponentBase.ComponentType.Edit, htmlHelper, container, null, null, null, uiHint, HtmlAttributes, true);
        }
#if MVC6
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string,Type> components, YetaWFComponentBase.ComponentType renderType, IHtmlHelper htmlHelper,
#else
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string,Type> components, YetaWFComponentBase.ComponentType renderType, HtmlHelper htmlHelper,
#endif
             object container, string propertyName, PropertyData propData, object model, string templateName, object htmlAttributes, bool validation)
        {
            Type compType;
            if (!components.TryGetValue(templateName, out compType))
                throw new InternalError($"Template {templateName} ({renderType}) not found");
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
                    throw new InternalError($"{compType.FullName} doesn't have an {nameof(IYetaWFContainer<object>.IncludeAsync)} method");
                Task methRetvalTask = (Task)miAsync.Invoke(component, null);
                await methRetvalTask;

                // Invoke RenderAsync
                // containers can support either a specific container type or object (as catch-all)
                miAsync = compType.GetMethod(nameof(IYetaWFContainer<object>.RenderContainerAsync), new Type[] { containerType });
                if (miAsync == null)
                    throw new InternalError($"{compType.FullName} doesn't have a {nameof(IYetaWFContainer<object>.RenderContainerAsync)} method accepting a model type {typeof(object).FullName}");
                Task<YHtmlString> methStringTask = (Task<YHtmlString>)miAsync.Invoke(component, new object[] { container });
                return await methStringTask;
            } else {
                // Component
                if (propData == null)
                    propData = ObjectSupport.GetPropertyData(containerType, propertyName);
                string fieldName = GetFieldName(htmlHelper, propertyName);
                component.SetRenderInfo(htmlHelper, container, propertyName, propData, fieldName, htmlAttributes, validation);
                // Invoke IncludeAsync
                MethodInfo miAsync = compType.GetMethod(nameof(IYetaWFComponent<object>.IncludeAsync), new Type[] { });
                if (miAsync == null)
                    throw new InternalError($"{compType.FullName} doesn't have an {nameof(IYetaWFComponent<object>.IncludeAsync)} method");
                Task methRetvalTask = (Task)miAsync.Invoke(component, null);
                await methRetvalTask;

                // Invoke RenderAsync
                miAsync = compType.GetMethod(nameof(IYetaWFComponent<object>.RenderAsync), new Type[] { propData.PropInfo.PropertyType });
                if (miAsync == null)
                    throw new InternalError($"{compType.FullName} doesn't have a {nameof(IYetaWFComponent<object>.RenderAsync)} method accepting a model type {propData.PropInfo.PropertyType.FullName}");
                Task<YHtmlString> methStringTask = (Task<YHtmlString>)miAsync.Invoke(component, new object[] { model });
                return await methStringTask;
            }
        }
#if MVC6
        private static string GetFieldName(IHtmlHelper htmlHelper, string name)
#else
        private static string GetFieldName(HtmlHelper htmlHelper, string name)
#endif
        {
            string fieldName = TryGetFieldName(htmlHelper, name);
            if (String.IsNullOrEmpty(fieldName))
                throw new InternalError("Missing field name");
            return fieldName;
        }
#if MVC6
        private static string TryGetFieldName(IHtmlHelper htmlHelper, string name)
#else
        private static string TryGetFieldName(HtmlHelper htmlHelper, string name)
#endif
        {
            string fieldName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            if (String.IsNullOrEmpty(fieldName))
                return null;
            // remove known grid prefix
            const string prefix1 = "GridProductEntries.GridDataRecords.record.";
            if (fieldName.StartsWith(prefix1)) fieldName = fieldName.Substring(prefix1.Length);
            return fieldName;
        }
    }
}
