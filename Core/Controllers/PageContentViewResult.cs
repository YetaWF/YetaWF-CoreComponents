﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using System.Linq;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewEngines;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    internal class PageContentViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        private IViewRenderService _viewRenderService;

        public PageViewResult(IViewRenderService _viewRenderService, ViewDataDictionary viewData, ITempDataDictionary tempData, PageContentController.DataIn dataIn) {
            this._viewRenderService = _viewRenderService;
#else
        public PageContentViewResult(ViewDataDictionary viewData, TempDataDictionary tempData, PageContentController.DataIn dataIn) {
#endif
            ViewData = viewData;
            TempData = tempData;
            DataIn = dataIn;
        }
#if MVC6
        public ITempDataDictionary TempData { get; set; }
#else
        public TempDataDictionary TempData { get; set; }
#endif
        public IView View { get; set; }
        public ViewDataDictionary ViewData { get; set; }
        public PageContentController.DataIn DataIn { get; set; }
#if MVC6
        public override async Task ExecuteResultAsync(ActionContext context) {
#else
        public override void ExecuteResult(ControllerContext context) {
#endif
            if (context == null)
                throw new ArgumentNullException("context");

            Manager.PageTitle = Manager.CurrentPage.Title;

            PageDefinition currPage = Manager.CurrentPage;
            SkinAccess skinAccess = new SkinAccess();
            SkinDefinition skin = SkinDefinition.EvaluatedSkin(currPage, Manager.IsInPopup);
            string skinCollection = skin.Collection;

            SkinDefinition skinContent = new Skins.SkinDefinition { Collection = SkinAccess.FallbackSkinCollectionName, FileName = "PageContent.cshtml" };
            string virtPath = skinAccess.PhysicalPageUrl(skinContent, Manager.IsInPopup);
            if (!File.Exists(YetaWFManager.UrlToPhysical(virtPath)))
                throw new InternalError("No page content skin available {0}.{1}", skinContent.Collection, skinContent.FileName);

            // set new character dimensions
            int charWidth, charHeight;
            skinAccess.GetPageCharacterSizes(out charWidth, out charHeight);
            Manager.NewCharSize(charWidth, charHeight);

            PageContentController.PageContentData cr = new PageContentController.PageContentData();
            ViewData.Model = cr;
            ViewData["DataIn"] = DataIn;
#if MVC6
            await _viewRenderService.RenderToStringAsync(context, "~/wwwroot" + virtPath, ViewData);
#else
            View = new PageView(virtPath);
            using (StringWriter writer = new StringWriter()) {
                ViewContext viewContext = new ViewContext(context, View, ViewData, TempData, writer);
                View.Render(viewContext, writer);
            }
#endif
            Manager.PopCharSize();

            Manager.ScriptManager.AddVolatileOption("Basics", "PageGuid", Manager.CurrentPage.PageGuid);

            Manager.CssManager.Render(cr);
            cr.CssFiles = cr.CssFiles.Except(DataIn.__KnownCss).ToList(); // eliminate css we already have

            Manager.ScriptManager.Render(cr);
            Manager.ScriptManager.RenderEndofPageScripts(cr);
            cr.ScriptFiles = cr.ScriptFiles.Except(DataIn.__KnownScripts).ToList(); // eliminate scripts we already have

            if (Manager.Deployed) {
                if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.AnalyticsContent))
                    cr.AnalyticsContent = Manager.CurrentPage.AnalyticsContent;
                else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.AnalyticsContent))
                    cr.AnalyticsContent = Manager.CurrentSite.AnalyticsContent;
                if (!string.IsNullOrWhiteSpace(cr.AnalyticsContent))
                    cr.AnalyticsContent = cr.AnalyticsContent.Replace("<<Url>>", Manager.CurrentPage.EvaluatedCanonicalUrl);
            }
            cr.PageTitle = Manager.PageTitle.ToString();
            cr.PageCssClasses = Manager.CurrentPage.CssClass;
            cr.CanonicalUrl = Manager.CurrentPage.EvaluatedCanonicalUrl;
            UriBuilder ub = new UriBuilder(cr.CanonicalUrl);
            cr.LocalUrl = QueryHelper.ToUrl(ub.Path, ub.Query);

            string json = YetaWFManager.JsonSerialize(ViewData.Model);
            context.HttpContext.Response.ContentType = "application/json";
#if MVC6
            byte[] btes = Encoding.ASCII.GetBytes(json);
            await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
            context.HttpContext.Response.Output.Write(json);
#endif
        }
    }
}
