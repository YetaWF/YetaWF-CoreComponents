/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    /// <summary>
    /// This static class implements services used by the Grid component.
    /// </summary>
    public static class Grid {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Grid), name, defaultValue, parms); }

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Defines the appearance of actions in a grid.
        /// </summary>
        public enum GridActionsEnum {
            /// <summary>
            /// Actions in grids are displayed as icons.
            /// </summary>
            [EnumDescription("Icons", "Actions in grids are displayed as icons")]
            Icons = 0,
            /// <summary>
            /// If more than one action is available they are displayed as a dropdown menu, accessible through a button, otherwise a single icon is displayed.
            /// </summary>
            [EnumDescription("Dropdown Menu", "If more than one action is available they are displayed as a dropdown menu, accessible through a button, otherwise a single icon is displayed")]
            DropdownMenu = 1,
        }

        /// <summary>
        /// This class implements grid layout, sorting and filtering information, so this information can be saved when it is updated by user actions, so it can be restored later (for example, if a page is reloaded).
        /// Applications should not manipulate these settings directly.
        /// </summary>
        public class GridSavedSettings {

            /// <summary>
            /// Defines the columns and their current settings.
            /// </summary>
            public GridDefinition.ColumnDictionary Columns { get; set; }

            /// <summary>
            /// Defines the current grid page size, i.e., the maximum number of records shown per page.
            /// </summary>
            public int PageSize { get; set; }

            /// <summary>
            /// Defines the current page number shown. Page numbers are 1 based. These should be 0 based as they are 0 based in other classes, but oh well. Someday....
            /// </summary>
            public int CurrentPage { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            public GridSavedSettings() {
                Columns = new GridDefinition.ColumnDictionary();
                PageSize = 10;
                CurrentPage = 1;
            }

            /// <summary>
            /// Returns the current sort order for columns.
            /// </summary>
            /// <returns>A list of columns that have a defined sort order.</returns>
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
            /// <summary>
            /// Returns the current filter settings for columns.
            /// </summary>
            /// <returns>A list of columns that have a defined filter setting.</returns>
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
        /// <summary>
        /// Loads grid settings that have been previously saved for a specific module.
        /// If no saved settings are available, default settings are returned.
        /// </summary>
        /// <param name="moduleGuid">The module Guid of the module for which grid settings have been saved.</param>
        /// <param name="defaultInitialPage">Defines the default initial page within the grid. This page number is 1 based.</param>
        /// <param name="defaultPageSize">Defines the default initial page size of the grid.</param>
        /// <returns>Returns grid settings for the specified module.</returns>
        /// <remarks>Grid settings that are saved on behalf of modules are used whenever the module is displayed. This means that the same settings apply even if a module is used on several pages.
        ///
        /// This method is not used by applications. It is reserved for component implementation.</remarks>
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
        /// <summary>
        /// Save grid settings for a specific module.
        /// </summary>
        /// <param name="moduleGuid">The module Guid of the module for which grid settings are saved.</param>
        /// <param name="gridSavedSettings">The grid settings to be saved.</param>
        /// <remarks>This method is not used by applications. It is reserved for component implementation.</remarks>
        public static void SaveModuleSettings(Guid moduleGuid, GridSavedSettings gridSavedSettings) {
            SettingsDictionary modSettings = Manager.SessionSettings.GetModuleSettings(moduleGuid);
            if (modSettings != null) {
                modSettings.SetValue<GridSavedSettings>("GridSavedSettings", gridSavedSettings);
                modSettings.Save();
            }
        }

        internal static void SaveSettings(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filter, Guid? settingsModuleGuid = null) {

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
        /// Used to change a sort column when another column is used to sort rather than the displayed property.
        /// This method translates the displayed property name to the actual sort column name.
        /// </summary>
        /// <remarks>
        /// This is typically used when one property is displayed in a grid, but another property should be used for sort purposes.
        /// It is called before retrieving data from a data provider to translate the grid's column(s) to the column(s) used to sort/filter by the data provider.
        ///
        /// The parameter <paramref name="sort"/> only supports 1 sort field.
        /// </remarks>
        /// <param name="displayProperty">The name of the property as displayed by the grid.</param>
        /// <param name="realColumn">The name of the property that should be used by the data provider for sort/filter purposes.</param>
        /// <param name="filters">A collection describing the filtering criteria.</param>
        /// <param name="sort">A collection describing the sort order.</param>
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
        /// <summary>
        /// Loads the grid column definitions for a grid.
        /// </summary>
        /// <param name="gridDef">The GridDefinition object describing the grid.</param>
        /// <returns>A ObjectSupport.ReadGridDictionaryInfo object describing the grid.</returns>
        /// <remarks>This method is not used by applications. It is reserved for component implementation.</remarks>
        public static async Task<ObjectSupport.ReadGridDictionaryInfo> LoadGridColumnDefinitionsAsync(GridDefinition gridDef) {
            if (gridDef.CachedDict == null)
                gridDef.CachedDict = await LoadGridColumnDefinitionsAsync(gridDef.RecordType);
            return gridDef.CachedDict;
        }

        /// <summary>
        /// Loads the grid column definitions for a grid based on its record type.
        /// </summary>
        /// <param name="recordType">The record type for which grid column definitions are to be loaded.</param>
        /// <returns>A ObjectSupport.ReadGridDictionaryInfo object describing the grid.</returns>
        /// <remarks>This method is not used by applications. It is reserved for component implementation.</remarks>
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
            ObjectSupport.ReadGridDictionaryInfo predefInfo = await ObjectSupport.ReadGridDictionaryAsync(package, recordType, Utility.UrlToPhysical(predefUrl));
            if (!predefInfo.Success)
                throw new InternalError("No grid definition exists for {0}", file);
            info = predefInfo;
            ObjectSupport.ReadGridDictionaryInfo customInfo = await ObjectSupport.ReadGridDictionaryAsync(package, recordType, Utility.UrlToPhysical(customUrl));
            if (customInfo.Success)
                info = customInfo;
            if (info.ColumnInfo.Count == 0)
                throw new InternalError("No grid definition exists for {0}", file);
            return info;
        }
        /// <summary>
        /// Normalize filters.
        /// RESEARCH! The purpose of this method is unclear.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="filters">A collection describing the filtering criteria.</param>
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