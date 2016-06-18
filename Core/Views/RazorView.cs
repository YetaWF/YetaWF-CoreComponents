/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
            }
            public void Dispose() { Dispose(true); }
            protected virtual void Dispose(bool disposing) {
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
        public enum ButtonTypeEnum {
            Submit = 0,
            Cancel = 1,
            Button = 2,
            Empty = 3,
            Apply = 4,
            ConditionalSubmit = 5, /* Like Submit but is removed when we don't have a return url (used together with an apply button) */
            ConditionalCancel = 6, /* Like Cancel but doesn't consider whether we have a return url */
        }
        public class FormButton {
            public string Text { get; set; }
            public string Title { get; set; }
            public string Name { get; set; }
            public string Id { get; set; }
            public bool Hidden { get; set; }
            public ButtonTypeEnum ButtonType { get; set; }
            public ModuleAction Action { get; private set; }
            public ModuleAction.RenderModeEnum RenderAs { get; set; }

            protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

            public FormButton() { }
            public FormButton(ModuleAction action, ModuleAction.RenderModeEnum renderAs = ModuleAction.RenderModeEnum.Button) {
                ButtonType = action != null ? ButtonTypeEnum.Button : ButtonTypeEnum.Empty;
                Action = action;
                RenderAs = renderAs;
            }
            public MvcHtmlString Render() {
                if (ButtonType == ButtonTypeEnum.Empty)
                    return MvcHtmlString.Empty;
                if (Action != null) {
                    if (RenderAs == ModuleAction.RenderModeEnum.IconsOnly)
                        return Action.RenderAsIcon();
                    else
                        return Action.RenderAsButton();
                } else {
                    TagBuilder tag = new TagBuilder("input");

                    string text = Text;
                    switch (ButtonType) {
                        case ButtonTypeEnum.Submit:
                        case ButtonTypeEnum.ConditionalSubmit:
                            if (ButtonType == ButtonTypeEnum.ConditionalSubmit && !Manager.IsInPopup && !Manager.HaveReturnToUrl) {
                                // if we don't have anyplace to return to and we're not in a popup we don't need a submit button
                                return MvcHtmlString.Empty;
                            }
                            if (string.IsNullOrWhiteSpace(text)) text = this.__ResStr("btnSave", "Save");
                            tag.Attributes.Add("type", "submit");
                            break;
                        case ButtonTypeEnum.Apply:
                            if (string.IsNullOrWhiteSpace(text)) text = this.__ResStr("btnApply", "Apply");
                            tag.Attributes.Add("type", "button");
                            tag.Attributes.Add(Forms.CssDataApplyButton, "");
                            break;
                        default:
                        case ButtonTypeEnum.Button:
                            tag.Attributes.Add("type", "button");
                            break;
                        case ButtonTypeEnum.Cancel:
                        case ButtonTypeEnum.ConditionalCancel:
                            if (ButtonType == ButtonTypeEnum.ConditionalCancel && !Manager.IsInPopup && !Manager.HaveReturnToUrl) {
                                // if we don't have anyplace to return to and we're not in a popup we don't need a cancel button
                                return MvcHtmlString.Empty;
                            }
                            if (string.IsNullOrWhiteSpace(text)) text = this.__ResStr("btnCancel", "Cancel");
                            tag.Attributes.Add("type", "button");
                            tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Forms.CssFormCancel));
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(Id))
                        tag.Attributes.Add("id", Id);
                    if (!string.IsNullOrWhiteSpace(Name))
                        tag.Attributes.Add("name", Name);
                    if (Hidden)
                        tag.Attributes.Add("style", "display:none");
                    if (!string.IsNullOrWhiteSpace(Title))
                        tag.Attributes.Add("title", Title);
                    tag.Attributes.Add("value", text);
                    return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag));
                }
            }
        }
        public MvcHtmlString FormButtons(List<FormButton> buttons, int dummy = 0) {
            return FormButtons(buttons.ToArray());
        }

        public MvcHtmlString FormButtons(FormButton[] buttons, int dummy = 0) {
            HtmlBuilder hb = new HtmlBuilder();
            if (Module.ShowFormButtons || Manager.EditMode) {
                hb.Append("<div class='t_detailsbuttons {0}'>", Globals.CssModuleNoPrint);
                foreach (var button in buttons) {
                    hb.Append(button.Render());
                }
                hb.Append("</div>");
            }
            return hb.ToMvcHtmlString();
        }
    }

    public class RazorView {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

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
