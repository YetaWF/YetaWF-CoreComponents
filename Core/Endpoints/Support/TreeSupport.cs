/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints {

    public class TreeSupport {

        public const string GetRecords = "GetRecords";

        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Data sent from client to update a tree partial view.
        /// </summary>
        public class TreeAdditionPartialViewData<TYPE> : PartialView.PartialViewData {
            public TYPE Entry { get; set; } = default!;
        }

        /// <summary>
        /// Returns rendered tree contents.
        /// </summary>
        /// <remarks>Used for tree components.</remarks>
        public static async Task<IResult> GetTreePartialAsync<TYPE>(HttpContext context, ModuleDefinition? module, PartialView.PartialViewData pvData, TreeDefinition treeModel, List<TYPE> addData) {
            List<object> data = (from l in addData select(object)l).ToList<object>();
            DataSourceResult ds = new DataSourceResult() {
                Data = data,
                Total = data.Count,
            };
            TreePartialData treePartial = new TreePartialData {
                TreeDef = treeModel,
                Data = ds,
            };
            return await PartialView.RenderPartialView(context, "TreePartialDataView", module, pvData, treePartial, "application/json");
        }
    }
}
