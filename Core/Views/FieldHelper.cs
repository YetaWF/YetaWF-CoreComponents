/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views {

    public static class FieldHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static string FieldName(this HtmlHelper htmlHelper, string name) {
            string fieldName = htmlHelper.TryFieldName(name);
            if (String.IsNullOrEmpty(fieldName))
                throw new InternalError("Missing name argument.");
            return fieldName;
        }
        public static string TryFieldName(this HtmlHelper htmlHelper, string name) {
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
        public static string MakeId(this HtmlHelper htmlHelper, TagBuilder tag) {
            string id = (from a in tag.Attributes where string.Compare(a.Key, "id", true) == 0 select a.Value).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(id)) {
                id = Manager.UniqueId();
                tag.Attributes.Add("id", id);
            }
            return id;
        }

        public static string FieldSetup(this HtmlHelper htmlHelper, TagBuilder tag, string name, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true, bool Anonymous = false) {

            if (Anonymous && Validation) throw new InternalError("Can't use validation with anonymous input fields");

            if (HtmlAttributes != null)
                tag.MergeAttributes(FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes), true);
            string fullName = ModelNameOverride ?? htmlHelper.FieldName(name);
            if (!Anonymous) {
                tag.MergeAttribute("name", fullName, true);
                if (Validation) {
                    // error state
                    htmlHelper.AddErrorClass(tag, ModelNameOverride ?? name);
                    // client side validation
                    if (Validation)
                        htmlHelper.AddValidation(tag, ModelNameOverride ?? name);
                }
            }
            return fullName;
        }
        public static RouteValueDictionary AnonymousObjectToHtmlAttributes(object htmlAttributes) {
            if (htmlAttributes as RouteValueDictionary != null) return (RouteValueDictionary) htmlAttributes;
            return HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
        }
        public static void AddErrorClass(this HtmlHelper htmlHelper, TagBuilder tagBuilder, string name) {
            string cls = htmlHelper.GetErrorClass(name).ToString();
            if (!string.IsNullOrWhiteSpace(cls))
                tagBuilder.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cls));
        }
        public static MvcHtmlString GetErrorClass(this HtmlHelper htmlHelper, string name) {
            ModelState modelState;
            if (htmlHelper.ViewData.ModelState.TryGetValue(name, out modelState)) {
                if (modelState.Errors.Count > 0)
                    return MvcHtmlString.Create(HtmlHelper.ValidationInputCssClassName);
            }
            return MvcHtmlString.Empty;
        }
        public static void AddValidation(this HtmlHelper htmlHelper, TagBuilder tagBuilder, string name) {
            ModelMetadata metadata = ModelMetadata.FromStringExpression(name, htmlHelper.ViewContext.ViewData);
            tagBuilder.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata));
        }
    }
}
