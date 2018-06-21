/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace YetaWF.Core.Pages {

    public interface IRazorPageLifetime { //$$$$ NEEDED?
        void BeginRender(ViewContext context);
        Task EndRenderAsync(ViewContext context);
    }
}
#else
#endif
