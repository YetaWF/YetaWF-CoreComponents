/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Text.RegularExpressions;
using YetaWF.Core.Image;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    public class PageProcessing {

        public PageProcessing(YetaWFManager manager) { Manager = manager; }

        protected YetaWFManager Manager { get; private set; }

        private static readonly Regex reHead = new Regex("<\\s*head\\s*>");
        private static readonly Regex reEndHead = new Regex("</\\s*head\\s*>");
        private static readonly Regex reEndBody = new Regex("</\\s*body\\s*>");

        public string PostProcessHtml(string pageHtml) {
            // complete page html in pageHtml
            pageHtml = ProcessImages(pageHtml);

            string yetawfMsg;
            if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.Compression) {
                yetawfMsg = "/**** Powered by Yet Another Web Framework - http://YetaWF.com - (c) Copyright <<YEAR>> Softel vdm, Inc. */";
            } else {
                yetawfMsg = "\n" +
                    "/*****************************************/\n" +
                    "/* Powered by Yet Another Web Framework  */\n" +
                    "/* http://YetaWF.com                     */\n" +
                    "/* (c) Copyright <<YEAR>> - Softel vdm, Inc. */\n" +
                    "/*****************************************/" +
                    "\n";
            }
            yetawfMsg = yetawfMsg.Replace("<<YEAR>>", DateTime.Now.Year.ToString());//local time
            // <head>+yetawfMsg replaces <head>
            pageHtml = reHead.Replace(pageHtml, "<head><!-- " + yetawfMsg + " -->", 1);

            // <link rel="alternate">
            string linkAlt = Manager.LinkAltManager.Render().ToString();
            if (string.IsNullOrWhiteSpace(linkAlt))
                linkAlt = "";

            // <link rel="stylesheet">
            string css = Manager.CssManager.Render().ToString();
            if (string.IsNullOrWhiteSpace(css))
                css = "";

            // <script ..>
            string js = Manager.ScriptManager.Render().ToString();
            if (string.IsNullOrWhiteSpace(js))
                js = "";

            // linkAlt+css+js+</head> replaces </head>
            pageHtml = reEndHead.Replace(pageHtml, linkAlt + css + js + "</head>", 1);

            // endofpage-js + </body> replace </body>
            string endstuff = Manager.ScriptManager.RenderEndofPageScripts().ToString();
            pageHtml = reEndBody.Replace(pageHtml, endstuff + "</body>", 1);

            Variables vars = new Variables(Manager) { DoubleEscape = true, CurlyBraces = !Manager.EditMode };
            pageHtml = vars.ReplaceVariables(pageHtml);// variable substitution

            //DEBUG:  pageHtml has entire page

            return pageHtml;
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
