﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Controllers.Shared {

    // Inspired by http://stackoverflow.com/questions/26916664/html-action-in-asp-net-core

    public static class HtmlHelperActionExtensions {

        public static IHtmlContent Action(this IHtmlHelper helper, ModuleDefinition module, string action, object parameters = null) {
            var controller = (string)helper.ViewContext.RouteData.Values["controller"];
            return Action(helper, module, action, controller, parameters);
        }

        public static IHtmlContent Action(this IHtmlHelper helper, ModuleDefinition module, string action, string controller, object parameters = null) {
            var area = (string)helper.ViewContext.RouteData.Values["area"];
            return Action(helper, module, action, controller, area, parameters);
        }

        public static IHtmlContent Action(this IHtmlHelper helper, ModuleDefinition module, string action, string controller, string area, object parameters = null) {
            if (action == null)
                throw new ArgumentNullException("action");
            if (controller == null)
                throw new ArgumentNullException("controller");
            if (area == null)
                throw new ArgumentNullException("area");
            var task = RenderActionAsync(helper, module, action, controller, area, parameters);
            return task.Result;
        }

        private static async Task<IHtmlContent> RenderActionAsync(this IHtmlHelper helper, ModuleDefinition module, string action, string controller, string area, object parameters = null) {
            // fetching required services for invocation
            var httpContext = YetaWFManager.Manager.CurrentContext;
            IActionInvokerFactory actionInvokerFactory = (IActionInvokerFactory)YetaWFManager.ServiceProvider.GetService(typeof(IActionInvokerFactory));
            IActionSelectorDecisionTreeProvider actionSelector = (IActionSelectorDecisionTreeProvider)YetaWFManager.ServiceProvider.GetService(typeof(IActionSelectorDecisionTreeProvider));

            // creating new action invocation context
            var routeData = new RouteData();
            var routeParams = new RouteValueDictionary(parameters ?? new { });
            var routeValues = new RouteValueDictionary(new { area = area, controller = controller, action = action });

            foreach (var router in helper.ViewContext.RouteData.Routers)
                routeData.PushState(router, null, null);

            routeData.PushState(null, routeValues, null);
            routeData.PushState(null, routeParams, null);

            // We need to defeat caching of the current UrlHelper for the new context otherwise areas don't work
            object oldUrlHelper = null;
            httpContext.Items.TryGetValue(typeof(IUrlHelper), out oldUrlHelper);
            httpContext.Items.Remove(typeof(IUrlHelper));

            var actionDescriptor = actionSelector.DecisionTree.Select(routeValues).FirstOrDefault();
            string content = null;
            if (actionDescriptor != null) {

                ActionContext actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

                // invoke action and retrieve the response body
                IActionInvoker invoker = actionInvokerFactory.CreateInvoker(actionContext);
                if (invoker != null) {
                    Stream body = httpContext.Response.Body;
                    using (httpContext.Response.Body = new MemoryStream()) {
                        await invoker.InvokeAsync().ContinueWith(task => {
                            if (task.IsFaulted) {
                                content = ModuleDefinition.ProcessModuleError(task.Exception, module.ModuleName).ToString();
                            } else if (task.IsCompleted) {
                                httpContext.Response.Body.Position = 0;
                                using (var reader = new StreamReader(httpContext.Response.Body))
                                    content = reader.ReadToEnd();
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

            return new HtmlString(content);
        }
    }
}

#else
#endif