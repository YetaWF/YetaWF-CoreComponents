/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
#endif

namespace YetaWF.Core.Views.Shared {

    public class ReferencedModules<TModel> : RazorTemplate<TModel> { }

    public static class ReferencedModulesHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public class GridModel {
            [UIHint("Grid")]
            public GridDefinition GridDef { get; set; }
        }

        public class GridEdit {

            [Caption("Use"), Description("Select to include this module")]
            [UIHint("Boolean")]
            public bool UsesModule { get; set; }

            [Caption("Name"), Description("Module Name")]
            [UIHint("String"), ReadOnly]
            public string Name { get; set; }

            [Caption("Description"), Description("Module Description")]
            [UIHint("String"), ReadOnly]
            public string Description { get; set; }

            [Caption("Module"), Description("Module")]
            [UIHint("String"), ReadOnly]
            public string PermanentName { get; set; }

            [Caption("Guid"), Description("Module Guid")]
            [UIHint("Guid"), ReadOnly]
            public Guid ModuleGuid { get; set; } // this name must match the name used in the class ReferencedModule
        }

        public static DataSourceResult GetDataSourceResult(SerializableList<ModuleDefinition.ReferencedModule> model) {

            List<AddOnManager.Module> allMods = Manager.AddOnManager.GetUniqueInvokedCssModules();

            List<GridEdit> mods = new List<GridEdit>();
            foreach (AddOnManager.Module allMod in allMods) {
                ModuleDefinition modDef = ModuleDefinition.CreateUniqueModule(allMod.ModuleType);
                if (modDef != null) {
                    mods.Add( new GridEdit{
                        Name = modDef.Name,
                        Description = modDef.Description,
                        PermanentName = modDef.PermanentModuleName,
                        ModuleGuid = modDef.ModuleGuid,
                        UsesModule = (from m in model where m.ModuleGuid == allMod.ModuleGuid select m).FirstOrDefault() != null
                    });
                }
            }
            DataSourceResult data = new DataSourceResult {
                Data = mods.ToList<object>(),
                Total = mods.Count,
            };
            return data;
        }
#if MVC6
        public static HtmlString RenderReferencedModules<TModel>(this IHtmlHelper<TModel> htmlHelper, string name, SerializableList<ModuleDefinition.ReferencedModule> model) {
#else
        public static HtmlString RenderReferencedModules<TModel>(this HtmlHelper<TModel> htmlHelper, string name, SerializableList<ModuleDefinition.ReferencedModule> model) {
#endif
            bool header;
            if (!htmlHelper.TryGetControlInfo<bool>("", "Header", out header))
                header = true;
            GridModel grid = new GridModel() {
                GridDef = new GridDefinition() {
                    RecordType = typeof(GridEdit),
                    Data = GetDataSourceResult(model),
                    SupportReload = false,
                    PageSizes = new List<int>(),
                    InitialPageSize = 10,
                    ShowHeader = header,
                    ReadOnly = false,
                }
            };
#if MVC6
            return new HtmlString(htmlHelper.DisplayFor(m => grid.GridDef).AsString());
#else
            return htmlHelper.DisplayFor(m => grid.GridDef);
#endif
        }

        public class GridDisplay {

            [Caption("Name"), Description("Module Name")]
            [UIHint("String"), ReadOnly]
            public string Name { get; set; }

            [Caption("Description"), Description("Module Description")]
            [UIHint("String"), ReadOnly]
            public string Description { get; set; }

            [Caption("Module"), Description("Module")]
            [UIHint("String"), ReadOnly]
            public string PermanentName { get; set; }
        }

        public static DataSourceResult GetDataSourceResultDisplay(SerializableList<ModuleDefinition.ReferencedModule> model) {

            List<AddOnManager.Module> allMods = Manager.AddOnManager.GetUniqueInvokedCssModules();

            List<GridDisplay> mods = new List<GridDisplay>();
            foreach (AddOnManager.Module allMod in allMods) {
                if ((from m in model where m.ModuleGuid == allMod.ModuleGuid select m).FirstOrDefault() != null) {
                    ModuleDefinition modDef = ModuleDefinition.CreateUniqueModule(allMod.ModuleType);
                    if (modDef != null) {
                        mods.Add(new GridDisplay {
                            Name = modDef.Name,
                            Description = modDef.Description,
                            PermanentName = modDef.PermanentModuleName,
                        });
                    }
                }
            }
            DataSourceResult data = new DataSourceResult {
                Data = mods.ToList<object>(),
                Total = mods.Count,
            };
            return data;
        }
#if MVC6
        public static HtmlString RenderReferencedModulesDisplay<TModel>(this IHtmlHelper<TModel> htmlHelper, string name, SerializableList<ModuleDefinition.ReferencedModule> model) {
#else
        public static HtmlString RenderReferencedModulesDisplay<TModel>(this HtmlHelper<TModel> htmlHelper, string name, SerializableList<ModuleDefinition.ReferencedModule> model) {
#endif
            bool header;
            if (!htmlHelper.TryGetControlInfo<bool>("", "Header", out header))
                header = true;
            GridModel grid = new GridModel() {
                GridDef = new GridDefinition() {
                    RecordType = typeof(GridDisplay),
                    Data = GetDataSourceResult(model),
                    SupportReload = false,
                    PageSizes = new List<int>(),
                    InitialPageSize = 10,
                    ShowHeader = header,
                    ReadOnly = true,
                }
            };
#if MVC6
            return new HtmlString(htmlHelper.DisplayFor(m => grid.GridDef).AsString());
#else
            return htmlHelper.DisplayFor(m => grid.GridDef);
#endif
        }
        public static void AddReference(this SerializableList<ModuleDefinition.ReferencedModule> refMods, Guid modGuid) {
            ModuleDefinition.ReferencedModule r = new ModuleDefinition.ReferencedModule { ModuleGuid = modGuid };
            if (!refMods.Contains(r))
                refMods.Add(r);
        }
        public static void RemoveReference(this SerializableList<ModuleDefinition.ReferencedModule> refMods, Guid modGuid) {
            ModuleDefinition.ReferencedModule r = new ModuleDefinition.ReferencedModule { ModuleGuid = modGuid };
            if (refMods.Contains(r))
                refMods.Remove(r);
        }
        public static void ToggleReference(this SerializableList<ModuleDefinition.ReferencedModule> refMods, Guid modGuid, bool on) {
            if (on)
                refMods.AddReference(modGuid);
            else
                refMods.RemoveReference(modGuid);
        }
    }
}