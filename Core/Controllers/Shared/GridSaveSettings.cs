/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Models;
using YetaWF.Core.Views.Shared;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers.Shared {

    /// <summary>
    /// Grid template support.
    /// </summary>
    public class GridHelperController : YetaWFController {

        /// <summary>
        /// Saves a grid's user-defined column widths.
        /// </summary>
        /// <remarks>This is invoked by client-side code via Ajax whenever a grid's column widths change.
        ///
        /// Used in conjunction with client-side code and the ModuleSelection template.</remarks>
        [HttpPost]
        public ActionResult GridSaveColumnWidths(Guid settingsModuleGuid, Dictionary<string, int> columns) {
            GridHelper.GridSavedSettings gridSavedSettings = GridHelper.LoadModuleSettings(settingsModuleGuid);
            foreach (var col in columns) {
                if (gridSavedSettings.Columns.ContainsKey(col.Key))
                    gridSavedSettings.Columns[col.Key].Width = col.Value;
                else
                    gridSavedSettings.Columns.Add(col.Key, new GridDefinition.ColumnInfo() { Width = col.Value });
            }
            GridHelper.SaveModuleSettings(settingsModuleGuid, gridSavedSettings);
            return new EmptyResult();
        }
    }
}
