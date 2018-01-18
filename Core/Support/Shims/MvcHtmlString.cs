/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

#if MVC6
#else
    public static class MvcHtmlStringExtender {
        public static string AsString(this MvcHtmlString mvcHtmlString) {
            return mvcHtmlString.ToString();
        }
    }
#endif
}