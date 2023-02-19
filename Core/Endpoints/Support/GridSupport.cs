/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints {

    public class GridSupport {

        public const string BrowseGridData = "BrowseGridData";
        public const string DisplaySortFilter = "DisplaySortFilter";
        public const string EditSortFilter = "EditSortFilter";

        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Data sent from client to update a grid partial view.
        /// </summary>
        public class GridPartialViewData : PartialView.PartialViewData {
            public string Data { get; set; } = null!;
            public string FieldPrefix { get; set; } = null!;
            public int Skip { get; set; }
            public int Take { get; set; }
            public bool Search { get; set; }
            public List<DataProviderSortInfo>? Sorts { get; set; }
            public List<DataProviderFilterInfo>? Filters { get; set; }

            /// <summary>
            /// Changes filter logic for string search.
            /// </summary>
            internal void UpdateSearchLogic() {
                if (Search && Filters != null) {
                    foreach (DataProviderFilterInfo filter in Filters)
                        filter.Logic = "||";
                }
            }
        }

        public class GridAdditionPartialViewData<TYPE> : PartialView.PartialViewData {
            public List<TYPE> GridData { get; set; } = null!;
        }

        /// <summary>
        /// Returns rendered grid contents.
        /// </summary>
        /// <returns>Returns rendered grid contents.</returns>
        public static async Task<IResult> GetGridPartialAsync(HttpContext context, ModuleDefinition? module, GridDefinition gridModel, GridPartialViewData gridPvData) {
            gridPvData.UpdateSearchLogic();
            DataSourceResult ds = await gridModel.DirectDataAsync(gridPvData.Skip, gridPvData.Take, gridPvData.Sorts?.ToList(), gridPvData.Filters?.ToList());// copy sort/filter in case changes are made (we save this later)
            return await GetGridPartialAsync(context, module, gridModel, ds, gridPvData);
        }

        /// <summary>
        /// Returns rendered grid contents.
        /// </summary>
        /// <returns>Returns rendered grid contents.</returns>
        /// <remarks>Used for static grids.</remarks>
        public static async Task<IResult> GetGridPartialAsync<TYPE>(HttpContext context, ModuleDefinition? module, GridDefinition gridModel, GridPartialViewData gridPvData) {
            List<TYPE> list = Utility.JsonDeserialize<List<TYPE>>(gridPvData.Data);
            List<object> objList = (from l in list select (object)l).ToList();
            gridPvData.UpdateSearchLogic();
            DataSourceResult ds = gridModel.SortFilterStaticData!(objList, 0, int.MaxValue, gridPvData.Sorts?.ToList(), gridPvData.Filters?.ToList());// copy sort/filter in case changes are made (we save this later)
            return await GetGridPartialAsync(context, module, gridModel, ds, gridPvData, objList);
        }

        private static async Task<IResult> GetGridPartialAsync(HttpContext context, ModuleDefinition? module, GridDefinition gridModel, DataSourceResult data, GridPartialViewData gridPvData, List<object>? objList = null) {
            GridPartialData gridPartialModel = new GridPartialData() {
                Data = data,
                StaticData = objList,
                Skip = gridPvData.Skip,
                Take = gridPvData.Take,
                Sorts = gridPvData.Sorts,
                Filters = gridPvData.Filters,
                Search = gridPvData.Search,
                FieldPrefix = gridPvData.FieldPrefix,
                GridDef = gridModel,
            };
            return await PartialView.RenderPartialView(context, "GridPartialDataView", module, gridPvData, gridPartialModel, "application/json", PureContent: true);
        }

        /// <summary>
        /// Returns a rendered grid record.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pvData"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static async Task<IResult> GetGridRecordAsync(HttpContext context, PartialView.PartialViewData pvData, GridRecordData model) {
            return await PartialView.RenderPartialView(context, "GridRecord", null, pvData, model, "application/json", PureContent: true);
        }
    }
}
