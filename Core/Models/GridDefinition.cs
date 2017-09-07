/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

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

        public class GridEntryDefinition {
            public GridEntryDefinition(string prefix, int recNumber, object model) {
                if (string.IsNullOrWhiteSpace(prefix))
                    throw new InternalError("Missing prefix argument");
                Prefix = prefix;
                RecNumber = recNumber;
                Model = model;
            }
            public string Prefix { get; private set; }
            public object Model { get; private set; }
            public int RecNumber { get; private set; }
        }

        public enum SortBy {
            NotSpecified = 0, Ascending, Descending
        };
        public class ColumnInfo {
            public SortBy Sort { get; set; }
            public int Width{ get; set; }
        }
        public class ColumnDictionary : SerializableDictionary<string, ColumnInfo> { }

        // set up by application
        public string AjaxUrl { get; set; } // remote data
        public object ExtraData { get; set; }// additional data to return during ajax callback
        public DataSourceResult Data { get; set; } // local data
        public Guid ModuleGuid { get; set; }
        public Type RecordType { get; set; }
        public List<DataProviderFilterInfo> Filters { get; set; }// server side filtering
        public Guid SettingsModuleGuid { get; set; } // the module guid used to save/restore grid settings and is optional
        public bool SupportReload { get; set; } // whether the data can be reloaded by the user (reload button)
        public bool ShowHeader { get; set; }
        public bool SizeToFit { get; set; } // resizes all columns to fit available width
        public string NoRecordsText { get; set; }// text shown when there are no records
        public bool HandleLocalInput { get; set; } // store input in local datasource for submit
        public bool? ShowFilter { get; set; } // if null use user settings, otherwise use ShowFilter true/false overriding any other defaults

        // other settings
        public string Id { get; set; } // html id of the grid
        public const int MaxPages = 999999999;// indicator for All pages in PageSizes
        public List<int> PageSizes { get; set; }
        public int InitialPageSize { get; set; }
        public int PagerButtons { get; set; }// # of paging buttons

        public bool ReadOnly { get; set; }// entire grid is read/only
        public bool CanAddOrDelete { get; set; }// items can be added or deleted (local data)
        public string DeleteProperty { get; set; } // for grid add/delete provide the property used to add/delete a record
        public string DisplayProperty { get; set; } // for grid add/delete provide the property that has the displayable key when adding/deleting a record
        public object ResourceRedirect { get; set; } // redirect for Caption/Description attributes

        // The following items are cached by GridHelper.LoadGridColumnDefinitions - don't mess with it
        public string CachedSortCol { get; set; }
        public GridDefinition.SortBy CachedSortDir { get; set; }
        public Dictionary<string, GridColumnInfo> CachedDict { get; set; }


        // used by templates to communicate record # being rendered
        public int RecordCount { get; set; }

        public GridDefinition() {

            SupportReload = true;
            ShowHeader = true;
            SizeToFit = false;
            NoRecordsText = this.__ResStr("noRecs", "(None)");
            HandleLocalInput = true;
            ShowFilter = null;

            Id = YetaWFManager.Manager.UniqueId("grid");
            PageSizes = new List<int>() { 10, 20, 50 };
            InitialPageSize = 10;
            PagerButtons = 6;
            SettingsModuleGuid = Guid.Empty;
            ReadOnly = true;
            CanAddOrDelete = false;
            DeleteProperty = null;
            DisplayProperty = null;
            ExtraData = null;
        }
        [UIHint("GridDataRecords")]
        public List<object> GridDataRecords { get { return Data.Data; } }
    }
}
