/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Modules;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Support;
using YetaWF.Core.Views;

namespace YetaWF.Core.Endpoints {

    public class PartialView {

        public class PartialViewData {
            public YetaWFManager.UniqueIdInfo __UniqueIdCounters { get; set; } = null!;
            public Guid __ModuleGuid { get; set; } // The module for which the partial view is rendered
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
        public static async Task<IResult> RenderPartialView(HttpContext context, string viewName, ModuleDefinition? module, PartialViewData? pvData, object? model, string contentType, 
            bool PureContent = false, bool PartialForm = true,
            ScriptBuilder? Script = null) {

            if (pvData is null) {
                // Manager.UniqueIdCounters set by caller
            } else {
                if (module is null)
                    module = await YetaWFEndpoints.GetModuleAsync(pvData.__ModuleGuid);
                Manager.UniqueIdCounters = pvData.__UniqueIdCounters;
            }
            if (module is null) throw new InternalError("Module is required");
            Manager.NextUniqueIdPrefix();// get the next unique id prefix (so we don't have any conflicts when replacing modules)

            ModuleDefinition? oldMod = Manager.CurrentModule;
            Manager.CurrentModule = module;

            string viewHtml;
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb)) {

                YHtmlHelper htmlHelper = new YHtmlHelper(module?.ModelState);
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

                if (!PureContent)
                    viewHtml = await PostRenderAsync(htmlHelper, context, module, viewHtml, Script, PartialForm);
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

        private static readonly Regex reStartDiv = new Regex(@"\s*<"); // first html
        private static readonly Regex reEndDiv = new Regex(@"</div>\s*$"); // very last div

        private static async Task<string> PostRenderAsync(YHtmlHelper htmlHelper, HttpContext context, ModuleDefinition? module, string viewHtml, ScriptBuilder? Script, bool PartialForm) {

            HttpResponse response = context.Response;

            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);

            //$$$ if (Manager.CurrentPage != null) Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);
            if (module != null)
                Manager.AddOnManager.AddExplicitlyInvokedModules(module.ReferencedModules);

            //if (ForcePopup)
            //    viewHtml += "<script>YVolatile.Basics.ForcePopup=true;</script>";

            viewHtml += (await htmlHelper.RenderReferencedModule_AjaxAsync()).ToString();
            viewHtml = await PostProcessView.ProcessAsync(htmlHelper, module, viewHtml, PartialForm: PartialForm);

            if (Script != null)
                Manager.ScriptManager.AddLastWhenReadyOnce(Script);

            if (Manager.UniqueIdCounters.IsTracked)
                Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdCounters", Manager.UniqueIdCounters);

            // add generated scripts
            string js = await Manager.ScriptManager.RenderVolatileChangesAsync() ?? "";
            js += await Manager.ScriptManager.RenderAjaxAsync() ?? "";

            viewHtml = reStartDiv.Replace(viewHtml, "<", 1);
            viewHtml = reEndDiv.Replace(viewHtml, js + "</div>", 1);

            // DEBUG: viewHtml is the complete response to the Ajax request

            viewHtml = WhiteSpaceResponseFilter.Compress(viewHtml);
            return viewHtml;
        }
    }
}

