/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using YetaWF.Core.Models;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Controllers.Shared
{
    public class GridHelperController : YetaWFController {

        /// <summary>
        /// Save a grid's user-defined column widths
        /// </summary>
        [HttpPost]
        public ActionResult GridSaveColumnWidths(Guid settingsModuleGuid, Dictionary<string,int> columns)
        {
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
