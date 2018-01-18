/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers.Shared {

    /// <summary>
    /// PageSkin and PopupSkin template support.
    /// </summary>
    public class SkinHelperController : YetaWFController {

        /// <summary>
        /// Returns &lt;option&gt; HTML to replace a select statement with page skins for the provided skin collection.
        /// </summary>
        /// <param name="skinCollection">The name of the skin collection.</param>
        /// <returns>&lt;option&gt; HTML to replace a select statement with page skins.
        ///
        /// Works in conjunction with client-side code and the PageSkin template.</returns>
        [AllowPost]
        [ResourceAuthorize(CoreInfo.Resource_SkinLists)]
        public ActionResult GetPageSkins(string skinCollection) {
            SkinAccess skinAccess = new SkinAccess();
            PageSkinList skinList = skinAccess.GetAllPageSkins(skinCollection);
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(PageSkinHelper.RenderReplacementSkinsForCollection(skinList));
            return new YJsonResult { Data = sb.ToString() };
        }
        /// <summary>
        /// Returns &lt;option&gt; HTML to replace a select statement with popup skins for the provided skin collection.
        /// </summary>
        /// <param name="skinCollection">The name of the skin collection.</param>
        /// <returns>&lt;option&gt; HTML to replace a select statement with popup skins.
        ///
        /// Works in conjunction with client-side code and the PopupSkin template.</returns>
        [AllowPost]
        [ResourceAuthorize(CoreInfo.Resource_SkinLists)]
        public ActionResult GetPopupPageSkins(string skinCollection) {
            SkinAccess skinAccess = new SkinAccess();
            PageSkinList skinList = skinAccess.GetAllPopupSkins(skinCollection);
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(PageSkinHelper.RenderReplacementSkinsForCollection(skinList));
            return new YJsonResult { Data = sb.ToString() };
        }
    }
}
