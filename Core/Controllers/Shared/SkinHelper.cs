/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Controllers.Shared {
    public class SkinHelperController : YetaWFController
    {
        // returns html <option> to replace a select statement with new page skins
        [HttpPost]
        [ResourceAuthorize(CoreInfo.Resource_SkinLists)]
        public ActionResult GetPageSkins(string skinCollection) {
            SkinAccess skinAccess = new SkinAccess();
            PageSkinList skinList = skinAccess.GetAllPageSkins(skinCollection);
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(PageSkinHelper.RenderReplacementSkinsForCollection(skinList));
            return new YJsonResult { Data = sb.ToString() };
        }
        // returns html <option> to replace a select statement with new popup skins
        [HttpPost]
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
