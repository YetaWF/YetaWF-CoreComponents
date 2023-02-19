/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YetaWF.Core.Models;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Support {

    /// <summary>
    /// This class is used to build HTML content.
    /// </summary>
    public class HtmlBuilder {

        /// <summary>
        /// Constructor.
        /// </summary>
        public HtmlBuilder() { }

        private readonly StringBuilder _hb = new StringBuilder();

        /// <summary>
        /// Appends a string to the current HTML content.
        /// </summary>
        /// <param name="s"></param>
        public void Append(string? s) {
            if (s == null) return;
            _hb.Append(s);
        }
        /// <summary>
        /// Appends a string to the current HTML content.
        /// </summary>
        /// <param name="s">A composite format string.</param>
        /// <param name="parms">An array of objects to format.</param>
        public void Append(string? s, params object?[] parms) {
            if (s == null) return;
            _hb.AppendFormat(s, parms);
        }
        /// <summary>
        /// Converts the value of this instance to a System.String.
        /// </summary>
        /// <returns> A string whose value is the same as this instance.</returns>
        public new string ToString() {
            return _hb.ToString();
        }

        /// <summary>
        ///  Gets or sets the length of the current YetaWF.Core.Support.HtmlBuilder object.
        /// </summary>
        public int Length {
            get { return _hb.Length; }
            set { _hb.Length = value; }
        }

        /// <summary>
        /// Replaces all occurrences of a specified string in this instance with another specified string.
        /// </summary>
        /// <param name="oldStr">The string to replace.</param>
        /// <param name="newStr">The string that replaces oldValue, or null.</param>
        /// <returns></returns>
        public StringBuilder Replace(string oldStr, string? newStr) {
            return _hb.Replace(oldStr, newStr);
        }

        /// <summary>
        /// Removes the specified range of characters from this instance.
        /// </summary>
        /// <param name="startIndex">The zero-based position in this instance where removal begins.</param>
        /// <param name="length">The number of characters to remove.</param>
        public void Remove(int startIndex, int length) {
            _hb.Remove(startIndex, length);
        }
        /// <summary>
        /// Removes the last character.
        /// </summary>
        public void RemoveLast() {
            if (_hb.Length > 0)
                _hb.Remove(_hb.Length - 1, 1);
        }

        /// <summary>
        /// Returns a string of formatted attributes given a dictionary of attributes.
        /// </summary>
        /// <param name="attributes">The dictionary of HTML attributes to format.</param>
        /// <remarks>Adds a leading space to the resulting string if attributes are available.</remarks>
        /// <returns>Returns all formatted attributes (with leading space). If no attributes are defined, an empty string is returned.
        /// Attributes with name "id" or "class" are never generated.</returns>
        public static string Attributes(IDictionary<string, object?>? attributes) {
            HtmlBuilder hb = new HtmlBuilder();
            if (attributes != null) {
                foreach (KeyValuePair<string, object?> entry in attributes) {
                    string key = entry.Key.ToLower();
                    if (key == "id" || key == "class") continue;
                    string? value;
                    if (entry.Value is MultiString s)
                        value = s;
                    else if (entry.Value is string)
                        value = (string?)entry.Value;
                    else
                        value = entry.Value?.ToString();
                    hb.Append($" {key.Replace("_", "-")}='{Utility.HAE(value)}'");
                }
            }
            return hb.ToString();
        }

        public static void AddClass(IDictionary<string, object?> htmlAttributes, string cls) {
            if (htmlAttributes.TryGetValue("class", out object? classes)) {
                classes = CssManager.CombineCss((string?)classes, cls);
                htmlAttributes["class"] = classes;
            } else {
                htmlAttributes.Add("class", cls);
            }
        }

        /// <summary>
        /// Returns the id defined in <paramref name="attributes"/>, or a new id.
        /// </summary>
        /// <param name="attributes">The dictionary of HTML attributes.</param>
        /// <returns>Returns the id defined in <paramref name="attributes"/>, or a new id.</returns>
        /// <example>
        /// HtmlBuilder hb = new HtmlBuilder();
        /// string id = HtmlBuilder.GetId(HtmlAttributes);
        /// hb.Append($@"<input {FieldSetup(Validation ? FieldType.Validated : FieldType.Normal)} id='{id}' class='{TemplateClass} t_edit yt_intvalue_base{HtmlBuilder.GetClass(HtmlAttributes)}' maxlength='20' value='{model?.ToString()}'>");
        /// </example>
        public static string GetId(IDictionary<string, object?>? attributes) {
            return GetIdCond(attributes) ?? YetaWFManager.Manager.UniqueId("c");
        }

        /// <summary>
        /// Returns the id defined in <paramref name="attributes"/>, or null.
        /// </summary>
        /// <param name="attributes">The dictionary of HTML attributes.</param>
        /// <returns>Returns the id defined in <paramref name="attributes"/>, or null.</returns>
        public static string? GetIdCond(IDictionary<string, object?>? attributes) {
            if (attributes != null) {
                string? key = (from a in attributes.Keys where a.ToLower() == "id" select a).FirstOrDefault();
                if (key != null)
                    return (string?)attributes[key];
            }
            return null;
        }

        /// <summary>
        /// Returns the CSS classes defined in <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The dictionary of HTML attributes.</param>
        /// <returns>Returns the CSS classes defined in <paramref name="attributes"/>, with a leading space. An empty string is returned if no classes are defined.</returns>
        /// <example>
        /// HtmlBuilder hb = new HtmlBuilder();
        /// string id = HtmlBuilder.GetId(HtmlAttributes);
        /// hb.Append($@"<input{FieldSetup(Validation ? FieldType.Validated : FieldType.Normal)} id='{id}' class='{TemplateClass} t_edit yt_intvalue_base{HtmlBuilder.GetClass(HtmlAttributes)}' maxlength='20' value='{model?.ToString()}'>");
        /// </example>
        public static string GetClasses(IDictionary<string, object?>? attributes, string? extraCss = null) {
            string? css = extraCss;
            if (attributes != null && attributes.ContainsKey("class"))
                css = CssManager.CombineCss(css, (string?)attributes["class"]);
            if (string.IsNullOrWhiteSpace(css))
                return string.Empty;
            return $" {css}";
        }

        /// <summary>
        /// Returns a complete class= CSS attribute including all classes defined in <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">The dictionary of HTML attributes.</param>
        /// <returns>Returns a complete class= CSS attribute including all classes defined in <paramref name="attributes"/>. An empty string is returned if no classes are defined.</returns>
        public static string GetClassAttribute(IDictionary<string, object?>? attributes, string? extraCss = null) {
            string? css = extraCss;
            if (attributes != null && attributes.ContainsKey("class"))
                css = CssManager.CombineCss(css, (string?)attributes["class"]);
            if (string.IsNullOrWhiteSpace(css))
                return string.Empty;
            return $" class='{css}'";
        }

        /// <summary>
        /// Creates and returns an antiforgery token (HTML).
        /// </summary>
        /// <param name="htmlHelper">An instance of a YHtmlHelper.</param>
        /// <returns>Returns an antiforgery token (HTML).</returns>
        public static string AntiForgeryToken() {
            if (YetaWFManager.Manager.AntiForgeryTokenHTML == null) {
                IAntiforgery? antiForgery = (IAntiforgery?)YetaWFManager.Manager.ServiceProvider.GetService(typeof(IAntiforgery));
                IHtmlContent ihtmlContent = antiForgery!.GetHtml(YetaWFManager.Manager.CurrentContext);
                using (System.IO.StringWriter writer = new System.IO.StringWriter()) {
                    ihtmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                    YetaWFManager.Manager.AntiForgeryTokenHTML = writer.ToString();
                }
            }
            return YetaWFManager.Manager.AntiForgeryTokenHTML;
        }

        /// <summary>
        /// Converts an anonymous object, a RouteValueDictionary or a Dictionary&lt;string, object&gt; object to a dictionary.
        /// </summary>
        /// <param name="htmlAttributes">An anonymous object, a RouteValueDictionary or a Dictionary&lt;string, object&gt; object.</param>
        /// <returns>Returns a dictionary with the key/values of the provided object <paramref name="htmlAttributes"/>.</returns>
        /// <remarks>
        /// This is intended for use with HTML attributes that may use different containers (an anonymous object, a RouteValueDictionary or a Dictionary&lt;string, object&gt; object).
        ///
        /// If an anonymous object is used, underscore characters (_) are replaced with hyphens (-) in the keys of the specified HTML attributes.</remarks>
        public static IDictionary<string, object?> AnonymousObjectToHtmlAttributes(object? htmlAttributes) {
            if (htmlAttributes == null) return new Dictionary<string, object?>();
            if (htmlAttributes as RouteValueDictionary != null) return (RouteValueDictionary)htmlAttributes;
            if (htmlAttributes as Dictionary<string, object> != null) return (Dictionary<string, object?>)htmlAttributes;
            return Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
        }
    }
}
