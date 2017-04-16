/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class ExtHiddenHelper<TModel> : RazorTemplate<TModel> { }

    public static class ExtHiddenExtensions {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ExtHiddenExtensions), name, defaultValue, parms); }

#if MVC6
        public static HtmlString ExtHiddenFor<TModel, TValue>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, string htmlFieldName = null, int dummy = 0, object HtmlAttributes = null, bool ShowVariable = false, string Caption = null) {
            ModelExplorer modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData, htmlHelper.MetadataProvider);
            ModelMetadata metadata = modelExplorer.Metadata;
#else
        public static HtmlString ExtHiddenFor<TModel, TValue>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, string htmlFieldName = null, int dummy = 0, object HtmlAttributes = null, bool ShowVariable = false, string Caption = null) {
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
#endif
            if (string.IsNullOrWhiteSpace(htmlFieldName))
                htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            return ExtHiddenHelper(htmlHelper, metadata, htmlFieldName, null, FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes), ShowVariable: ShowVariable, Caption: Caption);
        }
#if MVC6
        public static HtmlString RenderHidden(this IHtmlHelper htmlHelper, string name, object value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Anonymous = false, bool Validation = false) {
#else
        public static HtmlString RenderHidden(this HtmlHelper htmlHelper, string name, object value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Anonymous = false, bool Validation = false) {
#endif
            if (value == null) value = "";
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Anonymous: Anonymous, Validation: Validation);
            tag.MergeAttribute("type", "hidden");
            tag.MergeAttribute("value", value.ToString());
            return tag.ToHtmlString(TagRenderMode.StartTag);
        }
#if MVC6
        private static HtmlString ExtHiddenHelper(IHtmlHelper htmlHelper, object model, string htmlFieldName, string labelText, IDictionary<string, object> htmlAttributes = null, bool ShowVariable = false, string Caption = null)
#else
        private static HtmlString ExtHiddenHelper(HtmlHelper htmlHelper, object model, string htmlFieldName, string labelText, IDictionary<string, object> htmlAttributes = null, bool ShowVariable = false, string Caption = null)
#endif
        {
            if (model == null) return HtmlStringExtender.Empty;
            string value = model.ToString();
            TagBuilder tag = new TagBuilder("input");
            tag.Attributes.Add("type", "hidden");
            tag.Attributes.Add("name", htmlHelper.FieldName(htmlFieldName));
            tag.Attributes.Add("value", value);
            tag.MergeAttributes(htmlAttributes, replaceExisting: true);
            return tag.ToHtmlString(TagRenderMode.StartTag);
        }
    }
}
