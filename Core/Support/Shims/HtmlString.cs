/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public static class HtmlStringExtender  {
#if MVC6
#else
        public static readonly HtmlString Empty = new HtmlString("");
#endif
    }
}
