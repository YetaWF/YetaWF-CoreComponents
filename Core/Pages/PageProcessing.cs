/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Image;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    public class PageProcessing {

        public PageProcessing(YetaWFManager manager) { Manager = manager; }

        protected YetaWFManager Manager { get; private set; }

        public async Task<string> PostProcessHtmlAsync(string pageHtml) {

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            pageHtml = vars.ReplaceVariables(pageHtml);// variable substitution

            // complete page html in pageHtml
            pageHtml = ProcessImages(pageHtml);

            SiteDefinition currentSite = Manager.CurrentSite;

            string yetawfMsg;
            if (!currentSite.DEBUGMODE && currentSite.Compression) {
                yetawfMsg = $"/**** Powered by Yet Another Web Framework - https://YetaWF.com - (c) Copyright {DateTime.Now.Year.ToString()} Softel vdm, Inc. */";// local time
            } else {
                yetawfMsg = "\n" +
                    "/*****************************************/\n" +
                    "/* Powered by Yet Another Web Framework  */\n" +
                    "/* https://YetaWF.com                    */\n" +
                    $"/* (c) Copyright {DateTime.Now.Year.ToString()} - Softel vdm, Inc. */\n" + // local time
                    "/*****************************************/" +
                    "\n";
            }
            // <head>+yetawfMsg replaces <head>
            pageHtml = ReplaceOnce(pageHtml, "<head>", "<head><!-- " + yetawfMsg + " -->");

            // <link rel="alternate">
            string linkAlt = Manager.LinkAltManager.Render();
            if (string.IsNullOrWhiteSpace(linkAlt))
                linkAlt = "";

            // <link rel="stylesheet">
            string css = "";
            if (currentSite.CssLocation == Site.CssLocationEnum.Top)
                css = await Manager.CssManager.RenderAsync();

            string head = "";
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraHead))
                head = Manager.CurrentPage.ExtraHead;
            else if (!string.IsNullOrWhiteSpace(currentSite.ExtraHead))
                head = currentSite.ExtraHead;

            // linkAlt+css+js+</head> replaces </head>
            string js = "";
            if (currentSite.JSLocation == Site.JSLocationEnum.Top)
                js = await Manager.ScriptManager.RenderAsync();
            pageHtml = ReplaceOnce(pageHtml, "</head>", linkAlt + css + js + head + "</head>");

            string bodyStart = "";
            if (!currentSite.DisableMinimizeFUOC && (currentSite.JSLocation == Site.JSLocationEnum.Bottom || currentSite.CssLocation == Site.CssLocationEnum.Bottom))
                bodyStart += "<script>document.body.style.display='none';</script>";
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraBodyTop))
                bodyStart += Manager.CurrentPage.ExtraBodyTop;
            else if (!string.IsNullOrWhiteSpace(currentSite.ExtraBodyTop))
                bodyStart += currentSite.ExtraBodyTop;
            if (!string.IsNullOrWhiteSpace(bodyStart))
                pageHtml = ReplaceBodyTag(pageHtml, bodyStart);

            // css + js + endofpage-js + </body> replaces </body>
            // <script ..>
            js = "";
            if (currentSite.JSLocation == Site.JSLocationEnum.Bottom)
                js = await Manager.ScriptManager.RenderAsync();
            css = "";
            if (currentSite.CssLocation == Site.CssLocationEnum.Bottom)
                css = await Manager.CssManager.RenderAsync();

            string endstuff = css;
            if (!currentSite.DisableMinimizeFUOC && (currentSite.JSLocation == Site.JSLocationEnum.Bottom || currentSite.CssLocation == Site.CssLocationEnum.Bottom))
                endstuff += "<script>document.body.style.display='block';</script>";
            endstuff += js;
            endstuff += Manager.ScriptManager.RenderEndofPageScripts();
            if (YetaWFManager.Deployed) {
                if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.Analytics))
                    endstuff += Manager.CurrentPage.Analytics;
                else if (!string.IsNullOrWhiteSpace(currentSite.Analytics))
                    endstuff += currentSite.Analytics;
            }
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraBodyBottom))
                endstuff += Manager.CurrentPage.ExtraBodyBottom;
            else if (!string.IsNullOrWhiteSpace(currentSite.ExtraBodyBottom))
                endstuff += currentSite.ExtraBodyBottom;

            pageHtml = ReplaceOnce(pageHtml, "</body>", endstuff + "</body>");

            //DEBUG:  pageHtml has entire page

            return pageHtml;
        }

        private string ReplaceBodyTag(string pageHtml, string bodyExtra) {
            int index = pageHtml.IndexOf("<body", StringComparison.Ordinal);
            if (index < 0)
                throw new InternalError("Page without <body> tag");
            int endIndex = pageHtml.IndexOf('>', index + 5);
            if (endIndex < 0)
                throw new InternalError("Page has a <body tag without ending >");
            return pageHtml.Substring(0, endIndex + 1) + bodyExtra + pageHtml.Substring(endIndex + 1);
        }
        private string ReplaceOnce(string pageHtml, string searchString, string replaceString) {
            int index = pageHtml.IndexOf(searchString, StringComparison.Ordinal);
            if (index >= 0) {
                if (index > 0)
                    pageHtml = pageHtml.Substring(0, index) + replaceString + pageHtml.Substring(index + searchString.Length);
                else
                    pageHtml = replaceString + pageHtml.Substring(index + searchString.Length);
            }
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
