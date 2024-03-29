﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using System;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    // Inspired by http://stackoverflow.com/questions/26916664/html-action-in-asp-net-core

    internal static class HtmlHelperActionExtensions {

        public static async Task<ActionInfo> ActionAsync(this YHtmlHelper htmlHelper, ModuleDefinition module, string action, object? parameters = null) {
            var controller = (string)htmlHelper.RouteData.Values["controller"] !;
            return await ActionAsync(htmlHelper, module, action, controller, parameters);
        }

        public static async Task<ActionInfo> ActionAsync(this YHtmlHelper htmlHelper, ModuleDefinition module, string action, string controller, object? parameters = null) {
            var area = (string)htmlHelper.RouteData.Values["area"] !;
            return await ActionAsync(htmlHelper, module, action, controller, area, parameters);
        }

        public static async Task<ActionInfo> ActionAsync(this YHtmlHelper htmlHelper, ModuleDefinition module, string action, string controller, string area, object? parameters = null) {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (area == null)
                throw new ArgumentNullException("area");
            return await RenderActionAsync(htmlHelper, module, action, controller, area, parameters);
        }

        private static async Task<ActionInfo> RenderActionAsync(this YHtmlHelper htmlHelper, ModuleDefinition module, string action, string controller, string area, object? parameters = null) {
            // fetching required services for invocation
            var httpContext = YetaWFManager.Manager.CurrentContext;
            IActionInvokerFactory actionInvokerFactory = (IActionInvokerFactory)YetaWFManager.ServiceProvider.GetService(typeof(IActionInvokerFactory)) !;
            IActionSelector actionSelector = (IActionSelector)YetaWFManager.ServiceProvider.GetService(typeof(IActionSelector)) !;

            // creating new action invocation context
            var routeData = new RouteData();
            var routeParams = new RouteValueDictionary(parameters ?? new { });
            var routeValues = new RouteValueDictionary(new { area = area, controller = controller, action = action, ModuleDefinition = module });

            foreach (var router in htmlHelper.RouteData.Routers)
                routeData.PushState(router, null, null);

            routeData.PushState(null, routeValues, null);
            routeData.PushState(null, routeParams, null);

            // We need to defeat caching of the current UrlHelper for the new context otherwise areas don't work
            httpContext.Items.TryGetValue(typeof(IUrlHelper), out object? oldUrlHelper);
            httpContext.Items.Remove(typeof(IUrlHelper));

            // Microsoft.AspNetCore.Routing.RouteContext
            RouteContext routeContext = new RouteContext(httpContext) { RouteData = routeData };
            var candidates = actionSelector.SelectCandidates(routeContext);
            if (candidates == null || candidates.Count == 0)
                throw new InternalError("No route candidates found - /{0}/{1}/{2}", area, controller, action);

            ActionInfo info = new ActionInfo();

            ActionDescriptor? actionDescriptor = actionSelector.SelectBestCandidate(routeContext, candidates);
            if (actionDescriptor != null) {

                ActionContext actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

                // invoke action and retrieve the response body
                IActionInvoker? invoker = actionInvokerFactory.CreateInvoker(actionContext);
                if (invoker != null) {
                    Stream body = httpContext.Response.Body;
                    using (httpContext.Response.Body = new MemoryStream()) {
                        await invoker.InvokeAsync().ContinueWith(async task => {
                            if (task.IsFaulted) {
                                info.HTML = ModuleDefinition.ProcessModuleError(task.Exception!, module.ModuleName).ToString();
                                info.Failed = true;
                            } else if (task.IsCompleted) {
                                httpContext.Response.Body.Position = 0;
                                using (var reader = new StreamReader(httpContext.Response.Body)) {
                                    info.HTML = await reader.ReadToEndAsync();
                                }
                            }
                        });
                    }
                    httpContext.Response.Body = body;
                }
            } else  {
                throw new InternalError("No route found - /{0}/{1}/{2}", area, controller, action);
            }

            // restore original urlhelper
            httpContext.Items.Remove(typeof(IUrlHelper));
            httpContext.Items.Add(typeof(IUrlHelper), oldUrlHelper);

            return info;
        }
    }
}
