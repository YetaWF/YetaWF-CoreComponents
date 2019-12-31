/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Support;
using System.Reflection;
using YetaWF.Core.Modules;
using YetaWF.Core.Extensions;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    /// <summary>
    /// This static class implements extension methods for YetaWF views.
    /// </summary>
    public static class YetaWFViewExtender {

        /// <summary>
        /// The string appended to a view name to obtain the partial view name.
        /// A partial view is the portion of the view between &lt;form&gt; and &lt;/form&gt; tags.
        /// </summary>
        public const string PartialSuffix = "_Partial";

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Tests whether a valid view exists.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <returns>Returns true if a valid view can be found.</returns>
        /// <remarks>This is used by the framework for debugging/testing purposes only.</remarks>
        public static bool IsSupported(string viewName) {
            Type viewType;
            string v = viewName.TrimEnd(PartialSuffix);
            if (!YetaWFComponentBaseStartup.GetViews().TryGetValue(v, out viewType))
                return false;
            return true;
        }

        /// <summary>
        /// Renders a view.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="module">The module on behalf of which this view is rendered.</param>
        /// <param name="model">The view's data model to render.</param>
        /// <returns>Returns HTML with the rendered view.</returns>
        public static async Task<string> ForViewAsync(this YHtmlHelper htmlHelper, string viewName, ModuleDefinition module, object model) {

            Type viewType;
            string v = viewName.TrimEnd(PartialSuffix);
            if (!YetaWFComponentBaseStartup.GetViews().TryGetValue(v, out viewType))
                throw new InternalError($"View {viewName} not found");
            YetaWFViewBase view = (YetaWFViewBase)Activator.CreateInstance(viewType);
            view.SetRenderInfo(htmlHelper, module);

            Type moduleType = module.GetType();
            Type modelType = model == null ? typeof(object) : model.GetType();

            // Find RenderViewAsync/RenderPartialViewAsync
            bool partial = viewName.EndsWith(PartialSuffix);
            string methodName = partial ? nameof(IYetaWFView2<object, object>.RenderPartialViewAsync) : nameof(IYetaWFView2<object, object>.RenderViewAsync);
            MethodInfo miAsync = viewType.GetMethod(methodName, new Type[] { moduleType, modelType });
            if (miAsync == null)
                throw new InternalError($"View {viewName} ({viewType.FullName}) doesn't have a {methodName} method accepting a module type {moduleType.FullName} and model type {modelType.FullName}");

            // Add support for this view
            string shortName;
            if (view.Package.IsCorePackage || view.Package.Product.StartsWith("Components")) {
                shortName = v;
            } else {
                string[] s = v.Split(new char[] { '_' }, 3);
#if DEBUG
                if (s.Length != 3) throw new InternalError($"Invalid view name {viewName}");
                if (s[0] != view.Package.Domain) throw new InternalError($"Invalid domain in view name {viewName}");
                if (s[1] != view.Package.Product) throw new InternalError($"Invalid product in view name {viewName}");
#endif
                shortName = s[2];
            }
            await Manager.AddOnManager.TryAddAddOnNamedAsync(view.Package.AreaName, shortName);

            // Invoke RenderViewAsync/RenderPartialViewAsync
            Task<string> methStringTask = (Task<string>)miAsync.Invoke(view, new object[] { module, model });
            string yhtml = await methStringTask;
#if DEBUG
            if (!string.IsNullOrWhiteSpace(yhtml)) {
                if (yhtml.Contains("System.Threading.Tasks.Task"))
                    throw new InternalError($"View {viewName} contains System.Threading.Tasks.Task - check for missing \"await\" - generated HTML: \"{yhtml}\"");
                if (yhtml.Contains("Microsoft.AspNetCore.Mvc.Rendering"))
                    throw new InternalError($"View {viewName} contains Microsoft.AspNetCore.Mvc.Rendering - check for missing \"ToString()\" - generated HTML: \"{yhtml}\"");
            }
#endif
            return yhtml;
        }
    }
}
