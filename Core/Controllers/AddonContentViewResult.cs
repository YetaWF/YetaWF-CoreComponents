/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.Controllers {

    internal class AddonContentViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public AddonContentViewResult(AddonContentController.DataIn dataIn) {
            DataIn = dataIn;
        }

        private AddonContentController.DataIn DataIn { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context) {
            PageContentController.PageContentData cr = new PageContentController.PageContentData();

            try {

                if (context == null)
                    throw new ArgumentNullException("context");

                foreach (AddonContentController.AddonDescription addon in DataIn.Addons) {
                    await Manager.AddOnManager.AddAddOnNamedAsync(addon.AreaName, addon.ShortName, addon.Argument1);
                }

                await Manager.CssManager.RenderAsync(cr, DataIn.KnownCss);
                await Manager.ScriptManager.RenderAsync(cr, DataIn.KnownScripts);
                Manager.ScriptManager.RenderEndofPageScripts(cr);

            } catch (Exception exc) {
                cr.Status = Logging.AddErrorLog(ErrorHandling.FormatExceptionMessage(exc));
            }


            string json = Utility.JsonSerialize(cr);
            context.HttpContext.Response.ContentType = "application/json";

            // This is worth gzip'ing - client-side always requests gzip (it's us) so no need to check whether it was asked for.
            // gzip encoding is performed by middleware
            byte[] btes = Encoding.ASCII.GetBytes(json);
            await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
        }
    }
}
