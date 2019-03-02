/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Html;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {

    /// <summary>
    /// Encapsulates the HtmlString type to unify ASP.NET and ASP.NET Core source code.
    /// </summary>
    public class YHtmlString : HtmlString {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The string value.</param>
        public YHtmlString(string value) : base(value) { }
#if MVC6
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The string value.</param>
        public YHtmlString(HtmlString value) : base(value.AsString()) { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The string value.</param>
        public YHtmlString(IHtmlContent value) : base(value.AsString()) { }
#else
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The string value.</param>
        public YHtmlString(MvcHtmlString value) : base(value.ToString()) { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The string value.</param>
        public YHtmlString(HtmlString value) : base(value.ToString()) { }
#endif
        /// <summary>
        /// Constructor.
        /// </summary>
        public YHtmlString() : base("") { }
    }
}
