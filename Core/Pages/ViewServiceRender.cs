/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Support;

// Inspired by https://ppolyzos.com/2016/09/09/asp-net-core-render-view-to-string/

namespace YetaWF.Core.Pages {

    public interface IViewRenderService {
        Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, object model, Func<IHtmlHelper, ActionContext, string, string> postRender = null);
    }

    public class ViewRenderService : IViewRenderService {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public ViewRenderService(IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider) {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, object model, Func<IHtmlHelper, ActionContext, string, string> postRender = null) {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            //var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter()) {
                ViewEngineResult viewResult;
                if (viewName.StartsWith("~/"))
                    viewResult = _razorViewEngine.GetView(executingFilePath: viewName, viewPath: viewName, isMainPage: true);
                else
                    viewResult = _razorViewEngine.FindView(actionContext, viewName, true);
                if (viewResult.View == null)
                    throw new InternalError("{0} does not match any available view", viewName);

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), actionContext.ModelState) {
                    Model = model,
                };
                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );
                await viewResult.View.RenderAsync(viewContext);
                if (postRender != null) {
                    YetaWFRazorView razorView = viewResult.View as YetaWFRazorView;
                    if (razorView != null) {
                        dynamic page = razorView.RazorPage;// we can't access Html directly because the page/template is derived from YetaWF.Core.Views.RazorView<,>
                        return postRender(page.Html, actionContext, sw.ToString());
                    }
                }
                return sw.ToString();
            }
        }
    }
}
#else
#endif