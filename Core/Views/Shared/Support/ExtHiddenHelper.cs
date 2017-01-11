/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class ExtHiddenHelper<TModel> : RazorTemplate<TModel> { }

    public static class ExtHiddenExtensions {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ExtHiddenExtensions), name, defaultValue, parms); }

        public static MvcHtmlString ExtHiddenFor<TModel, TValue>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, string htmlFieldName = null, int dummy = 0, object HtmlAttributes = null, bool ShowVariable = false, string Caption = null) {
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            if (string.IsNullOrWhiteSpace(htmlFieldName))
                htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            return ExtHiddenHelper(htmlHelper, metadata, htmlFieldName, null, FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes), ShowVariable: ShowVariable, Caption: Caption);
        }
        //public static MvcHtmlString ExtHidden(this HtmlHelper htmlHelper, string expression, object htmlAttributes = null, bool ShowVariable = false, string Caption = null) {
        //    ModelMetadata metadata = ModelMetadata.FromStringExpression(expression, htmlHelper.ViewData);
        //    string htmlFieldName = ExpressionHelper.GetExpressionText(expression);
        //    return ExtHiddenHelper(htmlHelper, metadata, htmlFieldName, null, FieldHelper.AnonymousObjectToHtmlAttributes(htmlAttributes), ShowVariable: ShowVariable, Caption: Caption);
        //}

        public static MvcHtmlString RenderHidden(this HtmlHelper htmlHelper, string name, object value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Anonymous = false, bool Validation = false) {
            if (value == null) value = "";
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Anonymous: Anonymous, Validation: Validation);
            tag.MergeAttribute("type", "hidden");
            tag.MergeAttribute("value", value.ToString());
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag));
        }

        private static MvcHtmlString ExtHiddenHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string htmlFieldName, string labelText, IDictionary<string, object> htmlAttributes = null, bool ShowVariable = false, string Caption = null) {

            if (metadata.Model == null) return MvcHtmlString.Empty;
            string value = metadata.Model.ToString();
            TagBuilder tag = new TagBuilder("input");
            tag.Attributes.Add("type", "hidden");
            tag.Attributes.Add("name", htmlHelper.FieldName(htmlFieldName));
            tag.Attributes.Add("value", value);
            tag.MergeAttributes(htmlAttributes, replaceExisting: true);
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag));
        }
    }
}
