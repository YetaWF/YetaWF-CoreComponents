/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Repository;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
#endif

namespace YetaWF.Core.Components {

    public static class Grid {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Grid), name, defaultValue, parms); }

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

            public List<DataProviderSortInfo> GetSortInfo() {
                foreach (var keyVal in Columns) {
                    string colName = keyVal.Key;
                    GridDefinition.ColumnInfo col = keyVal.Value;
                    if (col.Sort != GridDefinition.SortBy.NotSpecified) {
                        return new List<DataProviderSortInfo>() {
                            new DataProviderSortInfo {
                                Field = colName,
                                Order = col.Sort == GridDefinition.SortBy.Descending ? DataProviderSortInfo.SortDirection.Descending : DataProviderSortInfo.SortDirection.Ascending ,
                            },
                        };
                    }
                }
                return null;
            }
            public List<DataProviderFilterInfo> GetFilterInfo() {
                List<DataProviderFilterInfo> list = new List<DataProvider.DataProviderFilterInfo>();
                foreach (var keyVal in Columns) {
                    string colName = keyVal.Key;
                    GridDefinition.ColumnInfo col = keyVal.Value;
                    if (!string.IsNullOrWhiteSpace(col.FilterOperator)) {
                        list.Add(new DataProviderFilterInfo {
                            Field = colName,
                            Operator = col.FilterOperator,
                            ValueAsString = col.FilterValue,
                        });
                    }
                }
                return list.Count > 0 ? list : null;
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

        public static void SaveSettings(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filter, Guid? settingsModuleGuid = null) {

            // save the current sort order and page size
            if (settingsModuleGuid != null && settingsModuleGuid != Guid.Empty) {
                Grid.GridSavedSettings gridSavedSettings = Grid.LoadModuleSettings((Guid)settingsModuleGuid);
                gridSavedSettings.PageSize = take;
                if (take == 0)
                    gridSavedSettings.CurrentPage = 1;
                else
                    gridSavedSettings.CurrentPage = Math.Max(1, skip / take + 1);
                foreach (GridDefinition.ColumnInfo col in gridSavedSettings.Columns.Values)
                    col.Sort = GridDefinition.SortBy.NotSpecified;
                if (sort != null) {
                    foreach (var sortCol in sort) {
                        GridDefinition.SortBy sortDir = (sortCol.Order == DataProviderSortInfo.SortDirection.Ascending) ? GridDefinition.SortBy.Ascending : GridDefinition.SortBy.Descending;
                        if (gridSavedSettings.Columns.ContainsKey(sortCol.Field))
                            gridSavedSettings.Columns[sortCol.Field].Sort = sortDir;
                        else
                            gridSavedSettings.Columns.Add(sortCol.Field, new GridDefinition.ColumnInfo { Sort = sortDir });
                    }
                }
                foreach (GridDefinition.ColumnInfo col in gridSavedSettings.Columns.Values) {
                    col.FilterOperator = null;
                    col.FilterValue = null;
                }
                if (filter != null) {
                    foreach (var filterCol in filter) {
                        if (gridSavedSettings.Columns.ContainsKey(filterCol.Field)) {
                            gridSavedSettings.Columns[filterCol.Field].FilterOperator = filterCol.Operator;
                            gridSavedSettings.Columns[filterCol.Field].FilterValue = filterCol.ValueAsString;
                        } else {
                            gridSavedSettings.Columns.Add(filterCol.Field, new GridDefinition.ColumnInfo {
                                FilterOperator = filterCol.Operator,
                                FilterValue = filterCol.ValueAsString,
                            });
                        }
                    }
                }
                Grid.SaveModuleSettings((Guid)settingsModuleGuid, gridSavedSettings);
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
        public static async Task<ObjectSupport.ReadGridDictionaryInfo> LoadGridColumnDefinitionsAsync(Grid2Definition gridDef) {
            if (gridDef.CachedDict == null)
                gridDef.CachedDict = await LoadGridColumnDefinitionsAsync(gridDef.RecordType);
            return gridDef.CachedDict;
        }

        public class LoadGridColumnDefinitionsInfo {
            public string SortColumn { get; set; }
            public GridDefinition.SortBy SortBy { get; set; }
        }

        public static async Task<ObjectSupport.ReadGridDictionaryInfo> LoadGridColumnDefinitionsAsync(Type recordType) {
            Dictionary<string, GridColumnInfo> dict = new Dictionary<string, GridColumnInfo>();
            string className = recordType.FullName.Split(new char[] { '.' }).Last();
            string[] s = className.Split(new char[] { '+' });
            int len = s.Length;
            if (len != 2) throw new InternalError("Unexpected class {0} in record type {1}", className, recordType.FullName);
            string controller = s[0];
            string model = s[1];
            string file = controller + "." + model;
            Package package = Package.GetPackageFromType(recordType);
            string predefUrl = VersionManager.GetAddOnPackageUrl(package.AreaName) + "Grids/" + file;
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