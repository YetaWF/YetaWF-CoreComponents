/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    public interface IYetaWFCoreRendering {
        Package GetImplementingPackage();
        Task AddStandardAddOnsAsync();
        Task AddSkinAddOnsAsync();
        Task AddFormsAddOnsAsync();
        Task AddPopupsAddOnsAsync();
#if MVC6
        Task<YHtmlString> RenderViewAsync(IHtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss);
#else
        Task<YHtmlString> RenderViewAsync(HtmlHelper htmlHelper, ModuleDefinition module, string viewHtml, bool UsePartialFormCss);
#endif
        Task<YHtmlString> RenderModuleLinksAsync(ModuleDefinition mod, ModuleAction.RenderModeEnum renderMode, string cssClass);
        Task<YHtmlString> RenderModuleMenuAsync(ModuleDefinition mod);

        Task<YHtmlString> RenderMenuListAsync(MenuList menu, string id = null, string cssClass = null,
#if MVC6
            IHtmlHelper HtmlHelper = null
#else
            HtmlHelper HtmlHelper = null
#endif
            );

        Task<YHtmlString> RenderModuleActionAsync(ModuleAction action, ModuleAction.RenderModeEnum mode, string id);
        Task<YHtmlString> RenderFormButtonAsync(FormButton formButton);
    }

    public class YetaWFCoreRendering {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public static IYetaWFCoreRendering Render {
            get {
                if (_render == null) throw new InternalError("No IYetaWFCoreRendering handler installed");
                return _render;
            }
            set {
                if (_render != null) throw new InternalError("IYetaWFCoreRendering handler already installed");
                _render = value;
            }
        }
        private static IYetaWFCoreRendering _render;

        public static async Task AddTemplateAsync(string uiHintTemplate) {
            if (YetaWFComponentBaseStartup.GetComponentsEdit().ContainsKey(uiHintTemplate) || YetaWFComponentBaseStartup.GetComponentsDisplay().ContainsKey(uiHintTemplate)) {
                Package package = Render.GetImplementingPackage();
                await Manager.AddOnManager.AddTemplateAsync(package.AreaName, uiHintTemplate);
            }
        }
    }
}
