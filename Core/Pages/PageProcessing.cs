/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Image;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    public class PageProcessing {

        public PageProcessing(YetaWFManager manager) { Manager = manager; }

        protected YetaWFManager Manager { get; private set; }

        private static readonly Regex reHead = new Regex("<\\s*head\\s*>", RegexOptions.Compiled);
        private static readonly Regex reEndHead = new Regex("</\\s*head\\s*>", RegexOptions.Compiled);
        private static readonly Regex reStartBody = new Regex("<\\s*body[^>]*>", RegexOptions.Compiled);
        private static readonly Regex reEndBody = new Regex("</\\s*body\\s*>", RegexOptions.Compiled);

        public async Task<string> PostProcessHtmlAsync(string pageHtml) {

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            pageHtml = vars.ReplaceVariables(pageHtml);// variable substitution

            // complete page html in pageHtml
            pageHtml = ProcessImages(pageHtml);

            SiteDefinition currentSite = Manager.CurrentSite;

            string yetawfMsg;
            if (!currentSite.DEBUGMODE && currentSite.Compression) {
                yetawfMsg = "/**** Powered by Yet Another Web Framework - https://YetaWF.com - (c) Copyright <<YEAR>> Softel vdm, Inc. */";
            } else {
                yetawfMsg = "\n" +
                    "/*****************************************/\n" +
                    "/* Powered by Yet Another Web Framework  */\n" +
                    "/* https://YetaWF.com                    */\n" +
                    "/* (c) Copyright <<YEAR>> - Softel vdm, Inc. */\n" +
                    "/*****************************************/" +
                    "\n";
            }
            yetawfMsg = yetawfMsg.Replace("<<YEAR>>", DateTime.Now.Year.ToString());//local time
            // <head>+yetawfMsg replaces <head>
            pageHtml = reHead.Replace(pageHtml, (m) => "<head><!-- " + yetawfMsg + " -->", 1);

            // <link rel="alternate">
            string linkAlt = Manager.LinkAltManager.Render().ToString();
            if (string.IsNullOrWhiteSpace(linkAlt))
                linkAlt = "";

            // <link rel="stylesheet">
            string css = "";
            if (currentSite.CssLocation == Site.CssLocationEnum.Top)
                css = (await Manager.CssManager.RenderAsync()).ToString();

            string head = "";
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraHead))
                head = Manager.CurrentPage.ExtraHead;
            else if (!string.IsNullOrWhiteSpace(currentSite.ExtraHead))
                head = currentSite.ExtraHead;

            // linkAlt+css+js+</head> replaces </head>
            string js = "";
            if (currentSite.JSLocation == Site.JSLocationEnum.Top)
                js = (await Manager.ScriptManager.RenderAsync()).ToString();
            pageHtml = reEndHead.Replace(pageHtml, (m) => linkAlt + css + js + head + "</head>", 1);

            string bodyStart = "";
            if (!currentSite.DisableMinimizeFUOC && (currentSite.JSLocation == Site.JSLocationEnum.Bottom || currentSite.CssLocation == Site.CssLocationEnum.Bottom))
                bodyStart += "<script>document.body.style.display='none';</script>";
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraBodyTop))
                bodyStart += Manager.CurrentPage.ExtraBodyTop;
            else if (!string.IsNullOrWhiteSpace(currentSite.ExtraBodyTop))
                bodyStart += currentSite.ExtraBodyTop;
            if (!string.IsNullOrWhiteSpace(bodyStart))
                pageHtml = reStartBody.Replace(pageHtml, (m) => m.Value + bodyStart, 1);

            // css + js + endofpage-js + </body> replaces </body>
            // <script ..>
            js = "";
            if (currentSite.JSLocation == Site.JSLocationEnum.Bottom)
                js = (await Manager.ScriptManager.RenderAsync()).ToString();
            css = "";
            if (currentSite.CssLocation == Site.CssLocationEnum.Bottom)
                css = (await Manager.CssManager.RenderAsync()).ToString();

            string endstuff = css;
            if (!currentSite.DisableMinimizeFUOC && (currentSite.JSLocation == Site.JSLocationEnum.Bottom || currentSite.CssLocation == Site.CssLocationEnum.Bottom))
                endstuff += "<script>document.body.style.display='block';</script>";
            endstuff += js;
            endstuff += Manager.ScriptManager.RenderEndofPageScripts();
            if (Manager.Deployed) {
                if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.Analytics))
                    endstuff += Manager.CurrentPage.Analytics;
                else if (!string.IsNullOrWhiteSpace(currentSite.Analytics))
                    endstuff += currentSite.Analytics;
            }
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraBodyBottom))
                endstuff += Manager.CurrentPage.ExtraBodyBottom;
            else if (!string.IsNullOrWhiteSpace(currentSite.ExtraBodyBottom))
                endstuff += currentSite.ExtraBodyBottom;

            pageHtml = reEndBody.Replace(pageHtml, (m) => endstuff + "</body>", 1);

            //DEBUG:  pageHtml has entire page

            return pageHtml;
        }

        /// <summary>
        /// Post process a rendered pane so it can be returned to the client (used during Unified Page Sets dynamic module processing).
        /// </summary>
        /// <param name="paneHtml"></param>
        /// <returns></returns>
        public string PostProcessContentHtml(string paneHtml) {

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            paneHtml = vars.ReplaceVariables(paneHtml);// variable substitution

            // fix up image urls for cdn use
            return ProcessImages(paneHtml);
        }

        private string ProcessImages(string pageHtml) {
            if (Manager.CurrentSite.CanUseCDN || Manager.CurrentSite.CanUseStaticDomain)
                return ImageSupport.ProcessImagesAsCDN(pageHtml);
            return pageHtml;
        }
    }
}
