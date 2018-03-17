/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers.Shared {

    /// <summary>
    /// ModuleSelection template support.
    /// </summary>
    public class ModuleSelectionHelperController : YetaWFController {
        /// <summary>
        /// Returns data to replace a dropdownlist's data with new modules given a package name.
        /// </summary>
        /// <param name="areaName">The area name of the package.</param>
        /// <returns>JSON containing a data source to update the dropdownlist.
        ///
        /// Used in conjunction with client-side code and the ModuleSelection template.</returns>
        [AllowPost]
        [ResourceAuthorize(CoreInfo.Resource_ModuleLists)]
        public ActionResult GetPackageModulesNew(string areaName) {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(ModuleSelectionHelper.RenderReplacementPackageModulesNew(areaName));
            return new YJsonResult { Data = sb.ToString() };
        }
        /// <summary>
        /// Returns data to replace a dropdownlist's data with existing designed modules given a package name.
        /// </summary>
        /// <param name="areaName">The area name of the package.</param>
        /// <returns>JSON containing a data source to update the dropdownlist.
        ///
        /// Used in conjunction with client-side code and the ModuleSelection template.</returns>
        [AllowPost]
        [ResourceAuthorize(CoreInfo.Resource_ModuleLists)]
        public async Task<ActionResult> GetPackageModulesDesigned(string areaName) {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(await ModuleSelectionHelper.RenderReplacementPackageModulesDesignedAsync(areaName));
            return new YJsonResult { Data = sb.ToString() };
        }
        /// <summary>
        /// Returns data to replace a dropdownlist's data with existing designed modules given a package name.
        /// </summary>
        /// <param name="areaName">The area name of the package.</param>
        /// <returns>JSON containing a data source to update the dropdownlist.
        ///
        /// Used in conjunction with client-side code and the ModuleSelection template.</returns>
        [AllowPost]
        [ResourceAuthorize(CoreInfo.Resource_ModuleLists)]
        public async Task<ActionResult> GetPackageModulesDesignedFromGuid(Guid modGuid) {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(await ModuleSelectionHelper.RenderReplacementPackageModulesDesignedAsync(modGuid));
            return new YJsonResult { Data = sb.ToString() };
        }
    }
}
