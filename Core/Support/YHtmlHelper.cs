/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

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
    }
}

