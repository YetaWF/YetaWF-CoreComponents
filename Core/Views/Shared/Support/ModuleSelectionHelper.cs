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

    public class ModuleSelection<TModel> : RazorTemplate<TModel> { }

    public static class ModuleSelectionHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleSelectionHelper), name, defaultValue, parms); }

        public static bool ExistingModulesExist() {
            return DesignedModules.LoadDesignedModules().Count > 0;
        }

        public static MvcHtmlString RenderModuleSelectionDD(this HtmlHelper htmlHelper, string name, Guid? modGuid, object HtmlAttributes = null) {

            bool newMods = htmlHelper.GetControlInfo<bool>(name, "New", false);

            List<SelectionItem<Guid>> list;
            if (newMods) {
                list = (
                    from module in InstalledModules.Modules orderby module.Value.DisplayName.ToString() select
                        new SelectionItem<Guid> {
                            Text = module.Value.DisplayName.ToString(),
                            Value = module.Key,
                            Tooltip = module.Value.Summary,
                        }).ToList<SelectionItem<Guid>>();
            } else {
                list = (
                    from module in DesignedModules.LoadDesignedModules() orderby module.Name select
                        new SelectionItem<Guid> {
                            Text = module.Name,
                            Value = module.ModuleGuid,
                            Tooltip = module.Description,
                        }).ToList<SelectionItem<Guid>>();
            }
            list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("select", "(select)"), Value = Guid.Empty });

            return htmlHelper.RenderDropDownSelectionList<Guid>(name, modGuid ?? Guid.Empty, list, HtmlAttributes: HtmlAttributes);
        }
        public static MvcHtmlString RenderModuleSelectionLink(this HtmlHelper htmlHelper, Guid? modGuid) {

            HtmlBuilder hb = new HtmlBuilder();

            // link
            TagBuilder tag = new TagBuilder("a");

            tag.MergeAttribute("href", ModuleDefinition.GetModulePermanentUrl(modGuid ?? Guid.Empty));
            tag.MergeAttribute("target", "_blank");
            tag.Attributes.Add(Basics.CssTooltip, __ResStr("linkTT", "Click to preview the module in a new window - not all modules can be displayed correctly and may require additional parameters"));

            // image
            Package currentPackage = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            SkinImages skinImages = new SkinImages();
            string imageUrl = skinImages.FindIcon_Template("ModulePreview.png", currentPackage, "ModuleSelection");
            TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("linkAlt", "Preview"));

            tag.InnerHtml += tagImg.ToString(TagRenderMode.StartTag);
            hb.Append(tag.ToString());

            return MvcHtmlString.Create(hb.ToString());
        }
        public static MvcHtmlString RenderModuleSelectionDisplay(this HtmlHelper htmlHelper, string name, Guid? modGuid) {

            HtmlBuilder hb = new HtmlBuilder();

            InstalledModules.ModuleTypeEntry entry = null;
            if (modGuid != null && modGuid != Guid.Empty)
                entry = InstalledModules.TryFindModuleEntry((Guid)modGuid);

            //<div class="t_select">
            //    .name
            //</div>
            //<div class="t_link">
            //    .link
            //</div>
            //<div class="t_description">
            //    .description
            //</div>

            TagBuilder tag = new TagBuilder("div");
            tag.AddCssClass("t_select");
            tag.SetInnerText((entry == null) ? __ResStr("noLink", "(not found - {0})", modGuid.ToString() ) : entry.DisplayName.ToString());
            hb.Append(tag.ToString());

            tag = new TagBuilder("div");
            tag.AddCssClass("t_link");

            if (entry != null) {
                TagBuilder link = new TagBuilder("a");
                link.MergeAttribute("href", ModuleDefinition.GetModulePermanentUrl((Guid)modGuid));
                link.MergeAttribute("target", "_blank");
                link.Attributes.Add(Basics.CssTooltip, __ResStr("linkTT", "Click to preview the module in a new window - not all modules can be displayed correctly and may require additional parameters"));
                tag.InnerHtml = link.ToString();
            }
            hb.Append(tag.ToString());

            tag = new TagBuilder("div");
            tag.AddCssClass("t_description");
            if (entry == null)
                tag.InnerHtml  = "&nbsp;";
            else
                tag.SetInnerText(entry.Summary.ToString());
            hb.Append(tag.ToString());

            return MvcHtmlString.Create(hb.ToString());
        }
    }
}
