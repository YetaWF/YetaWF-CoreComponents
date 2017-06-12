/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class ExtLabel<TModel> : RazorTemplate<TModel> { }

    public static class ExtLabelExtensions {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ExtLabelExtensions), name, defaultValue, parms); }
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

#if MVC6
        public static HtmlString ExtLabelFor<TModel, TValue>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, string htmlFieldName = null, int dummy = 0, object HtmlAttributes = null, bool ShowVariable = false, string Caption = null) {
            ModelExplorer modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData, htmlHelper.MetadataProvider);
            ModelMetadata metadata = modelExplorer.Metadata;
#else
        public static HtmlString ExtLabelFor<TModel, TValue>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, string htmlFieldName = null, int dummy = 0, object HtmlAttributes = null, bool ShowVariable = false, string Caption = null) {
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
#endif
            if (string.IsNullOrWhiteSpace(htmlFieldName))
                htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            return ExtLabelHelper(htmlHelper, metadata, htmlFieldName, null, FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes), ShowVariable: ShowVariable, Caption: Caption);
        }
#if MVC6
        public static HtmlString ExtLabel(this IHtmlHelper htmlHelper, string expression, object htmlAttributes = null, bool ShowVariable = false, string Caption = null, bool SuppressIfEmpty = false) {
            ModelExplorer modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, htmlHelper.ViewData, htmlHelper.MetadataProvider);
            ModelMetadata metadata = modelExplorer.Metadata;
#else
        public static HtmlString ExtLabel(this HtmlHelper htmlHelper, string expression, object htmlAttributes = null, bool ShowVariable = false, string Caption = null, bool SuppressIfEmpty = false) {
            ModelMetadata metadata = ModelMetadata.FromStringExpression(expression, htmlHelper.ViewData);
#endif
            string htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            return ExtLabelHelper(htmlHelper, metadata, htmlFieldName, null, FieldHelper.AnonymousObjectToHtmlAttributes(htmlAttributes), ShowVariable: ShowVariable, Caption: Caption, SuppressIfEmpty: SuppressIfEmpty);
        }
#if MVC6
        private static HtmlString ExtLabelHelper(IHtmlHelper htmlHelper, ModelMetadata metadata, string htmlFieldName, string labelText, IDictionary<string, object> htmlAttributes = null, bool ShowVariable = false, string Caption = null, string Description = null, string HelpLink = null, bool SuppressIfEmpty = false)
#else
        private static HtmlString ExtLabelHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string htmlFieldName, string labelText, IDictionary<string, object> htmlAttributes = null, bool ShowVariable = false, string Caption = null, string Description = null, string HelpLink = null, bool SuppressIfEmpty = false)
#endif
        {
            PropertyData propData = ObjectSupport.GetPropertyData(metadata.ContainerType, metadata.PropertyName);

            string description = Description ?? propData.GetDescription(metadata.ContainerType);
            if (!string.IsNullOrWhiteSpace(description)) {
                if (ShowVariable)
                    description = __ResStr("showVarFmt", "{0} (Variable {1})", description, htmlFieldName);
                htmlAttributes.Add(Basics.CssTooltip, YetaWFManager.HtmlAttributeEncode(description));
            }
            string label = Caption ?? propData.GetCaption(metadata.ContainerType);
            string helpLink = HelpLink ?? propData.GetHelpLink(metadata.ContainerType);

            StringBuilder sb = new StringBuilder();

            TagBuilder tagLabel = new TagBuilder("label");

            if (string.IsNullOrWhiteSpace(label)) {
                if (SuppressIfEmpty)
                    return HtmlStringExtender.Empty;
                tagLabel.SetInnerHtml("&nbsp;");
            }  else
                tagLabel.SetInnerText(label);
            tagLabel.MergeAttributes(htmlAttributes, replaceExisting: true);
            sb.Append(tagLabel.ToString(TagRenderMode.Normal));

            if (!string.IsNullOrWhiteSpace(helpLink)) {
                TagBuilder tagA = new TagBuilder("a");
                tagA.Attributes.Add("href", YetaWFManager.UrlEncodePath(helpLink));
                tagA.Attributes.Add("target", "_blank");
                tagA.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule("yt_extlabel_img"));
                Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                SkinImages skinImages = new SkinImages();
                string imageUrl = skinImages.FindIcon_Template("HelpLink.png", currentPackage, "ExtLabel");
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("altHelp", "Help"));
                tagA.SetInnerHtml(tagImg.ToString(TagRenderMode.StartTag));
                sb.Append(tagA.ToString(TagRenderMode.Normal));
            }
            return new HtmlString(sb.ToString());
        }
#if MVC6
        public static HtmlString RenderExtLabel(this IHtmlHelper htmlHelper, string text, int dummy = 0, string ToolTip = null, IDictionary<string, object> htmlAttributes = null) {
#else
        public static HtmlString RenderExtLabel(this HtmlHelper htmlHelper, string text, int dummy = 0, string ToolTip = null, IDictionary<string, object> htmlAttributes = null) {
#endif
            if (string.IsNullOrWhiteSpace(text)) return HtmlStringExtender.Empty;

            TagBuilder tag = new TagBuilder("label");
            tag.MergeAttributes(htmlAttributes, replaceExisting: true);
            if (!string.IsNullOrWhiteSpace(ToolTip))
                tag.Attributes.Add(Basics.CssTooltip, YetaWFManager.HtmlAttributeEncode(ToolTip));
            tag.SetInnerText(text);
            return tag.ToHtmlString(TagRenderMode.Normal);
        }
    }
}
