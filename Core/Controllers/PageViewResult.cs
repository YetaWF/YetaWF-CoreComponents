﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;

namespace YetaWF.Core.Controllers {

    internal class PageViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public PageViewResult() { }

        public override async Task ExecuteResultAsync(ActionContext context) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            PageDefinition requestedPage = Manager.CurrentPage;
            Manager.PageTitle = requestedPage.Title;

            Manager.ScriptManager.AddVolatileOption("Basics", "PageGuid", requestedPage.PageGuid);
            Manager.ScriptManager.AddVolatileOption("Basics", "TemporaryPage", requestedPage.Temporary);

            bool staticPage = false;
            if (YetaWFManager.Deployed)
                staticPage = requestedPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && !Manager.HaveUser && string.Compare(Manager.HostUsed, Manager.CurrentSite.SiteDomain, true) == 0;
            Manager.RenderStaticPage = staticPage;

            SkinAccess skinAccess = new SkinAccess();
            string pageViewName = skinAccess.GetViewName(requestedPage.PopupPage);
            SkinDefinition skin = Manager.CurrentSite.Skin;
            string skinCollection = skin.Collection!;

            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
            Manager.AddOnManager.AddExplicitlyInvokedModules(requestedPage.ReferencedModules);


            // set new character dimensions and popup info
            PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
            if (Manager.IsInPopup) {
                Manager.ScriptManager.AddVolatileOption("Skin", "PopupWidth", pageSkin.Width);// Skin size in a popup window
                Manager.ScriptManager.AddVolatileOption("Skin", "PopupHeight", pageSkin.Height);
                Manager.ScriptManager.AddVolatileOption("Skin", "PopupMaximize", pageSkin.MaximizeButton);
            }
            Manager.LastUpdated = requestedPage.Updated;

            // Skins first. Skins can/should really only add CSS files.
            await Manager.AddOnManager.AddSkinAsync(skinCollection, Manager.CurrentSite.Theme ?? SiteDefinition.DefaultTheme); 
            await YetaWFCoreRendering.Render.AddStandardAddOnsAsync();

            Manager.ScriptManager.AddVolatileOption("Skin", "MinWidthForPopups", Manager.SkinInfo.MinWidthForPopups);
            Manager.ScriptManager.AddVolatileOption("Skin", "MinWidthForCondense", Manager.SkinInfo.MinWidthForCondense);

            await YetaWFCoreRendering.Render.AddSkinAddOnsAsync();
            await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF_Core", "SkinBasics");

            YHtmlHelper htmlHelper = new YHtmlHelper(context, null);
            string pageHtml = await htmlHelper.ForPageAsync(pageViewName);

            Manager.ScriptManager.AddLast("$YetaWF", "$YetaWF.initPage();");// end of page, initialization - this is the first thing that runs
            pageHtml = ProcessInlineScripts(pageHtml);

            await Manager.AddOnManager.AddSkinCustomizationAsync(skinCollection);

            if (Manager.UniqueIdCounters.IsTracked)
                Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdCounters", Manager.UniqueIdCounters);
            Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedCssBundleFiles", Manager.CssManager.GetBundleFiles());
            Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedScriptBundleFiles", Manager.ScriptManager.GetBundleFiles());
            ModuleDefinitionExtensions.AddVolatileOptionsUniqueModuleAddOns(MarkPrevious: true);

            PageProcessing pageProc = new PageProcessing(Manager);
            pageHtml = await pageProc.PostProcessHtmlAsync(pageHtml);
            pageHtml = WhiteSpaceResponseFilter.Compress(pageHtml);

            if (staticPage) {
                await Manager.StaticPageManager.AddPageAsync(requestedPage.Url, requestedPage.StaticPage == PageDefinition.StaticPageEnum.YesMemory, pageHtml, Manager.LastUpdated);
                // Last-Modified is dependent on which user is logged on (if any) and any module that generates data which changes each time will defeat last-modified
                // so is only helpful for static pages and can't be used for dynamic pages
                context.HttpContext.Response.Headers.Add("Last-Modified", string.Format("{0:R}", Manager.LastUpdated));
            } else if (Manager.HaveUser && requestedPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && Manager.HostUsed.ToLower() == Manager.CurrentSite.SiteDomain.ToLower()) {
                // if we have a user for what would be a static page, we have to make sure the last modified date is set to override any previously
                // served page to the then anonymous user before he/she logged on.
                context.HttpContext.Response.Headers.Add("Last-Modified", string.Format("{0:R}", DateTime.UtcNow));
            }
            context.HttpContext.Response.Headers.Add("Content-Type", "text/html");

            byte[] btes = Encoding.UTF8.GetBytes(pageHtml);
            await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
        }

        /// <summary>
        /// Moves all &lt;script&gt;&lt;/script&gt; snippets to the end of the page.
        /// </summary>
        /// <param name="viewHtml">The contents of the view.</param>
        /// <returns>The contents of the view with all &lt;script&gt;&lt;/script&gt; snippets removed.</returns>
        /// <remarks>Components and views do NOT generate &lt;script&gt;&lt;/script&gt; tags. They must use Manager.ScriptManager.AddLast instead.
        /// This is only used to move &lt;script&gt;&lt;/script&gt; sections that were added in YetaWF.Text modules.
        /// </remarks>
        internal static string ProcessInlineScripts(string viewHtml) {
            // code snippets must use <script></script> (without any attributes)
            int pos = 0;
            for ( ; ; ) {
                int index = viewHtml.IndexOf("<script>", pos, StringComparison.Ordinal);
                if (index < 0)
                    break;
                int endIndex = viewHtml.IndexOf("</script>", index + 8, StringComparison.Ordinal);
                if (endIndex < 0)
                    throw new InternalError("Missing </script> in view");
                YetaWFManager.Manager.ScriptManager.AddLast(viewHtml.Substring(index + 8, endIndex - index - 8));
                viewHtml = viewHtml.Remove(index, endIndex + 9 - index);
                pos = index;
            }
            return viewHtml;
        }
    }
}
