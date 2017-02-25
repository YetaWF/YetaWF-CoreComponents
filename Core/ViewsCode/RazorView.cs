﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Addons;
using YetaWF.Core.Controllers;
using YetaWF.Core.Image;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;
#if MVC6
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading.Tasks;
#else
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
#endif

namespace YetaWF.Core.Views {


#if MVC6
    public class RazorView<TModule, TModel> : Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>, IRazorPageLifetime
                where TModule: ModuleDefinition where TModel : class  {
#else
    public class RazorView<TModule, TModel> : WebViewPage<TModel>
                where TModule: ModuleDefinition where TModel: class {
#endif
        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public RazorView() { }
#if MVC6
        private HtmlHelper<TModel> GetHtml() {
            dynamic page = this;
            return page.Html;
        }
#else
        private HtmlHelper<TModel> GetHtml() {
            return Html;
        }
#endif
#if MVC6
        public override async Task ExecuteAsync() { await Task.FromResult(0); }
#else
        public override void Execute() { }
        public override void ExecutePageHierarchy() {
            BeginRender(null);
            base.ExecutePageHierarchy();
            EndRender(null);
        }
#endif
        public void BeginRender(ViewContext context) {
#if MVC6
            // NOTE: the page has not been activated when using MVC6 so all data has to be extracted from context.
            // context is null with MVC5
            ModuleDefinition module = (ModuleDefinition)context.ViewData[Globals.RVD_ModuleDefinition];
            TModel model = (TModel) context.ViewData.Model;
#else
            ModuleDefinition module = Module;
            TModel model = (TModel) Model;
#endif
            Manager.PushModel(model);
            _oldMod = Manager.CurrentModule;
            Manager.CurrentModule = module;
        }

        public void EndRender(ViewContext context) {
            Manager.CurrentModule = _oldMod;
            Manager.PopModel();
        }
        ModuleDefinition _oldMod;

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
                if (_module == null) {
                    _module = ViewData[Globals.RVD_ModuleDefinition] as TModule;
                    if (_module == null)
                        throw new InternalError("No ModuleDefinition available in view {0} {1}.", GetType().FullName);
                }
                return (TModule)_module;
            }
        }
        private object _module;

        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT
        // CONTROLLER/VIEW SUPPORT

#if MVC6
#else
        public new TModel Model {
            get {
                return ViewData.Model;
            }
        }
#endif
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
#if MVC6
            public JSDocumentReady(IHtmlHelper<TModel> Html)
#else
            public JSDocumentReady(HtmlHelper<TModel> Html)
#endif
            {
                this.Html = Html;
                DisposableTracker.AddObject(this);
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
                if (disposing) DisposableTracker.RemoveObject(this);
                Html.ViewContext.Writer.Write("});");
            }
            //~JSDocumentReady() { Dispose(false); }
#if MVC6
            public IHtmlHelper<TModel> Html { get; set; }
#else
            public HtmlHelper<TModel> Html { get; set; }
#endif
        }
        protected JSDocumentReady DocumentReady() {
            if (!Manager.IsAjaxRequest) {
#if MVC6
                IHtmlHelper<TModel> htmlHelper = GetHtml();
#else
                HtmlHelper<TModel> htmlHelper = GetHtml();
#endif
                htmlHelper.ViewContext.Writer.Write("$(document).ready(function(){\n");
                return new JSDocumentReady(htmlHelper);
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

            IDictionary<string,object> rvd = FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes);
            if (SaveReturnUrl)
                rvd.Add(Basics.CssSaveReturnUrl, "");
            rvd.Add("class", Forms.CssFormAjax);

#if MVC6
            HtmlHelper<TModel> htmlHelper = GetHtml();
            return htmlHelper.BeginForm(_viewName, Module.Controller, null, FormMethod.Post, false, rvd);
#else
            return Html.BeginForm(_viewName, Module.Controller, null, FormMethod.Post, rvd);
#endif
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
                        viewHtml = GetHtml().Partial(partialViewName, _model).AsString();
                    else
                        viewHtml = GetHtml().Partial(partialViewName).AsString();
                }

                //DEBUG:  viewHtml has the entire partial FORM

            } catch (Exception) {
                Manager.InPartialView = false;
                throw;
            }

            Manager.InPartialView = false;
#if MVC6
            viewHtml = RazorViewExtensions.PostProcessViewHtml(GetHtml(), Module, viewHtml);
#else
            viewHtml = RazorViewExtensions.PostProcessViewHtml(Html, Module, viewHtml);
#endif
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

    public static class RazorViewExtensions {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        internal static string PostProcessViewHtml(IHtmlHelper htmlHelper, ModuleDefinition module, string viewHtml) {
#else
        internal static string PostProcessViewHtml(HtmlHelper htmlHelper, ModuleDefinition module, string viewHtml) {
#endif
            HtmlBuilder hb = new HtmlBuilder();

            TagBuilder tag = new TagBuilder("div");
            tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Forms.CssFormPartial));
            string divId = null;
            if (Manager.IsAjaxRequest) {
                divId = Manager.UniqueId();
                tag.Attributes.Add("id", divId);
            }
            hb.Append(tag.ToString(TagRenderMode.StartTag));

            hb.Append(htmlHelper.AntiForgeryToken());
            // id required below because MVC6 DefaultHtmlGenerator generates id (which can become duplicates if multiple forms contain the same hidden fields)
            hb.Append(htmlHelper.Hidden(Basics.ModuleGuid, module.ModuleGuid, new { id = Manager.UniqueId() }));
            hb.Append(htmlHelper.Hidden(Forms.UniqueIdPrefix, Manager.UniqueIdPrefix, new { id = Manager.UniqueId() }));

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