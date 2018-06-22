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

namespace YetaWF.Core.Pages {

    public interface IViewRenderService {
        Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, ViewDataDictionary viewData);
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

        public async Task<string> RenderToStringAsync(ActionContext actionContext, string viewName, ViewDataDictionary viewData) {

            using (var sw = new StringWriter()) {
                ViewEngineResult viewResult;
                if (!viewName.StartsWith("~/"))
                    throw new InternalError($"nameof(RenderToStringAsync) can only be used with pages");

                viewResult = _razorViewEngine.GetView(executingFilePath: viewName, viewPath: viewName, isMainPage: true);
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
                return sw.ToString();
            }
        }
    }
}
#else
#endif