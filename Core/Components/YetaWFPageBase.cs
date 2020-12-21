/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// This interface is implemented by pages.
    /// The framework calls the RenderPageHeaderAsync and RenderPageBodyAsync methods to render the page.
    /// </summary>
    public interface IYetaWFPage {
        /// <summary>
        /// Returns the names of all panes available in this page.
        /// </summary>
        /// <returns>Returns a collection of pane names available in this page.</returns>
        List<string> GetPanes();
        /// <summary>
        /// Renders the page body (&lt;body&gt;, contents and &lt;/body&gt;).
        /// </summary>
        /// <returns>The HTML representing the page body.</returns>
        Task<string> RenderPageBodyAsync();
        /// <summary>
        /// Renders the page header (everything before &lt;body&gt;).
        /// </summary>
        /// <returns>The HTML representing the page header.</returns>
        Task<string> RenderPageHeaderAsync();
        /// <summary>
        /// Called by the framework for additional processing to be performed.
        /// </summary>
        /// <remarks>A possible use for this method is to add fonts to a page.</remarks>
        Task AdditionalProcessingAsync();
    }

    /// <summary>
    /// The base class for all pages used in YetaWF.
    /// </summary>
    public abstract class YetaWFPageBase {

        /// <summary>
        /// The YetaWF.Core.Support.Manager instance of current HTTP request.
        /// </summary>
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public YetaWFPageBase() {
            Package = GetPackage();
        }

        /// <summary>
        /// Defines the package implementing this page.
        /// </summary>
        public readonly Package Package;

        /// <summary>
        /// The YHtmlHelper instance.
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
        private YHtmlHelper? _htmlHelper;

        /// <summary>
        /// Sets rendering information for the page.
        /// </summary>
        /// <param name="htmlHelper">The YHtmlHelper instance.</param>
        public void SetRenderInfo(YHtmlHelper htmlHelper) {
            HtmlHelper = htmlHelper;
        }

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
        public string HAE(string? text) {
            return Utility.HAE(text);
        }
        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as HTML.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns encoded HTML.</returns>
        public string HE(string? text) {
            return Utility.HE(text);
        }

        /// <summary>
        /// Encodes the provided <paramref name="text"/> suitable for use as a JavaScript string.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>Returns encoded JavaScript string.
        /// The string to encode should not use surrounding quotes.
        /// These must be added after encoding.
        /// </returns>
        public string JE(string? text) {
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
        /// Returns the package implementing the page.
        /// </summary>
        /// <returns>Returns the package implementing the page.</returns>
        public abstract Package GetPackage();
        /// <summary>
        /// Returns the name of the page.
        /// </summary>
        /// <returns>Returns the name of the page.</returns>
        public abstract string GetPageName();

        public async Task<string> RenderPaneAsync(string pane, string? cssClass = null, bool Conditional = true, bool Unified = false) {

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
                    sb.Append(await Manager.CurrentPage.RenderPaneAsync(HtmlHelper, pane, cssClass, Conditional: Conditional, UnifiedMainPage: realPage));
                }
                Manager.CurrentPage = realPage;
                return sb.ToString();
            } else {
                return await Manager.CurrentPage.RenderPaneAsync(HtmlHelper, pane, cssClass, Conditional: Conditional);
            }
        }
    }
}
