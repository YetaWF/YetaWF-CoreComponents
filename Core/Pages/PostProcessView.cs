/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Image;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views {

    internal static class PostProcessView {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        internal static async Task<string> ProcessAsync(YHtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss = true) {

            viewHtml = await YetaWFCoreRendering.Render.RenderViewAsync(htmlHelper, module, viewHtml, UsePartialFormCss);

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            viewHtml = vars.ReplaceModuleVariables(module, viewHtml);

            viewHtml = ProcessImages(viewHtml);
            return viewHtml;
        }

        private static string ProcessImages(string viewHtml) {
            if (!Manager.IsPostRequest) return viewHtml; // we'll handle it in PageProcessing.PostProcessHtml
            if (Manager.CurrentSite.CanUseCDN || Manager.CurrentSite.CanUseStaticDomain)
                return ImageSupport.ProcessImagesAsCDN(viewHtml);
            return viewHtml;
        }
    }
}
