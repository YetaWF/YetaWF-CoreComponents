﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public static class HtmlStringExtender  {

        public static readonly HtmlString Empty = new HtmlString("");

    }
}