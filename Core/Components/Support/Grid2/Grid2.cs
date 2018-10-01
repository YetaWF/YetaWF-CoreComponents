using System.Collections.Generic;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Models;

namespace YetaWF.Core.Components {

    public class Grid2PartialData {
        public string FieldPrefix { get; set; }
        public Grid2Definition GridDef { get; set; }
        public DataSourceResult Data { get; set; }
        public List<object> StaticData { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<DataProviderSortInfo> Sorts { get; set; }
        public List<DataProviderFilterInfo> Filters { get; set; }
    }
    public class Grid2RecordData {
        public Grid2Definition GridDef { get; set; }
        public object Data { get; set; }
        public string StaticData { get; set; }
        public string FieldPrefix { get; set; }
    }
}
