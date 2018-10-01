/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Models;
using YetaWF.Core.Components;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Grid template support.
    /// </summary>
    public class GridSaveSettingsController : YetaWFController {

        /// <summary>
        /// Saves a grid's user-defined column widths.
        /// </summary>
        /// <remarks>This is invoked by client-side code via Ajax whenever a grid's column widths change.
        ///
        /// Used in conjunction with client-side code and the ModuleSelection template.</remarks>
        [AllowPost]
        public ActionResult GridSaveColumnWidths(Guid settingsModuleGuid, Dictionary<string, int> columns) {
            Grid.GridSavedSettings gridSavedSettings = Grid.LoadModuleSettings(settingsModuleGuid);
            foreach (var col in columns) {
                if (gridSavedSettings.Columns.ContainsKey(col.Key))
                    gridSavedSettings.Columns[col.Key].Width = col.Value;
                else
                    gridSavedSettings.Columns.Add(col.Key, new GridDefinition.ColumnInfo() { Width = col.Value });
            }
            Grid.SaveModuleSettings(settingsModuleGuid, gridSavedSettings);
            return new EmptyResult();
        }
    }
}
