/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Text;
#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    /// <summary>
    /// The tag's rending mode.
    /// </summary>
    public enum YTagRenderMode {
        /// <summary>
        /// Normal - &lt;tag&gt; &lt;/tag&gt;
        /// </summary>
        Normal,
        /// <summary>
        /// Start tag only - &lt;tag&gt;
        /// </summary>
        StartTag,
        /// <summary>
        /// End tag only - &lt;/tag&gt;
        /// </summary>
        EndTag,
        /// <summary>
        /// Self closing tag - &lt;tag /&gt;
        /// </summary>
        SelfClosing
    }

    /// <summary>
    /// One instance of the YTagBuilder class is used to build and render one HTML tag (simlar to MVC's TagBuilder type).
    /// </summary>
    public class YTagBuilder {

        /// <summary>
        /// The tag name.
        /// </summary>
        public string Tag { get; private set; }
        /// <summary>
        /// A dictionary of the tag's HTML attributes.
        /// </summary>
        public Dictionary<string, string> Attributes { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tag">The tag name.</param>
        public YTagBuilder(string tag) {
            Tag = tag;
            Attributes = new Dictionary<string, string>();
        }

        /// <summary>
        /// Sets the tag's inner text, encoded for HTML.
        /// </summary>
        /// <param name="text">The inner text.</param>
        public void SetInnerText(string text) {
            InnerHtml = YetaWFManager.HtmlEncode(text);
        }

        /// <summary>
        /// Defines the tag's inner HTML.
        /// </summary>
        public string InnerHtml { get; set; }

        /// <summary>
        /// Adds a CSS class to the tag.
        /// </summary>
        /// <param name="value"></param>
        public void AddCssClass(string value) {
            string currentValue;
            if (Attributes.TryGetValue("class", out currentValue))
                Attributes["class"] = currentValue + " " + value;
            else
                Attributes["class"] = value;
        }

        /// <summary>
        /// Merges a given dictionary of HTML attributes into the tag's HTML attributes.
        /// </summary>
        /// <param name="attributes">The dictionary of HTML attributes to merge into the tag's HTML attributes.</param>
        /// <param name="replaceExisting">Set to true to replace any existing attributes, false otherwise (duplicates are ignored).</param>
        public void MergeAttributes<TKey, TValue>(IDictionary<TKey, TValue> attributes, bool replaceExisting) {
            if (attributes != null) {
                foreach (var entry in attributes) {
                    string key = Convert.ToString(entry.Key);
                    string value = Convert.ToString(entry.Value);
                    MergeAttribute(key, value, replaceExisting);
                }
            }
        }
        /// <summary>
        /// Adds a new attribute. Does not replace an already existing attribute.
        /// </summary>
        /// <param name="key">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        /// <remarks>If a "class" attribute is added, existing CSS classes are preserved and the new class added to the existing CSS classes.
        /// </remarks>
        public void MergeAttribute(string key, string value) {
            MergeAttribute(key, value, replaceExisting: false);
        }
        /// <summary>
        /// Adds a new attribute.
        /// </summary>
        /// <param name="key">The attribute name.</param>
        /// <param name="value">The attribute value.</param>
        /// <param name="replaceExisting">Set to true to replace an existing attribute, false otherwise (duplicates are ignored).</param>
        /// <remarks>If a "class" attribute is added, existing CSS classes are preserved and the new class added to the existing CSS classes (<paramref name="replaceExisting"/> true), otherwise the class attribute is replaced.
        /// </remarks>
        public void MergeAttribute(string key, string value, bool replaceExisting) {
            if (string.IsNullOrWhiteSpace(key))
                throw new InternalError($"Invalid attribute key");
            if (replaceExisting) {
                Attributes[key] = value;
            } else {
                if (!Attributes.ContainsKey(key))
                    Attributes[key] = value;
                else if (key == "class") // special case for class as it's cumulative
                    AddCssClass(value);
            }
        }
        /// <summary>
        /// Returns the current tag as HTML with default rendering &lt;tag&gt; &lt;/tag&gt;.
        /// </summary>
        /// <returns>Returns the current tag as HTML.</returns>
        public new string ToString() {
            return ToString(YTagRenderMode.Normal);
        }
        /// <summary>
        /// Returns the current tag as HTML with specified rendering.
        /// </summary>
        /// <returns>Returns the current tag as HTML.</returns>
        public string ToString(YTagRenderMode renderMode) {
            StringBuilder sb = new StringBuilder();
            switch (renderMode) {
                case YTagRenderMode.StartTag:
                    sb.Append('<').Append(Tag);
                    AppendAttributes(sb);
                    sb.Append('>');
                    break;
                case YTagRenderMode.EndTag:
                    sb.Append("</").Append(Tag).Append('>');
                    break;
                case YTagRenderMode.SelfClosing:
                    sb.Append('<').Append(Tag);
                    AppendAttributes(sb);
                    sb.Append(" />");
                    break;
                default:
                    sb.Append('<').Append(Tag);
                    AppendAttributes(sb);
                    sb.Append('>').Append(InnerHtml).Append("</").Append(Tag).Append('>');
                    break;
            }
            return sb.ToString();
        }
        /// <summary>
        /// Returns the current tag as HTML with specified rendering.
        /// </summary>
        /// <returns>Returns the current tag as HTML.</returns>
        public HtmlString ToHtmlString(YTagRenderMode renderMode) {
            return new HtmlString(ToString(renderMode));
        }
        /// <summary>
        /// Returns the current tag as HTML with specified rendering.
        /// </summary>
        /// <returns>Returns the current tag as HTML.</returns>
        public YHtmlString ToYHtmlString(YTagRenderMode renderMode) {
            return new YHtmlString(ToString(renderMode));
        }
        private void AppendAttributes(StringBuilder sb) {
            foreach (var attribute in Attributes) {
                string key = attribute.Key;
                if (String.Equals(key, "id", StringComparison.Ordinal /* case-sensitive */) && String.IsNullOrEmpty(attribute.Value))
                    continue;
                string value = YetaWFManager.HtmlAttributeEncode(attribute.Value);
                sb.Append(' ').Append(key).Append("=\"").Append(value).Append('"');
            }
        }
    }
}
