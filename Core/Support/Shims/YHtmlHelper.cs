/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;

namespace YetaWF.Core.Support {

    /// <summary>
    /// A class implementing a minimal HtmlHelper after removing Razor.
    /// HtmlHelper in MVC is an abomination with a gazillion extension methods.
    /// Since we dropped Razor, we don't need it.
    /// </summary>
    public class YHtmlHelper {

        public ActionContext ActionContext { get; private set; }
        public RouteData RouteData { get { return ActionContext.RouteData; } }
        public ModelStateDictionary ModelState { get; private set; }

        public YHtmlHelper(ActionContext actionContext, ModelStateDictionary? modelState) {
            ActionContext = actionContext;
            ModelState = modelState ?? new ModelStateDictionary();
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

        /// <summary>
        /// Converts a dictionary of HTML attributes to a string.
        /// </summary>
        /// <param name="dict">The dictionary of HTML attributes.</param>
        /// <returns>Any keys containing "_" are replaced with "-".</returns>
        public static string HtmlAttributesToString(IDictionary<string, object?> dict) {
            HtmlBuilder hb = new HtmlBuilder();
            foreach(string key in dict.Keys) {
                hb.Append($" {key.Replace("_", "-")}={Utility.HAE((string?)dict[key])}");
            }
            return hb.ToString();
        }
    }
    /// <summary>
    /// Static class implementing HtmlHelper/IHtmlHelper extension methods.
    /// </summary>
    public static class YHtmlHelperExtender {

        /// <summary>
        /// Creates and returns an antiforgery token (HTML).
        /// </summary>
        /// <param name="htmlHelper">An instance of a YHtmlHelper.</param>
        /// <returns>Returns an antiforgery token (HTML).</returns>
        public static string AntiForgeryToken(this YHtmlHelper htmlHelper) {
            if (YetaWFManager.Manager.AntiForgeryTokenHTML == null) {
                IAntiforgery? antiForgery = (IAntiforgery?)YetaWFManager.ServiceProvider.GetService(typeof(IAntiforgery));
                IHtmlContent ihtmlContent = antiForgery!.GetHtml(YetaWFManager.Manager.CurrentContext);
                using (System.IO.StringWriter writer = new System.IO.StringWriter()) {
                    ihtmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                    YetaWFManager.Manager.AntiForgeryTokenHTML = writer.ToString();
                }
            }
            return YetaWFManager.Manager.AntiForgeryTokenHTML;
        }
    }
}

