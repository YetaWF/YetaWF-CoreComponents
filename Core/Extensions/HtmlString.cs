/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web;
#endif

namespace YetaWF.Core.Extensions {

    public static class HtmlStringExtensions {

        public static bool IsEmpty(this HtmlString htmlString) {
            return htmlString.ToString().Length == 0;
        }
    }
}
