/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Site;

namespace YetaWF.Core.Support {

    public static partial class StartupRequest {

        public static async Task StartRequestAsync(HttpContext httpContext, bool? isStaticHost, bool initOnly = false) {

            // all code here is synchronous until a Manager is available. All async requests are satisfied from cache so there is no impact.

            HttpRequest httpReq = httpContext.Request;
            Uri uri = new Uri(UriHelper.GetDisplayUrl(httpReq));

            // Determine which Site folder to use based on URL provided
            bool forcedHost = false, newSwitch = false;
            bool staticHost = false;
            bool testHost = false;
            bool loopBack = uri.IsLoopback;
            string host = YetaWFManager.GetRequestedDomain(httpContext, uri, loopBack, loopBack ? (string?)httpReq.Query[Globals.Link_ForceSite] : string.Empty, out forcedHost, out newSwitch);
            string? host2 = null;

            SiteDefinition? site = null;
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
                            if (initOnly)
                                throw new InternalError("Couldn't obtain a SiteDefinition object - forced host not found");
                            Logging.AddErrorLog("404 Not Found");
                            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
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

            YetaWFManager manager = YetaWFManager.MakeInstance(httpContext, host);

            // Site properties are ONLY valid AFTER this call to YetaWFManager.MakeInstance

            manager.CurrentSite = site;
            manager.IsStaticSite = staticHost;
            manager.IsTestSite = testHost;
            manager.IsLocalHost = loopBack;

            manager.HostUsed = uri.Host;
            manager.HostPortUsed = uri.Port;
            manager.HostSchemeUsed = uri.Scheme;

            UriBuilder uriBuilder = new UriBuilder(uri);
            uriBuilder.Scheme = manager.HostSchemeUsed;
            uriBuilder.Port = manager.HostPortUsed;
            uriBuilder.Host = manager.HostUsed;
            manager.CurrentRequestUrl = uriBuilder.ToString();

            if (forcedHost && newSwitch) {
                if (initOnly)
                    throw new InternalError($"{nameof(forcedHost)} or {nameof(newSwitch)} not supported in {nameof(initOnly)} mode");
                if (!manager.HasSuperUserRole) { // if superuser, don't log off (we could be creating a new site)
                    // A somewhat naive way to log a user off, but it's working well and also handles 3rd party logins correctly.
                    // Since this is only used during site development, it's not critical
                    string? logoffUrl = WebConfigHelper.GetValue<string?>("Application", "LogoffUrl");
                    if (string.IsNullOrWhiteSpace(logoffUrl))
                        throw new InternalError("Application LogoffUrl not defined in web.cofig/appsettings.json - this is required to switch between sites so we can log off the site-specific currently logged in user");
                    Uri newUri = new Uri("http://" + host);// new site to display
                    logoffUrl += Utility.UrlEncodeArgs(newUri.ToString());
                    Logging.AddLog("302 Found - {0}", logoffUrl).Truncate(100);
                    httpContext.Response.StatusCode = StatusCodes.Status302Found;
                    httpContext.Response.Headers.Add("Location", manager.CurrentSite.MakeUrl(logoffUrl));
                    return;
                }
            }
            // Make sure we're using the "official" URL, otherwise redirect 301
            if (!staticHost && !forcedHost && !testHost && !loopBack && site.EnforceSiteUrl) {
                if (initOnly)
                    throw new InternalError($"{nameof(site.EnforceSiteUrl)} not supported in {nameof(initOnly)} mode");
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
                        Logging.AddLog("301 Moved Permanently - {0}", newUrl.ToString()).Truncate(100);
                        httpContext.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                        httpContext.Response.Headers.Add("Location", newUrl.ToString());
                        return;
                    }
                }
            }
            // IE rejects our querystrings that have encoded "?" (%3D) even though that's completely valid
            // so we have to turn of XSS protection (which is not necessary in YetaWF anyway)
            if (!initOnly)
                httpContext.Response.Headers.Add("X-Xss-Protection", "0");

            if (!initOnly && manager.IsStaticSite)
                RemoveCookiesForStatics(httpContext);
        }

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
    }
}

