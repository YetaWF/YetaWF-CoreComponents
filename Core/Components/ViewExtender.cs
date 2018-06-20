using System;
using System.Threading.Tasks;
using YetaWF.Core.Support;
using System.Reflection;
using YetaWF.Core.Modules;
using YetaWF.Core.Extensions;
#if MVC6
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    public static class YetaWFViewExtender {

        public const string PartialSuffix = "_Partial";

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static bool IsSupported(string viewName) {
            Type viewType;
            string v = viewName.TrimEnd(PartialSuffix);
            if (!YetaWFComponentBaseStartup.GetViews().TryGetValue(v, out viewType))
                return false;
            return true;
        }

#if MVC6
        public static async Task<YHtmlString> ForViewAsync(this IHtmlHelper htmlHelper,
#else
        public static async Task<YHtmlString> ForViewAsync(this HtmlHelper htmlHelper,
#endif
                string viewName, ModuleDefinition module, object model) {


            Type viewType;
            string v = viewName.TrimEnd(PartialSuffix);
            if (!YetaWFComponentBaseStartup.GetViews().TryGetValue(v, out viewType))
                throw new InternalError($"View {viewName} not found");
            YetaWFViewBase view = (YetaWFViewBase)Activator.CreateInstance(viewType);
            view.SetRenderInfo(htmlHelper, module);

            Type moduleType = module.GetType();
            Type modelType = model.GetType();

            // Find RenderViewAsync/RenderPartialViewAsync
            bool partial = viewName.EndsWith(PartialSuffix);
            string methodName = partial ? nameof(IYetaWFView2<object, object>.RenderPartialViewAsync) : nameof(IYetaWFView2<object, object>.RenderViewAsync);
            MethodInfo miAsync = viewType.GetMethod(methodName, new Type[] { moduleType, modelType });
            if (miAsync == null)
                throw new InternalError($"View {viewName} ({viewType.FullName}) doesn't have a {methodName} method accepting a module type {moduleType.FullName} and model type {modelType.FullName}");

            // Invoke RenderViewAsync/RenderPartialViewAsync
            Task<YHtmlString> methStringTask = (Task<YHtmlString>)miAsync.Invoke(view, new object[] { module, model });
            YHtmlString yhtml = await methStringTask;
#if DEBUG
            if (yhtml.ToString().Contains("System.Threading.Tasks.Task"))
                throw new InternalError($"View {viewName} contains System.Threading.Tasks.Task - check for missing \"await\"");
#endif
            return yhtml;
        }
    }
}
