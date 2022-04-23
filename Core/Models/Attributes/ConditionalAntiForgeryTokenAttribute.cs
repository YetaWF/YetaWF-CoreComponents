/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    /// <summary>
    /// Antiforgery token validation.
    /// </summary>
    /// <remarks>
    /// An antiforgery token cannot be used on a static page.
    ///
    /// There is nothing conditional about this despite the name. This is a legacy remnant.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ConditionalAntiForgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter {

        public int Order { get; set; } = 1000;

        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) {
            return serviceProvider.GetRequiredService<ConditionalAntiForgeryTokenFilter>();
        }
    }

    public class ConditionalAntiForgeryTokenFilter : IAsyncAuthorizationFilter, IAntiforgeryPolicy {

        private readonly IAntiforgery _antiforgery;

        public ConditionalAntiForgeryTokenFilter(IAntiforgery antiforgery) {
            if (antiforgery == null)
                throw new ArgumentNullException(nameof(antiforgery));
            _antiforgery = antiforgery;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (ShouldValidate(context)) {
                try {
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                } catch (AntiforgeryValidationException) {
                    context.Result = new BadRequestResult();
                }
            }
        }

        protected virtual bool ShouldValidate(AuthorizationFilterContext context) {
            return true;
        }
    }
}
