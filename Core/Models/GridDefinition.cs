/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    public class GridColumnInfo {
        public int ChWidth { get; set; }
        public int PixWidth { get; set; }
        public bool Sortable { get; set; }
        public bool Locked { get; set; }
        public bool Hidden { get; set; }
        public bool OnlySubmitWhenChecked { get; set; }
        public GridHAlignmentEnum Alignment { get; set; }
        public int Icons { get; set; }
        public List<FilterOptionEnum> FilterOptions { get; set; }
        public enum FilterOptionEnum {
            Equal = 1,
            NotEqual,
            LessThan,
            LessEqual,
            GreaterThan,
            GreaterEqual,
            StartsWith,
            NotStartsWith,
            Contains,
            NotContains,
            Endswith,
            NotEndswith,
            All = 0xffff,
        }

        public GridColumnInfo() {
            PixWidth = ChWidth = 0;
            Sortable = false;
            Locked = false;
            Hidden = false;
            OnlySubmitWhenChecked = false;
            Alignment = GridHAlignmentEnum.Unspecified;
            Icons = 0;
            FilterOptions = new List<FilterOptionEnum>();
        }
    }

    public class GridDefinition {

        public enum SortBy {
            NotSpecified = 0, Ascending, Descending
        };
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
    }

    public class Grid2Definition {

        public enum SizeStyleEnum {
            SizeGiven = 0,
            SizeToFit = 1,
            SizeAuto = 2,
        }

        // set up by application
        public Type RecordType { get; set; }
        public string AjaxUrl { get; set; } // remote data
        public Func<int, int, List<DataProviderSortInfo>, List<DataProviderFilterInfo>, Task<DataSourceResult>> DirectDataAsync { get; set; }
        public Func<List<object>, int, int, List<DataProviderSortInfo>, List<DataProviderFilterInfo>, DataSourceResult> SortFilterStaticData { get; set; }

        public object ExtraData { get; set; }// additional data to return during ajax callback

        public Guid ModuleGuid { get; set; }
        public Guid? SettingsModuleGuid { get; set; } // the module guid used to save/restore grid settings and is optional

        public bool SupportReload { get; set; } // whether the data can be reloaded by the user (reload button, ajax only)
        public bool ShowHeader { get; set; }
        public bool? ShowFilter { get; set; } // if null use user settings, otherwise use ShowFilter true/false overriding any other defaults
        public bool ShowPager { get; set; }
        public string NoRecordsText { get; set; }// text shown when there are no records
        public SizeStyleEnum SizeStyle { get; set; }

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

        // The following items are cached by GridHelper.LoadGridColumnDefinitions - don't mess with it
        public ObjectSupport.ReadGridDictionaryInfo CachedDict { get; set; }

        public Grid2Definition() {

            SupportReload = true;
            ShowHeader = true;
            ShowPager = true;
            NoRecordsText = this.__ResStr("noRecs", "(None)");
            ShowFilter = null;
            UseSkinFormatting = true;

            Id = YetaWFManager.Manager.UniqueId("grid");
            PageSizes = new List<int>() { 10, 20, 50 };
            InitialPageSize = 10;
            SettingsModuleGuid = null;
            ExtraData = null;
        }
    }
}
