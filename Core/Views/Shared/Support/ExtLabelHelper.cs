/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class ExtLabel<TModel> : RazorTemplate<TModel> { }

    public static class ExtLabelExtensions {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ExtLabelExtensions), name, defaultValue, parms); }
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static MvcHtmlString ExtLabelFor<TModel, TValue>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TValue>> expression, string htmlFieldName = null, int dummy = 0, object HtmlAttributes = null, bool ShowVariable = false, string Caption = null) {
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            if (string.IsNullOrWhiteSpace(htmlFieldName))
                htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            return ExtLabelHelper(htmlHelper, metadata, htmlFieldName, null, FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes), ShowVariable: ShowVariable, Caption: Caption);
        }
        public static MvcHtmlString ExtLabel(this HtmlHelper htmlHelper, string expression, object htmlAttributes = null, bool ShowVariable = false, string Caption = null) {
            ModelMetadata metadata = ModelMetadata.FromStringExpression(expression, htmlHelper.ViewData);
            string htmlFieldName = ExpressionHelper.GetExpressionText(expression);
            return ExtLabelHelper(htmlHelper, metadata, htmlFieldName, null, FieldHelper.AnonymousObjectToHtmlAttributes(htmlAttributes), ShowVariable: ShowVariable, Caption: Caption);
        }

        private static MvcHtmlString ExtLabelHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string htmlFieldName, string labelText, IDictionary<string, object> htmlAttributes = null, bool ShowVariable = false, string Caption = null, string Description = null, string HelpLink = null) {

            PropertyData propData = ObjectSupport.GetPropertyData(metadata.ContainerType, metadata.PropertyName);

            string description = Description ?? propData.GetDescription(metadata.Container);
            if (!string.IsNullOrWhiteSpace(description)) {
                if (ShowVariable)
                    description = __ResStr("showVarFmt", "{0} (Variable {1})", description, htmlFieldName);
                htmlAttributes.Add(Basics.CssTooltip, description);
            }
            string label = Caption ?? propData.GetCaption(metadata.Container);
            string helpLink = HelpLink ?? propData.GetHelpLink(metadata.Container);

            StringBuilder sb = new StringBuilder();

            TagBuilder tagLabel = new TagBuilder("label");

            if (string.IsNullOrWhiteSpace(label))
                tagLabel.InnerHtml = "&nbsp;";
            else
                tagLabel.SetInnerText(label);
            tagLabel.MergeAttributes(htmlAttributes, replaceExisting: true);
            sb.Append(tagLabel.ToString());

            if (!string.IsNullOrWhiteSpace(helpLink)) {
                TagBuilder tagA = new TagBuilder("a");
                tagA.Attributes.Add("href", YetaWFManager.UrlEncodePath(helpLink));
                tagA.Attributes.Add("target", "_blank");
                tagA.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule("yt_extlabel_img"));
                Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                SkinImages skinImages = new SkinImages();
                string imageUrl = skinImages.FindIcon_Template("HelpLink.png", currentPackage, "ExtLabel");
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("altHelp", "Help"));
                tagA.InnerHtml = tagImg.ToString(TagRenderMode.StartTag);
                sb.Append(tagA.ToString());
            }
            return MvcHtmlString.Create(sb.ToString());
        }
        public static MvcHtmlString RenderExtLabel(this HtmlHelper htmlHelper, string text, int dummy = 0, string ToolTip = null, IDictionary<string, object> htmlAttributes = null) {

            if (string.IsNullOrWhiteSpace(text)) return MvcHtmlString.Empty;

            TagBuilder tag = new TagBuilder("label");
            tag.MergeAttributes(htmlAttributes, replaceExisting: true);
            if (!string.IsNullOrWhiteSpace(ToolTip))
                tag.Attributes.Add(Basics.CssTooltip, ToolTip);
            tag.SetInnerText(text);
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }
    }
}
