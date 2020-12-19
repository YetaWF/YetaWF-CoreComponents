/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins.Pages {

    /// <summary>
    /// The base class for popup pages.
    /// </summary>
    public abstract class PopupPageBase : YetaWFPageBase, IYetaWFPage {

        /// <summary>
        /// Returns the package implementing the page.
        /// </summary>
        /// <returns>Returns the package implementing the page.</returns>
        public override Package GetPackage() { return AreaRegistration.CurrentPackage; }

        /// <summary>
        /// Returns the names of all panes available in this page.
        /// </summary>
        /// <returns>Returns a collection of pane names available in this page.</returns>
        public List<string> GetPanes() {
            return new List<string> { Globals.MainPane };
        }

        /// <summary>
        /// Renders the page header (everything before &lt;body&gt;).
        /// </summary>
        /// <returns>The HTML representing the page header.</returns>
        public Task<string> RenderPageHeaderAsync() {

            HtmlBuilder hb = new HtmlBuilder();

            string favIcon = Manager.CurrentPage.FavIconLink;
            if (string.IsNullOrEmpty(favIcon))
                favIcon = Manager.CurrentSite.FavIconLink;

            hb.Append($@"
<!DOCTYPE html>
<html lang='{Manager.CurrentPage.GetPageLanguageId()}'>
<head>
    <meta http-equiv='Content-Type' content='text/html; charset=UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    {Manager.MetatagsHtml}
    {Manager.PageTitleHtml}
    {Manager.CurrentPage.HrefLangHtml}
</head>");

            return Task.FromResult(hb.ToString());
        }

        /// <summary>
        /// Renders the page body (&lt;body&gt;, contents and &lt;/body&gt;).
        /// </summary>
        /// <returns>The HTML representing the page body.</returns>
        public async Task<string> RenderPageBodyAsync() {

            HtmlBuilder hb = new HtmlBuilder();

            hb.Append($@"
<body class='{Manager.PageCss()}'>
    {await RenderPaneAsync(Globals.MainPane, "MainPane AnyPane")}
    {await HtmlHelper.RenderUniqueModuleAddOnsAsync()}
</body>");

            return hb.ToString();
        }

        /// <summary>
        /// Called by the framework for additional processing to be performed.
        /// </summary>
        /// <remarks>A possible use for this method is to add fonts to a page.</remarks>
        public virtual Task AdditionalProcessingAsync() { return Task.CompletedTask; }
    }
    /// <summary>
    /// Implements the Popup page.
    /// </summary>
    public class PopupPage : PopupPageBase {
        /// <summary>
        /// Returns the name of the page.
        /// </summary>
        /// <returns>Returns the name of the page.</returns>
        public override string GetPageName() { return "Popup"; }
    }
    /// <summary>
    /// Implements the PopupSmall page.
    /// </summary>
    public class PopupSmallPage : PopupPageBase {
        /// <summary>
        /// Returns the name of the page.
        /// </summary>
        /// <returns>Returns the name of the page.</returns>
        public override string GetPageName() { return "PopupSmall"; }
    }
    /// <summary>
    /// Implements the PopupMedium page.
    /// </summary>
    public class PopupMediumPage : PopupPageBase {
        /// <summary>
        /// Returns the name of the page.
        /// </summary>
        /// <returns>Returns the name of the page.</returns>
        public override string GetPageName() { return "PopupMedium"; }
    }
}
