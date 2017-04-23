/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
#endif

namespace YetaWF.Core.Views {

    public static class FieldHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static string FieldName(this IHtmlHelper htmlHelper, string name) {
#else
        public static string FieldName(this HtmlHelper htmlHelper, string name) {
#endif
            string fieldName = htmlHelper.TryFieldName(name);
            if (String.IsNullOrEmpty(fieldName))
                throw new InternalError("Missing name argument.");
            return fieldName;
        }
#if MVC6
        public static string TryFieldName(this IHtmlHelper htmlHelper, string name) {
#else
        public static string TryFieldName(this HtmlHelper htmlHelper, string name) {
#endif
            string fieldName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            if (String.IsNullOrEmpty(fieldName))
                return null;
            // remove known grid prefix
            const string prefix1 = "GridProductEntries.GridDataRecords.record.";
            if (fieldName.StartsWith(prefix1)) fieldName = fieldName.Substring(prefix1.Length);
            return fieldName;
        }
        /// <summary>
        /// Creates a new id on the specified tag or returns the existing id.
        /// </summary>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="tag">The tag.</param>
        /// <returns>An existing id or the id that was added to the tag.</returns>
#if MVC6
        public static string MakeId(this IHtmlHelper htmlHelper, TagBuilder tag) {
#else
        public static string MakeId(this HtmlHelper htmlHelper, TagBuilder tag) {
#endif
            string id = (from a in tag.Attributes where string.Compare(a.Key, "id", true) == 0 select a.Value).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(id)) {
                id = Manager.UniqueId();
                tag.Attributes.Add("id", id);
            }
            return id;
        }
#if MVC6
        public static string FieldSetup(this IHtmlHelper htmlHelper, TagBuilder tag, string name, object HtmlAttributes = null, bool Validation = true, bool Anonymous = false) {
#else
        public static string FieldSetup(this HtmlHelper htmlHelper, TagBuilder tag, string name, object HtmlAttributes = null, bool Validation = true, bool Anonymous = false) {
#endif
            if (Anonymous && Validation) throw new InternalError("Can't use validation with anonymous input fields");

            if (HtmlAttributes != null)
                tag.MergeAttributes(FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes), true);
            string fullName = htmlHelper.FieldName(name);
            if (!Anonymous) {
                tag.MergeAttribute("name", fullName, true);
                if (Validation) {
                    // error state
                    htmlHelper.AddErrorClass(tag, fullName);
                    // client side validation
                    if (Validation)
                        htmlHelper.AddValidation(tag, name);
                }
            }
            return fullName;
        }
        public static IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes) {
            if (htmlAttributes as RouteValueDictionary != null) return (RouteValueDictionary)htmlAttributes;
            return HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
        }
#if MVC6
        public static void AddErrorClass(this IHtmlHelper htmlHelper, TagBuilder tagBuilder, string name) {
#else
        public static void AddErrorClass(this HtmlHelper htmlHelper, TagBuilder tagBuilder, string name) {
#endif
            string cls = htmlHelper.GetErrorClass(name).ToString();
            if (!string.IsNullOrWhiteSpace(cls))
                tagBuilder.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cls));
        }
#if MVC6
        public static HtmlString GetErrorClass(this IHtmlHelper htmlHelper, string name) {
#else
        public static HtmlString GetErrorClass(this HtmlHelper htmlHelper, string name) {
#endif
#if MVC6
            ModelStateEntry modelState;
#else
            ModelState modelState;
#endif
            if (htmlHelper.ViewData.ModelState.TryGetValue(name, out modelState)) {
                if (modelState.Errors.Count > 0)
                    return new HtmlString(HtmlHelper.ValidationInputCssClassName);
            }
            return HtmlStringExtender.Empty;
        }
#if MVC6
        public static void AddValidation(this IHtmlHelper htmlHelper, TagBuilder tagBuilder, string name) {
#else
        public static void AddValidation(this HtmlHelper htmlHelper, TagBuilder tagBuilder, string name) {
#endif
#if MVC6
            ModelExplorer modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, htmlHelper.ViewData, htmlHelper.MetadataProvider);
            ValidationHtmlAttributeProvider valHtmlAttrProvider = (ValidationHtmlAttributeProvider)YetaWFManager.ServiceProvider.GetService(typeof(ValidationHtmlAttributeProvider));
            valHtmlAttrProvider.AddAndTrackValidationAttributes(htmlHelper.ViewContext, modelExplorer, name, tagBuilder.Attributes);
#else
            ModelMetadata metadata = ModelMetadata.FromStringExpression(name, htmlHelper.ViewContext.ViewData);
            tagBuilder.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata));
#endif
            // patch up auto-generated "required" validation (added by MVC) and rename our own customrequired validation to required
            if (tagBuilder.Attributes.ContainsKey("data-val-required")) {
                tagBuilder.Attributes.Remove("data-val-required");
            }
            if (tagBuilder.Attributes.ContainsKey("data-val-customrequired")) {
                tagBuilder.Attributes.Add("data-val-required", tagBuilder.Attributes["data-val-customrequired"]);
                tagBuilder.Attributes.Remove("data-val-customrequired");
            }
        }
    }
}
