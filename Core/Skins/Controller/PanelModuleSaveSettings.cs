/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc;
using System;
using YetaWF.Core.Controllers;
using YetaWF.Core.Support.Repository;

namespace YetaWF.Core.Skins.Controllers {

    /// <summary>
    /// Module support.
    /// </summary>
    public class PanelModuleSaveSettingsController : YetaWFController {

        /// <summary>
        /// Saves a panel module's epxand/collapse status.
        /// </summary>
        /// <remarks>This is invoked by client-side code via Ajax whenever a panel module's expand/collapse status changes.
        ///
        /// Used in conjunction with client-side code.</remarks>
        [AllowPost]
        public ActionResult SaveExpandCollapse(Guid moduleGuid, bool expanded) {
            SettingsDictionary modSettings = Manager.SessionSettings.GetModuleSettings(moduleGuid);
            modSettings.SetValue<bool>("PanelExpanded", expanded);
            modSettings.Save();
            return new EmptyResult();
        }
    }
}
