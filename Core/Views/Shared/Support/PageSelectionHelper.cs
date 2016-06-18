/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class PageSelection<TModel> : RazorTemplate<TModel> { }

    public static class PageSelectionHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PageSelectionHelper), name, defaultValue, parms); }

        public static MvcHtmlString RenderPageSelectionDD(this HtmlHelper htmlHelper, string name, Guid? pageGuid) {

            List<SelectionItem<Guid>> list;
            list = (
                from page in PageDefinition.GetDesignedPages() orderby page.Url select
                    new SelectionItem<Guid> {
                        Text = page.Url,
                        Value = page.PageGuid,
                    }).ToList<SelectionItem<Guid>>();
            list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("select", "(select)"), Value = Guid.Empty });

            return htmlHelper.RenderDropDownSelectionList<Guid>(name, pageGuid ?? Guid.Empty, list);
        }
        public static MvcHtmlString RenderPageSelectionLink(this HtmlHelper htmlHelper, Guid? pageGuid) {

            HtmlBuilder hb = new HtmlBuilder();

            // link
            TagBuilder tag = new TagBuilder("a");

            PageDefinition page = null;
            if (pageGuid != null)
                page = PageDefinition.Load((Guid)pageGuid);

            tag.MergeAttribute("href", (page != null ? page.CompleteUrl : ""));
            tag.MergeAttribute("target", "_blank");
            tag.Attributes.Add(Basics.CssTooltip, __ResStr("linkTT", "Click to preview the page in a new window - not all pages can be displayed correctly and may require additional parameters"));

            // image
            Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();
            string imageUrl = skinImages.FindIcon_Template("PagePreview.png", currentPackage, "PageSelection");
            TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("linkAlt", "Preview"));

            tag.InnerHtml += tagImg.ToString(TagRenderMode.StartTag);
            hb.Append(tag.ToString());

            return MvcHtmlString.Create(hb.ToString());
        }
    }
}
