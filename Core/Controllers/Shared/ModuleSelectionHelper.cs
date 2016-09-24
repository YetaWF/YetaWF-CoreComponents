/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Controllers.Shared {
    public class ModuleSelectionHelperController : YetaWFController {
        /// <summary>
        /// Returns data to replace a dropdownlist's data with new modules given a package name.
        /// </summary>
        /// <param name="areaName">The area name of the package.</param>
        /// <returns>JSON containing a data source to update the dropdownlist.</returns>
        [HttpPost]
        [ResourceAuthorize(CoreInfo.Resource_ModuleLists)]
        public ActionResult GetPackageModulesNew(string areaName) {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(ModuleSelectionHelper.RenderReplacementPackageModulesNew(areaName));
            return new JsonResult { Data = sb.ToString() };
        }
        /// <summary>
        /// Returns data to replace a dropdownlist's data with existing designed modules given a package name.
        /// </summary>
        /// <param name="areaName">The area name of the package.</param>
        /// <returns>JSON containing a data source to update the dropdownlist.</returns>
        [HttpPost]
        [ResourceAuthorize(CoreInfo.Resource_ModuleLists)]
        public ActionResult GetPackageModulesDesigned(string areaName) {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(ModuleSelectionHelper.RenderReplacementPackageModulesDesigned(areaName));
            return new JsonResult { Data = sb.ToString() };
        }
    }
}
