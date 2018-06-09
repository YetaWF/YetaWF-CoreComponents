/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
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

    // used by pages and popups
#if MVC6
    public class RazorPage : RazorPage<object>, IRazorPageLifetime { }
#else
    public class RazorPage : RazorPage<object> { }
#endif


    // used by templates //$$$$REMOVE
#if MVC6
    public class RazorTemplate<TModel> : RazorPage<TModel>, IRazorPageLifetime
#else
    public class RazorTemplate<TModel> : RazorPage<TModel>
#endif
    {
        public override bool IsTemplate { get { return true; } }
    }

    // used by views
#if MVC6
    public class RazorPage<TModel> : Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel> {

        public RazorPage() : base() {  }

#else
    public class RazorPage<TModel> : System.Web.Mvc.WebViewPage {
#endif
        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

#if MVC6
        public override async Task ExecuteAsync() { await Task.FromResult(0); }
#else
        public override void Execute() { }
#endif
#if MVC6
        internal IHtmlHelper<TModel> GetHtml() {
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
#if MVC6
#else
        public new TModel Model {
            get {
                return (TModel)base.Model;
            }
        }
#endif
        public TModel GetModel() {
#if MVC6
            return (TModel) Model;
#else
            return (TModel) base.Model;
#endif
        }
        public PageDefinition CurrentPage {
            get {
                return Manager.CurrentPage;
            }
        }
#if MVC6
#else
        public void RenderView(ViewContext viewContext) {
            WebPageContext wpc = new WebPageContext(viewContext.HttpContext, this, Model);
            ExecutePageHierarchy(wpc, viewContext.Writer, this);
        }
#endif
        public async Task<HtmlString> RenderPaneAsync(string pane, string cssClass = null, bool Conditional = true, bool Unified = false) {
            if (IsTemplate)
                throw new InternalError("Can't use RenderPane in templates");

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
            if (IsTemplate)
                throw new InternalError("Can't use PaneSet in templates");
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
                Task<HtmlString> RenderPageContentAsync() {
            PageContentController.PageContentData model = (PageContentController.PageContentData)(object)ViewData.Model;
            PageContentController.DataIn dataIn = (PageContentController.DataIn)ViewData["DataIn"];
#if MVC6
            await CurrentPage.RenderPaneContentsAsync((IHtmlHelper<object>)GetHtml(), dataIn, model);
            return null;
#else
            YetaWFManager.Syncify(async () => { // sorry MVC5, just no async for you :-(
                await CurrentPage.RenderPaneContentsAsync((HtmlHelper<object>)GetHtml(), dataIn, model);
            });
            return Task.FromResult<HtmlString>(new HtmlString(""));
#endif
        }

        // used by templates
        public string ControlId {
            get {
                if (string.IsNullOrEmpty(_controlId))
                    _controlId = Manager.UniqueId("ctrl");
                return _controlId;
            }
        }
        private string _controlId;

        public string DivId {
            get {
                if (string.IsNullOrEmpty(_divId))
                    _divId = Manager.UniqueId("div");
                return _divId;
            }
        }
        private string _divId;

        public string UniqueId(string name = "b") {
            return Manager.UniqueId(name);
        }

        public HtmlString JSEncode(object obj) {
            return new HtmlString(YetaWFManager.JsonSerialize(obj));
        }

        // DOCUMENTREADY
        // DOCUMENTREADY
        // DOCUMENTREADY

        protected class JSDocumentReady : IDisposable {
#if MVC6
            public JSDocumentReady(IHtmlHelper<TModel> Html)
#else
            public JSDocumentReady(HtmlHelper<object> Html)
#endif
            {
                this.Html = Html;
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                if (disposing) DisposableTracker.RemoveObject(this);
                while (CloseParen > 0) {
                    Html.ViewContext.Writer.Write("}");
                    CloseParen = CloseParen - 1;
                }
                Html.ViewContext.Writer.Write("}});");
            }
            //~JSDocumentReady() { Dispose(false); }
#if MVC6
            public IHtmlHelper<TModel> Html { get; set; }
#else
            public HtmlHelper<object> Html { get; set; }
#endif
            public int CloseParen { get; internal set; }
        }
        protected JSDocumentReady DocumentReady(string id) { //$$$remove ???
#if MVC6
            IHtmlHelper<TModel> htmlHelper;
#else
            HtmlHelper<object> htmlHelper;
#endif
            htmlHelper = GetHtml();
            htmlHelper.ViewContext.Writer.Write("YetaWF_Basics.whenReadyOnce.push({{callback: function ($tag) {{ if ($tag.has('#{0}').length > 0) {{\n", id);
            return new JSDocumentReady(htmlHelper) { CloseParen = 1 };
        }
        protected JSDocumentReady DocumentReady() {
#if MVC6
            IHtmlHelper<TModel> htmlHelper = GetHtml();
#else
            HtmlHelper<object> htmlHelper = GetHtml();
#endif
            htmlHelper.ViewContext.Writer.Write("YetaWF_Basics.whenReadyOnce.push({callback: function ($tag) {\n");
            return new JSDocumentReady(htmlHelper);
        }
#if MVC6
#else
        public override void ExecutePageHierarchy() {
            YetaWFManager.Syncify(async () => { // rendering needs to be sync (for templates)
                BeginRender(null);
                base.ExecutePageHierarchy();
                await EndRenderAsync(null);
            });
        }
#endif
        public void BeginRender(ViewContext context) {
            // NOTE: the page has not been activated when using MVC6 so all data has to be extracted from context.
            // context is null with MVC5
            if (IsTemplate) {//$$$REMOVE
#if MVC6
                string path = Path;
#else
                string path = VirtualPath;
#endif
                string[] pathParts = path.Split(new char[] { '/' });
                int partsCount = pathParts.Length;
                if (partsCount >= 3) {
                    if (pathParts[partsCount - 3] == "Shared" && (pathParts[partsCount - 2] == "DisplayTemplates" || pathParts[partsCount - 2] == "EditorTemplates")) {
                        // standard template
                    } else if (pathParts[partsCount - 2] == "Shared") {
                        // special shared template
                    } else
                        throw new InternalError("Unexpected template {0}", path);
                    _templateName = pathParts[partsCount - 1];
                    if (!_templateName.EndsWith(".cshtml"))
                        throw new InternalError("Unexpected template {0}", path);
                    _templateName = _templateName.Substring(0, _templateName.Length - 7);
                    string[] parts = _templateName.Split(new char[] { '_' });
                    if (parts.Length == 3) {
                        _domain = parts[0];
                        _product = parts[1];
                        _templateName = parts[2];
                    } else if (parts.Length == 1) {
                        _domain = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Domain;
                        _product = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Product;
                    } else
                        throw new InternalError("template name for {0} should have the format \"domain_product_template\"", _templateName);
                } else
                    throw new InternalError("Unexpected template {0}", path);
            }
#if MVC6
            Manager.PushModel(context.ViewData.Model);
#else
            Manager.PushModel(GetModel());
#endif
        }
        private string _product { get; set; }
        private string _domain { get; set; }
        private string _templateName { get; set; }

        public Task EndRenderAsync(ViewContext context) {
            if (IsTemplate) {
                Manager.PopModel();
                //$$$all this can be deleted - no template support
                //if (!string.IsNullOrWhiteSpace(_domain) && !string.IsNullOrWhiteSpace(_product))
                //    await Manager.AddOnManager.AddTemplateAsync(_domain, _product, _templateName);
            } else {
                Manager.PopModel();
            }
            return Task.CompletedTask;
        }

        public virtual bool IsTemplate { get { return false; } }
    }
}
