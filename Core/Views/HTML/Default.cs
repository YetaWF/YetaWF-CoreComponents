/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Controllers;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Skins.Pages {

    /// <summary>
    /// Implements the standard Default page.
    /// </summary>
    public class DefaultPage : YetaWFPageBase, IYetaWFPage {

        /// <summary>
        /// Returns the name of the page.
        /// </summary>
        /// <returns>Returns the name of the page.</returns>
        public override string GetPageName() { return "Default"; }

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
        /// Renders the page.
        /// </summary>
        /// <returns>The HTML representing the page.</returns>
        public async Task<YHtmlString> RenderPageAsync() {

            HtmlBuilder hb = new HtmlBuilder();

            string favIcon = Manager.CurrentPage.FavIconLink;
            if (string.IsNullOrEmpty(favIcon))
                favIcon = Manager.CurrentSite.FavIconLink;

            string copyright = Manager.CurrentPage.CopyrightEvaluated;
            if (string.IsNullOrEmpty(copyright))
                copyright = Manager.CurrentSite.CopyrightEvaluated;

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
</head>
<body class='{Manager.PageCss()}'>
    <noscript><div class='yDivWarning' style='height:100px;text-align:center;vertical-align:middle'>This site requires Javascript</div></noscript>
    <div class='pageOuterWrapper'>
        {await HtmlHelper.RenderModuleAsync("YetaWF.TinyLogin", "YetaWF.Modules.TinyLogin.Modules.TinyLoginModule")}
        {await HtmlHelper.RenderModuleAsync("YetaWF.Menus", "YetaWF.Modules.Menus.Modules.MainMenuModule")}

        {await RenderPaneAsync("", "MainPane AnyPane", Unified: true)}

        <div class='pageFooter'>{YetaWFManager.HtmlEncode(copyright)}</div>

        {await HtmlHelper.RenderPageControlAsync(new System.Guid("{466C0CCA-3E63-43f3-8754-F4267767EED1}"))}
    </div>
    {await HtmlHelper.RenderUniqueModuleAddOnsAsync()}
</body>
</html>");

            return hb.ToYHtmlString();
        }
    }
}
