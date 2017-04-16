/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    public static class TagBuilderExtension {

#if MVC6
        public static string ToString(this TagBuilder tagBuilder, TagRenderMode mode) {
            tagBuilder.TagRenderMode = mode;
            return tagBuilder.GetString();
        }
        public static string GetString(this TagBuilder tagBuilder) {
            using (var writer = new System.IO.StringWriter()) {
                tagBuilder.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }
        public static void SetInnerText(this TagBuilder tagBuilder, string text) {
            tagBuilder.InnerHtml.Clear();
            tagBuilder.InnerHtml.Append(text);
        }
        public static void SetInnerHtml(this TagBuilder tagBuilder, string text) {
            tagBuilder.InnerHtml.Clear();
            tagBuilder.InnerHtml.AppendHtml(text);
        }
        public static string GetInnerHtml(this TagBuilder tagBuilder) {
            using (var writer = new System.IO.StringWriter()) {
                tagBuilder.InnerHtml.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }
        public static HtmlString ToHtmlString(this TagBuilder tagBuilder) {
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }
        public static HtmlString ToHtmlString(this TagBuilder tagBuilder, TagRenderMode mode) {
            return new HtmlString(tagBuilder.ToString(mode));
        }
#else
        public static HtmlString ToHtmlString(this TagBuilder tagBuilder) {
            return new HtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }
        public static HtmlString ToHtmlString(this TagBuilder tagBuilder, TagRenderMode mode) {
            return new HtmlString(tagBuilder.ToString(mode));
        }
        public static string GetInnerHtml(this TagBuilder tagBuilder) {
            return tagBuilder.InnerHtml;
        }
        public static void SetInnerHtml(this TagBuilder tagBuilder, string text) {
            tagBuilder.InnerHtml = text;
        }
#endif
    }
}
