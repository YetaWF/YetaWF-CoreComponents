/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using System.Threading.Tasks;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Components;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class Url<TModel> : RazorTemplate<TModel> { }

    public static class UrlHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(UrlHelper), name, defaultValue, parms); }
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static async Task<HtmlString> RenderUrlDisplayAsync(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
#else
        public static async Task<HtmlString> RenderUrlDisplayAsync(this HtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
#endif
            HtmlBuilder hb = new HtmlBuilder();

            string hrefUrl;
            if (!htmlHelper.TryGetParentModelSupportProperty<string>(name, "Url", out hrefUrl))
                hrefUrl = model;

            if (string.IsNullOrWhiteSpace(hrefUrl)) {
                // no link
                TagBuilder tag = new TagBuilder("span");
                htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Anonymous: true, Validation: false);

                string cssClass = htmlHelper.GetControlInfo<string>(name, "CssClass", "");
                if (!string.IsNullOrWhiteSpace(cssClass))
                    tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));

                if (!string.IsNullOrWhiteSpace(Tooltip))
                    tag.MergeAttribute(Basics.CssTooltip, Tooltip);

                tag.SetInnerText(model);
                return tag.ToHtmlString(TagRenderMode.Normal);

            } else {
                // link
                TagBuilder tag = new TagBuilder("a");
                htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Anonymous: true, Validation: false);

                string cssClass = htmlHelper.GetControlInfo<string>(name, "CssClass", "");
                if (!string.IsNullOrWhiteSpace(cssClass))
                    tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));

                tag.MergeAttribute("href", YetaWFManager.UrlEncodePath(hrefUrl));
                tag.MergeAttribute("target", "_blank");
                tag.MergeAttribute("rel", "nofollow noopener noreferrer");
                if (!string.IsNullOrWhiteSpace(Tooltip))
                    tag.MergeAttribute(Basics.CssTooltip, Tooltip);
                string text;
                if (!htmlHelper.TryGetParentModelSupportProperty<string>(name, "Text", out text))
                    text = model;
                tag.SetInnerText(text);

                // image
                Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                SkinImages skinImages = new SkinImages();
                string imageUrl = await skinImages.FindIcon_TemplateAsync("UrlRemote.png", currentPackage, "Url");
                TagBuilder tagImg = ImageHTML.BuildKnownImageTag(imageUrl, alt: __ResStr("altText", "Remote Url"));

                tag.SetInnerHtml(tag.GetInnerHtml() + tagImg.ToString(TagRenderMode.StartTag));
                hb.Append(tag.ToString(TagRenderMode.Normal));

                return hb.ToHtmlString();
            }
        }
#if MVC6
        public static async Task<HtmlString> RenderUrlSelAsync(this IHtmlHelper htmlHelper, string name, UrlTypeEnum type, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#else
        public static async Task<HtmlString> RenderUrlSelAsync(this HtmlHelper htmlHelper, string name, UrlTypeEnum type, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#endif
            List<SelectionItem<int>> items = new List<SelectionItem<int>>();
            if ((type & UrlTypeEnum.Local) != 0) {
                items.Add(new SelectionItem<int> {
                    Text = __ResStr("selLocal", "Designed Page"),
                    Value = 1,
                    Tooltip = __ResStr("selLocalTT", "Select for local, designed pages"),
                });
            }
            if ((type & UrlTypeEnum.Remote) != 0) {
                items.Add(new SelectionItem<int> {
                    Text = __ResStr("selRemote", "Local/Remote Url"),
                    Value = 2,
                    Tooltip = __ResStr("selRemoteTT", "Select to enter a Url (local or remote) - Can contain query string arguments - Local Urls start with /, remote Urls with http:// or https://"),
                });
            }
            if ((type & UrlTypeEnum.New) != 0)
                throw new InternalError("New url not supported by this template");

            return await htmlHelper.RenderDropDownSelectionListAsync(name, 0, items, HtmlAttributes: HtmlAttributes, Validation: Validation);
        }
#if MVC6
        public static async Task<HtmlString> RenderUrlDDAsync(this IHtmlHelper htmlHelper, string name, string url, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#else
        public static async Task<HtmlString> RenderUrlDDAsync(this HtmlHelper htmlHelper, string name, string url, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#endif
            List<string> pages = await PageDefinition.GetDesignedUrlsAsync();

            // get list of desired pages (ignore users that are invalid, they may have been deleted)
            List<SelectionItem<string>> list = new List<SelectionItem<string>>();
            foreach (var page in pages) {
                list.Add(new SelectionItem<string> {
                    Text = page,
                    //Tooltip = __ResStr("selPage", "Select page {0}", page),
                    Value = page,
                });
            }
            list = (from l in list orderby l.Text select l).ToList();
            list.Insert(0, new SelectionItem<string> { Text = __ResStr("select", "(select)"), Value = "" });

            return await htmlHelper.RenderDropDownSelectionListAsync<string>(name, url, list, HtmlAttributes: HtmlAttributes, Validation: Validation);
        }
#if MVC6
        public static async Task<HtmlString> RenderUrlLinkAndImageAsync(this IHtmlHelper htmlHelper, string url) {
#else
        public static async Task<HtmlString> RenderUrlLinkAndImageAsync(this HtmlHelper htmlHelper, string url) {
#endif
            HtmlBuilder hb = new HtmlBuilder();

            // link
            TagBuilder tag = new TagBuilder("a");

            tag.MergeAttribute("href", YetaWFManager.UrlEncodePath(url));
            tag.MergeAttribute("target", "_blank");
            tag.MergeAttribute("rel", "nofollow noopener noreferrer");

            // image
            Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();
            string imageUrl = await skinImages.FindIcon_TemplateAsync("UrlRemote.png", currentPackage, "Url");
            TagBuilder tagImg = ImageHTML.BuildKnownImageTag(imageUrl, alt: __ResStr("altText", "Remote Url"));

            tag.SetInnerHtml(tag.GetInnerHtml() + tagImg.ToString(TagRenderMode.StartTag));
            hb.Append(tag.ToString(TagRenderMode.Normal));

            return hb.ToHtmlString();
        }
    }
}
