/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using System.Threading.Tasks;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
#endif

namespace YetaWF.Core.Pages {

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


    // used by templates
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
        public bool IsAjax {
            get { return YetaWFManager.Manager.IsAjaxRequest; }
        }
#else
#endif

#if MVC6
#else
        public void RenderView(ViewContext viewContext) {
            WebPageContext wpc = new WebPageContext(viewContext.HttpContext, this, Model);
            ExecutePageHierarchy(wpc, viewContext.Writer, this);
        }
#endif

        public HtmlString RenderPane(string pane, string cssClass = null, bool Conditional = true) {
            if (IsTemplate)
                throw new InternalError("Can't use RenderPane in templates");
#if MVC6
            return CurrentPage.RenderPane((IHtmlHelper<object>)GetHtml(), pane, cssClass, Conditional: Conditional);
#else
            return CurrentPage.RenderPane((HtmlHelper<object>)GetHtml(), pane, cssClass, Conditional: Conditional);
#endif
        }
        public PageDefinition.PaneSet PaneSet(string cssClass = null, bool Conditional = true, bool SameHeight = true) {
            if (IsTemplate)
                throw new InternalError("Can't use Pane in templates");
#if MVC6
            return CurrentPage.RenderPaneSet((IHtmlHelper<object>)GetHtml(), cssClass, Conditional: Conditional, SameHeight: SameHeight);
#else
            return CurrentPage.RenderPaneSet((HtmlHelper<object>)GetHtml(), cssClass, Conditional: Conditional, SameHeight: SameHeight);
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
            return new HtmlString(YetaWFManager.Jser.Serialize(obj));
        }

        // DOCUMENTREADY
        // DOCUMENTREADY
        // DOCUMENTREADY

        protected class JSDocumentReady : IDisposable {
#if MVC6
            public JSDocumentReady(IHtmlHelper<TModel> Html) {
#else
            public JSDocumentReady(HtmlHelper<object> Html) {
#endif
                this.Html = Html;
                IsAjax = YetaWFManager.Manager.IsAjaxRequest;
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                if (disposing) DisposableTracker.RemoveObject(this);
                if (IsAjax) {
                    Html.ViewContext.Writer.Write("}");
                    Html.ViewContext.Writer.Write("});");
                    Html.ViewContext.Writer.Write("}");
                } else {
                    Html.ViewContext.Writer.Write("});");
                }
            }
            //~JSDocumentReady() { Dispose(false); }

#if MVC6
            public IHtmlHelper<TModel> Html { get; set; }
#else
            public HtmlHelper<object> Html { get; set; }
#endif
            private bool IsAjax { get; set; }
        }
        protected JSDocumentReady DocumentReady() {
#if MVC6
            IHtmlHelper<TModel> htmlHelper;
#else
            HtmlHelper<object> htmlHelper;
#endif
            htmlHelper = GetHtml();
            if (Manager.IsAjaxRequest) {
                htmlHelper.ViewContext.Writer.Write("if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {");
                htmlHelper.ViewContext.Writer.Write("YetaWF_Forms.partialFormActions1.push({");
                htmlHelper.ViewContext.Writer.Write("callback: function () {");
                return new JSDocumentReady(htmlHelper);
            } else {
                htmlHelper.ViewContext.Writer.Write("$(document).ready(function(){\n");
                return new JSDocumentReady(htmlHelper);
            }
        }

#if MVC6
#else
        public override void ExecutePageHierarchy() {
            BeginRender(null);
            base.ExecutePageHierarchy();
            EndRender(null);
        }
#endif
        public void BeginRender(ViewContext context) {
            // NOTE: the page has not been activated when using MVC6 so all data has to be extracted from context.
            // context is null with MVC5
            if (IsTemplate) {
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

        public void EndRender(ViewContext context) {
            if (IsTemplate) {
                Manager.PopModel();
                if (!string.IsNullOrWhiteSpace(_domain) && !string.IsNullOrWhiteSpace(_product))
                    Manager.AddOnManager.AddTemplate(_domain, _product, _templateName);
            } else {
                Manager.PopModel();
            }
        }

        public virtual bool IsTemplate { get { return false; } }
    }
}
