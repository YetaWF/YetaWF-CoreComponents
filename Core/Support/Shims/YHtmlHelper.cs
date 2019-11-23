/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
using System.Linq;
using YetaWF.Core.Controllers;
using YetaWF.Core.Addons;
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

        public static string AntiForgeryToken(this YHtmlHelper htmlHelper) {
            if (YetaWFManager.Manager.AntiForgeryTokenHTML == null) {
#if MVC6
                IAntiforgery antiForgery = (IAntiforgery)YetaWFManager.ServiceProvider.GetService(typeof(IAntiforgery));
                IHtmlContent ihtmlContent  = antiForgery.GetHtml(YetaWFManager.Manager.CurrentContext);
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

        /// <summary>
        /// Returns the client-side validation message for a component with the specified field name.
        /// </summary>
        /// <param name="containerFieldPrefix">The prefix used to build the final field name (for nested fields). May be null.</param>
        /// <param name="fieldName">The HTML field name.</param>
        /// <returns>Returns the client-side validation message for the component with the specified field name.</returns>
        public static string ValidationMessage(this YHtmlHelper htmlHelper, string containerFieldPrefix, string fieldName) {
            if (!string.IsNullOrEmpty(containerFieldPrefix))
                fieldName = containerFieldPrefix + "." + fieldName;
            return htmlHelper.BuildValidationMessage(fieldName);
        }

        public static string BuildValidationMessage(this YHtmlHelper htmlHelper, string fieldName) {
            var modelState = htmlHelper.ModelState[fieldName];
            string error = null;
            bool hasError = false;
            if (modelState == null) {
                // no errors
            } else {
                IEnumerable<string> errors = (from e in modelState.Errors select e.ErrorMessage);
                hasError = errors.Any();
                if (hasError)
                    error = errors.First();
            }

            YTagBuilder tagBuilder = new YTagBuilder("span");
            tagBuilder.MergeAttribute("data-v-for", fieldName);
            tagBuilder.AddCssClass(hasError ? "v-error" : "v-valid");

            if (hasError) {
                // we're building the same client side in validation.ts, make sure to keep in sync
                // <img src="${$YetaWF.htmlAttrEscape(YConfigs.Forms.CssWarningIconUrl)}" name=${name} class="${YConfigs.Forms.CssWarningIcon}" ${YConfigs.Basics.CssTooltip}="${$YetaWF.htmlAttrEscape(val.M)}"/>
                YTagBuilder tagImg = new YTagBuilder("img");
                string url = "/Addons/YetaWF/Core/_Skins/Standard/Icons/WarningIcon.png";///$$$$$
                tagImg.Attributes.Add("src", url);
                tagImg.Attributes.Add("name", fieldName);
                tagImg.AddCssClass(Forms.CssWarningIcon);
                tagImg.Attributes.Add(Basics.CssTooltip, error);
                tagBuilder.InnerHtml = tagImg.ToString();
            }
            return tagBuilder.ToString(YTagRenderMode.Normal);
        }
    }
}

