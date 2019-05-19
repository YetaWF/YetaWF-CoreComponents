/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using YetaWF.Core.IO;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Components;
using System.Reflection;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Text;
using System.Threading.Tasks;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    internal class PageViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public PageViewResult() { }

#if MVC6
        public override async Task ExecuteResultAsync(ActionContext context) {
#else
        public override void ExecuteResult(ControllerContext context) {
            YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you :-(
#endif
                if (context == null)
                    throw new ArgumentNullException("context");

                PageDefinition requestedPage = Manager.CurrentPage;
                PageDefinition masterPage = Manager.CurrentPage;
                Manager.PageTitle = requestedPage.Title;

                // Unified pages
                Manager.UnifiedMode = PageDefinition.UnifiedModeEnum.None;
                if (!Manager.IsInPopup && !Manager.EditMode && PageDefinition.GetUnifiedPageInfoAsync != null && !requestedPage.Temporary) {
                    // Load all unified pages that this page is part of
                    PageDefinition.UnifiedInfo info = await PageDefinition.GetUnifiedPageInfoAsync(requestedPage.UnifiedSetGuid, requestedPage.SelectedSkin.Collection, requestedPage.SelectedSkin.FileName);
                    if (info != null && !info.Disabled && info.Mode != PageDefinition.UnifiedModeEnum.None) {
                        // Load the master page for this set
                        masterPage = await PageDefinition.LoadAsync(info.MasterPageGuid);
                        if (masterPage != null) {
                            // get pages that are part of unified set
                            Manager.UnifiedPages = new List<PageDefinition>();
                            if (info.Mode == PageDefinition.UnifiedModeEnum.DynamicContent || info.Mode == PageDefinition.UnifiedModeEnum.SkinDynamicContent) {
                                Manager.UnifiedPages.Add(requestedPage);
                            } else {
                                if (info.PageGuids != null && info.PageGuids.Count > 0) {
                                    foreach (Guid guid in info.PageGuids) {

                                        PageDefinition page = await PageDefinition.LoadAsync(guid);
                                        if (page != null) {
                                            Manager.UnifiedPages.Add(page);
                                            Manager.AddOnManager.AddExplicitlyInvokedModules(page.ReferencedModules);
                                        }
                                    };
                                }
                            }
                            Manager.UnifiedMode = info.Mode;
                            Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedAnimation", info.Animation);
                            Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedSetGuid", info.UnifiedSetGuid.ToString());
                            if (info.Mode == PageDefinition.UnifiedModeEnum.SkinDynamicContent) {
                                Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedPopups", info.Popups);
                                Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedSkinCollection", info.PageSkinCollectionName);
                                Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedSkinName", info.PageSkinFileName);
                            } else if (info.Mode == PageDefinition.UnifiedModeEnum.DynamicContent) {
                                Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedPopups", info.Popups);
                            } else
                                Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedPopups", false);
                        } else {
                            // master page was not found, probably deleted, just ignore
                            masterPage = Manager.CurrentPage;
                        }
                    }
                }
                Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedMode", (int)Manager.UnifiedMode);
                Manager.ScriptManager.AddVolatileOption("Basics", "PageGuid", requestedPage.PageGuid);
                Manager.ScriptManager.AddVolatileOption("Basics", "TemporaryPage", requestedPage.Temporary);

                bool staticPage = false;
                if (Manager.Deployed)
                    staticPage = requestedPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && !Manager.HaveUser;
                Manager.RenderStaticPage = staticPage;

                SkinAccess skinAccess = new SkinAccess();
                SkinDefinition skin = SkinDefinition.EvaluatedSkin(masterPage, Manager.IsInPopup);
                string pageViewName = skinAccess.GetPageViewName(skin, Manager.IsInPopup);
                string skinCollection = skin.Collection;

                Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
                Manager.AddOnManager.AddExplicitlyInvokedModules(requestedPage.ReferencedModules);


                // set new character dimensions and popup info
                PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
                Manager.NewCharSize(pageSkin.CharWidthAvg, pageSkin.CharHeight);
                Manager.ScriptManager.AddVolatileOption("Basics", "CharWidthAvg", pageSkin.CharWidthAvg);
                Manager.ScriptManager.AddVolatileOption("Basics", "CharHeight", pageSkin.CharHeight);
                if (Manager.IsInPopup) {
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupWidth", pageSkin.Width);// Skin size in a popup window
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupHeight", pageSkin.Height);
                    Manager.ScriptManager.AddVolatileOption("Skin", "PopupMaximize", pageSkin.MaximizeButton);
                }
                Manager.LastUpdated = requestedPage.Updated;

                await YetaWFCoreRendering.Render.AddStandardAddOnsAsync();
                await Manager.SetSkinOptions();
                await YetaWFCoreRendering.Render.AddSkinAddOnsAsync();
                await Manager.AddOnManager.AddSkinAsync(skinCollection);

                YHtmlHelper htmlHelper =
#if MVC6
                    new YHtmlHelper(context, null);
#else
                    new YHtmlHelper(context.RequestContext, null);
#endif
                string pageHtml = await htmlHelper.ForPageAsync(pageViewName);

                Manager.ScriptManager.AddLast("$YetaWF", "$YetaWF.initPage();");// end of page, initialization - this is the first thing that runs
                pageHtml = ProcessInlineScripts(pageHtml);

                await Manager.AddOnManager.AddSkinCustomizationAsync(skinCollection);
                Manager.PopCharSize();

                if (Manager.UnifiedMode == PageDefinition.UnifiedModeEnum.DynamicContent || Manager.UnifiedMode == PageDefinition.UnifiedModeEnum.SkinDynamicContent) {
                    Manager.NextUniqueIdPrefix();// get the next unique id prefix (so we don't have any conflicts when replacing modules)
                    Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdPrefixCounter", Manager.UniqueIdPrefixCounter);
                    Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedCssBundleFiles", Manager.CssManager.GetBundleFiles());
                    Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedScriptBundleFiles", Manager.ScriptManager.GetBundleFiles());
                }
                ModuleDefinitionExtensions.AddVolatileOptionsUniqueModuleAddOns(MarkPrevious: true);

                PageProcessing pageProc = new PageProcessing(Manager);
                pageHtml = await pageProc.PostProcessHtmlAsync(pageHtml);
                if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.Compression)
                    pageHtml = WhiteSpaceResponseFilter.Compress(Manager, pageHtml);

                if (staticPage) {
                    await Manager.StaticPageManager.AddPageAsync(requestedPage.Url, requestedPage.StaticPage == PageDefinition.StaticPageEnum.YesMemory, pageHtml, Manager.LastUpdated);
                    // Last-Modified is dependent on which user is logged on (if any) and any module that generates data which changes each time will defeat last-modified
                    // so is only helpful for static pages and can't be used for dynamic pages
                    context.HttpContext.Response.Headers.Add("Last-Modified", string.Format("{0:R}", Manager.LastUpdated));
                } else if (Manager.HaveUser && requestedPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages) {
                    // if we have a user for what would be a static page, we have to make sure the last modified date is set to override any previously
                    // served page to the then anonymous user before he/she logged on.
                    context.HttpContext.Response.Headers.Add("Last-Modified", string.Format("{0:R}", DateTime.UtcNow));
                }
                context.HttpContext.Response.Headers.Add("Content-Type", "text/html");
#if MVC6
                byte[] btes = Encoding.ASCII.GetBytes(pageHtml);
                await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
                context.HttpContext.Response.Output.Write(pageHtml);

            });
#endif
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
                int index = viewHtml.IndexOf("<script>", pos);
                if (index < 0)
                    break;
                int endIndex = viewHtml.IndexOf("</script>", pos + 8);
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
