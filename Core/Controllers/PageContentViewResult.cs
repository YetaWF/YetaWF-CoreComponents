/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;
#else
using System.IO.Compression;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    internal class PageContentViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public PageContentViewResult(PageContentController.DataIn dataIn) {
            DataIn = dataIn;
        }

        public PageContentController.DataIn DataIn { get; set; }

#if MVC6
        public override async Task ExecuteResultAsync(ActionContext context) {
#else
        public override void ExecuteResult(ControllerContext context) {
            YetaWFManager.Syncify(async () => { // Sorry, no async for you MVC5
#endif
                if (context == null)
                    throw new ArgumentNullException("context");

                Manager.PageTitle = Manager.CurrentPage.Title;

                PageDefinition currPage = Manager.CurrentPage;
                SkinAccess skinAccess = new SkinAccess();

                Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
                Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);

                // set new character dimensions and popup info
                PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
                Manager.NewCharSize(pageSkin.CharWidthAvg, pageSkin.CharHeight);
                if (Manager.IsInPopup) {
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupWidth", pageSkin.Width);// Skin size in a popup window
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupHeight", pageSkin.Height);
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupMaximize", pageSkin.MaximizeButton);
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupCss", pageSkin.Css);
                }

                PageContentController.PageContentData cr = new PageContentController.PageContentData();
                YHtmlHelper htmlHelper =
#if MVC6
                    new YHtmlHelper(context, null);
#else
                    new YHtmlHelper(context.RequestContext, null);
#endif
                await Manager.CurrentPage.RenderPaneContentsAsync(htmlHelper, DataIn, cr);

                //Manager.PopCharSize();

                if (Manager.UniqueIdCounters.IsTracked)
                    Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdCounters", Manager.UniqueIdCounters);

                Manager.ScriptManager.AddVolatileOption("Basics", "OriginList", Manager.OriginList ?? new List<Origin>());

                Manager.ScriptManager.AddVolatileOption("Basics", "PageGuid", Manager.CurrentPage.PageGuid);
                Manager.ScriptManager.AddVolatileOption("Basics", "TemporaryPage", Manager.CurrentPage.Temporary);
                ModuleDefinitionExtensions.AddVolatileOptionsUniqueModuleAddOns();

                await Manager.CssManager.RenderAsync(cr, DataIn.KnownCss);
                await Manager.ScriptManager.RenderAsync(cr, DataIn.KnownScripts);
                Manager.ScriptManager.RenderEndofPageScripts(cr);

                if (Manager.Deployed) {
                    if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.AnalyticsContent))
                        cr.AnalyticsContent = Manager.CurrentPage.AnalyticsContent;
                    else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.AnalyticsContent))
                        cr.AnalyticsContent = Manager.CurrentSite.AnalyticsContent;
                    if (!string.IsNullOrWhiteSpace(cr.AnalyticsContent))
                        cr.AnalyticsContent = cr.AnalyticsContent.Replace("<<Url>>", Utility.JserEncode(Manager.CurrentPage.EvaluatedCanonicalUrl));
                }
                cr.PageTitle = Manager.PageTitle.ToString();
                cr.PageCssClasses = Manager.CurrentPage.GetCssClass();
                cr.CanonicalUrl = Manager.CurrentPage.EvaluatedCanonicalUrl;
                UriBuilder ub = new UriBuilder(cr.CanonicalUrl);
                cr.LocalUrl = QueryHelper.ToUrl(ub.Path, ub.Query);

                string json = Utility.JsonSerialize(cr);
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
