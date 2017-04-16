/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Web.Helpers;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    /// <summary>
    /// Antiforgery token validation.
    /// </summary>
    /// <remarks>
    /// If a site enables static pages, anti-forgery tokens are not validated for anonymous users (ONLY). This exposes no
    /// security concerns as anti-forgery tokens are intended to prevent Cross-Site Request Forgery (CSRF) for authenticated users (i.e., NOT anonymous users).
    ///
    /// If static pages are not enabled for a site, anti-forgery tokens are used and validated in all cases (anonymous or authenticated users).
    /// </remarks>
#if MVC6
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ConditionalAntiForgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter {
#else
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ConditionalAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter {
#endif
        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        private YetaWFManager Manager { get { return YetaWFManager.Manager; } }

#if MVC6
        public int Order { get; set; } = 1000;

        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) {
            return serviceProvider.GetRequiredService<ConditionalAntiForgeryTokenFilter>();
        }
#else
        public ConditionalAntiForgeryTokenAttribute() : this(AntiForgery.Validate) { }

        internal ConditionalAntiForgeryTokenAttribute(Action validateAction) {
            ValidateAction = validateAction;
        }
        internal Action ValidateAction { get; private set; }

        public void OnAuthorization(AuthorizationContext filterContext) {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");
            if (!Manager.HaveUser && Manager.CurrentSite.StaticPages) {
                ; // don't validate AntiForgeryToken when we use static pages and have an anonymous user
            } else
                ValidateAction();
        }
#endif
    }

#if MVC6
    public class ConditionalAntiForgeryTokenFilter : IAsyncAuthorizationFilter, IAntiforgeryPolicy {

        private readonly IAntiforgery _antiforgery;

        private YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public ConditionalAntiForgeryTokenFilter(IAntiforgery antiforgery) {
            if (antiforgery == null) {
                throw new ArgumentNullException(nameof(antiforgery));
            }

            _antiforgery = antiforgery;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (ShouldValidate(context)) {
                try {
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                } catch (AntiforgeryValidationException) {
                    context.Result = new BadRequestResult();
                }
            }
        }

        protected virtual bool ShouldValidate(AuthorizationFilterContext context) {
            if (!Manager.HaveUser && Manager.CurrentSite.StaticPages)
                return false; // don't validate AntiForgeryToken when we use static pages and have an anonymous user
            return true;
        }
    }
#else
#endif
}
