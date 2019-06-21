/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Threading.Tasks;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Site;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections.Generic;
using YetaWF.Core.Support;
#else
using System.Collections.Specialized;
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public static class StartupRequest {

        public static async Task StartRequestAsync(HttpContext httpContext, bool? isStaticHost) {

            // all code here is synchronous until a Manager is available. All async requests are satisfied from cache so there is no impact.

            HttpRequest httpReq = httpContext.Request;
#if MVC6
            Uri uri = new Uri(UriHelper.GetDisplayUrl(httpReq));
#else
            Uri uri = httpReq.Url;

            // Url rewrite can cause "Cannot use a leading .. to exit above the top directory."
            //http://stackoverflow.com/questions/3826299/asp-net-mvc-urlhelper-generateurl-exception-cannot-use-a-leading-to-exit-ab
            httpReq.ServerVariables.Remove("IIS_WasUrlRewritten");
#endif
            // Determine which Site folder to use based on URL provided
            bool forcedHost = false, newSwitch = false;
            bool staticHost = false;
            bool testHost = false;
            bool loopBack = uri.IsLoopback;
#if MVC6
            string host = YetaWFManager.GetRequestedDomain(uri, loopBack, loopBack ? (string)httpReq.Query[Globals.Link_ForceSite] : null, out forcedHost, out newSwitch);
#else
            string host = YetaWFManager.GetRequestedDomain(uri, loopBack, loopBack ? httpReq.QueryString[Globals.Link_ForceSite] : null, out forcedHost, out newSwitch);
#endif
            string host2 = null;

            SiteDefinition site = null;
            if (isStaticHost != false)
                site = await SiteDefinition.LoadStaticSiteDefinitionAsync(host);
            if (site != null) {
                if (forcedHost || newSwitch) throw new InternalError("Static item for forced or new host");
                staticHost = true;
            } else {
                // check if such a site definition exists (accounting for www. or other subdomain)
                string[] domParts = host.Split(new char[] { '.' });
                if (domParts.Length > 2) {
                    if (domParts.Length > 3 || domParts[0] != "www")
                        host2 = host;
                    host = string.Join(".", domParts, domParts.Length - 2, 2);// get just domain as a fallback
                }
#if DEBUG
                // check if this is a test/forwarded site
                if (site == null && !string.IsNullOrWhiteSpace(host2)) {
                    site = await SiteDefinition.LoadTestSiteDefinitionAsync(host2);
                    if (site != null)
                        host = host2;
                    testHost = site != null;
                }
                if (site == null && !string.IsNullOrWhiteSpace(host)) {
                    site = await SiteDefinition.LoadTestSiteDefinitionAsync(host);
                    testHost = site != null;
                }
#endif
                if (site == null && !string.IsNullOrWhiteSpace(host2)) {
                    site = await SiteDefinition.LoadSiteDefinitionAsync(host2);
                    if (site != null)
                        host = host2;
                }
                if (site == null) {
                    site = await SiteDefinition.LoadSiteDefinitionAsync(host);
                    if (site == null) {
                        if (forcedHost) { // non-existent site requested
#if MVC6
                            Logging.AddErrorLog("404 Not Found");
                            httpContext.Response.StatusCode = 404;
#else
                            httpContext.Response.Status = Logging.AddErrorLog("404 Not Found");
                            httpContext.ApplicationInstance.CompleteRequest();
#endif
                            return;
                        }
                        site = await SiteDefinition.LoadSiteDefinitionAsync(null);
                        if (site == null) {
                            if (SiteDefinition.INITIAL_INSTALL) {
                                // use a skeleton site for initial install
                                // this will be updated when the model is installed
                                site = new SiteDefinition {
                                    Identity = SiteDefinition.SiteIdentitySeed,
                                    SiteDomain = host,
                                };
                            } else {
                                throw new InternalError("Couldn't obtain a SiteDefinition object");
                            }
                        }
                    }
                }
            }
            // We have a valid request for a known domain or the default domain
            // create a YetaWFManager object to keep track of everything (it serves
            // as a global anchor for everything we need to know while processing this request)
#if MVC6
            YetaWFManager manager = YetaWFManager.MakeInstance(httpContext, host);
#else
            YetaWFManager manager = YetaWFManager.MakeInstance(host);
#endif
            // Site properties are ONLY valid AFTER this call to YetaWFManager.MakeInstance

            manager.CurrentSite = site;
            manager.IsStaticSite = staticHost;
            manager.IsTestSite = testHost;
            manager.IsLocalHost = loopBack;

            string hostUsed, portUsed, schemeUsed;
#if MVC6
            hostUsed = YetaWFManager.HttpContextAccessor.HttpContext.Request.Headers["X-Forwarded-Host"];
            portUsed = YetaWFManager.HttpContextAccessor.HttpContext.Request.Headers["X-Forwarded-Port"];
            schemeUsed = YetaWFManager.HttpContextAccessor.HttpContext.Request.Headers["X-Forwarded-Proto"];
#else
            hostUsed = httpContext.Request.Headers["X-Forwarded-Host"];
            portUsed = httpContext.Request.Headers["X-Forwarded-Port"];
            schemeUsed = httpContext.Request.Headers["X-Forwarded-Proto"];
#endif
            manager.HostUsed = hostUsed ?? uri.Host;
            manager.HostPortUsed = uri.Port;
            manager.HostSchemeUsed = uri.Scheme;

            if (forcedHost && newSwitch) {
                if (!manager.HasSuperUserRole) { // if superuser, don't log off (we could be creating a new site)
                    // A somewhat naive way to log a user off, but it's working well and also handles 3rd party logins correctly.
                    // Since this is only used during site development, it's not critical
                    string logoffUrl = WebConfigHelper.GetValue<string>("MvcApplication", "LogoffUrl", null);
                    if (string.IsNullOrWhiteSpace(logoffUrl))
                        throw new InternalError("MvcApplication LogoffUrl not defined in web.cofig/appsettings.json - this is required to switch between sites so we can log off the site-specific currently logged in user");
                    Uri newUri = new Uri("http://" + host);// new site to display
                    logoffUrl += Utility.UrlEncodeArgs(newUri.ToString());
#if MVC6
                    Logging.AddLog("302 Found - {0}", logoffUrl).Truncate(100);
                    httpContext.Response.StatusCode = 302;
                    httpContext.Response.Headers.Add("Location", manager.CurrentSite.MakeUrl(logoffUrl));
#else
                    httpContext.Response.Status = Logging.AddLog("302 Found - {0}", logoffUrl).Truncate(100);
                    httpContext.Response.AddHeader("Location", manager.CurrentSite.MakeUrl(logoffUrl));
                    httpContext.ApplicationInstance.CompleteRequest();
#endif
                    return;
                }
            }
            // Make sure we're using the "official" URL, otherwise redirect 301
            if (!staticHost && !forcedHost && !testHost && !loopBack && site.EnforceSiteUrl) {
                if (uri.IsAbsoluteUri) {
                    if (string.Compare(manager.HostUsed, site.SiteDomain, true) != 0) {
                        UriBuilder newUrl = new UriBuilder(uri) {
                            Host = site.SiteDomain
                        };
                        if (site.EnforceSitePort) {
                            if (newUrl.Scheme == "https") {
                                newUrl.Port = site.PortNumberSSLEval;
                            } else {
                                newUrl.Port = site.PortNumberEval;
                            }
                        }
#if MVC6
                        Logging.AddLog("301 Moved Permanently - {0}", newUrl.ToString()).Truncate(100);
                        httpContext.Response.StatusCode = 301;
                        httpContext.Response.Headers.Add("Location", newUrl.ToString());
#else
                        httpContext.Response.Status = Logging.AddLog("301 Moved Permanently - {0}", newUrl.ToString()).Truncate(100);
                        httpContext.Response.AddHeader("Location", newUrl.ToString());
                        httpContext.ApplicationInstance.CompleteRequest();
#endif
                        return;
                    }
                }
            }
            // IE rejects our querystrings that have encoded "?" (%3D) even though that's completely valid
            // so we have to turn of XSS protection (which is not necessary in YetaWF anyway)
            httpContext.Response.Headers.Add("X-Xss-Protection", "0");

#if MVC6
            if (manager.IsStaticSite)
                RemoveCookiesForStatics(httpContext);
#else
            // MVC5 removes cookies in MvcApplication_EndRequest
#endif
        }

#if MVC6
        private static void RemoveCookiesForStatics(HttpContext context) {
            // Clear all cookies for static requests
            List<string> cookiesToClear = new List<string>();
            foreach (string name in context.Request.Cookies.Keys) cookiesToClear.Add(name);
            foreach (string name in cookiesToClear) {
                context.Response.Cookies.Delete(name);
            }
            // this cookie is added by filehndlr.image
            //context.Response.Cookies.Delete("ASP.NET_SessionId");
        }
        private static Uri RemoveQsKeyFromUri(Uri uri, IQueryCollection queryColl, string qsKey) {
            UriBuilder newUri = new UriBuilder(uri);
            QueryHelper query = QueryHelper.FromQueryCollection(queryColl);
            query.Remove(qsKey);
            newUri.Query = query.ToQueryString();
            return newUri.Uri;
        }
#else
        private static Uri RemoveQsKeyFromUri(Uri uri, string qsKey) {
            UriBuilder newUri = new UriBuilder(uri);
            NameValueCollection qs = System.Web.HttpUtility.ParseQueryString(newUri.Query);
            qs.Remove(qsKey);
            newUri.Query = qs.ToString();
            return newUri.Uri;
        }
#endif
    }
}

