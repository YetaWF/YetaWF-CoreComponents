using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Controllers;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Views;
#if MVC6
#else
using System.Web.Mvc;
using System.Web.Routing;
#endif

namespace YetaWF.Core.Components {

    public interface IYetaWFView<TMODULE, TMODEL> {
        Task<YHtmlString> RenderViewAsync(TMODULE module, TMODEL model);
    }
    public interface IYetaWFView2<TMODULE, TMODEL> : IYetaWFView<TMODULE, TMODEL> {
        Task<YHtmlString> RenderPartialViewAsync(TMODULE module, TMODEL model);
    }

    public abstract class YetaWFViewBase {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public YetaWFViewBase() {
            Package = GetPackage();
        }
#if MVC6
        public IHtmlHelper htmlHelper
#else
        public HtmlHelper HtmlHelper
#endif
        {
            get {
                if (_htmlHelper == null) throw new InternalError("No htmlHelper available");
                return _htmlHelper;
            }
            private set {
                _htmlHelper = value;
            }
        }
#if MVC6
        private IHtmlHelper _htmlHelper;
#else
        private HtmlHelper _htmlHelper;
#endif

        private ModuleDefinition ModuleBase { get; set; }

        public void SetRenderInfo(
#if MVC6
            IHtmlHelper htmlHelper
#else
            HtmlHelper htmlHelper
#endif
                , ModuleDefinition moduleBase
        ) {
            HtmlHelper = htmlHelper;
            ModuleBase = moduleBase;
        }

        public readonly Package Package;

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

        public string UniqueId(string name = "a") {
            return Manager.UniqueId(name);
        }

        public string HAE(string text) {
            return YetaWFManager.HtmlAttributeEncode(text);
        }
        public string HE(string text) {
            return YetaWFManager.HtmlEncode(text);
        }
        public string JE(string text) {
            return YetaWFManager.JserEncode(text);
        }

        public abstract Package GetPackage();
        public abstract string GetViewName();

        // FORM
        // FORM
        // FORM

        protected async Task<string> RenderBeginFormAsync(object HtmlAttributes = null, bool SaveReturnUrl = false, bool ValidateImmediately = false, string ActionName = null, string ControllerName = null, bool Pure = false) {

            Manager.NextUniqueIdPrefix();
            await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF", "Core", "Forms");

            if (string.IsNullOrWhiteSpace(ActionName))
                ActionName = GetViewName() + "_Partial";
            if (string.IsNullOrWhiteSpace(ControllerName))
                ControllerName = ModuleBase.Controller;

            IDictionary<string, object> rvd = AnonymousObjectToHtmlAttributes(HtmlAttributes);
            if (SaveReturnUrl)
                rvd.Add(Basics.CssSaveReturnUrl, "");

            if (!Pure) {
                string css = null;
                if (Manager.CurrentSite.FormErrorsImmed)
                    css = YetaWFManager.CombineCss(css, "yValidateImmediately");
                css = YetaWFManager.CombineCss(css, Forms.CssFormAjax);
                rvd.Add("class", css);
            }
#if MVC6
            //$$$verify MVC6 Source
#endif
            YTagBuilder tagBuilder = new YTagBuilder("form");
            tagBuilder.MergeAttributes(rvd, true);
            string formAction = UrlHelper.GenerateUrl(null /* routeName */, ActionName, ControllerName, null, HtmlHelper.RouteCollection, HtmlHelper.ViewContext.RequestContext, true /* includeImplicitMvcValues */);
            tagBuilder.MergeAttribute("action", formAction, true);
            tagBuilder.MergeAttribute("method", "post", true);

            return tagBuilder.ToString(YTagRenderMode.StartTag);
        }
        protected Task<string> RenderEndFormAsync() {
            return Task.FromResult("</form>");
        }
        private IDictionary<string, object> AnonymousObjectToHtmlAttributes(object htmlAttributes) {
            if (htmlAttributes as RouteValueDictionary != null) return (RouteValueDictionary)htmlAttributes;
            if (htmlAttributes as Dictionary<string, object> != null) return (Dictionary<string, object>)htmlAttributes;
            return HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
        }

        // PartialForm rendering called during regular form processing (not ajax)
        public async Task<YHtmlString> PartialForm(Func<Task<YHtmlString>> renderPartial, bool UsePartialFormCss = true, bool ShowView = true) {
            if (Manager.InPartialView)
                throw new InternalError("Already in partial form");
            Manager.InPartialView = true;

            YHtmlString viewHtml = new YHtmlString();

            try {
                if (ShowView)
                    viewHtml = await renderPartial();

                //DEBUG:  viewHtml has the entire partial FORM

            } catch (Exception) {
                throw;
            } finally {
                Manager.InPartialView = false;
            }
            string html = RazorViewExtensions.PostProcessViewHtml(HtmlHelper, ModuleBase, viewHtml.ToString(), UsePartialFormCss: UsePartialFormCss);
            return new YHtmlString(html);
        }

        public async Task<YHtmlString> FormButtonsAsync(List<FormButton> buttons, int dummy = 0) {
            return await FormButtonsAsync(buttons.ToArray());
        }
        public async Task<YHtmlString> FormButtonsAsync(FormButton[] buttons, int dummy = 0) {
            HtmlBuilder hb = new HtmlBuilder();
            if (ModuleBase.ShowFormButtons || Manager.EditMode) {
                hb.Append("<div class='t_detailsbuttons {0}'>", Globals.CssModuleNoPrint);
                foreach (FormButton button in buttons) {
                    hb.Append(await button.RenderAsync());
                }
                hb.Append("</div>");
            }
            return hb.ToYHtmlString();
        }
    }
}
