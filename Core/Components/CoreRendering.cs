using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
#if MVC6
#else
#endif

namespace YetaWF.Core.Components {

    public interface IYetaWFCoreRendering {
        Task<YHtmlString> RenderModuleMenuAsync(ModuleDefinition mod);
        Task<YHtmlString> RenderModuleLinksAsync(ModuleDefinition mod, ModuleAction.RenderModeEnum renderMode, string cssClass);
    }

    public class YetaWFCoreRendering {

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
    }
}
