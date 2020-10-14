/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

    /// <summary>
    /// An instance of this class describes a grid. The implementation of the grid is deferred to a component provider.
    /// </summary>
    public class GridDefinition {

        protected static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(GridDefinition), name, defaultValue, parms); }

        public enum SortBy {
            NotSpecified = 0, Ascending, Descending
        };

        public enum SizeStyleEnum {
            SizeGiven = 0,
            SizeToFit = 1,
            SizeAuto = 2,
        }

        public class ColumnInfo {
            public SortBy Sort { get; set; }
            public int Width { get; set; }
            public string FilterOperator { get; set; }
            public string FilterValue { get; set; }

            public ColumnInfo() {
                Width = -1;
            }
        }
        public class ColumnDictionary : SerializableDictionary<string, ColumnInfo> { }

        // set up by application
        public Type RecordType { get; set; }
        public string AjaxUrl { get; set; } // remote data
        public Func<int, int, List<DataProviderSortInfo>, List<DataProviderFilterInfo>, Task<DataSourceResult>> DirectDataAsync { get; set; }
        public Func<List<object>, int, int, List<DataProviderSortInfo>, List<DataProviderFilterInfo>, DataSourceResult> SortFilterStaticData { get; set; }

        public object ExtraData { get; set; }// additional data to return during ajax callback

        public Guid ModuleGuid { get; set; } // the module owning the grid
        public Guid? SettingsModuleGuid { get; set; } // the module guid used to save/restore grid settings and is optional

        public bool SupportReload { get; set; } // whether the data can be reloaded by the user (reload button, ajax only)
        public bool ShowHeader { get; set; }
        public bool? ShowFilter { get; set; } // if null use user settings, otherwise use ShowFilter true/false overriding any other defaults
        public bool ShowPager { get; set; }
        public bool Reorderable { get; set; }
        public SizeStyleEnum SizeStyle { get; set; }
        public bool HighlightOnClick { get; set; }

        public ColumnDictionary InitialFilters { get; set; }

        public string NoRecordsText { get; set; }// text shown when there are no records

        public int InitialPageSize { get; set; }
        public List<int> PageSizes { get; set; }
        public const int MaxPages = 999999999;// indicator for All pages in PageSizes

        public bool UseSkinFormatting { get; set; } // use skin theme (jquery-ui)
        public int? DropdownActionWidth { get; set; } // width in characters of action dropdown

        // other settings
        public string Id { get; set; } // html id of the grid

        // Delete record (static only)
        public string DeletedMessage { get; set; }
        public string DeleteConfirmationMessage { get; set; }
        public string DeletedColumnDisplay { get; set; }

        public object ResourceRedirect { get; set; } // redirect for Caption/Description attributes

        public bool IsStatic { get { return SortFilterStaticData != null; } }

        // The following can be used by a component implementation to cache data for the duration of the GridDefinition object.
        public object CachedData { get; set; }

        public GridDefinition() {
            SupportReload = true;
            ShowHeader = true;
            ShowPager = true;
            NoRecordsText = __ResStr("noRecs", "(None)");
            ShowFilter = null;
            UseSkinFormatting = true;
            HighlightOnClick = true;
            InitialFilters = new ColumnDictionary();
            SizeStyle = GridDefinition.SizeStyleEnum.SizeToFit;

            Id = YetaWFManager.Manager.UniqueId("grid");
            PageSizes = new List<int>() { 10, 20, 50 };
            InitialPageSize = 10;
            SettingsModuleGuid = null;
            ExtraData = null;
        }
        public static DataSourceResult DontSortFilter(List<object> data, int skip, int take, List<DataProviderSortInfo> sorts, List<DataProviderFilterInfo> filters) {
            return new DataSourceResult {
                Data = data,
                Total = data.Count,
            };
        }
    }

    /// <summary>
    /// This static class defines basic services offered by the Grid component.
    /// </summary>
    public static class Grid {

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
            /// <summary>
            /// Displayed as a dropdown menu, accessible through a small button without text.
            /// </summary>
            [EnumDescription("Mini Dropdown Menu", "Displayed as a dropdown menu, accessible through a small button without text")]
            Mini = 2,
        }

        /// <summary>
        /// Defines the appearance of actions in a grid.
        /// </summary>
        public enum GridActionsUserEnum {
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
    }

    /// <summary>
    /// Describes the records to be rendered for a grid.
    ///
    /// This class is not used by applications. It is reserved for component implementation.
    /// An instance of the GridPartialData class defines all data to be rendered to replace a grid component's contents.
    /// The implementation of rendering the grid data is deferred to a component provider.
    /// </summary>
    public class GridPartialData {
        /// <summary>
        /// The prefix to be prepended to any field name generated.
        /// </summary>
        public string FieldPrefix { get; set; }
        /// <summary>
        /// The GridDefinition object describing the current grid.
        /// </summary>
        public GridDefinition GridDef { get; set; }
        /// <summary>
        /// The collection of data to be rendered.
        /// </summary>
        public DataSourceResult Data { get; set; }
        /// <summary>
        /// The collection of static data to be rendered.
        /// </summary>
        public List<object> StaticData { get; set; }
        /// <summary>
        /// The number of records skipped (paging).
        /// </summary>
        public int Skip { get; set; }
        /// <summary>
        /// The number of records retrieved (paging).
        /// </summary>
        public int Take { get; set; }
        /// <summary>
        /// The sort order of the grid's columns.
        /// </summary>
        public List<DataProviderSortInfo> Sorts { get; set; }
        /// <summary>
        /// The filter options for the grid.
        /// </summary>
        public List<DataProviderFilterInfo> Filters { get; set; }
    }
    /// <summary>
    /// Describes one grid record.
    ///
    /// This class is not used by applications. It is reserved for component implementation.
    /// An instance of the GridRecordData class defines one record to be rendered by a grid component.
    /// The implementation of rendering the grid data is deferred to a component provider.
    /// </summary>
    public class GridRecordData {
        /// <summary>
        /// The GridDefinition object describing the current grid.
        /// </summary>
        public GridDefinition GridDef { get; set; }
        /// <summary>
        /// The data representing the current record.
        /// </summary>
        public object Data { get; set; }
        /// <summary>
        /// The static data representing the current record.
        /// </summary>
        public string StaticData { get; set; }
        /// <summary>
        /// The prefix to be prepended to any field name generated.
        /// </summary>
        public string FieldPrefix { get; set; }
    }
}
