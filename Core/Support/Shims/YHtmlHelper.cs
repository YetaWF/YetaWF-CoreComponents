/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

//#if MVC6//$$$
//using Microsoft.AspNetCore.Mvc;
//#else
//using System.Web.Mvc;
//#endif

using System.Collections.Generic;
using System.Text;
using System.Web.Routing;
using System.Linq;
#if MVC6
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    /// <summary>
    /// A class implementing an HtmlHelper/IHtmlHelper placeholder while removing Razor.
    /// </summary>
    public class YHtmlHelper {

        public RequestContext RequestContext { get; private set; }
        public ModelStateDictionary ModelState { get; private set; }

        public YHtmlHelper(RequestContext requestContext, ModelStateDictionary modelState) {
            this.RequestContext = requestContext;
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
                using (System.IO.StringWriter writer = new System.IO.StringWriter()) {
                    IHtmlContent ihtmlContent = htmlHelper.AntiForgeryToken();
                    ihtmlContent.WriteTo(writer, HtmlEncoder.Default);
                    Manager.AntiForgeryTokenHTML = writer.ToString();
                }
#else
                YetaWFManager.Manager.AntiForgeryTokenHTML = System.Web.Helpers.AntiForgery.GetHtml().ToString();
#endif
            }
            return YetaWFManager.Manager.AntiForgeryTokenHTML;
        }

        public static string ValidationSummary(this YHtmlHelper htmlHelper) {
            IEnumerable<ModelError> errors = null;
            errors = htmlHelper.ModelState.SelectMany(c => c.Value.Errors);

            bool hasErrors = errors != null && errors.Any();
            if (!hasErrors) {
                return null;
            } else {
                YTagBuilder tagBuilder = new YTagBuilder("div");
                tagBuilder.AddCssClass(hasErrors ? "validation-summary-errors" : "validation-summary-valid");
                tagBuilder.MergeAttribute("data-valmsg-summary", "true");

                StringBuilder builder = new StringBuilder();
                builder.AppendLine("<ul>");
                foreach (var error in errors) {
                    builder.Append("<li>");
                    builder.Append(YetaWFManager.HtmlEncode(error.ErrorMessage));
                    builder.AppendLine("</li>");
                }
                builder.Append("</ul>");

                tagBuilder.InnerHtml = builder.ToString();
                return tagBuilder.ToString(YTagRenderMode.Normal);
            }
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
            tagBuilder.InnerHtml = YetaWFManager.HtmlEncode(error);
            bool replaceValidationMessageContents = string.IsNullOrWhiteSpace(error);
            tagBuilder.MergeAttribute("data-valmsg-for", fieldName);
            tagBuilder.MergeAttribute("data-valmsg-replace", replaceValidationMessageContents.ToString().ToLowerInvariant());

            tagBuilder.AddCssClass(hasError ? "field-validation-error" : "field-validation-valid");
            return tagBuilder.ToString(YTagRenderMode.Normal);
        }
    }
}

