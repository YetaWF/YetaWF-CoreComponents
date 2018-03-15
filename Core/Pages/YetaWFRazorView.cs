/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace YetaWF.Core.Pages {

    // TODO: Research use of IPageFilter, IAsyncPageFilter (introduced with ASP.NET Core MVC 2.0 Preview 2)

    public class YetaWFRazorView : RazorView {
        public YetaWFRazorView(
            IRazorViewEngine viewEngine,
            IRazorPageActivator pageActivator,
            IReadOnlyList<IRazorPage> viewStartPages,
            IRazorPage razorPage,
            HtmlEncoder htmlEncoder,
            DiagnosticSource diagnosticSource) : base(viewEngine, pageActivator, viewStartPages, razorPage, htmlEncoder, diagnosticSource) { }


        public override async Task RenderAsync(ViewContext context) {
            BeginRender();
            IRazorPageLifetime pageLifetime = RazorPage as IRazorPageLifetime;
            // Ideally the page would be activated before we call BeginRender, but that would
            // require more work (replacing more mvc code) and there are no clean interfaces to do so
            // so we'll pass the context instead.
            if (pageLifetime != null)
                pageLifetime.BeginRender(context);
            await base.RenderAsync(context);
            if (pageLifetime != null)
                await pageLifetime.EndRenderAsync(context);
            EndRender();
        }
        public virtual void BeginRender() { }
        public virtual void EndRender() { }
    }
}
#else
#endif