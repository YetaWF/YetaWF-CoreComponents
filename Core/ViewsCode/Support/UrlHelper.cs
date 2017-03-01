/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class Url<TModel> : RazorTemplate<TModel> { }

    public static class UrlHelperEx {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(UrlHelper), name, defaultValue, parms); }
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        [Flags]
        public enum UrlTypeEnum {
            Local = 1,
            Remote = 2,
            New = 4, // Local by definition
        }
    }
    public static class UrlHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(UrlHelper), name, defaultValue, parms); }
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static HtmlString RenderUrlDisplay(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
#else
        public static HtmlString RenderUrlDisplay(this HtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
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
                if (!string.IsNullOrWhiteSpace(Tooltip))
                    tag.MergeAttribute(Basics.CssTooltip, Tooltip);
                string text;
                if (!htmlHelper.TryGetParentModelSupportProperty<string>(name, "Text", out text))
                    text = model;
                tag.SetInnerText(text);

                // image
                Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                SkinImages skinImages = new SkinImages();
                string imageUrl = skinImages.FindIcon_Template("UrlRemote.png", currentPackage, "Url");
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("altText", "Remote Url"));

                tag.SetInnerHtml(tag.GetInnerHtml() + tagImg.ToString(TagRenderMode.StartTag));
                hb.Append(tag.ToString(TagRenderMode.Normal));

                return hb.ToHtmlString();
            }
        }
#if MVC6
        public static HtmlString RenderUrlSel(this IHtmlHelper htmlHelper, string name, UrlHelperEx.UrlTypeEnum type, int dummy = 0, object HtmlAttributes = null) {
#else
        public static HtmlString RenderUrlSel(this HtmlHelper htmlHelper, string name, UrlHelperEx.UrlTypeEnum type, int dummy = 0, object HtmlAttributes = null) {
#endif
            List<SelectionItem<int>> items = new List<Shared.SelectionItem<int>>();
            if ((type & UrlHelperEx.UrlTypeEnum.Local) != 0) {
                items.Add(new SelectionItem<int> {
                    Text = __ResStr("selLocal", "Designed Page"),
                    Value = 1,
                    Tooltip = __ResStr("selLocalTT", "Select for local, designed pages"),
                });
            }
            if ((type & UrlHelperEx.UrlTypeEnum.Remote) != 0) {
                items.Add(new SelectionItem<int> {
                    Text = __ResStr("selRemote", "Local/Remote Url"),
                    Value = 2,
                    Tooltip = __ResStr("selRemoteTT", "Select to enter a Url (local or remote) - Can contain query string arguments - Local Urls start with /, remote Urls with http:// or https://"),
                });
            }
            if((type & UrlHelperEx.UrlTypeEnum.New) != 0)
                throw new InternalError("New url not supported by this template");

            return htmlHelper.RenderDropDownSelectionList(name, 0, items, HtmlAttributes: HtmlAttributes);
        }
#if MVC6
        public static HtmlString RenderUrlDD(this IHtmlHelper htmlHelper, string name, string url, int dummy = 0, object HtmlAttributes = null) {
#else
        public static HtmlString RenderUrlDD(this HtmlHelper htmlHelper, string name, string url, int dummy = 0, object HtmlAttributes = null) {
#endif
            List<string> pages = PageDefinition.GetDesignedUrls();

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

            return htmlHelper.RenderDropDownSelectionList<string>(name, url, list, HtmlAttributes: HtmlAttributes);
        }
#if MVC6
        public static HtmlString RenderUrlLinkAndImage(this IHtmlHelper htmlHelper, string url) {
#else
        public static HtmlString RenderUrlLinkAndImage(this HtmlHelper htmlHelper, string url) {
#endif
            HtmlBuilder hb = new HtmlBuilder();

            // link
            TagBuilder tag = new TagBuilder("a");

            tag.MergeAttribute("href", YetaWFManager.UrlEncodePath(url));
            tag.MergeAttribute("target", "_blank");

            // image
            Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();
            string imageUrl = skinImages.FindIcon_Template("UrlRemote.png", currentPackage, "Url");
            TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("altText", "Remote Url"));

            tag.SetInnerHtml(tag.GetInnerHtml() + tagImg.ToString(TagRenderMode.StartTag));
            hb.Append(tag.ToString(TagRenderMode.Normal));

            return hb.ToHtmlString();
        }
    }
}
