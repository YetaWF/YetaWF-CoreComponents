/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Repository;

namespace YetaWF.Core.Views.Shared {

    public class Grid<TModel> : RazorTemplate<TModel> { }

    public class DataSourceResult {
        public List<object> Data { get; set; } // one page of data
        public int Total { get; set; } // total # of records
        public string FieldPrefix { get; set; } // Html field prefix (only used for partial rendering)

        public int RecordCount { get; set; }// used by templates to communicate record # being rendered
    }

    public static class GridHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

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
        public static MvcHtmlString GetSettingsSaveColumnWidthsUrl(this HtmlHelper<object> htmlHelper) {
            string settingsSaveUrl = YetaWFManager.UrlFor(typeof(YetaWF.Core.Controllers.Shared.GridHelperController), "GridSaveColumnWidths");
            return MvcHtmlString.Create(settingsSaveUrl);
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
        public static Dictionary<string, GridColumnInfo> LoadGridColumnDefinitions(GridDefinition gridDef, ref string sortCol, ref GridDefinition.SortBy sortDir) {
            if (gridDef.CachedDict != null) {
                sortCol = gridDef.CachedSortCol;
                sortDir = gridDef.CachedSortDir;
            } else {
                gridDef.CachedDict = LoadGridColumnDefinitions(gridDef.RecordType, ref sortCol, ref sortDir);
                gridDef.CachedSortCol = sortCol;
                gridDef.CachedSortDir = sortDir;
            }
            return gridDef.CachedDict;
        }
        public static Dictionary<string, GridColumnInfo> LoadGridColumnDefinitions(Type recordType, ref string sortCol, ref GridDefinition.SortBy sortDir) {
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
            string predefUrl = VersionManager.GetAddOnModuleUrl(package.Domain, package.Product) + "Grids/" + file;
            string customUrl = VersionManager.GetCustomUrlFromUrl(predefUrl);
            Dictionary<string, GridColumnInfo> predefDict = ObjectSupport.ReadGridDictionary(package, recordType, YetaWFManager.UrlToPhysical(predefUrl), ref sortCol, ref sortDir);
            Dictionary<string, GridColumnInfo> customDict = ObjectSupport.ReadGridDictionary(package, recordType, YetaWFManager.UrlToPhysical(customUrl), ref sortCol, ref sortDir);
            foreach (var p in predefDict) dict[p.Key] = p.Value;
            foreach (var p in customDict) dict[p.Key] = p.Value;
            if (dict.Count == 0)
                throw new InternalError("No grid definition exists for {0}", file);
            return dict;
        }
        public static List<PropertyListEntry> GetHiddenGridProperties(object obj, GridDefinition gridDef) {
            string sortCol = null;
            GridDefinition.SortBy sortDir = GridDefinition.SortBy.NotSpecified;
            Dictionary<string, GridColumnInfo> dict = GridHelper.LoadGridColumnDefinitions(gridDef, ref sortCol, ref sortDir);
            return GetHiddenGridProperties(obj, dict);
        }
        public static List<PropertyListEntry> GetHiddenGridProperties(object obj) {
            string sortCol = null;
            GridDefinition.SortBy sortDir = GridDefinition.SortBy.NotSpecified;
            Dictionary<string, GridColumnInfo> dict = GridHelper.LoadGridColumnDefinitions(obj.GetType(), ref sortCol, ref sortDir);
            return GetHiddenGridProperties(obj, dict);
        }
        private static List<PropertyListEntry> GetHiddenGridProperties(object obj, Dictionary<string, GridColumnInfo> dict) {
            List<PropertyListEntry> list = new List<PropertyListEntry>();
            foreach (var d in dict) {
                string propName = d.Key;
                GridColumnInfo gridCol = d.Value;
                if (gridCol.Hidden) {
                    PropertyData prop = ObjectSupport.GetPropertyData(obj.GetType(), propName);
                    list.Add(new PropertyListEntry(prop.Name, prop.GetPropertyValue<object>(obj), "Hidden", false, false, null, null, false, SubmitFormOnChangeAttribute.SubmitTypeEnum.None));
                }
            }
            return list;
        }
        public static List<PropertyListEntry> GetGridProperties(object obj, GridDefinition gridDef) {
            string sortCol = null;
            GridDefinition.SortBy sortDir = GridDefinition.SortBy.NotSpecified;
            Dictionary<string, GridColumnInfo> dict = GridHelper.LoadGridColumnDefinitions(gridDef, ref sortCol, ref sortDir);
            return GetGridProperties(obj, dict);
        }
        public static List<PropertyListEntry> GetGridProperties(object obj) {
            string sortCol = null;
            GridDefinition.SortBy sortDir = GridDefinition.SortBy.NotSpecified;
            Dictionary<string, GridColumnInfo> dict = GridHelper.LoadGridColumnDefinitions(obj.GetType(), ref sortCol, ref sortDir);
            return GetGridProperties(obj, dict);
        }
        private static List<PropertyListEntry> GetGridProperties(object obj, Dictionary<string, GridColumnInfo> dict) {
            List<PropertyListEntry> list = new List<PropertyListEntry>();
            foreach (var d in dict) {
                string propName = d.Key;
                PropertyData prop = ObjectSupport.GetPropertyData(obj.GetType(), propName);
                list.Add(new PropertyListEntry(prop.Name, prop.GetPropertyValue<object>(obj), prop.UIHint, !prop.ReadOnly, false, null, null, false, SubmitFormOnChangeAttribute.SubmitTypeEnum.None));
            }
            return list;
        }
        public static MvcHtmlString RenderExtraData<TModel>(this HtmlHelper<TModel> htmlHelper, GridDefinition gridDef) {
            return MvcHtmlString.Create(YetaWFManager.HtmlEncode(YetaWFManager.Jser.Serialize(gridDef.ExtraData)));
        }

        public static MvcHtmlString GetDataFieldPrefix<TModel>(this HtmlHelper<TModel> htmlHelper, GridDefinition gridDef) {
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
            return MvcHtmlString.Create(prefix);
        }

        public static void NormalizeFilters(Type type, List<DataProviderFilterInfo> filters) {
            if (filters != null) {
                foreach (DataProviderFilterInfo f in filters) {
                    if (f.Value != null && f.Value.GetType() == typeof(string[])) {
                        // change the string[] to a string
                        string[] vals = (string[])f.Value;
                        if (vals.Length == 1) {
                            f.Value = vals[0];
                            NormalizeFilterProperty(f, type, f.Field);
                        }
                    } else if (f.Filters != null) {
                        NormalizeFilters(type, f.Filters);
                    }
                }
            }
        }
        private static void NormalizeFilterProperty(DataProviderFilterInfo filter, Type type, string field) {
            string[] parts = field.Split(new char[] { '.' });
            Type objType = type;
            PropertyData prop = null;
            foreach (string part in parts) {
                prop = ObjectSupport.GetPropertyData(objType, part);
                if (prop == null) throw new InternalError("Property {0} not found in type {1}", part, objType.Name);
                objType = prop.PropInfo.PropertyType;
            }
            if (prop == null) throw new InternalError("Can't evaluate field {0} in type {1}", field, objType.Name);
            if (objType != typeof(string)) {
                if (objType.IsEnum) {
                    try { filter.Value = Convert.ToInt32(filter.Value); filter.Value = Enum.ToObject(objType, filter.Value); } catch (Exception) { }
                } else if (objType == typeof(DateTime) || objType == typeof(DateTime?)) {
                    try { filter.Value = Localize.Formatting.GetUtcDateTime(Convert.ToDateTime(filter.Value)); } catch (Exception) { filter.Value = DateTime.MinValue; }
                } else if (objType == typeof(int) || objType == typeof(int?)) {
                    try { filter.Value = Convert.ToInt32(filter.Value); } catch (Exception) { filter.Value = 0; }
                } else if (objType == typeof(long) || objType == typeof(long?)) {
                    try { filter.Value = Convert.ToInt64(filter.Value); } catch (Exception) { filter.Value = 0; }
                } else if (objType == typeof(bool) || objType == typeof(bool?)) {
                    try { filter.Value = Convert.ToBoolean(filter.Value); } catch (Exception) { filter.Value = true; }
                } else if (objType == typeof(MultiString)) {
                    try { filter.Value = new MultiString((string)filter.Value); } catch (Exception) { filter.Value = new MultiString(); }
                }
            }
        }
    }
}