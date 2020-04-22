/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    /// <summary>
    /// This interface is implemented by views.
    /// The framework calls the RenderViewAsync method to render the view.
    /// </summary>
    /// <typeparam name="TMODULE">The module type implementing the view.</typeparam>
    /// <typeparam name="TMODEL">The type of the model rendered by the view.</typeparam>
    public interface IYetaWFView<TMODULE, TMODEL> {
        /// <summary>
        /// Renders the view.
        /// </summary>
        /// <param name="module">The module on behalf of which the view is rendered.</param>
        /// <param name="model">The model being rendered by the view.</param>
        /// <returns>The HTML representing the view.</returns>
        Task<string> RenderViewAsync(TMODULE module, TMODEL model);
    }
    /// <summary>
    /// This interface is implemented by views.
    /// The framework calls the RenderPartialViewAsync method to render the partial view.
    /// A partial view is the portion of the view between &lt;form&gt; and &lt;/form&gt; tags.
    /// </summary>
    /// <typeparam name="TMODULE">The module type implementing the view.</typeparam>
    /// <typeparam name="TMODEL">The type of the model rendered by the view.</typeparam>
    public interface IYetaWFView2<TMODULE, TMODEL> : IYetaWFView<TMODULE, TMODEL> {
        /// <summary>
        /// Renders the view's partial view.
        /// A partial view is the portion of the view between &lt;form&gt; and &lt;/form&gt; tags.
        /// </summary>
        /// <param name="module">The module on behalf of which the partial view is rendered.</param>
        /// <param name="model">The model being rendered by the partial view.</param>
        /// <returns>The HTML representing the partial view.</returns>
        Task<string> RenderPartialViewAsync(TMODULE module, TMODEL model);
    }

    /// <summary>
    /// The base class for all views used in YetaWF.
    /// </summary>
    public abstract class YetaWFViewBase {

        /// <summary>
        /// The YetaWF.Core.Support.Manager instance of current HTTP request.
        /// </summary>
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public YetaWFViewBase() {
            Package = GetPackage();
        }

        /// <summary>
        /// Defines the package implementing this view.
        /// </summary>
        public readonly Package Package;

        /// <summary>
        /// The HtmlHelper instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public YHtmlHelper HtmlHelper {
            get {
                if (_htmlHelper == null) throw new InternalError("No htmlHelper available");
                return _htmlHelper;
            }
            private set {
                _htmlHelper = value;
            }
        }
        private YHtmlHelper _htmlHelper;

        /// <summary>
        /// The module on behalf of which this view is rendered.
        /// </summary>
        protected ModuleDefinition ModuleBase { get; set; }

        /// <summary>
        /// Sets rendering information for the view.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="moduleBase">The module on behalf of which this view is rendered.</param>
        public void SetRenderInfo(YHtmlHelper htmlHelper, ModuleDefinition moduleBase) {
            HtmlHelper = htmlHelper;
            ModuleBase = moduleBase;
        }

        /// <summary>
        /// The HTML id of this view.
        /// </summary>
        public string ControlId {
            get {
                if (string.IsNullOrEmpty(_controlId))
                    _controlId = Manager.UniqueId("ctrl");
                return _controlId;
            }
        }
        private string _controlId;

        /// <summary>
        /// The HTML id used for a &lt;div&gt; tag.
        /// </summary>
        /// <remarks>This is a convenience property, so a view can reference one of its &lt;div&gt; tags by id.
        ///</remarks>
        public string DivId {
            get {
                if (string.IsNullOrEmpty(_divId))
                    _divId = Manager.UniqueId("div");
                return _divId;
            }
        }
        private string _divId;

        /// <summary>
        /// Returns a unique HTML id.
        /// </summary>
        /// <param name="name">A string prefix prepended to the generated id.</param>
        /// <returns>A unique HTML id.</returns>
        /// <remarks>Every call to the Unique() method returns a new, unique id.</remarks>
        public string UniqueId(string name = "a") {
            return Manager.UniqueId(name);
        }

        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as an HTML attribute data value.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns an encoded HTML attribute data value.</returns>
        public string HAE(string text) {
            return Utility.HtmlAttributeEncode(text);
        }
        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as HTML.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns encoded HTML.</returns>
        public string HE(string text) {
            return Utility.HtmlEncode(text);
        }
        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as a JavaScript string.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns encoded JavaScript string.
        /// The string to encode should not use surrounding quotes.
        /// These must be added after encoding.
        /// </returns>
        public string JE(string text) {
            return Utility.JserEncode(text);
        }
        /// <summary>
        /// Encodes the provided <paramref name="val"/> a JavaScript true/false string.
        /// </summary>
        /// <param name="val">The value to encode.</param>
        /// <returns>Returns a JavaScript true/false string.</returns>
        public static string JE(bool val) {
            return val ? "true" : "false";
        }

        /// <summary>
        /// Returns the package implementing the view.
        /// </summary>
        /// <returns>Returns the package implementing the view.</returns>
        public abstract Package GetPackage();
        /// <summary>
        /// Returns the name of the view.
        /// </summary>
        /// <returns>Returns the name of the view.</returns>
        public abstract string GetViewName();

        /// <summary>
        /// Renders a partial form view (the portion between &lt;form&gt; and &lt;/form&gt;).
        /// </summary>
        /// <param name="renderPartial">Method called to render the view portion.</param>
        /// <param name="UsePartialFormCss">Defines whether partial form CSS is used.</param>
        /// <param name="ShowView">Defines whether the partial view rendering method <paramref name="renderPartial"/> is called.</param>
        /// <returns>Returns the partial form as HTML.</returns>
        public async Task<string> PartialForm(Func<Task<string>> renderPartial, bool UsePartialFormCss = true, bool ShowView = true) {
            // PartialForm rendering called during regular form processing (not ajax)
            if (Manager.InPartialView)
                throw new InternalError("Already in partial form");
            Manager.InPartialView = true;

            string viewHtml = null;

            try {
                if (ShowView)
                    viewHtml = await renderPartial();

                //DEBUG:  viewHtml has the entire partial FORM

            } catch (Exception) {
                throw;
            } finally {
                Manager.InPartialView = false;
            }
            string html = await PostProcessView.ProcessAsync(HtmlHelper, ModuleBase, viewHtml, UsePartialFormCss: UsePartialFormCss);
            return html;
        }

        /// <summary>
        /// Renders form buttons.
        /// </summary>
        /// <param name="buttons">The collection of form buttons to render.</param>
        /// <returns>Returns the rendered form buttons as HTML.</returns>
        public async Task<string> FormButtonsAsync(List<FormButton> buttons, string CssClass = "t_detailsbuttons") {
            return await FormButtonsAsync(buttons.ToArray(), CssClass);
        }
        /// <summary>
        /// Renders form buttons.
        /// </summary>
        /// <param name="buttons">The array of form buttons to render.</param>
        /// <returns>Returns the rendered form buttons as HTML.</returns>
        public async Task<string> FormButtonsAsync(FormButton[] buttons, string CssClass = "t_detailsbuttons") {
            HtmlBuilder hb = new HtmlBuilder();
            if (ModuleBase.ShowFormButtons || Manager.EditMode) {
                hb.Append("<div class='{0} {1}'>", CssClass, Globals.CssModuleNoPrint);
                foreach (FormButton button in buttons) {
                    hb.Append((await button.RenderAsync()));
                }
                hb.Append("</div>");
            }
            return hb.ToString();
        }
    }
}
