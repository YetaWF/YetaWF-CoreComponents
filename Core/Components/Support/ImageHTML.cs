﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.Extensions;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// This static class implements helper methods formatting HTML for images and sprites.
    /// </summary>
    public static class ImageHTML {

        /// <summary>
        /// Defines the URL used to display an image using the YetaWF ImageHttpHandler.
        /// </summary>
        public const string FormatUrlString = "/FileHndlr.image?Type={0}&Location={1}&Name={2}"; // Url for an image
        /// <summary>
        /// Defines the URL used to display an image using the ImageHttpHandler. The generated HTML includes the image size and allows increasing/decreasing the image.
        /// </summary>
        public const string FormatUrlWithSizeString = "/FileHndlr.image?Type={0}&Location={1}&Name={2}&Width={3}&Height={4}&Stretch={5}"; // Url for an image (resized to fit)

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static string FormatUrl(string imageType, string? location, string? name, int width = 0, int height = 0,
                string? CacheBuster = null, bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any,
                bool Stretch = false) {
            string url;
            if (width > 0 && height > 0) {
                url = string.Format(FormatUrlWithSizeString, Utility.UrlEncodeArgs(imageType), Utility.UrlEncodeArgs(location), Utility.UrlEncodeArgs(name),
                    width, height, Stretch ? "1" : "0");
            } else {
                url = string.Format(FormatUrlString, Utility.UrlEncodeArgs(imageType), Utility.UrlEncodeArgs(location), Utility.UrlEncodeArgs(name));
            }
            if (!string.IsNullOrWhiteSpace(CacheBuster))
                url += url.AddUrlCacheBuster(CacheBuster);
            url = Manager.GetCDNUrl(url);
            if (ExternalUrl) {
                // This is a local url, make the final url an external url, i.e., http(s)://
                if (url.StartsWith("/"))
                    url = Manager.CurrentSite.MakeUrl(url, PagePageSecurity: SecurityType);
            }
            return url;
        }

        /// <summary>
        /// Defines the predefined sprites used by the YetaWF Core package.
        /// </summary>
        /// <remarks>
        /// The key is the name of a predefined sprite. The value are the CSS classes used with an &lt;i&gt; tag to render the image.</remarks>
        public static readonly Dictionary<string, string> PredefSpriteIcons = new Dictionary<string, string> {
           { "#Add", "yic yic_add" },
           { "#Browse", "yic yic_browse" },
           { "#Collapse", "yic yic_collapse" },
           { "#Config", "yic yic_config" },
           { "#Display", "yic yic_display" },
           { "#Edit", "yic yic_edit" },
           { "#Expand", "yic yic_expand" },
           { "#Generic", "yic yic_generic" },
           { "#Help", "yic yic_help" },
           { "#Preview", "yic yic_preview" },
           { "#Remove", "yic yic_remove" },
           { "#RemoveLight", "yic yic_removelight" },
           { "#Warning", "yic yic_warning" },
        };

        public static string BuildKnownIcon(string url, string? title = null, string? alt = null, string? id = null, string? cssClass = null, string? name = null, Dictionary<string, string>? sprites = null) {

            title ??= string.Empty;
            sprites ??= PredefSpriteIcons;

            if (!string.IsNullOrWhiteSpace(name))
                name = $@" name='{Utility.HAE(name)}'";
            if (!string.IsNullOrWhiteSpace(id))
                id = $@" id='{Utility.HAE(id)}'";
            if (!string.IsNullOrWhiteSpace(title))
                title = $@" title='{Utility.HAE(title)}'";

            string extraCss = Manager.AddOnManager.CheckInvokedCssModule(cssClass);

            if (sprites.TryGetValue(url, out string? css)) {

                extraCss = CssManager.CombineCss(extraCss, Manager.AddOnManager.CheckInvokedCssModule(css));

                css = string.Empty;
                if (!string.IsNullOrWhiteSpace(extraCss))
                    css = $" class='{extraCss}'";
                return $@"<i{css}{title}{id}{name}></i>";

            } else {

                if (string.IsNullOrWhiteSpace(alt))
                    alt = title;
                if (!string.IsNullOrWhiteSpace(alt))
                    alt = $@" alt='{Utility.HAE(alt)}'";

                css = string.Empty;
                if (!string.IsNullOrWhiteSpace(extraCss))
                    css = $" class='{extraCss}'";

                return $@"<img src='{Utility.HAE(Manager.GetCDNUrl(url))}'{css}{title}{alt}{id}{name}>";
            }
        }
    }
}
