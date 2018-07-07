/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;
using YetaWF.Core.Views;
#if MVC6
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public YetaWFViewBase() {
            Package = GetPackage();
        }
#if MVC6
        public IHtmlHelper HtmlHelper
#else
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
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

        protected ModuleDefinition ModuleBase { get; set; }

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
            string html = await PostProcessView.ProcessAsync(HtmlHelper, ModuleBase, viewHtml.ToString(), UsePartialFormCss: UsePartialFormCss);
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
