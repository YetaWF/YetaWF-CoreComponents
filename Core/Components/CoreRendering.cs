﻿using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using System.Web;
#if MVC6
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    public interface IYetaWFCoreRendering {
        Package GetImplementingPackage();
        Task<YHtmlString> RenderModuleLinksAsync(ModuleDefinition mod, ModuleAction.RenderModeEnum renderMode, string cssClass);
        Task<YHtmlString> RenderModuleMenuAsync(ModuleDefinition mod);

        Task<YHtmlString> RenderMenuListAsync(MenuList menu, string id = null, string cssClass = null,
#if MVC6
            IHtmlHelper HtmlHelper = null
#else
            HtmlHelper HtmlHelper = null
#endif
            );

        Task<YHtmlString> RenderModuleActionAsync(ModuleAction action, ModuleAction.RenderModeEnum mode, string id,
            bool NewWindow = false, bool Popup = false, bool PopupEdit = false, bool Post = false, bool Nothing = false, bool OuterWindow = false);
        Task<YHtmlString> RenderFormButtonAsync(FormButton formButton);
    }

    public class YetaWFCoreRendering {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

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
                await Manager.AddOnManager.AddTemplateAsync(package.Domain, package.Product, uiHintTemplate);
            }
        }
    }
}
