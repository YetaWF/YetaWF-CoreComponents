/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Base class for all Plain pages.
    /// </summary>
    public abstract class PlainPageBase : YetaWFPageBase, IYetaWFPage {

        /// <summary>
        /// Returns the name of the page.
        /// </summary>
        /// <returns>Returns the name of the page.</returns>
        public override string GetPageName() { return "Plain"; }

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
        public virtual Task<string> RenderPageHeaderAsync() {

            HtmlBuilder hb = new HtmlBuilder();

            string? favIcon = Manager.CurrentPage.FavIconLink;
            if (string.IsNullOrEmpty(favIcon))
                favIcon = Manager.CurrentSite.FavIconLink;

            hb.Append($@"
<!DOCTYPE html>
<html lang='{Manager.CurrentPage.GetPageLanguageId()}'>
<head>
    <meta http-equiv='Content-Type' content='text/html; charset=UTF-8' />
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    {Manager.MetatagsHtml}
    {Manager.PageTitleHtml}
    {favIcon}
    {Manager.CurrentPage.CanonicalUrlLink}
    {Manager.CurrentPage.HrefLangHtml}
</head>");

            return Task.FromResult(hb.ToString());
        }

        /// <summary>
        /// Renders the page body (&lt;body&gt;, contents and &lt;/body&gt;).
        /// </summary>
        /// <returns>The HTML representing the page body.</returns>
        public virtual async Task<string> RenderPageBodyAsync() {

            HtmlBuilder hb = new HtmlBuilder();

            string? copyright = Manager.CurrentPage.CopyrightEvaluated;
            if (string.IsNullOrEmpty(copyright))
                copyright = Manager.CurrentSite.CopyrightEvaluated;

            hb.Append($@"
<body class='{Manager.PageCss()}'>
    {await HtmlHelper.RenderPageStatus(WantLocked:false)}
    <div class='MainPane'>
        {await RenderPaneAsync(Globals.MainPane, "MainPane AnyPane", Unified: true)}
    </div>
    <div class='pageFooter'>{HE(copyright)}</div>
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
}
