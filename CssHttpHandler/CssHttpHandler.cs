/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Text;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Threading.Tasks;
#else
using System.Web;
using System.Web.SessionState;
#endif

namespace YetaWF.Core.HttpHandler {

#if MVC6
    public class CssMiddleware {

        private readonly RequestDelegate _next;
        private CssHttpHandler Handler;

        public CssMiddleware(RequestDelegate next) {
            _next = next;
            Handler = new CssHttpHandler();
        }

        public async Task InvokeAsync(HttpContext context) {
            await Handler.ProcessRequest(context);
            //await _next(context);
        }
    }

    public class CssHttpHandler
#else
    public class CssHttpHandler : IHttpHandler, IReadOnlySessionState
#endif
    {

        // IHttpHandler
        // IHttpHandler
        // IHttpHandler

#if MVC6
        public async Task ProcessRequest(HttpContext context) {
            await StartupRequest.StartRequestAsync(context, true);
#else
        public bool IsReusable {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context) {
#endif
            YetaWFManager manager = YetaWFManager.Manager;

            int charWidth, charHeight;
            GetCharSize(manager, out charWidth, out charHeight);
            bool processCharSize = charWidth > 0 && charHeight > 0;
            string fullUrl = context.Request.Path;
            string file;
#if MVC6
            if (fullUrl.StartsWith(Globals.VaultPrivateUrl)) {
                // Private Vault files are a special case as they're not within the website.
                // We need to make sure to only allow .css, .less, .scss files otherwise this would expose other files.
                if (!fullUrl.EndsWith(".css", true, CultureInfo.InvariantCulture) && !fullUrl.EndsWith(".less", true, CultureInfo.InvariantCulture) && !fullUrl.EndsWith(".scss", true, CultureInfo.InvariantCulture)) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found");
                    return;
                }
            }
#else
#endif
            file = YetaWFManager.UrlToPhysical(fullUrl);

            if (fullUrl.ContainsIgnoreCase(Globals.NodeModulesUrl) || fullUrl.ContainsIgnoreCase(Globals.BowerComponentsUrl) || fullUrl.ContainsIgnoreCase("/" + Globals.GlobalJavaScript + "/")) processCharSize = false;
            DateTime lastMod = File.GetLastWriteTimeUtc(file);

            // Cache verification?
            string ifNoneMatch = context.Request.Headers["If-None-Match"];
            if (ifNoneMatch.TruncateStart("W/") == GetETag()) {
                context.Response.ContentType = "text/css";
                context.Response.StatusCode = 304;
#if MVC6
#else
                context.Response.StatusDescription = "OK";
#endif
                context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
                YetaWFManager.SetStaticCacheInfo(context.Response);
                context.Response.Headers.Add("ETag", GetETag());
#if MVC6
#else
                context.ApplicationInstance.CompleteRequest();
#endif
                return;
            }

            // Send entire file
            byte[] bytes = null;
            string cacheKey = null;
            if (processCharSize && !manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
                try {
                    cacheKey = "CssHttpHandler_" + file + "_" + charWidth.ToString() + "_" + charHeight.ToString();
#if MVC6
                    IMemoryCache cache = (IMemoryCache)context.RequestServices.GetService(typeof(IMemoryCache));
                    bytes = cache.Get<byte[]>(cacheKey);
#else
                    if (System.Web.HttpRuntime.Cache[cacheKey] != null)
                        bytes = (byte[])System.Web.HttpRuntime.Cache[cacheKey];
#endif
                } catch (Exception) { processCharSize = false; } // this can fail for *.css requests without !CI=
            }
            if (bytes == null) {
                string text = "";
                try {
                    text = File.ReadAllText(file);
                } catch (Exception) {
                    context.Response.StatusCode = 404;
                    Logging.AddErrorLog("Not Found");
#if MVC6
#else
                    context.Response.StatusDescription = "Not Found";
                    context.ApplicationInstance.CompleteRequest();
#endif
                    return;
                }
                if (processCharSize) {
                    // process css - replace nn ch with pixel value, derived from avg char width
                    try {
                        text = manager.CssManager.ProcessCss(text, charWidth, charHeight);
                    } catch (Exception) { }// this can fail for *.css requests without !CI= in which case we use the text as-is
                    bytes = Encoding.ASCII.GetBytes(text);
                    if (!manager.CurrentSite.DEBUGMODE && manager.CurrentSite.AllowCacheUse) {
#if MVC6
                        IMemoryCache cache = (IMemoryCache)context.RequestServices.GetService(typeof(IMemoryCache));
                        cache.Set<byte[]>(cacheKey, bytes);
#else
                        System.Web.HttpRuntime.Cache[cacheKey] = bytes;
#endif
                    }
                } else {
                    bytes = Encoding.ASCII.GetBytes(text);
                }
            }
            context.Response.ContentType = "text/css";
            context.Response.StatusCode = 200;
#if MVC6
#else
            context.Response.StatusDescription = "OK";
#endif
            context.Response.Headers.Add("Last-Modified", String.Format("{0:r}", lastMod));
            YetaWFManager.SetStaticCacheInfo(context.Response);
            context.Response.Headers.Add("ETag", GetETag());
#if MVC6
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
#else
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.ApplicationInstance.CompleteRequest();
#endif
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
