/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using YetaWF.Core.Extensions;
using YetaWF.Core.Image;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
#endif

namespace YetaWF.Core.Views.Shared {

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
            if (!url.IsAbsoluteUrl()) {
                string file = YetaWFManager.UrlToPhysical(url);
                try {
                    //PERFORMANCE: This is a significant hit on performance
                    //System.Drawing.Image img = System.Drawing.Image.FromFile(file);
                    //tImg.MergeAttribute("width", img.Width.ToString());
                    //tImg.MergeAttribute("height", img.Height.ToString());
                    url += "?__yVrs=" + (File.GetLastWriteTime(file).Ticks / TimeSpan.TicksPerSecond).ToString();
                } catch { }
            }
            tImg.MergeAttribute("src", Manager.GetCDNUrl(url));
            return tImg;
        }
#if MVC6
        public static HtmlString RenderImageEdit(this IHtmlHelper<string> htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#else
        public static HtmlString RenderImageEdit(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null) {
#endif
            // the upload control
            FileUpload1 info = new FileUpload1() {
                SaveURL = YetaWFManager.UrlFor(typeof(YetaWF.Core.Controllers.Shared.ImageHelperController), "SaveImage", new { __ModuleGuid = Manager.CurrentModule.ModuleGuid }),
                RemoveURL = YetaWFManager.UrlFor(typeof(YetaWF.Core.Controllers.Shared.ImageHelperController), "RemoveImage", new { __ModuleGuid = Manager.CurrentModule.ModuleGuid }),
            };
#if MVC6
            return new HtmlString(htmlHelper.EditorFor(x => info, UIHintAttribute.TranslateHint("FileUpload1")).AsString());
#else
            return htmlHelper.EditorFor(x => info, UIHintAttribute.TranslateHint("FileUpload1"));
#endif
        }
#if MVC6
        public static HtmlString RenderImage(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0,
#else
        public static HtmlString RenderImage(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0,
#endif
                string CacheBuster = null,
                string Alt = null,
                bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any,
                bool ForceHttpHandler = false) {

            string imageType = htmlHelper.GetControlInfo<string>(name, "ImageType");
            int width, height;
            htmlHelper.TryGetControlInfo<int>(name, "Width", out width);
            htmlHelper.TryGetControlInfo<int>(name, "Height", out height);

            if (string.IsNullOrWhiteSpace(imageType) && model != null && model.IsAbsoluteUrl()) {

                if (width != 0 || height != 0) throw new InternalError("Can't use Width or Height with external Urls");

                TagBuilder img = new TagBuilder("img");
                img.Attributes.Add("src", model);
                img.Attributes.Add("alt", __ResStr("altImage", Alt??"Image"));
                return img.ToHtmlString(TagRenderMode.Normal);

            } else {

                if (string.IsNullOrWhiteSpace(imageType)) throw new InternalError("No ImageType specified");

                bool showMissing = htmlHelper.GetControlInfo<bool>(name, "ShowMissing", true);
                if (string.IsNullOrWhiteSpace(model) && !showMissing) return HtmlStringExtender.Empty;

                string imgTag = RenderImage(imageType, width, height, model, CacheBuster: CacheBuster, Alt:Alt, ExternalUrl: ExternalUrl, SecurityType: SecurityType, ForceHttpHandler: ForceHttpHandler);

                bool linkToImage = htmlHelper.GetControlInfo<bool>(name, "LinkToImage", false);
                if (linkToImage) {
                    TagBuilder link = new TagBuilder("a");
                    string imgUrl = FormatUrl(imageType, null, model, CacheBuster: CacheBuster, ForceHttpHandler: ForceHttpHandler);
                    link.MergeAttribute("href", Manager.GetCDNUrl(imgUrl));
                    link.MergeAttribute("target", "_blank");
                    link.SetInnerHtml(imgTag);
                    return link.ToHtmlString(TagRenderMode.Normal);
                } else
                    return new HtmlString(imgTag);
            }
        }
        public static string RenderImage(string imageType, int width, int height, string model,
                string CacheBuster = null, string Alt = null, bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any, bool ForceHttpHandler = false) {
            string url = FormatUrl(imageType, null, model, width, height, CacheBuster: CacheBuster, ExternalUrl: ExternalUrl, SecurityType: SecurityType, ForceHttpHandler: ForceHttpHandler);
            TagBuilder img = new TagBuilder("img");
            img.AddCssClass("t_preview");
            img.Attributes.Add("src", Manager.GetCDNUrl(url));
            img.Attributes.Add("alt", Alt??"Image");
            return img.ToString(TagRenderMode.StartTag);
        }
#if MVC6
        public static HtmlString RenderImageAttributes(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0) {
#else
        public static HtmlString RenderImageAttributes(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0) {
#endif
            if (model == null) return HtmlStringExtender.Empty;
            System.Drawing.Size size = ImageSupport.GetImageSize(model);
            if (size.IsEmpty) return HtmlStringExtender.Empty;

            return new HtmlString(__ResStr("imgAttr", "{0} x {1} (w x h)", size.Width, size.Height));
        }
#if MVC6
        public static HtmlString RenderImageDisplay(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0, string CacheBuster = null, string Alt = null, bool ForceHttpHandler = false) {
#else
        public static HtmlString RenderImageDisplay(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0, string CacheBuster = null, string Alt = null, bool ForceHttpHandler = false) {
#endif
            return htmlHelper.RenderImage(name, model, CacheBuster: CacheBuster, Alt: Alt, ForceHttpHandler: ForceHttpHandler);
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
