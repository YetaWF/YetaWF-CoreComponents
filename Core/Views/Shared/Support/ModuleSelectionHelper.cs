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
        /// <summary>
        /// Renders a dropdownlist of all packages implementing modules.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper.</param>
        /// <param name="name">The field name.</param>
        /// <param name="modGuid">An optional module guid of the currently selected module.</param>
        /// <param name="HtmlAttributes">Optional HTML attributes.</param>
        /// <returns></returns>
        public static MvcHtmlString RenderModuleSelectionPackages(this HtmlHelper htmlHelper, string name, bool newMods, Guid? moduleGuid, object HtmlAttributes = null) {
            string areaName = null;
            if (moduleGuid != null) {
                InstalledModules.ModuleTypeEntry modEntry = InstalledModules.TryFindModuleEntry((Guid)moduleGuid);
                if (modEntry != null)
                    areaName = modEntry.Package.AreaName;
                else
                    moduleGuid = null;
            }
            List<SelectionItem<string>> list = (
                from p in InstalledModules.Packages orderby p.Name select
                    new SelectionItem<string> {
                        Text = string.Format(__ResStr("package", "{0}", p.Name)),
                        Value = p.AreaName,
                        Tooltip = string.Format(__ResStr("packageTT", "{0} - {1}", p.Description.ToString(), p.CompanyDisplayName)),
                    }).ToList<SelectionItem<string>>();
            list = (from l in list orderby l.Text select l).ToList();
            list.Insert(0, new SelectionItem<string> { Text = __ResStr("selectPackage", "(select)"), Value = null });
            return htmlHelper.RenderDropDownSelectionList<string>("Packages", areaName, list, HtmlAttributes: HtmlAttributes);
        }

        public static MvcHtmlString RenderModuleSelection(this HtmlHelper htmlHelper, string name, bool newMods, Guid? moduleGuid, object HtmlAttributes = null) {
            string areaName = null;
            if (moduleGuid != null) {
                InstalledModules.ModuleTypeEntry modEntry = InstalledModules.TryFindModuleEntry((Guid)moduleGuid);
                if (modEntry != null)
                    areaName = modEntry.Package.AreaName;
                else
                    moduleGuid = null;
            }
            List<SelectionItem<Guid>> list = new List<SelectionItem<Guid>>();
            if (!string.IsNullOrWhiteSpace(areaName)) {
                if (newMods) {
                    list = (
                        from module in InstalledModules.Modules
                        where module.Value.Package.AreaName == areaName
                        orderby module.Value.DisplayName.ToString() select
                            new SelectionItem<Guid> {
                                Text = module.Value.DisplayName.ToString(),
                                Value = module.Key,
                                Tooltip = module.Value.Summary,
                            }).ToList<SelectionItem<Guid>>();
                } else {
                    list = (
                        from module in DesignedModules.LoadDesignedModules()
                        where module.AreaName == areaName
                        orderby module.Name select
                            new SelectionItem<Guid> {
                                Text = module.Name,
                                Value = module.ModuleGuid,
                                Tooltip = module.Description,
                            }).ToList<SelectionItem<Guid>>();
                }
            } else {
                list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("none", "(none)"), Value = Guid.Empty });
            }

            return htmlHelper.RenderDropDownSelectionList<Guid>(name, moduleGuid ?? Guid.Empty, list, HtmlAttributes: HtmlAttributes);
        }
        public static MvcHtmlString RenderReplacementPackageModulesNew(string areaName) {
            List<SelectionItem<Guid>> list = (
                from module in InstalledModules.Modules
                where module.Value.Package.AreaName == areaName
                orderby module.Value.DisplayName.ToString() select
                    new SelectionItem<Guid> {
                        Text = module.Value.DisplayName.ToString(),
                        Value = module.Key,
                        Tooltip = module.Value.Summary,
                    }).ToList<SelectionItem<Guid>>();
            if (list.Count == 0)
                list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("none", "(none)"), Value = Guid.Empty });
            return DropDownHelper.RenderDataSource(list);
        }
        public static MvcHtmlString RenderReplacementPackageModulesDesigned(string areaName) {
            List<SelectionItem<Guid>> list = (
                from module in DesignedModules.LoadDesignedModules()
                where module.AreaName == areaName
                orderby module.Name select
                    new SelectionItem<Guid> {
                        Text = module.Name,
                        Value = module.ModuleGuid,
                        Tooltip = module.Description,
                    }).ToList<SelectionItem<Guid>>();
            if (list.Count == 0)
                list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("none", "(none)"), Value = Guid.Empty });
            return DropDownHelper.RenderDataSource(list);
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

            string modName;
            if (entry == null) {
                if (modGuid == Guid.Empty)
                    modName = __ResStr("noLinkNone", "(none)");
                else
                    modName = __ResStr("noLink", "(not found - {0})", modGuid.ToString());
            } else
                modName = __ResStr("name", "{0} - {1}", entry.Package.Name, entry.DisplayName.ToString());

            TagBuilder tag = new TagBuilder("div");
            tag.AddCssClass("t_select");
            tag.SetInnerText(modName);
            hb.Append(tag.ToString());

            if (entry != null) {
                tag = new TagBuilder("div");
                tag.AddCssClass("t_link");
                tag.InnerHtml = htmlHelper.RenderModuleSelectionLink(modGuid).ToString();
                hb.Append(tag.ToString());
            }

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
