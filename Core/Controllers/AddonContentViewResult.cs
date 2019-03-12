/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;
#else
using System.IO.Compression;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    internal class AddonContentViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public AddonContentViewResult(AddonContentController.DataIn dataIn) {
            DataIn = dataIn;
        }

        private AddonContentController.DataIn DataIn { get; set; }

#if MVC6
        public override async Task ExecuteResultAsync(ActionContext context) {
#else
        public override void ExecuteResult(ControllerContext context) {
            YetaWFManager.Syncify(async () => { // Sorry, no async for you MVC5
#endif
                if (context == null)
                    throw new ArgumentNullException("context");

                foreach (AddonContentController.AddonDescription addon in DataIn.Addons) {
                    await Manager.AddOnManager.AddAddOnNamedAsync(addon.AreaName, addon.ShortName, addon.Argument1);
                }

                PageContentController.PageContentData cr = new PageContentController.PageContentData();

                await Manager.CssManager.RenderAsync(cr, DataIn.KnownCss);
                await Manager.ScriptManager.RenderAsync(cr, DataIn.KnownScripts);
                Manager.ScriptManager.RenderEndofPageScripts(cr);

                string json = YetaWFManager.JsonSerialize(cr);
                context.HttpContext.Response.ContentType = "application/json";

                // This is worth gzip'ing - client-side always requests gzip (it's us) so no need to check whether it was asked for.
#if MVC6
                // gzip encoding is performed by middleware
                byte[] btes = Encoding.ASCII.GetBytes(json);
                await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
                context.HttpContext.Response.AppendHeader("Content-encoding", "gzip");
                context.HttpContext.Response.Filter = new GZipStream(context.HttpContext.Response.Filter, CompressionMode.Compress);
                context.HttpContext.Response.Output.Write(json);
            });
#endif
        }
    }
}
