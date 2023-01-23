/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints {

    public class PartialView {

        public class PartialViewData {
            public YetaWFManager.UniqueIdInfo __UniqueIdInfo { get; set; } = null!;
            public Guid __ModuleGuid { get; set; } // The module for which the partial view is rendered
            public string __RequestVerificationToken { get; set; } = null!;
        }

        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        //public ModuleDefinition Module { get; set; } = null!;

        /// <summary>
        /// Renders a partial view.
        /// </summary>
        /// <param name="context">The HttpContext.</param>
        /// <param name="viewName">The name of the partial view.</param>
        /// <param name="model">The model.</param>
        /// <param name="contentType">The content type.</param>
        /// <returns>Returns the HTML for the requested partial view.</returns>
        public static async Task<IResult> RenderPartialView(HttpContext context, string viewName, ModuleDefinition? module, PartialViewData pvData, object? model, string contentType) {

            Manager.UniqueIdCounters = pvData.__UniqueIdInfo;
            Manager.NextUniqueIdPrefix();// get the next unique id prefix (so we don't have any conflicts when replacing modules)

            ModuleDefinition? oldMod = Manager.CurrentModule;
            if (module is null)
                Manager.CurrentModule = await YetaWFEndpoints.GetModuleAsync(pvData.__ModuleGuid);
            else
                Manager.CurrentModule = module;

            string viewHtml;
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb)) {

                YHtmlHelper htmlHelper = new YHtmlHelper(new Microsoft.AspNetCore.Mvc.ActionContext(), null); //$$$$$$ context.ModelState);

                bool inPartialView = Manager.InPartialView;//$$$ is this needed
                Manager.InPartialView = true;
                bool wantFocus = Manager.WantFocus;
                Manager.WantFocus = false;//$$$ Module.WantFocus;
                try {
                    viewHtml = await htmlHelper.ForViewAsync(viewName, Manager.CurrentModule, model);
                } catch (Exception) {
                    throw;
                } finally {
                    Manager.InPartialView = inPartialView;
                    Manager.WantFocus = wantFocus;
                }
            }
#if DEBUG
            if (sb.Length > 0)
                throw new InternalError($"View {viewName} wrote output using HtmlHelper, which is not supported - All output must be rendered using ForViewAsync and returned as a string - output rendered: \"{sb.ToString()}\"");
#endif

            Manager.CurrentModule = oldMod;

            return Results.Text(viewHtml, contentType);
        }

        /// <summary>
        /// Returns the module definition YetaWF.Core.Modules.ModuleDefinition for the requested module Guid.
        /// </summary>
        protected static async Task<ModuleDefinition> GetModuleAsync(Guid moduleGuid, string pvName) {
            ModuleDefinition? mod = await ModuleDefinition.LoadAsync(moduleGuid);
            if (mod == null)
                throw new InternalError($"No ModuleDefinition available in partial view {pvName}");
            return mod;
        }
    }
}

