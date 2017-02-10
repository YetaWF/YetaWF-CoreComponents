/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.SessionState;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.HttpHandler {
    public class CssHttpHandler : IHttpHandler, IReadOnlySessionState {

        // IHttpHandler
        // IHttpHandler
        // IHttpHandler

        public bool IsReusable {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context) {

            YetaWFManager manager = YetaWFManager.Manager;

            int charWidth, charHeight;
            GetCharSize(manager, out charWidth, out charHeight);
            bool processCharSize = charWidth > 0 && charHeight > 0;
            string fullUrl = context.Request.Path;
            string file = YetaWFManager.UrlToPhysical(fullUrl);
            // we only process scss files here in debug mode, otherwise they're compiled once in CssManager
            bool processNsass = manager.CurrentSite.DEBUGMODE && file.EndsWith(".scss", StringComparison.OrdinalIgnoreCase);
            // we only process scss files here in debug mode, otherwise they're compiled once in CssManager
            bool processLess = manager.CurrentSite.DEBUGMODE && file.EndsWith(".less", StringComparison.OrdinalIgnoreCase);

            if (fullUrl.ContainsIgnoreCase("/" + Globals.GlobalJavaScript + "/") || file.ContainsIgnoreCase(Globals.NugetScriptsUrl)) processCharSize = false;
            DateTime lastMod = File.GetLastWriteTimeUtc(file);

            // Cache verification?
            if (context.Request.Headers["If-None-Match"] == GetETag()) {
                context.Response.ContentType = "text/css";
                context.Response.StatusCode = 304;
                context.Response.StatusDescription = "OK";
                context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Headers.Add("ETag", GetETag());
                context.ApplicationInstance.CompleteRequest();
                return;
            }

            // Send entire file
            byte[] bytes = null;
            string cacheKey = null;
            if (processCharSize && !manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                try {
                    cacheKey = "CssHttpHandler_" + file + "_" + charWidth.ToString() + "_" + charHeight.ToString();
                    if (context.Cache[cacheKey] != null)
                        bytes = (byte[])context.Cache[cacheKey];
                } catch (Exception) { processCharSize = false; } // this can fail for *.css requests without !CI=
            }
            if (bytes == null) {
                string text = "";
                try {
                    text = File.ReadAllText(file);
                } catch (Exception) {
                    context.Response.StatusCode = 404;
                    context.Response.StatusDescription = Logging.AddErrorLog("Not Found");
                    context.ApplicationInstance.CompleteRequest();
                    return;
                }
                if (processCharSize) {
                    // process css - replace nn ch with pixel value, derived from avg char width
                    try {
                        Packer packer = new Packer();
                        text = packer.ProcessCss(text, charWidth, charHeight);
                    } catch (Exception) { }// this can fail for *.css requests without !CI= in which case we use the text as-is
                    if (processNsass)
                        text = CssManager.CompileNSass(file, text);
                    if (processLess)
                        text = CssManager.CompileLess(file, text);
                    bytes = Encoding.ASCII.GetBytes(text);
                    if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse)
                        manager.CurrentContext.Cache[cacheKey] = bytes;
                } else {
                    if (processNsass)
                        text = CssManager.CompileNSass(file, text);
                    if (processLess)
                        text = CssManager.CompileLess(file, text);
                    bytes = Encoding.ASCII.GetBytes(text);
                }
            }
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.ContentType = "text/css";
            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";
            context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Headers.Add("ETag", GetETag());
            context.ApplicationInstance.CompleteRequest();
        }
        private void GetCharSize(YetaWFManager manager, out int width, out int height) {
            width = 0;
            height = 0;
            string wh = manager.RequestForm[Globals.Link_CharInfo];
            if (wh == null)
                wh = manager.RequestQueryString[Globals.Link_CharInfo];
            if (!string.IsNullOrWhiteSpace(wh)) {
                string[] parts = wh.Split(new char[] { ',' });
                width = Convert.ToInt32(parts[0]);
                height = Convert.ToInt32(parts[1]);
            }
        }
        private static string GetETag() {
            return string.Format(@"""{0}""", YetaWFManager.CacheBuster);
        }
    }
}
