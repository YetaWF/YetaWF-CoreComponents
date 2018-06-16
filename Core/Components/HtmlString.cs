﻿#if MVC6
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public class YHtmlString : HtmlString {
        public YHtmlString(string value) : base(value) { }
        public YHtmlString(HtmlString value) : base(value.ToString()) { }
        public YHtmlString() : base("") { }
    }
}
