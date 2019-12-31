/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Data providers return collections of data objects using a DataSourceResult class instance.
    /// </summary>
    public class DataSourceResult {
        /// <summary>
        /// A collection of objects.
        /// </summary>
        public List<object> Data { get; set; } // one page of data
        /// <summary>
        /// The total number of records that satisfy the search criteria.
        /// Data providers may implement paging within a dataset, in which case the Total property does not reflect the size of the Data collection.
        /// </summary>
        public int Total { get; set; } // total # of records
    }
}