/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Text.RegularExpressions;
using YetaWF.Core.Image;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    public class PageProcessing {

        public PageProcessing(YetaWFManager manager) { Manager = manager; }

        protected YetaWFManager Manager { get; private set; }

        private static readonly Regex reHead = new Regex("<\\s*head\\s*>", RegexOptions.Compiled);
        private static readonly Regex reEndHead = new Regex("</\\s*head\\s*>", RegexOptions.Compiled);
        private static readonly Regex reStartBody = new Regex("<\\s*body[^>]*>", RegexOptions.Compiled);
        private static readonly Regex reEndBody = new Regex("</\\s*body\\s*>", RegexOptions.Compiled);

        public string PostProcessHtml(string pageHtml) {

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            pageHtml = vars.ReplaceVariables(pageHtml);// variable substitution

            // complete page html in pageHtml
            pageHtml = ProcessImages(pageHtml);

            string yetawfMsg;
            if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.Compression) {
                yetawfMsg = "/**** Powered by Yet Another Web Framework - https://YetaWF.com - (c) Copyright <<YEAR>> Softel vdm, Inc. */";
            } else {
                yetawfMsg = "\n" +
                    "/*****************************************/\n" +
                    "/* Powered by Yet Another Web Framework  */\n" +
                    "/* https://YetaWF.com                     */\n" +
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
            string css = Manager.CssManager.Render().ToString();
            if (string.IsNullOrWhiteSpace(css))
                css = "";

            string head = "";
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraHead))
                head = Manager.CurrentPage.ExtraHead;
            else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.ExtraHead))
                head = Manager.CurrentSite.ExtraHead;

            // linkAlt+css+js+</head> replaces </head>
            string js = "";
            if (Manager.CurrentSite.JSLocation == Site.JSLocationEnum.Top)
                js = Manager.ScriptManager.Render().ToString();
            pageHtml = reEndHead.Replace(pageHtml, (m) => linkAlt + css + js + head + "</head>", 1);

            string bodyStart = "";
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraBodyTop))
                bodyStart = Manager.CurrentPage.ExtraBodyTop;
            else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.ExtraBodyTop))
                bodyStart = Manager.CurrentSite.ExtraBodyTop;
            if (!string.IsNullOrWhiteSpace(bodyStart))
                pageHtml = reStartBody.Replace(pageHtml, (m) => m.Value + bodyStart, 1);

            // js + endofpage-js + </body> replaces </body>
            // <script ..>
            js = "";
            if (Manager.CurrentSite.JSLocation == Site.JSLocationEnum.Bottom)
                js = Manager.ScriptManager.Render().ToString();

            string endstuff = js;
            endstuff += Manager.ScriptManager.RenderEndofPageScripts();
            if (Manager.Deployed) {
                if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.Analytics))
                    endstuff += Manager.CurrentPage.Analytics;
                else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.Analytics))
                    endstuff += Manager.CurrentSite.Analytics;
            }
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.ExtraBodyBottom))
                endstuff += Manager.CurrentPage.ExtraBodyBottom;
            else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.ExtraBodyBottom))
                endstuff += Manager.CurrentSite.ExtraBodyBottom;

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
            if (Manager.CurrentSite.UseHttpHandler) {
                if (Manager.CurrentSite.CanUseCDN && Manager.CurrentSite.CDNFileImage)
                    return ImageSupport.ProcessImagesAsCDN(pageHtml);
                return pageHtml;
            }
            return ImageSupport.ProcessImagesAsStatic(pageHtml);
        }
    }
}
