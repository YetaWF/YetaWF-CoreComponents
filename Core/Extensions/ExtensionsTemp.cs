/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web;
using System.Web.Mvc;

namespace YetaWF.Core.Extensions {
    public static class ExtensionsTemp {
        // already exists in future versions  >MVC5
        public static IHtmlString ToHtmlString(this TagBuilder tag, TagRenderMode renderMode) {
            return new HtmlString(tag.ToString(renderMode));
        }
    }
}
