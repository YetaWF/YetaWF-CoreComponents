/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YetaWF.Core.IO;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
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
#if MVC6
        private IViewRenderService _viewRenderService;

        public PageViewResult(IViewRenderService _viewRenderService, ViewDataDictionary viewData, ITempDataDictionary tempData) {
            this._viewRenderService = _viewRenderService;
#else
        public PageViewResult(ViewDataDictionary viewData, TempDataDictionary tempData) {
#endif
            TempData = tempData;
            ViewData = viewData;
        }
#if MVC6
        public ITempDataDictionary TempData { get; set; }
#else
        public TempDataDictionary TempData { get; set; }
#endif
        public IView View { get; set; }
        public ViewDataDictionary ViewData { get; set; }

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
                    if (info != null && info.Mode != PageDefinition.UnifiedModeEnum.None) {
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

                bool staticPage = false;
                if (Manager.Deployed)
                    staticPage = requestedPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && !Manager.HaveUser;
                Manager.RenderStaticPage = staticPage;

                SkinAccess skinAccess = new SkinAccess();
                SkinDefinition skin = SkinDefinition.EvaluatedSkin(masterPage, Manager.IsInPopup);
                string skinCollection = skin.Collection;

                Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
                Manager.AddOnManager.AddExplicitlyInvokedModules(requestedPage.ReferencedModules);

                string virtPath = skinAccess.PhysicalPageUrl(skin, Manager.IsInPopup);
                if (!await FileSystem.FileSystemProvider.FileExistsAsync(YetaWFManager.UrlToPhysical(virtPath)))
                    throw new InternalError("No page skin available - file {0} not found", virtPath);

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

                await Manager.AddOnManager.AddStandardAddOnsAsync();
                await Manager.SetSkinOptions();
                await Manager.AddOnManager.AddSkinAsync(skinCollection);

                await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF", "Core", "Basics");
                if (Manager.IsInPopup)
                    await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF", "Core", "Popups");

                string pageHtml;
#if MVC6
                pageHtml = await _viewRenderService.RenderToStringAsync(context, "~/wwwroot" + virtPath, ViewData);
#else
                View = new PageView(virtPath);
                using (StringWriter writer = new StringWriter()) {
                    ViewContext viewContext = new ViewContext(context, View, ViewData, TempData, writer);
                    View.Render(viewContext, writer);
                    pageHtml = writer.ToString();
                }
#endif
                if (Manager.CurrentSite.JSLocation == Site.JSLocationEnum.Bottom)
                    pageHtml = ProcessInlineScripts(pageHtml);
                Manager.ScriptManager.AddLast("YetaWF_Basics", "YetaWF_Basics.initPage();");// end of page initialization

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
#if MVC6
                byte[] btes = Encoding.ASCII.GetBytes(pageHtml);
                await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
                context.HttpContext.Response.Output.Write(pageHtml);

            });
#endif
        }

        private string ProcessInlineScripts(string viewHtml) {
            viewHtml = _scriptTagsRe.Replace(viewHtml, new MatchEvaluator(SubstScriptTag));
            return viewHtml;
        }
        private string SubstScriptTag(Match match) {
            string code = match.Groups["code"].Value;
            Manager.ScriptManager.AddLast(code);
            return "";
        }
        // code snippets in cshtml must use <script></script> (without any attributes)
        static Regex _scriptTagsRe = new Regex(@"<script>(?'code'.*?)</script>", RegexOptions.Compiled | RegexOptions.Singleline);
    }

#if MVC6
#else
    internal class PageView : IView {

        public PageView(string virtPath) {
            VirtualPath = virtPath;
        }
        private string VirtualPath { get; set; }

        public void Render(ViewContext viewContext, TextWriter writer) {

            RazorPage razorPage = (RazorPage)RazorPage.CreateInstanceFromVirtualPath(VirtualPath);

            razorPage.ViewContext = viewContext;
            razorPage.ViewData = new ViewDataDictionary<object>(viewContext.ViewData);
            razorPage.InitHelpers();

            razorPage.RenderView(viewContext);
        }
    }
#endif
}
