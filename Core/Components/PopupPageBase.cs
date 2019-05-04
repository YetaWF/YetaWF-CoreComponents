/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// The base class for popup pages.
    /// </summary>
    public abstract class PopupPageBase : YetaWFPageBase, IYetaWFPage {

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
        public virtual async Task<string> RenderPageBodyAsync() {

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
}
