/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    public class RazorPage : RazorPage<object> { }

    public class RazorTemplate<TModel> : RazorPage<TModel> {
        public override bool IsTemplate { get { return true; } }
    }

    public class RazorPage<TModel> : System.Web.Mvc.WebViewPage {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public RazorPage() : base() { }
        public override void Execute() { }
        // public new HtmlHelper Html { get { return base.Html; } }

        /// <summary>
        /// Get a localized string from resources
        /// </summary>
        public string __ResStr(string name, string defaultValue, params object[] parms) {
            return ResourceAccessHelper.__ResStr(this, name, defaultValue, parms);
        }

        public new TModel Model {
            get {
                return (TModel) base.Model;
            }
        }

        public PageDefinition CurrentPage {
            get {
                Debug.Assert(Manager.CurrentPage != null);
                return Manager.CurrentPage;
            }
        }

        public void RenderView(ViewContext viewContext) {

            WebPageContext wpc = new WebPageContext(viewContext.HttpContext, this, Model);
            ExecutePageHierarchy(wpc, viewContext.Writer, this);
        }
        public IHtmlString RenderPane(string pane, string cssClass = null, bool Conditional = true) {
            return CurrentPage.RenderPane(Html, pane, cssClass, Conditional: Conditional);
        }
        public PageDefinition.PaneSet PaneSet(string cssClass = null, bool Conditional = true, bool SameHeight = true) {
            return CurrentPage.RenderPaneSet(Html, cssClass, Conditional: Conditional, SameHeight: SameHeight);
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

        public MvcHtmlString JSEncode(object obj) {
            return MvcHtmlString.Create(YetaWFManager.Jser.Serialize(obj));
        }

        // DOCUMENTREADY
        // DOCUMENTREADY
        // DOCUMENTREADY

        protected class JSDocumentReady : IDisposable {
            public JSDocumentReady(HtmlHelper<object> Html) {
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

            public HtmlHelper<object> Html { get; set; }
            private bool IsAjax { get; set; }
        }
        protected JSDocumentReady DocumentReady() {
            if (IsAjax) {
                Html.ViewContext.Writer.Write("if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {");
                Html.ViewContext.Writer.Write("YetaWF_Forms.partialFormActions1.push({");
                Html.ViewContext.Writer.Write("callback: function () {");
                return new JSDocumentReady(Html);
            } else {
                Html.ViewContext.Writer.Write("$(document).ready(function(){\n");
                return new JSDocumentReady(Html);
            }
        }

        public override void ExecutePageHierarchy() {
            if (IsTemplate) {
                // for templates, add the required support through the addonmanager
                string path = VirtualPath;
                string[] pathParts = path.Split(new char[] { '/' });
                int partsCount = pathParts.Length;
                string domain = "", product = "";
                string templateName = "";
                if (partsCount >= 3) {
                    if (pathParts[partsCount - 3] == "Shared" && (pathParts[partsCount - 2] == "DisplayTemplates" || pathParts[partsCount - 2] == "EditorTemplates")) {
                        // standard template
                    } else if (pathParts[partsCount - 2] == "Shared") {
                        // special shared template
                    } else
                        throw new InternalError("Unexpected template {0}", path);
                    templateName = pathParts[partsCount - 1];
                    if (!templateName.EndsWith(".cshtml"))
                        throw new InternalError("Unexpected template {0}", path);
                    templateName = templateName.Substring(0, templateName.Length - 7);
                    string[] parts = templateName.Split(new char[] { '_' });
                    if (parts.Length == 3) {
                        domain = parts[0];
                        product = parts[1];
                        templateName = parts[2];
                    } else if (parts.Length == 1) {
                        domain = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Domain;
                        product = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Product;
                    } else
                        throw new InternalError("template name for {0} should have the format \"domain_product_template\"", templateName);
                } else
                    throw new InternalError("Unexpected template {0}", path);
                Manager.PushModel(Model);
                base.ExecutePageHierarchy();
                Manager.PopModel();
                if (!string.IsNullOrWhiteSpace(domain) && !string.IsNullOrWhiteSpace(product))
                    Manager.AddOnManager.AddTemplate(domain, product, templateName);
            } else {
                Manager.PushModel(Model);
                base.ExecutePageHierarchy();
                Manager.PopModel();
            }
        }

        public virtual bool IsTemplate { get { return false; } }
    }
}
