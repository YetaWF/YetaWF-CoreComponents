/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Support;
using YetaWF.Core.Pages;
using YetaWF.Core.Extensions;
#if MVC6
#else
using System.Web.Mvc;
#endif

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

        public static TagBuilder BuildKnownImageTag(string url, string title = null, string alt = null, string id = null, string cssClass = null) { //$$$remove
            title = title ?? "";
            alt = alt ?? title;
            TagBuilder tImg = new TagBuilder("img");
            if (!string.IsNullOrWhiteSpace(alt))
                tImg.MergeAttribute("alt", alt);
            if (!string.IsNullOrWhiteSpace(cssClass))
                tImg.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));
            if (!string.IsNullOrWhiteSpace(title))
                tImg.MergeAttribute("title", title);
            if (!string.IsNullOrWhiteSpace(id))
                tImg.Attributes.Add("id", id);
            tImg.MergeAttribute("src", Manager.GetCDNUrl(url));
            return tImg;
        }
        public static YTagBuilder BuildKnownImageYTag(string url, string title = null, string alt = null, string id = null, string cssClass = null) {
            title = title ?? "";
            alt = alt ?? title;
            YTagBuilder tImg = new YTagBuilder("img");
            if (!string.IsNullOrWhiteSpace(alt))
                tImg.MergeAttribute("alt", alt);
            if (!string.IsNullOrWhiteSpace(cssClass))
                tImg.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));
            if (!string.IsNullOrWhiteSpace(title))
                tImg.MergeAttribute("title", title);
            if (!string.IsNullOrWhiteSpace(id))
                tImg.Attributes.Add("id", id);
            tImg.MergeAttribute("src", Manager.GetCDNUrl(url));
            return tImg;
        }
    }
}
