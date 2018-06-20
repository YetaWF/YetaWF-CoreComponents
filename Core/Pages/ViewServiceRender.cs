/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

//$$$ NO LONGER NEEDED???

namespace YetaWF.Core.Pages {

    public interface IViewRenderService {
        Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, ViewDataDictionary viewData, Func<IHtmlHelper, ActionContext, string, Task<string>> postRenderAsync = null);
    }

    public class ViewRenderService : IViewRenderService {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IModelMetadataProvider _modelMetaDataProvider;

        public ViewRenderService(IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IModelMetadataProvider modelMetaDataProvider) {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _modelMetaDataProvider = modelMetaDataProvider;
        }

        public async Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, ViewDataDictionary viewData, Func<IHtmlHelper, ActionContext, string, Task<string>> postRenderAsync = null) {

            using (var sw = new StringWriter()) {
                ViewEngineResult viewResult;
                if (viewName.StartsWith("~/"))
                    viewResult = _razorViewEngine.GetView(executingFilePath: viewName, viewPath: viewName, isMainPage: true);
                else
                    viewResult = _razorViewEngine.FindView(actionContext, viewName, true);
                if (viewResult.View == null)
                    throw new InternalError("{0} does not match any available view", viewName);

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewData,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );
                await viewResult.View.RenderAsync(viewContext);
                if (postRenderAsync != null) {
                    YetaWFRazorView razorView = viewResult.View as YetaWFRazorView;
                    if (razorView != null) {
                        dynamic page = razorView.RazorPage;// we can't access Html directly because the page/template is derived from YetaWF.Core.Views.RazorView<,>
                        return await postRenderAsync(page.Html, actionContext, sw.ToString());
                    }
                }
                return sw.ToString();
            }
        }
    }
}
#else
#endif