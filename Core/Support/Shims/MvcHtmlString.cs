/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    // TODO: Eliminate use of MvcHtmlString

#if MVC6
    public sealed class MvcHtmlString : HtmlString {

        public static new MvcHtmlString Empty = new MvcHtmlString("");

        public MvcHtmlString(string value) : base(value) { }

        public static MvcHtmlString Create(string value) {
            return new MvcHtmlString(value);
        }
        public static MvcHtmlString Create(IHtmlContent iHtmlContent) {
            if (iHtmlContent == null) return MvcHtmlString.Empty;
            return new MvcHtmlString(iHtmlContent.AsString());
        }
        public static bool IsNullOrEmpty(MvcHtmlString value) {
            return (value == null || value.Value.Length == 0);
        }
        public static implicit operator string(MvcHtmlString mvcHtmlString) {
            return mvcHtmlString.Value;
        }
    }
#else
    public static class MvcHtmlStringExtender {
        public static string AsString(this MvcHtmlString mvcHtmlString) {
            return mvcHtmlString.ToString();
        }
    }
#endif
}
