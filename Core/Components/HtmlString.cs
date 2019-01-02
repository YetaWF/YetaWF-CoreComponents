/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    public class YHtmlString : HtmlString {
        public YHtmlString(string value) : base(value) { }
#if MVC6
        public YHtmlString(HtmlString value) : base(value.AsString()) { }
        public YHtmlString(IHtmlContent value) : base(value.AsString()) { }
#else
        public YHtmlString(MvcHtmlString value) : base(value.ToString()) { }
        public YHtmlString(HtmlString value) : base(value.ToString()) { }
#endif
        public YHtmlString() : base("") { }
    }
}
