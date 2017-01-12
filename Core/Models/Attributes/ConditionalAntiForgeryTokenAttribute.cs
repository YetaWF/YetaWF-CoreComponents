/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Web.Helpers;
using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    /// <summary>
    /// Antiforgery token validation.
    /// </summary>
    /// <remarks>
    /// If a site enables static pages, anti-forgery tokens are not validate for anonymous users (ONLY). This exposes no
    /// security concerns as anti-forgery tokens are intended to prevent Cross-Site Request Forgery (CSRF) for authenticated users (i.e., NOT anonymous users).
    ///
    /// If static pages are not enabled for a site, anti-forgery tokens are used and validated in all cases (anonymous or authenticated users).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ConditionalAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        private YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public ConditionalAntiForgeryTokenAttribute() : this(AntiForgery.Validate) { }

        internal ConditionalAntiForgeryTokenAttribute(Action validateAction) {
            ValidateAction = validateAction;
        }
        internal Action ValidateAction { get; private set; }

        public void OnAuthorization(AuthorizationContext filterContext) {
            if (filterContext == null) {
                throw new ArgumentNullException("filterContext");
            }
            if (!Manager.HaveUser && Manager.CurrentSite.StaticPages)
                ; // don't validate AntiForgeryToken when we use static pages and have an anonymous user
            else
                ValidateAction();
        }
    }
}
