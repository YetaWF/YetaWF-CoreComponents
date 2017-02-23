/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class GridDeleteEntry<TModel> : RazorTemplate<TModel> { }

    public static class GridDeleteEntryHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(GridDeleteEntryHelper), name, defaultValue, parms); }
#if MVC6
        public static MvcHtmlString RenderGridDeleteEntry(this IHtmlHelper htmlHelper, string name,
#else
        public static MvcHtmlString RenderGridDeleteEntry(this HtmlHelper htmlHelper, string name,
#endif
                int dummy = 0, object HtmlAttributes = null, string Tooltip = null) {
            TagBuilder tag = new TagBuilder("span");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: false, Anonymous: true);

            Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();
            string imageUrl = skinImages.FindIcon_Package("#Remove", currentPackage);
            TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("altRemove", "Remove"));
            tagImg.MergeAttribute("name", "DeleteAction", true);
            tag.SetInnerHtml(tagImg.ToString(TagRenderMode.StartTag));

            if (!string.IsNullOrWhiteSpace(Tooltip))
                tag.MergeAttribute(Basics.CssTooltipSpan, Tooltip);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }
    }
}