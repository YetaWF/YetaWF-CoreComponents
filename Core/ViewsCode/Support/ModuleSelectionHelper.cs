﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif
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
#if MVC6
        public static MvcHtmlString RenderModuleSelectionPackages(this IHtmlHelper htmlHelper, string name, bool newMods, Guid? moduleGuid, object HtmlAttributes = null) {
#else
        public static MvcHtmlString RenderModuleSelectionPackages(this HtmlHelper htmlHelper, string name, bool newMods, Guid? moduleGuid, object HtmlAttributes = null) {
#endif
            string areaName = GetAreaNameFromGuid(newMods, moduleGuid);
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
#if MVC6
        public static MvcHtmlString RenderModuleSelection(this IHtmlHelper htmlHelper, string name, bool newMods, Guid? moduleGuid, object HtmlAttributes = null) {
#else
        public static MvcHtmlString RenderModuleSelection(this HtmlHelper htmlHelper, string name, bool newMods, Guid? moduleGuid, object HtmlAttributes = null) {
#endif
            string areaName = GetAreaNameFromGuid(newMods, moduleGuid);
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
            }
            list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("none", "(none)"), Value = Guid.Empty });
            return htmlHelper.RenderDropDownSelectionList<Guid>(name, moduleGuid ?? Guid.Empty, list, HtmlAttributes: HtmlAttributes);
        }
        private static string GetAreaNameFromGuid(bool newMods, Guid? moduleGuid) {
            if (moduleGuid != null) {
                if (newMods) {
                    InstalledModules.ModuleTypeEntry modEntry = InstalledModules.TryFindModuleEntry((Guid)moduleGuid);
                    if (modEntry != null)
                        return modEntry.Package.AreaName;
                    else
                        moduleGuid = null;
                } else {
                    return (from m in DesignedModules.LoadDesignedModules() where m.ModuleGuid == (Guid)moduleGuid select m.AreaName).FirstOrDefault();
                }
            }
            return null;
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
            list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("none", "(none)"), Value = Guid.Empty });
            return DropDownHelper.RenderDataSource(areaName, list);
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
            list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("none", "(none)"), Value = Guid.Empty });
            return DropDownHelper.RenderDataSource(areaName, list);
        }
        public static MvcHtmlString RenderReplacementPackageModulesDesigned(Guid modGuid) {
            List<DesignedModule> designedMods = DesignedModules.LoadDesignedModules();
            string areaName = GetAreaNameFromGuid(false, modGuid);
            List<SelectionItem<Guid>> list = (
                from module in designedMods
                where module.AreaName == areaName
                orderby module.Name select
                    new SelectionItem<Guid> {
                        Text = module.Name,
                        Value = module.ModuleGuid,
                        Tooltip = module.Description,
                    }).ToList<SelectionItem<Guid>>();
            list.Insert(0, new SelectionItem<Guid> { Text = __ResStr("none", "(none)"), Value = Guid.Empty });
            return DropDownHelper.RenderDataSource(areaName, list);
        }
#if MVC6
        public static MvcHtmlString RenderModuleSelectionLink(this IHtmlHelper htmlHelper, Guid? modGuid) {
#else
        public static MvcHtmlString RenderModuleSelectionLink(this HtmlHelper htmlHelper, Guid? modGuid) {
#endif
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

            tag.SetInnerHtml(tag.GetInnerHtml() + tagImg.ToString(TagRenderMode.StartTag));
            hb.Append(tag.ToString(TagRenderMode.Normal));

            return MvcHtmlString.Create(hb.ToString());
        }
#if MVC6
        public static MvcHtmlString RenderModuleSelectionDisplay(this IHtmlHelper htmlHelper, string name, Guid? modGuid) {
#else
        public static MvcHtmlString RenderModuleSelectionDisplay(this HtmlHelper htmlHelper, string name, Guid? modGuid) {
#endif
        HtmlBuilder hb = new HtmlBuilder();
            bool newMods = htmlHelper.GetControlInfo<bool>("", "New", false);

            ModuleDefinition mod = null;
            if (modGuid != null)
                mod = ModuleDefinition.Load((Guid)modGuid, AllowNone: true);

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
            if (mod == null) {
                if (modGuid == Guid.Empty)
                    modName = __ResStr("noLinkNone", "(none)");
                else
                    modName = __ResStr("noLink", "(not found - {0})", modGuid.ToString());
            } else {
                Package package = Package.GetPackageFromType(mod.GetType());
                modName = __ResStr("name", "{0} - {1}", package.Name, mod.Name);
            }

            TagBuilder tag = new TagBuilder("div");
            tag.AddCssClass("t_select");
            tag.SetInnerText(modName);
            hb.Append(tag.ToString(TagRenderMode.Normal));

            if (mod != null) {
                tag = new TagBuilder("div");
                tag.AddCssClass("t_link");
                tag.SetInnerHtml(htmlHelper.RenderModuleSelectionLink(modGuid).ToString());
                hb.Append(tag.ToString(TagRenderMode.Normal));
            }

            tag = new TagBuilder("div");
            tag.AddCssClass("t_description");
            if (mod == null)
                tag.SetInnerHtml("&nbsp;");
            else
                tag.SetInnerText(mod.Description.ToString());
            hb.Append(tag.ToString(TagRenderMode.Normal));

            return MvcHtmlString.Create(hb.ToString());
        }
    }
}