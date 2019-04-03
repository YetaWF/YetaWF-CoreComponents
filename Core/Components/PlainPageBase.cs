﻿/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
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
        public Task<YHtmlString> RenderPageHeaderAsync() {

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
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    {Manager.MetatagsHtml}
    {Manager.PageTitleHtml}
    {favIcon}
    {Manager.CurrentPage.CanonicalUrlLink}
    {Manager.CurrentPage.HrefLangHtml}
</head>");

            return Task.FromResult(hb.ToYHtmlString());
        }

        /// <summary>
        /// Renders the page body (&lt;body&gt;, contents and &lt;/body&gt;).
        /// </summary>
        /// <returns>The HTML representing the page body.</returns>
        public async Task<YHtmlString> RenderPageBodyAsync() {

            HtmlBuilder hb = new HtmlBuilder();

            string copyright = Manager.CurrentPage.CopyrightEvaluated;
            if (string.IsNullOrEmpty(copyright))
                copyright = Manager.CurrentSite.CopyrightEvaluated;

            hb.Append($@"
<body class='{Manager.PageCss()}'>
    <noscript><div class='yDivWarning' style='height:100px;text-align:center;vertical-align:middle'>This site requires Javascript</div></noscript>
    {await HtmlHelper.RenderEditControlAsync(new Guid("{267f00cc-c619-4854-baed-9e4b812d7e95}"))}
    {await HtmlHelper.RenderPageControlAsync(new Guid("{466C0CCA-3E63-43f3-8754-F4267767EED1}"))}
    <div class='MainPane'>
        {await RenderPaneAsync(Globals.MainPane, "MainPane AnyPane", Unified: true)}
    </div>
    <div class='pageFooter'>{HE(copyright)}</div>
    {await HtmlHelper.RenderUniqueModuleAddOnsAsync()}
</body>");

            return hb.ToYHtmlString();
        }
    }
}
