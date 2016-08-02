﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class Url<TModel> : RazorTemplate<TModel> { }

    public static class UrlHelperEx {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(UrlHelperEx), name, defaultValue, parms); }
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        [Flags]
        public enum UrlTypeEnum {
            Local = 1,
            Remote = 2,
            New = 4, // Local by definition
        }

        public static MvcHtmlString RenderUrlDisplay(this HtmlHelper htmlHelper, string name, string url, int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {

            HtmlBuilder hb = new HtmlBuilder();

            string hrefUrl;
            if (!htmlHelper.TryGetParentModelSupportProperty<string>(name, "Url", out hrefUrl))
                hrefUrl = url;

            if (string.IsNullOrWhiteSpace(hrefUrl)) {
                // no link
                TagBuilder tag = new TagBuilder("span");
                htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Anonymous: true, Validation: false);

                string cssClass = htmlHelper.GetControlInfo<string>(name, "CssClass", "");
                if (!string.IsNullOrWhiteSpace(cssClass))
                    tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));

                if (!string.IsNullOrWhiteSpace(Tooltip))
                    tag.MergeAttribute(Basics.CssTooltip, Tooltip);

                tag.SetInnerText(url);
                return MvcHtmlString.Create(tag.ToString());

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
                tag.SetInnerText(url);

                // image
                Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                SkinImages skinImages = new SkinImages();
                string imageUrl = skinImages.FindIcon_Template("UrlRemote.png", currentPackage, "Url");
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("altText", "Remote Url"));

                tag.InnerHtml += tagImg.ToString(TagRenderMode.StartTag);
                hb.Append(tag.ToString());

                return MvcHtmlString.Create(hb.ToString());
            }
        }
        public static MvcHtmlString RenderUrlDD(this HtmlHelper htmlHelper, string name, string url, int dummy = 0, object HtmlAttributes = null) {

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
        public static MvcHtmlString RenderUrlLinkAndImage(this HtmlHelper htmlHelper, string url) {

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

            tag.InnerHtml += tagImg.ToString(TagRenderMode.StartTag);
            hb.Append(tag.ToString());

            return MvcHtmlString.Create(hb.ToString());
        }
    }
}
