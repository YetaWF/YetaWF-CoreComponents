/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using YetaWF.Core.Image;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace  YetaWF.Core.Views.Shared {

    public class Image<TModel> : RazorTemplate<TModel> { }

    public static class ImageHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ImageHelper), name, defaultValue, parms); }
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static TagBuilder BuildKnownImageTag(string url, string title = null, string alt = null, string id = null, string cssClass = null) {
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
            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("//")) {
                string file = YetaWFManager.UrlToPhysical(url);
                try {
                    System.Drawing.Image img = System.Drawing.Image.FromFile(file);
                    tImg.MergeAttribute("width", img.Width.ToString());
                    tImg.MergeAttribute("height", img.Height.ToString());
                    url += "?__yVrs=" + (File.GetLastWriteTime(file).Ticks / TimeSpan.TicksPerSecond).ToString();
                } catch { }
            }
            tImg.MergeAttribute("src", Manager.GetCDNUrl(url));
            return tImg;
        }

        public static MvcHtmlString RenderImage(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
            // the upload control
            FileUpload1 info = new FileUpload1() {
                SaveURL = YetaWFManager.UrlFor(typeof(YetaWF.Core.Controllers.Shared.ImageHelperController), "SaveImage", new { __ModuleGuid = Manager.CurrentModule.ModuleGuid }),
                RemoveURL = YetaWFManager.UrlFor(typeof(YetaWF.Core.Controllers.Shared.ImageHelperController), "RemoveImage", new { __ModuleGuid = Manager.CurrentModule.ModuleGuid }),
            };
            return htmlHelper.EditorFor(x => info, UIHintAttribute.TranslateHint("FileUpload1"));
        }
        public static MvcHtmlString RenderImagePreview(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0,
                string CacheBuster = null,
                bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any,
                bool ForceHttpHandler = false) {

            string imageType = htmlHelper.GetControlInfo<string>(name, "ImageType");
            if (string.IsNullOrWhiteSpace(imageType)) throw new InternalError("No ImageType specified");

            int width, height;
            htmlHelper.TryGetControlInfo<int>(name, "Width", out width);
            htmlHelper.TryGetControlInfo<int>(name, "Height", out height);

            bool showMissing = htmlHelper.GetControlInfo<bool>(name, "ShowMissing", true);
            if (string.IsNullOrWhiteSpace(model) && !showMissing) return MvcHtmlString.Empty;

            string imgTag = RenderImagePreview(imageType, width, height, model, CacheBuster: CacheBuster, ExternalUrl: ExternalUrl, SecurityType: SecurityType, ForceHttpHandler: ForceHttpHandler);

            bool linkToImage = htmlHelper.GetControlInfo<bool>(name, "LinkToImage", false);
            if (linkToImage) {
                TagBuilder link = new TagBuilder("a");
                string imgUrl = FormatUrl(imageType, null, model, CacheBuster: CacheBuster, ForceHttpHandler: ForceHttpHandler);
                link.MergeAttribute("href", Manager.GetCDNUrl(imgUrl));
                link.MergeAttribute("target", "_blank");
                link.InnerHtml = imgTag;
                return MvcHtmlString.Create(link.ToString());
            } else
                return MvcHtmlString.Create(imgTag);
        }
        public static string RenderImagePreview(string imageType, int width, int height, string model,
                string CacheBuster = null, bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any, bool ForceHttpHandler = false) {
            string url = FormatUrl(imageType, null, model, width, height, CacheBuster: CacheBuster, ExternalUrl: ExternalUrl, SecurityType: SecurityType, ForceHttpHandler: ForceHttpHandler);
            TagBuilder img = new TagBuilder("img");
            img.AddCssClass("t_preview");
            img.Attributes.Add("src", Manager.GetCDNUrl(url));
            img.Attributes.Add("alt", __ResStr("altPreview", "Preview Image"));
            return img.ToString(TagRenderMode.StartTag);
        }

        public static MvcHtmlString RenderImageAttributes(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0) {
            if (model == null) return MvcHtmlString.Empty;
            System.Drawing.Size size = ImageSupport.GetImageSize(model);
            if (size.IsEmpty) return MvcHtmlString.Empty;

            return MvcHtmlString.Create(__ResStr("imgAttr", "{0} x {1} (w x h)", size.Width, size.Height));
        }
        public static MvcHtmlString RenderImagePreviewDisplay(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0, string CacheBuster = null, bool ForceHttpHandler = false) {
            return htmlHelper.RenderImagePreview(name, model, CacheBuster: CacheBuster, ForceHttpHandler: ForceHttpHandler);
        }
        public static string FormatUrl(string imageType, string location, string name, int width = 0, int height = 0,
                string CacheBuster = null, bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any,
                bool Stretch = false,
                bool ForceHttpHandler = false) { // TODO: research whether can we get rid of ForceHttpHandler
            string url;
            if (width > 0 && height > 0) {
                if (ForceHttpHandler)
                    url = string.Format(Addons.Templates.Image.FormatUrlWithSizeForceHttpHandler, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name),
                        width, height, Stretch ? "1" : "0");
                else
                    url = string.Format(Addons.Templates.Image.FormatUrlWithSize, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name),
                        width, height, Stretch ? "1" : "0");
            } else {
                if (ForceHttpHandler)
                    url = string.Format(Addons.Templates.Image.FormatUrlForceHttpHandler, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name));
                else
                    url = string.Format(Addons.Templates.Image.FormatUrl, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name));
            }
            if (ExternalUrl) {
                // This is a local url, make the final url an external url, i.e., http(s)://
                if (url.StartsWith("/File.image") || url.StartsWith("/FileHndlr.image"))
                    url = Manager.GetCDNUrl(url);
                if (url.StartsWith("/"))
                    url = Manager.CurrentSite.MakeUrl(url, PagePageSecurity: SecurityType);
            }
            if (!string.IsNullOrWhiteSpace(CacheBuster)) {
                url += url.Contains("?") ? "&" : "?";
                url += string.Format("__yVrs={0}", CacheBuster);
            }
            return url;
        }
    }
}
