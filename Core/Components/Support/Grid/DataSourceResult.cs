/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;

namespace YetaWF.Core.Components {

    public class DataSourceResult {
        public List<object> Data { get; set; } // one page of data
        public int Total { get; set; } // total # of records
    }
}