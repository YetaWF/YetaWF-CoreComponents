/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
#if MVC6
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
#else
using System.Web.Routing;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    /// <summary>
    /// A class implementing a minimal HtmlHelper after removing Razor.
    /// HtmlHelper in MVC is an abomination with a gazillion extension methods.
    /// Since we dropped Razor, we don't need it.
    /// </summary>
    public class YHtmlHelper {

#if MVC6
        public ActionContext ActionContext { get; private set; }
        public RouteData RouteData { get { return ActionContext.RouteData; } }
#else
        public RequestContext RequestContext { get; private set; }
        public RouteData RouteData { get { return RequestContext.RouteData; } }
#endif
        public ModelStateDictionary ModelState { get; private set; }

#if MVC6
        public YHtmlHelper(ActionContext actionContext, ModelStateDictionary modelState) {
            ActionContext = actionContext;
            ModelState = modelState ?? new ModelStateDictionary();
        }
#else
        public YHtmlHelper(RequestContext requestContext, ModelStateDictionary modelState) {
            this.RequestContext = requestContext;
            ModelState = modelState ?? new ModelStateDictionary();
        }
#endif

        /// <summary>
        /// Converts an anonymous object, a RouteValueDictionary or a Dictionary&lt;string, object&gt; object to a dictionary.
        /// </summary>
        /// <param name="htmlAttributes">An anonymous object, a RouteValueDictionary or a Dictionary&lt;string, object&gt; object.</param>
        /// <returns>Returns a dictionary with the key/values of the provided object <paramref name="htmlAttributes"/>.</returns>
        /// <remarks>
        /// This is intended for use with HTML attributes that may use different containers (an anonymous object, a RouteValueDictionary or a Dictionary&lt;string, object&gt; object).
        ///
        /// If an anonymous object is used, underscore characters (_) are replaced with hyphens (-) in the keys of the specified HTML attributes.</remarks>
        public static IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes) {
            if (htmlAttributes as RouteValueDictionary != null) return (RouteValueDictionary)htmlAttributes;
            if (htmlAttributes as Dictionary<string, object> != null) return (Dictionary<string, object>)htmlAttributes;
#if MVC6
            return Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
#else
            return HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
#endif
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
#if MVC6
                IAntiforgery antiForgery = (IAntiforgery)YetaWFManager.ServiceProvider.GetService(typeof(IAntiforgery));
                IHtmlContent ihtmlContent = antiForgery.GetHtml(YetaWFManager.Manager.CurrentContext);
                using (System.IO.StringWriter writer = new System.IO.StringWriter()) {
                    ihtmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                    YetaWFManager.Manager.AntiForgeryTokenHTML = writer.ToString();
                }
#else
                YetaWFManager.Manager.AntiForgeryTokenHTML = System.Web.Helpers.AntiForgery.GetHtml().ToString();
#endif
            }
            return YetaWFManager.Manager.AntiForgeryTokenHTML;
        }
    }
}

