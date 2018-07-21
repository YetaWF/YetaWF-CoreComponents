﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
            await Manager.AddOnManager.TryAddAddOnNamedAsync(view.Package.Domain, view.Package.Product, shortName);

            // Invoke RenderViewAsync/RenderPartialViewAsync
            Task<YHtmlString> methStringTask = (Task<YHtmlString>)miAsync.Invoke(view, new object[] { module, model });
            YHtmlString yhtml = await methStringTask;
#if DEBUG
            string html = yhtml.ToString();
            if (html.ToString().Contains("System.Threading.Tasks.Task"))
                throw new InternalError($"View {viewName} contains System.Threading.Tasks.Task - check for missing \"await\" - generated HTML: \"{html}\"");
            if (html.Contains("Microsoft.AspNetCore.Mvc.Rendering"))
                throw new InternalError($"View {viewName} contains Microsoft.AspNetCore.Mvc.Rendering - check for missing \"ToString()\" - generated HTML: \"{html}\"");
#endif
            return yhtml;
        }
    }
}