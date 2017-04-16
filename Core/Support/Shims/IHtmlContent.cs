/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;

namespace YetaWF.Core.Support {

    public static class IHtmlContentExtender  {

        public static string AsString(this IHtmlContent iHtmlContent) {
            using (System.IO.StringWriter writer = new System.IO.StringWriter()) {
                iHtmlContent.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }
    }
}

#else
#endif
