/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using YetaWF.Core.Extensions;
using YetaWF.Core.Image;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using System.Threading.Tasks;
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
#if MVC6
        public static HtmlString RenderImageEdit(this IHtmlHelper<string> htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null) {
#else
        public static HtmlString RenderImageEdit(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null) {
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
                bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any) {

            string imageType = htmlHelper.GetControlInfo<string>(name, "ImageType");
            int width, height;
            htmlHelper.TryGetControlInfo<int>(name, "Width", out width);
            htmlHelper.TryGetControlInfo<int>(name, "Height", out height);

            if (string.IsNullOrWhiteSpace(imageType) && model != null && (model.IsAbsoluteUrl() || model.StartsWith("/"))) {

                if (width != 0 || height != 0) throw new InternalError("Can't use Width or Height with external Urls");

                TagBuilder img = new TagBuilder("img");
                img.Attributes.Add("src", model);
                img.Attributes.Add("alt", __ResStr("altImage", "{0}", Alt??"Image"));
                return img.ToHtmlString(TagRenderMode.Normal);

            } else {

                if (string.IsNullOrWhiteSpace(imageType)) throw new InternalError("No ImageType specified");

                bool showMissing = htmlHelper.GetControlInfo<bool>(name, "ShowMissing", true);
                if (string.IsNullOrWhiteSpace(model) && !showMissing) return HtmlStringExtender.Empty;

                string imgTag = RenderImage(imageType, width, height, model, CacheBuster: CacheBuster, Alt:Alt, ExternalUrl: ExternalUrl, SecurityType: SecurityType);

                bool linkToImage = htmlHelper.GetControlInfo<bool>(name, "LinkToImage", false);
                if (linkToImage) {
                    TagBuilder link = new TagBuilder("a");
                    string imgUrl = FormatUrl(imageType, null, model, CacheBuster: CacheBuster);
                    link.MergeAttribute("href", imgUrl);
                    link.MergeAttribute("target", "_blank");
                    link.MergeAttribute("rel", "noopener noreferrer");
                    link.SetInnerHtml(imgTag);
                    return link.ToHtmlString(TagRenderMode.Normal);
                } else
                    return new HtmlString(imgTag);
            }
        }
        public static string RenderImage(string imageType, int width, int height, string model,
                string CacheBuster = null, string Alt = null, bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any) {
            string url = FormatUrl(imageType, null, model, width, height, CacheBuster: CacheBuster, ExternalUrl: ExternalUrl, SecurityType: SecurityType);
            TagBuilder img = new TagBuilder("img");
            img.AddCssClass("t_preview");
            img.Attributes.Add("src", url);
            img.Attributes.Add("alt", Alt??"Image");
            return img.ToString(TagRenderMode.StartTag);
        }
#if MVC6
        public static async Task<HtmlString> RenderImageAttributesAsync(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0) {
#else
        public static async Task<HtmlString> RenderImageAttributesAsync(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0) {
#endif
            if (model == null) return HtmlStringExtender.Empty;
            System.Drawing.Size size = await ImageSupport.GetImageSizeAsync(model);
            if (size.IsEmpty) return HtmlStringExtender.Empty;

            return new HtmlString(__ResStr("imgAttr", "{0} x {1} (w x h)", size.Width, size.Height));
        }
#if MVC6
        public static HtmlString RenderImageDisplay(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0, string CacheBuster = null, string Alt = null) {
#else
        public static HtmlString RenderImageDisplay(this HtmlHelper<object> htmlHelper, string name, string model, int dummy = 0, string CacheBuster = null, string Alt = null) {
#endif
            return htmlHelper.RenderImage(name, model, Alt: Alt);
        }
        public static string FormatUrl(string imageType, string location, string name, int width = 0, int height = 0,
                string CacheBuster = null, bool ExternalUrl = false, PageDefinition.PageSecurityType SecurityType = PageDefinition.PageSecurityType.Any,
                bool Stretch = false) {
            string url;
            if (width > 0 && height > 0) {
                url = string.Format(Addons.Templates.Image.FormatUrlWithSize, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name),
                    width, height, Stretch ? "1" : "0");
            } else {
                url = string.Format(Addons.Templates.Image.FormatUrl, YetaWFManager.UrlEncodeArgs(imageType), YetaWFManager.UrlEncodeArgs(location), YetaWFManager.UrlEncodeArgs(name));
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
    }
}
