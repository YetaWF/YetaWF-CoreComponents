/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;
using YetaWF.Core.Controllers;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
#endif

namespace YetaWF.Core.Pages
{

#if MVC6
    public static class HtmlStringExtension {
        public static string ToHtmlString(this HtmlString htmlString) {
            return htmlString.Value;
        }
    }
#else
#endif

    // used by pages
#if MVC6
    public class RazorPage : Microsoft.AspNetCore.Mvc.Razor.RazorPage<object> {

        public RazorPage() : base() {  }

#else
    public class RazorPage : System.Web.Mvc.WebViewPage {
#endif
        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

#if MVC6
        public override async Task ExecuteAsync() { await Task.FromResult(0); }
#else
        public override void Execute() { }
#endif
#if MVC6
        internal IHtmlHelper<object> GetHtml() {
            dynamic page = this;
            return page.Html;
        }
#else
        private HtmlHelper<object> GetHtml() {
            return Html;
        }
#endif

        /// <summary>
        /// Get a localized string from resources
        /// </summary>
        public string __ResStr(string name, string defaultValue, params object[] parms) {
            return ResourceAccessHelper.__ResStr(this, name, defaultValue, parms);
        }

        public PageDefinition CurrentPage {
            get {
                return Manager.CurrentPage;
            }
        }
#if MVC6
#else
        public void RenderView(ViewContext viewContext) {
            WebPageContext wpc = new WebPageContext(viewContext.HttpContext, this, null);
            ExecutePageHierarchy(wpc, viewContext.Writer, this);
        }
#endif
        public async Task<HtmlString> RenderPaneAsync(string pane, string cssClass = null, bool Conditional = true, bool Unified = false) {

            if (!Manager.EditMode && Unified && Manager.UnifiedPages != null) {
                PageDefinition realPage = Manager.CurrentPage;
                StringBuilder sb = new StringBuilder();
                foreach (PageDefinition page in Manager.UnifiedPages) {
                    // for now we don't validate skins
                    // if (page.SelectedSkin.Collection != realPage.SelectedSkin.Collection)
                    //    throw new InternalError("The requested page {0} and the page {1}, part of the unified pages, don't use the same skin collection ({2} vs. {3})", realPage.Url, page.Url, realPage.SelectedSkin.Collection, page.SelectedSkin.Collection);
                    //if (page.SelectedSkin.FileName != realPage.SelectedSkin.FileName)
                    //    throw new InternalError("The requested page {0} and the page {1}, part of the unified pages, don't use the same skin file ({2} vs. {3})", realPage.Url, page.Url, realPage.SelectedSkin.FileName, page.SelectedSkin.FileName);
                    //if (page.TemplatePage != realPage.TemplatePage)
                    //    throw new InternalError("The requested page {0} and the page {1}, part of the unified pages, don't use the template page ({2} vs. {3})", realPage.Url, page.Url, realPage.TemplatePage.Url ?? "(none)", page.TemplatePage.Url ?? "(none)");
                    Manager.CurrentPage = page;
#if MVC6
                    sb.Append(await CurrentPage.RenderPaneAsync((IHtmlHelper<object>)GetHtml(), pane, cssClass, Conditional: Conditional, UnifiedMainPage: realPage));
#else
                    sb.Append(await CurrentPage.RenderPaneAsync((HtmlHelper<object>)GetHtml(), pane, cssClass, Conditional: Conditional, UnifiedMainPage: realPage));
#endif
                }
                Manager.CurrentPage = realPage;
                return new HtmlString(sb.ToString());
            } else {
#if MVC6
                return await CurrentPage.RenderPaneAsync((IHtmlHelper<object>)GetHtml(), pane, cssClass, Conditional: Conditional);
#else
                return await CurrentPage.RenderPaneAsync((HtmlHelper<object>)GetHtml(), pane, cssClass, Conditional: Conditional);
#endif
            }
        }
        public PageDefinition.PaneSet PaneSet(string cssClass = null, bool Conditional = true, bool SameHeight = true) {
#if MVC6
            return CurrentPage.RenderPaneSet((IHtmlHelper<object>)GetHtml(), cssClass, Conditional: Conditional, SameHeight: SameHeight);
#else
            return CurrentPage.RenderPaneSet((HtmlHelper<object>)GetHtml(), cssClass, Conditional: Conditional, SameHeight: SameHeight);
#endif
        }

        /// <summary>
        /// Used to render page contents for unified pages with dynamic content.
        /// </summary>
        /// <returns></returns>
        public
#if MVC6
            async
#endif
                Task<HtmlString> RenderPageContentAsync(bool MainOnly = false) {
            PageContentController.PageContentData model = (PageContentController.PageContentData)(object)ViewData.Model;
            PageContentController.DataIn dataIn = (PageContentController.DataIn)ViewData["DataIn"];
#if MVC6
            await CurrentPage.RenderPaneContentsAsync((IHtmlHelper<object>)GetHtml(), dataIn, model, MainOnly: MainOnly);
            return null;
#else
            YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you :-(
                await CurrentPage.RenderPaneContentsAsync((HtmlHelper<object>)GetHtml(), dataIn, model);
            });
            return Task.FromResult<HtmlString>(new HtmlString(""));
#endif
        }
    }
}
