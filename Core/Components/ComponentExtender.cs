using System;
using System.Threading.Tasks;
using YetaWF.Core.Support;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using System.Collections.Generic;
using System.Reflection;
#if MVC6
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    public static class YetaWFComponentExtender {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

#if MVC6
        public static bool IsSupported(this IHtmlHelper htmlHelper, object container, string propertyName) 
#else
        public static bool IsSupported(this HtmlHelper htmlHelper, object container, string propertyName)
#endif
        {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            object model = propData.PropInfo.GetValue(container, null);
            UIHintAttribute uiAttr = propData.TryGetAttribute<UIHintAttribute>();
            if (uiAttr == null)
                throw new InternalError("No UIHintAttribute found for property {propertyName}");
            Type compType;
            if (!YetaWFComponentBaseStartup.GetComponentsDisplay().TryGetValue(uiAttr.UIHint, out compType) &&
                    !YetaWFComponentBaseStartup.GetComponentsDisplay().TryGetValue(uiAttr.UIHint, out compType))
                return false;
            return true;
        }

#if MVC6
        public static async Task<HtmlString> ForDisplayAsync(this IHtmlHelper htmlHelper, object container, string propertyName, object HtmlAttributes = null) 
#else
        public static async Task<HtmlString> ForDisplayAsync(this HtmlHelper htmlHelper, object container, string propertyName, object HtmlAttributes = null)
#endif
        {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsDisplay(), "display", htmlHelper, container, propertyName, HtmlAttributes, false);
        }
#if MVC6
        public static async Task<HtmlString> ForEditAsync(this IHtmlHelper htmlHelper, object container, string propertyName, object HtmlAttributes = null, bool Validation = true)
#else
        public static async Task<HtmlString> ForEditAsync(this HtmlHelper htmlHelper, object container, string propertyName, object HtmlAttributes = null, bool Validation = true)
#endif
        {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), "edit", htmlHelper, container, propertyName, HtmlAttributes, Validation);
        }
#if MVC6
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string,Type> components, string renderType, IHtmlHelper htmlHelper, object container, string propertyName, object htmlAttributes, bool validation)
#else
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string, Type> components, string renderType, HtmlHelper htmlHelper, object container, string propertyName, object htmlAttributes, bool validation)
#endif
        {
            PropertyData propData = ObjectSupport.GetPropertyData(container.GetType(), propertyName);
            object model = propData.PropInfo.GetValue(container, null);
            UIHintAttribute uiAttr = propData.TryGetAttribute<UIHintAttribute>();
            if (uiAttr == null)
                throw new InternalError("No UIHintAttribute found for property {propertyName}");
            return await RenderComponentAsync(components, renderType, htmlHelper, container, propertyName, propData, model, uiAttr.UIHint, htmlAttributes, validation);
        }
#if MVC6
        public static async Task<HtmlString> ForEditAsync(this IHtmlHelper htmlHelper, object container, string propertyName, object model, string templateName, object HtmlAttributes = null, bool Validation = true)
#else
        public static async Task<HtmlString> ForEditAsync(this HtmlHelper htmlHelper, object container, string propertyName, object model, string templateName, object HtmlAttributes = null, bool Validation = true)
#endif
        {
            return await RenderComponentAsync(YetaWFComponentBaseStartup.GetComponentsEdit(), "edit", htmlHelper, container, propertyName, null, model, templateName, HtmlAttributes, Validation);
        }
#if MVC6
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string,Type> components, string renderType, IHtmlHelper htmlHelper, object container, string propertyName, PropertyData propData, object model, string templateName, object htmlAttributes, bool validation)
#else
        private static async Task<HtmlString> RenderComponentAsync(Dictionary<string,Type> components, string renderType, HtmlHelper htmlHelper, object container, string propertyName, PropertyData propData, object model, string templateName, object htmlAttributes, bool validation)
#endif
        {
            Type compType;
            if (!components.TryGetValue(templateName, out compType))
                throw new InternalError($"Template {templateName} ({renderType}) not found");
            YetaWFComponentBase component = (YetaWFComponentBase)Activator.CreateInstance(compType);

            // Get standard support for this package
            if (!Manager.ComponentPackagesSeen.Contains(component.Package)) {
                await component.IncludeStandardAsync();
                try {
                    Manager.ComponentPackagesSeen.Add(component.Package);
                } catch (Exception) { }
            }

            Type containerType = container.GetType();
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
            miAsync = compType.GetMethod(nameof(IYetaWFComponent<object>.RenderAsync), new Type[] { model.GetType() });
            if (miAsync == null)
                throw new InternalError($"{compType.FullName} doesn't have a {nameof(IYetaWFComponent<object>.RenderAsync)} method accepting a model type {model.GetType().FullName}");
            Task<YHtmlString> methStringTask = (Task<YHtmlString>)miAsync.Invoke(component, new object[] { model });
            return await methStringTask;
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
