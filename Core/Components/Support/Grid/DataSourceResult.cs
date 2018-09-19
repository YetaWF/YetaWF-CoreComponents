/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;

namespace YetaWF.Core.Components {

    public class DataSourceResult {
        public List<object> Data { get; set; } // one page of data
        public int Total { get; set; } // total # of records
        public string FieldPrefix { get; set; } // Html field prefix (only used for partial rendering) //$$$remove

        public int RecordCount { get; set; }// used by templates to communicate record # being rendered //$$$remove
    }
}