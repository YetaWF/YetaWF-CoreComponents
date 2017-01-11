/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using YetaWF.Core.Addons;
using YetaWF.Core.Controllers;
using YetaWF.Core.Image;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Views {

    public class RazorView<TModel> : WebViewPage<TModel> {
        public override void Execute() { }
    }

    public class RazorView<TModule, TModel> : WebViewPage<TModel>
            where TModule: ModuleDefinition
            where TModel: class {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public override void Execute() { }
        public override void ExecutePageHierarchy() {
            Manager.PushModel(Model);
            ModuleDefinition oldMod = Manager.CurrentModule;
            Manager.CurrentModule = Module;
            base.ExecutePageHierarchy();
            Manager.CurrentModule = oldMod;
            Manager.PopModel();
        }

        // LOCALIZATION
        // LOCALIZATION
        // LOCALIZATION

        /// <summary>
        /// Get a localized string from resources
        /// </summary>
        public string __ResStr(string name, string defaultValue, params object[] parms) {
            return ResourceAccessHelper.__ResStr(Module, name, defaultValue, parms);
        }
        /// <summary>
        /// Get a localized string from resources
        /// </summary>
        public MvcHtmlString __ResStrHtml(string name, string defaultValue, params object[] parms) {
            return MvcHtmlString.Create(ResourceAccessHelper.__ResStr(Module, name, defaultValue, parms));
        }

        // MODULE PROPERTIES
        // MODULE PROPERTIES
        // MODULE PROPERTIES

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public TModule Module {
            get {
                if (Equals(_module, default(TModule))) {
                    _module = (TModule) ViewData[Globals.RVD_ModuleDefinition];
                    if (_module == default(TModule))
                        throw new InternalError("No ModuleDefinition available in view {0} {1}.", GetType().FullName);
                }
                return _module;
            }
        }
        private TModule _module;

        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT

        public new TModel Model {
            get {
                return ViewData.Model;
            }
        }

        public PageDefinition CurrentPage {
            get {
                return Manager.CurrentPage;
            }
        }

        // generates a new unique Id
        protected string UniqueId(string name = "a") {
            return Manager.UniqueId(name);
        }

        // A unique <div> in a view (one per view)
        public string DivId {
            get {
                if (string.IsNullOrEmpty(_divId))
                    _divId = UniqueId("div");
                return _divId;
            }
        }
        private string _divId;

        // DOCUMENTREADY
        // DOCUMENTREADY
        // DOCUMENTREADY

        protected class JSDocumentReady : IDisposable {
            public JSDocumentReady(HtmlHelper<TModel> Html) {
                this.Html = Html;
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                if (disposing) DisposableTracker.RemoveObject(this);
                Html.ViewContext.Writer.Write("});");
            }
            //~JSDocumentReady() { Dispose(false); }

            public HtmlHelper<TModel> Html { get; set; }
        }
        protected JSDocumentReady DocumentReady() {
            if (!Manager.IsAjaxRequest) {
                Html.ViewContext.Writer.Write("$(document).ready(function(){\n");
                return new JSDocumentReady(Html);
            } else {
                return null;
            }
        }

        // FORM
        // FORM
        // FORM

        protected MvcForm Form(string actionName, int dummy = 0, object HtmlAttributes = null, object Model = null, bool SaveReturnUrl = false) {
            Manager.NextUniqueIdPrefix();
            Manager.AddOnManager.AddAddOn("YetaWF", "Core", "Forms");

            _viewName = actionName;
            _model = Model;

            RouteValueDictionary rvd = FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes);
            if (SaveReturnUrl)
                rvd.Add(Basics.CssSaveReturnUrl, "");
            rvd.Add("class", Forms.CssFormAjax);

            return Html.BeginForm(_viewName, Module.Controller, null, FormMethod.Post, rvd);
        }
        private string _viewName = null;
        private object _model = null;

        // PartialForm rendering called during regular form processing (not ajax)
        public MvcHtmlString PartialForm(string partialViewName = null)
        {
            if (Manager.InPartialView)
                throw new InternalError("Already in partial form");
            Manager.InPartialView = true;

            if (string.IsNullOrWhiteSpace(partialViewName))
                partialViewName = _viewName;

            string viewHtml = "";

            try {
                if (!string.IsNullOrWhiteSpace(partialViewName)) {
                    partialViewName = YetaWFController.MakeFullViewName(partialViewName, Module.Area);
                    if (_model != null)
                        viewHtml = Html.Partial(partialViewName, _model).ToString();
                    else
                        viewHtml = Html.Partial(partialViewName).ToString();
                }

                //DEBUG:  viewHtml has the entire partial FORM

            } catch (Exception) {
                Manager.InPartialView = false;
                throw;
            }

            Manager.InPartialView = false;

            viewHtml = RazorView.PostProcessViewHtml(Html, Module, viewHtml);
            return MvcHtmlString.Create(viewHtml);
        }
        public MvcHtmlString FormButtons(List<FormButton> buttons, int dummy = 0) {
            return FormButtons(buttons.ToArray());
        }

        public MvcHtmlString FormButtons(FormButton[] buttons, int dummy = 0) {
            HtmlBuilder hb = new HtmlBuilder();
            if (Module.ShowFormButtons || Manager.EditMode) {
                hb.Append("<div class='t_detailsbuttons {0}'>", Globals.CssModuleNoPrint);
                foreach (FormButton button in buttons) {
                    hb.Append(button.Render());
                }
                hb.Append("</div>");
            }
            return hb.ToMvcHtmlString();
        }
    }

    public static class RazorView {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        internal static string PostProcessViewHtml(HtmlHelper htmlHelper, ModuleDefinition module, string viewHtml) {

            HtmlBuilder hb = new HtmlBuilder();

            TagBuilder tag = new TagBuilder("div");
            tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Forms.CssFormPartial));
            string divId = null;
            if (Manager.IsAjaxRequest) {
                divId = Manager.UniqueId();
                tag.Attributes.Add("id", divId);
            }
            hb.Append(tag.ToString(TagRenderMode.StartTag));

            hb.Append(htmlHelper.AntiForgeryToken().ToString());
            hb.Append(htmlHelper.Hidden(Basics.ModuleGuid, module.ModuleGuid));
            hb.Append(htmlHelper.Hidden(Forms.UniqueIdPrefix, Manager.UniqueIdPrefix));

            viewHtml = ProcessImages(viewHtml);
            hb.Append(viewHtml);

            hb.Append(tag.ToString(TagRenderMode.EndTag));

            if (divId != null)
                Manager.ScriptManager.Add(string.Format("YetaWF_Forms.initPartialForm($('#{0}'));", divId));

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            return vars.ReplaceModuleVariables(module, hb.ToString());
        }

        private static string ProcessImages(string viewHtml) {
            if (!Manager.IsAjaxRequest) return viewHtml; // we'll handle it in RazorPage::PostProcessHtml
            if (Manager.CurrentSite.UseHttpHandler) {
                if (Manager.CurrentSite.CanUseCDN && Manager.CurrentSite.CDNFileImage)
                    return ImageSupport.ProcessImagesAsCDN(viewHtml);
                return viewHtml;
            } else {
                return ImageSupport.ProcessImagesAsStatic(viewHtml);
            }
        }
    }
}
