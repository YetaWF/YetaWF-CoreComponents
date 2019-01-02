/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using YetaWF.Core.Pages;
using YetaWF.Core.Extensions;
using System.Collections.Generic;

namespace YetaWF.Core.Components {

    public static class ImageHTML {

        public const string FormatUrlString = "/FileHndlr.image?Type={0}&Location={1}&Name={2}"; // Url for an image
        public const string FormatUrlWithSizeString = "/FileHndlr.image?Type={0}&Location={1}&Name={2}&Width={3}&Height={4}&Stretch={5}"; // Url for an image (resized to fit)

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static string FormatUrl(string imageType, string location, string name, int width = 0, int height = 0,
                string CacheBuster = null, bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any,
                bool Stretch = false) {
            string url;
            if (width > 0 && height > 0) {
                url = string.Format(FormatUrlWithSizeString, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name),
                    width, height, Stretch ? "1" : "0");
            } else {
                url = string.Format(FormatUrlString, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name));
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

        public static Dictionary<string, string> PredefSpriteIcons = new Dictionary<string, string> {
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

        public static string BuildKnownIcon(string url, string title = null, string id = null, string cssClass = null, string name = null, Dictionary<string, string> sprites = null) {

            title = title ?? "";
            sprites = sprites ?? PredefSpriteIcons;

            string css;
            if (sprites.TryGetValue(url, out css)) {

                YTagBuilder tIcon = new YTagBuilder("i");
                tIcon.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(css));
                if (!string.IsNullOrWhiteSpace(cssClass))
                    tIcon.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));
                if (!string.IsNullOrWhiteSpace(title))
                    tIcon.MergeAttribute("title", title);
                if (!string.IsNullOrWhiteSpace(id))
                    tIcon.Attributes.Add("id", id);
                if (!string.IsNullOrWhiteSpace(name))
                    tIcon.Attributes.Add("name", name);

                return tIcon.ToString(YTagRenderMode.Normal);

            } else {

                YTagBuilder tImg = new YTagBuilder("img");
                if (!string.IsNullOrWhiteSpace(title)) {
                    tImg.MergeAttribute("alt", title);
                    tImg.MergeAttribute("title", title);
                }
                if (!string.IsNullOrWhiteSpace(cssClass))
                    tImg.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));
                if (!string.IsNullOrWhiteSpace(id))
                    tImg.Attributes.Add("id", id);
                if (!string.IsNullOrWhiteSpace(name))
                    tImg.Attributes.Add("name", name);

                tImg.MergeAttribute("src", Manager.GetCDNUrl(url));

                return tImg.ToString(YTagRenderMode.StartTag);
            }
        }
    }
}
