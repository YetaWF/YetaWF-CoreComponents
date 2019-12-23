/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
#else

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using YetaWF.Core;
using YetaWF.Core.Log;
using YetaWF.Core.Support;

namespace YetaWF.Core.WebStartup {

    public class MvcApplication5 : System.Web.HttpApplication {

        private const string AppSettingsFile = "Appsettings.json";

        public MvcApplication5() {

            AcquireRequestState += new EventHandler(MvcApplication_AcquireRequestState);
            EndRequest += MvcApplication_EndRequest;
            Error += MvcApplication_Error;

            if (string.IsNullOrWhiteSpace(YetaWFManager.RootFolder)) {

                YetaWFManager.RootFolder = Utility.UrlToPhysical("/").TrimEnd(new char[] { '\\' });
                WebConfigHelper.InitAsync(Path.Combine(YetaWFManager.RootFolder, Globals.DataFolder, AppSettingsFile)).Wait();

                // retrieve the default domain to verify it has been defined in Appsettings.json
                string siteDomain = YetaWFManager.DefaultSiteName;
                // We're renaming the __RequestVerificationToken cookie (not the <input hidden> name) for anti-forgery.
                // We ran into this problem where we host https://yetawf.com in a YetaWF instance (one IIS site) and host
                // http://forum.yetawf.com (another IIS site elsewhere). Because they are the same domain name, just different
                // subdomains, browsers will return TWO __RequestVerificationToken cookies (when the user has visited both
                // sites) causing anti-forgery confusion. This can also be fixed with machinekey settings (forcing both sites
                // to use the same keys). But it is easier to just rename our COOKIE. While both sites will still get
                // both cookies, they now use different names, eliminating the confusion.
                // Please note the distinction between the __RequestVerificationToken COOKIE and the
                // __RequestVerificationToken hidden input field. Only the cookie is renamed. There is no need to
                // rename the input field.
                System.Web.Helpers.AntiForgeryConfig.CookieName = "__ReqVerToken_" + siteDomain;

                System.Web.Helpers.AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
            }
        }

        private void MvcApplication_EndRequest(object sender, EventArgs e) {
            // Clear all cookies for static requests
            if (YetaWFManager.HaveManager) {
                if (YetaWFManager.Manager.IsStaticSite) {
                    HttpContext context = HttpContext.Current;
                    List<string> cookiesToClear = new List<string>();
                    foreach (string name in context.Request.Cookies) cookiesToClear.Add(name);
                    foreach (string name in cookiesToClear) {
                        HttpCookie cookie = new HttpCookie(name, string.Empty);
                        cookie.Domain = YetaWFManager.Manager.CurrentSite.StaticDomain;
                        cookie.Expires = DateTime.Today.AddYears(-1);
                        context.Response.Cookies.Set(cookie);
                    }
                    // this cookie is added by filehndlr.image
                    if (context.Response.Cookies["ASP.NET_SessionId"] != null)
                        context.Response.Cookies.Remove("ASP.NET_SessionId");
                }
            }
        }

        void MvcApplication_Error(object sender, EventArgs e) {
            // flush log on error, but avoid log spamming
            if (LastError == null || DateTime.Now > ((DateTime)LastError).AddSeconds(10)) {
                Logging.ForceFlush();// make sure this is recorded immediately so we can see it in the log
                LastError = DateTime.Now;
            }
        }
        private static DateTime? LastError = null;// use local time

        void Session_Start(object sender, EventArgs e) {
            // Code that runs when a new session is started

            // Ensure SessionID in order to prevent the following exception
            // when the Application Pool Recycles
            // [HttpException]: Session state has created a session id, but cannot
            //                    save it because the response was already flushed ...
            string sessionId = Session.SessionID;
        }

        public void MvcApplication_AcquireRequestState(object sender, EventArgs e) {

            HttpRequest httpReq = HttpContext.Current.Request;
            StartupRequest.StartRequestAsync(HttpContext.Current, null).Wait(); // All code synchronous until a Manager is available. All async requests are satisfied from cache so there is no impact.
        }
    }

}

#endif