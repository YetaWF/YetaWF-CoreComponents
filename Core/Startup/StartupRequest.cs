using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Site;
using YetaWF.Core.Support;

#if MVC6

namespace YetaWF.Core.Support {

    public static class StartupRequest {

        public static async Task StartRequestAsync(HttpContext httpContext, bool isStaticHost) {

            HttpRequest httpReq = httpContext.Request;

            // Determine which Site folder to use based on URL provided
            bool forcedHost = false, newSwitch = false;
            bool staticHost = false;

            string host = httpReq.Query[Globals.Link_ForceSite];
            string host2 = null;

            host = httpReq.Query[Globals.Link_ForceSite];
            if (!string.IsNullOrWhiteSpace(host)) {
                newSwitch = true;
                YetaWFManager.SetRequestedDomain(host);
            }
            if (string.IsNullOrWhiteSpace(host) && httpContext.Session != null)
                host = httpContext.Session.GetString(Globals.Link_ForceSite);

            if (string.IsNullOrWhiteSpace(host))
                host = httpReq.Host.Host;
            else
                forcedHost = true;

            // beautify the host name a bit
            if (host.Length > 1)
                host = char.ToUpper(host[0]) + host.Substring(1).ToLower();
            else
                host = host.ToUpper();

            SiteDefinition site = null;
            if (isStaticHost)
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

                if (!string.IsNullOrWhiteSpace(host2)) {
                    site = await SiteDefinition.LoadSiteDefinitionAsync(host2);
                    if (site != null)
                        host = host2;
                }
                if (site == null) {
                    site = await SiteDefinition.LoadSiteDefinitionAsync(host);
                    if (site == null) {
                        if (forcedHost) { // non-existent site requested
                            Logging.AddErrorLog("404 Not Found");
                            httpContext.Response.StatusCode = 404;
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

            manager.HostUsed = httpReq.Host.Host;
            manager.HostPortUsed = httpReq.Host.Port ?? 80;
            manager.HostSchemeUsed = httpReq.Scheme;

            Uri uri = new Uri(UriHelper.GetDisplayUrl(httpReq));
            if (forcedHost && newSwitch) {
                if (!manager.HasSuperUserRole) { // if superuser, don't log off (we could be creating a new site)
                    // A somewhat naive way to log a user off, but it's working well and also handles 3rd party logins correctly.
                    // Since this is only used during site development, it's not critical
                    string logoffUrl = WebConfigHelper.GetValue<string>("MvcApplication", "LogoffUrl", null, Package: false);
                    if (string.IsNullOrWhiteSpace(logoffUrl))
                        throw new InternalError("MvcApplication LogoffUrl not defined in web.cofig/appsettings.json - this is required to switch between sites so we can log off the site-specific currently logged in user");
                    Uri newUri;
                    if (uri.IsLoopback) {
                        // add where we need to go next (w/o the forced domain, we're already on this domain (localhost))
                        newUri = RemoveQsKeyFromUri(uri, httpReq.Query, Globals.Link_ForceSite);
                    } else {
                        newUri = new Uri("http://" + host);// new site to display
                    }
                    logoffUrl += YetaWFManager.UrlEncodeArgs(newUri.ToString());
                    logoffUrl += (logoffUrl.Contains("?") ? "&" : "?") + "ResetForcedDomain=false";
                    Logging.AddLog("302 Found - {0}", logoffUrl).Truncate(100);
                    httpContext.Response.StatusCode = 302;
                    httpContext.Response.Headers.Add("Location", manager.CurrentSite.MakeUrl(logoffUrl));
                    return;
                }
            }
            // Make sure we're using the "official" URL, otherwise redirect 301
            if (!staticHost && site.EnforceSiteUrl) {
                if (uri.IsAbsoluteUri) {
                    if (!manager.IsLocalHost && !forcedHost && string.Compare(manager.HostUsed, site.SiteDomain, true) != 0) {
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
                        httpContext.Response.StatusCode = 301;
                        httpContext.Response.Headers.Add("Location", newUrl.ToString());
                        return;
                    }
                }
            }
            // IE rejects our querystrings that have encoded "?" (%3D) even though that's completely valid
            // so we have to turn of XSS protection (which is not necessary in YetaWF anyway)
            httpContext.Response.Headers.Add("X-Xss-Protection", "0");

            if (manager.IsStaticSite)
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

        private static Uri RemoveQsKeyFromUri(Uri uri, IQueryCollection queryColl, string qsKey) {
            UriBuilder newUri = new UriBuilder(uri);
            QueryHelper query = QueryHelper.FromQueryCollection(queryColl);
            query.Remove(qsKey);
            newUri.Query = query.ToQueryString();
            return newUri.Uri;
        }
    }
}

#else
#endif