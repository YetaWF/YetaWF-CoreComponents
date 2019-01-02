/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Image;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
using YetaWF.Core.Components;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views {

    public static class PostProcessView {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        internal static async Task<string> ProcessAsync(IHtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss = true) {
#else
        internal static async Task<string> ProcessAsync(HtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss = true) {
#endif
            viewHtml = (await YetaWFCoreRendering.Render.RenderViewAsync(htmlHelper, module, viewHtml, UsePartialFormCss)).ToString();

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            viewHtml = vars.ReplaceModuleVariables(module, viewHtml);

            viewHtml = ProcessImages(viewHtml);
            return viewHtml;
        }

        private static string ProcessImages(string viewHtml) {
            if (!Manager.IsPostRequest) return viewHtml; // we'll handle it in RazorPage::PostProcessHtml
            if (Manager.CurrentSite.CanUseCDN || Manager.CurrentSite.CanUseStaticDomain)
                return ImageSupport.ProcessImagesAsCDN(viewHtml);
            return viewHtml;
        }
    }
}
