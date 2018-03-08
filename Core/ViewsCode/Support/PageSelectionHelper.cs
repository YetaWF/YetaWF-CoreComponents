/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public class PageSelection<TModel> : RazorTemplate<TModel> { }

    public static class PageSelectionHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PageSelectionHelper), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderPageSelectionDD(this IHtmlHelper htmlHelper, string name, Guid? pageGuid, object HtmlAttributes = null) {
#else
        public static HtmlString RenderPageSelectionDD(this HtmlHelper htmlHelper, string name, Guid? pageGuid, object HtmlAttributes = null) {
#endif
            List<SelectionItem<Guid?>> list;
            list = (
                from page in PageDefinition.GetDesignedPages() orderby page.Url select
                    new SelectionItem<Guid?> {
                        Text = page.Url,
                        Value = page.PageGuid,
                    }).ToList<SelectionItem<Guid?>>();
            list.Insert(0, new SelectionItem<Guid?> { Text = __ResStr("select", "(select)"), Value = null });

            return htmlHelper.RenderDropDownSelectionList<Guid?>(name, pageGuid ?? Guid.Empty, list, HtmlAttributes: HtmlAttributes);
        }
#if MVC6
        public static async Task<HtmlString> HtmlString RenderPageSelectionLinkAsync(this IHtmlHelper htmlHelper, Guid? pageGuid) {
#else
        public static async Task<HtmlString> RenderPageSelectionLinkAsync(this HtmlHelper htmlHelper, Guid? pageGuid) {
#endif
            HtmlBuilder hb = new HtmlBuilder();

            // link
            TagBuilder tag = new TagBuilder("a");

            PageDefinition page = null;
            if (pageGuid != null)
                page = await PageDefinition.LoadAsync((Guid)pageGuid);

            tag.MergeAttribute("href", (page != null ? page.EvaluatedCanonicalUrl : ""));
            tag.MergeAttribute("target", "_blank");
            tag.MergeAttribute("rel", "nofollow noopener noreferrer");
            tag.Attributes.Add(Basics.CssTooltip, __ResStr("linkTT", "Click to preview the page in a new window - not all pages can be displayed correctly and may require additional parameters"));

            // image
            Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();
            string imageUrl = skinImages.FindIcon_Template("PagePreview.png", currentPackage, "PageSelection");
            TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("linkAlt", "Preview"));

            tag.SetInnerHtml(tag.GetInnerHtml() + tagImg.ToString(TagRenderMode.StartTag));
            hb.Append(tag.ToString(TagRenderMode.Normal));

            return hb.ToHtmlString();
        }
    }
}
