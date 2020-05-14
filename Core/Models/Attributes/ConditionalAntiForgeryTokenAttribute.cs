/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ConditionalAntiForgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter {
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        private YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public int Order { get; set; } = 1000;

        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) {
            return serviceProvider.GetRequiredService<ConditionalAntiForgeryTokenFilter>();
        }
    }

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
            if (!Manager.HaveUser && Manager.CurrentSite.StaticPages && Manager.HostUsed.ToLower() == Manager.CurrentSite.SiteDomain.ToLower())
                return false; // don't validate AntiForgeryToken when we use static pages and have an anonymous user
            return true;
        }
    }
}
