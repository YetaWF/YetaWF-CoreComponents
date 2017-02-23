/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Mvc.Rendering;

namespace YetaWF.Core.Pages {

    public interface IRazorPageLifetime {
        void BeginRender(ViewContext context);
        void EndRender(ViewContext context);
    }
}
#else
#endif
