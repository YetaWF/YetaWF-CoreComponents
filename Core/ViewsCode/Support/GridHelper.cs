/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Repository;
using YetaWF.Core.Localize;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class Grid<TModel> : RazorTemplate<TModel> { }

    public class DataSourceResult {
        public List<object> Data { get; set; } // one page of data
        public int Total { get; set; } // total # of records
        public string FieldPrefix { get; set; } // Html field prefix (only used for partial rendering)

        public int RecordCount { get; set; }// used by templates to communicate record # being rendered
    }

    public static class GridHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(GridHelper), name, defaultValue, parms); }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public enum GridActionsEnum {
            [EnumDescription("Icons", "Actions in grids are displayed as icons")]
            Icons = 0,
            [EnumDescription("Dropdown Menu", "If more than one action is available they are displayed as a dropdown menu, accessible through a button - Otherwise a single icon is displayed")]
            DropdownMenu = 1,
        }

        public class GridSavedSettings {
            public GridDefinition.ColumnDictionary Columns { get; set; }
            public int PageSize { get; set; }

            public int CurrentPage { get; set; }

            public GridSavedSettings() {
                Columns = new GridDefinition.ColumnDictionary();
                PageSize = 10;
                CurrentPage = 1;
            }
        }
        public static GridSavedSettings LoadModuleSettings(Guid moduleGuid, int defaultInitialPage = 1, int defaultPageSize = 10) {
            SettingsDictionary modSettings = Manager.SessionSettings.GetModuleSettings(moduleGuid);
            GridSavedSettings gridSavedSettings = modSettings.GetValue<GridSavedSettings>("GridSavedSettings");
            if (gridSavedSettings == null) {
                gridSavedSettings = new GridSavedSettings() {
                    CurrentPage = defaultInitialPage,
                    PageSize = defaultPageSize,
                };
            }
            return gridSavedSettings;
        }
        public static void SaveModuleSettings(Guid moduleGuid, GridSavedSettings gridSavedSettings) {
            SettingsDictionary modSettings = Manager.SessionSettings.GetModuleSettings(moduleGuid);
            if (modSettings != null) {
                modSettings.SetValue<GridSavedSettings>("GridSavedSettings", gridSavedSettings);
                modSettings.Save();
            }
        }
        /// <summary>
        /// Renders the url to save the column widths for a grid
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <returns></returns>
#if MVC6
        public static HtmlString GetSettingsSaveColumnWidthsUrl(this IHtmlHelper htmlHelper) {
#else
        public static HtmlString GetSettingsSaveColumnWidthsUrl(this HtmlHelper<object> htmlHelper) {
#endif
            string settingsSaveUrl = YetaWFManager.UrlFor(typeof(YetaWF.Core.Controllers.Shared.GridHelperController), "GridSaveColumnWidths");
            return new HtmlString(settingsSaveUrl);
        }

        public static void SaveSettings(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filter, Guid? settingsModuleGuid = null) {

            // save the current sort order and page size
            if (settingsModuleGuid != null && settingsModuleGuid != Guid.Empty) {
                GridHelper.GridSavedSettings gridSavedSettings = GridHelper.LoadModuleSettings((Guid)settingsModuleGuid);
                gridSavedSettings.PageSize = take;
                gridSavedSettings.CurrentPage = Math.Max(1, skip / take + 1);
                foreach (var col in gridSavedSettings.Columns)
                    col.Value.Sort = GridDefinition.SortBy.NotSpecified;
                if (sort != null) {
                    foreach (var sortCol in sort) {
                        GridDefinition.SortBy sortDir = (sortCol.Order == DataProviderSortInfo.SortDirection.Ascending) ? GridDefinition.SortBy.Ascending : GridDefinition.SortBy.Descending;
                        if (gridSavedSettings.Columns.ContainsKey(sortCol.Field))
                            gridSavedSettings.Columns[sortCol.Field].Sort = sortDir;
                        else
                            gridSavedSettings.Columns.Add(sortCol.Field, new GridDefinition.ColumnInfo { Width = -1, Sort = sortDir });
                    }
                }
                GridHelper.SaveModuleSettings((Guid)settingsModuleGuid, gridSavedSettings);
            }
        }

        /// <summary>
        /// Used to change sort column when another column is used to sort rather than the displayed property.
        /// Translates the displayed property name to the actual sort column name.
        /// </summary>
        /// <remarks>Only supports 1 sort field</remarks>
        // sortas, sort as
        public static void UpdateAlternateSortColumn(List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, string displayProperty, string realColumn) {
            if (sort != null && sort.Count == 1) {
                DataProviderSortInfo sortInfo = sort.First();
                if (sortInfo.Field == displayProperty)
                    sortInfo.Field = realColumn;
            }
            UpdateNavigatedFilters(filters, displayProperty, realColumn);
        }

        private static void UpdateNavigatedFilters(List<DataProviderFilterInfo> filters, string displayProperty, string realColumn) {
            if (filters == null) return;
            foreach (DataProviderFilterInfo f in filters) {
                if (f.Field == displayProperty) {
                    f.Field = realColumn;
                } else {
                    UpdateNavigatedFilters(f.Filters, displayProperty, realColumn);
                }
            }
        }
        public static async Task<ObjectSupport.ReadGridDictionaryInfo> LoadGridColumnDefinitionsAsync(GridDefinition gridDef) {
            if (gridDef.CachedDict == null)
                gridDef.CachedDict = await LoadGridColumnDefinitionsAsync(gridDef.RecordType);
            return gridDef.CachedDict;
        }

        public class LoadGridColumnDefinitionsInfo {
            public string SortColumn { get; set; }
            public GridDefinition.SortBy SortBy { get; set; }
        }

        public static async Task<ObjectSupport.ReadGridDictionaryInfo> LoadGridColumnDefinitionsAsync(Type recordType) {
            if (typeof(ModuleDefinition.GridAllowedRole).IsAssignableFrom(recordType)) {
                recordType = typeof(ModuleDefinition.GridAllowedRole);
            } else if (typeof(ModuleDefinition.GridAllowedUser).IsAssignableFrom(recordType)) {
                recordType = typeof(ModuleDefinition.GridAllowedUser);
            }
            Dictionary<string, GridColumnInfo> dict = new Dictionary<string, GridColumnInfo>();
            string className = recordType.FullName.Split(new char[] { '.' }).Last();
            string[] s = className.Split(new char[] { '+' });
            int len = s.Length;
            if (len != 2) throw new InternalError("Unexpected class {0} in record type {1}", className, recordType.FullName);
            string controller = s[0];
            string model = s[1];
            string file = controller + "." + model;
            Package package = Package.GetPackageFromType(recordType);
            string predefUrl = VersionManager.GetAddOnPackageUrl(package.Domain, package.Product) + "Grids/" + file;
            string customUrl = VersionManager.GetCustomUrlFromUrl(predefUrl);
            ObjectSupport.ReadGridDictionaryInfo info;
            ObjectSupport.ReadGridDictionaryInfo predefInfo = await ObjectSupport.ReadGridDictionaryAsync(package, recordType, YetaWFManager.UrlToPhysical(predefUrl));
            if (!predefInfo.Success)
                throw new InternalError("No grid definition exists for {0}", file);
            info = predefInfo;
            ObjectSupport.ReadGridDictionaryInfo customInfo = await ObjectSupport.ReadGridDictionaryAsync(package, recordType, YetaWFManager.UrlToPhysical(customUrl));
            if (customInfo.Success)
                info = customInfo;
            if (info.ColumnInfo.Count == 0)
                throw new InternalError("No grid definition exists for {0}", file);
            return info;
        }
        public static async Task<List<PropertyListEntry>> GetHiddenGridPropertiesAsync(object obj, GridDefinition gridDef) {
            ObjectSupport.ReadGridDictionaryInfo info = await GridHelper.LoadGridColumnDefinitionsAsync(gridDef);
            return GetHiddenGridProperties(obj, info.ColumnInfo);
        }
        public static async Task<List<PropertyListEntry>> GetHiddenGridPropertiesAsync(object obj) {
            ObjectSupport.ReadGridDictionaryInfo info = await GridHelper.LoadGridColumnDefinitionsAsync(obj.GetType());
            return GetHiddenGridProperties(obj, info.ColumnInfo);
        }
        private static List<PropertyListEntry> GetHiddenGridProperties(object obj, Dictionary<string, GridColumnInfo> dict) {
            List<PropertyListEntry> list = new List<PropertyListEntry>();
            foreach (var d in dict) {
                string propName = d.Key;
                GridColumnInfo gridCol = d.Value;
                if (gridCol.Hidden) {
                    PropertyData prop = ObjectSupport.GetPropertyData(obj.GetType(), propName);
                    list.Add(new PropertyListEntry(prop.Name, prop.GetPropertyValue<object>(obj), "Hidden", false, false, null, null, false, null, SubmitFormOnChangeAttribute.SubmitTypeEnum.None));
                }
            }
            return list;
        }
        public static async Task<List<PropertyListEntry>> GetGridPropertiesAsync(object obj, GridDefinition gridDef) {
            ObjectSupport.ReadGridDictionaryInfo info = await GridHelper.LoadGridColumnDefinitionsAsync(gridDef);
            return GetGridProperties(obj, info.ColumnInfo);
        }
        public static async Task<List<PropertyListEntry>> GetGridPropertiesAsync(object obj) {
            ObjectSupport.ReadGridDictionaryInfo info = await GridHelper.LoadGridColumnDefinitionsAsync(obj.GetType());
            return GetGridProperties(obj, info.ColumnInfo);
        }
        private static List<PropertyListEntry> GetGridProperties(object obj, Dictionary<string, GridColumnInfo> dict) {
            List<PropertyListEntry> list = new List<PropertyListEntry>();
            foreach (var d in dict) {
                string propName = d.Key;
                PropertyData prop = ObjectSupport.GetPropertyData(obj.GetType(), propName);
                list.Add(new PropertyListEntry(prop.Name, prop.GetPropertyValue<object>(obj), prop.UIHint, !prop.ReadOnly, false, null, null, false, null, SubmitFormOnChangeAttribute.SubmitTypeEnum.None));
            }
            return list;
        }
#if MVC6
        public static HtmlString RenderExtraData<TModel>(this IHtmlHelper<TModel> htmlHelper, GridDefinition gridDef) {
#else
        public static HtmlString RenderExtraData<TModel>(this HtmlHelper<TModel> htmlHelper, GridDefinition gridDef) {
#endif
            return new HtmlString(YetaWFManager.HtmlEncode(YetaWFManager.JsonSerialize(gridDef.ExtraData)));
        }
#if MVC6
        public static HtmlString GetDataFieldPrefix<TModel>(this IHtmlHelper<TModel> htmlHelper, GridDefinition gridDef) {
#else
        public static HtmlString GetDataFieldPrefix<TModel>(this HtmlHelper<TModel> htmlHelper, GridDefinition gridDef) {
#endif
            if (gridDef == null)
                throw new InternalError("Need a GridDefinition object");
            // the template should be either ".xxxModel.GridDef" or something like .xxxModel.GridDef.GridDataRecords.record
            // depending on whether this is local data or ajax data
            // we'll get the starting segment(s) and remove .xxxModel.GridDef.GridDataRecords.record - That was introduced by the Grid.cshtml templates
            string prefix = htmlHelper.ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix;
            List<string> segs = prefix.Split(new char[] { '.' }).ToList();
            // possible patterns:
            // 'yourModel.GridDataRecords.record'
            // 'yourModel.xxxModel.GridDef.GridDataRecords.record'
            // 'yourModel.xxxModel.GridDef'
            // remove everything except yourModel
            for (;;) {
                string last = segs.Last();
                if (last == "record")
                    segs.RemoveAt(segs.Count - 1);
                else if (last.StartsWith("GridDataRecords"))
                    segs.RemoveAt(segs.Count - 1);
                else if (last == "GridDef") {
                    segs.RemoveAt(segs.Count - 1);
                    if (segs.Count <= 1) break;
                    segs.RemoveAt(segs.Count - 1);// removes xxxModel which is always present
                } else
                    break;
            }
            if (segs.Count > 0)
                prefix = string.Join(".", segs);
            else
                prefix = "";
            return new HtmlString(prefix);
        }

        public static void NormalizeFilters(Type type, List<DataProviderFilterInfo> filters) {
            if (filters != null) {
                foreach (DataProviderFilterInfo f in filters) {
                    f.NormalizeFilterProperty(type);
                    NormalizeFilters(type, f.Filters);
                }
            }
        }
    }
}