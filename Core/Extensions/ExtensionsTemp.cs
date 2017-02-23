/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using System.Web;
using System.Web.Mvc;

namespace YetaWF.Core.Extensions {
    public static class ExtensionsTemp {
        // already exists in future versions  >MVC 5
        public static IHtmlString ToHtmlString(this TagBuilder tag, TagRenderMode renderMode) {
            return new HtmlString(tag.ToString(renderMode));
        }
    }
}

#endif
