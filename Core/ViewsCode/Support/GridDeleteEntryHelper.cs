﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    public class GridDeleteEntry<TModel> : RazorTemplate<TModel> { }

    public static class GridDeleteEntryHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(GridDeleteEntryHelper), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderGridDeleteEntry(this IHtmlHelper htmlHelper, string name,
#else
        public static HtmlString RenderGridDeleteEntry(this HtmlHelper htmlHelper, string name,
#endif
                int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
            TagBuilder tag = new TagBuilder("span");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: false, Anonymous: true);

            Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();
            string imageUrl = skinImages.FindIcon_Package("#RemoveLight", currentPackage);
            TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("altRemove", "Remove"));
            tagImg.MergeAttribute("name", "DeleteAction", true);
            tag.SetInnerHtml(tagImg.ToString(TagRenderMode.StartTag));

            if (!string.IsNullOrWhiteSpace(Tooltip))
                tag.MergeAttribute(Basics.CssTooltipSpan, YetaWFManager.HtmlAttributeEncode(Tooltip));

            return tag.ToHtmlString(TagRenderMode.Normal);
        }
    }
}