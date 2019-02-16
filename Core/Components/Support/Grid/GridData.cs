/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Models;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Describes the records to be rendered for a grid.
    ///
    /// This class is not used by applications. It is reserved for component implementation.
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
