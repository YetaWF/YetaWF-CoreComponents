/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
using YetaWF.Core.Log;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;

namespace YetaWF.Core.Controllers {

    internal class PageContentViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public PageContentViewResult(PageContentController.DataIn dataIn) {
            DataIn = dataIn;
        }

        public PageContentController.DataIn DataIn { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context) {

            PageContentController.PageContentData cr = new PageContentController.PageContentData();

            try {

                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                Manager.PageTitle = Manager.CurrentPage.Title;

                PageDefinition currPage = Manager.CurrentPage;
                SkinAccess skinAccess = new SkinAccess();

                Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
                Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);

                // set new character dimensions and popup info
                PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
                if (Manager.IsInPopup) {
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupWidth", pageSkin.Width);// Skin size in a popup window
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupHeight", pageSkin.Height);
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupMaximize", pageSkin.MaximizeButton);
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupCss", pageSkin.Css);
                }

                YHtmlHelper htmlHelper = new YHtmlHelper(context, null);
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

                if (YetaWFManager.Deployed) {
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
                if (cr.CanonicalUrl != null) {
                    UriBuilder ub = new UriBuilder(cr.CanonicalUrl);
                    cr.LocalUrl = QueryHelper.ToUrl(ub.Path, ub.Query);
                }

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
