﻿/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Text.RegularExpressions;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
using YetaWF.Core.Identity;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Controller for all full page requests within YetaWF.
    /// </summary>
    /// <remarks>This controller is a plain MVC controller because we don't want any startup processing to take place (like authorization, etc.)
    /// because we handle all this here. This controller is used for page and single module display (GET/HEAD requests only).</remarks>
    public class PageController : Controller {

        /// <summary>
        /// The YetaWFManager instance for the current HTTP request.
        /// </summary>
        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// The Show action handles all page requests within YetaWF.
        /// </summary>
        /// <param name="__path">The local Url requested.</param>
        /// <returns></returns>
        [AllowHttp("GET", "HEAD")]  // HEAD is only supported here (so dumb linkcheckers can see the pages)
        public async Task<ActionResult> Show(string __path) {
            // We come here for ANY page request (GET, HEAD only)

            if (!YetaWFManager.HaveManager) {
#if MVC6
                return new NotFoundObjectResult(__path);
#else
                throw new HttpException(404, string.Format("Url {0} not found", __path));
#endif
            }
            SiteDefinition site = Manager.CurrentSite;
            Uri uri = new Uri(Manager.CurrentRequestUrl);

            // process logging type callbacks
            await PageLogging.HandleCallbacksAsync(Manager.CurrentRequestUrl, false);

            // Mobile detection
            bool isMobile;
#if MVC6
            // The reason we need to do this is because we may or may not want to go into a popup
            // The only time we go into a popup without knowing whether we're on a mobile device is
            // through a client-side action request (popup.js). The CLIENT should determine whether we're in a mobile
            // device. Popups are limited by screen size so a simple screen size check would suffice.
            // However, if we want to redirect to a mobile site (different Url) for a first-time request, we need to
            // analyze headers (user-agent).
            isMobile = IsMobileBrowser(Manager.CurrentRequest);
            //Manager.ActiveDevice = IsMobileBrowser(Manager.CurrentRequest) ? YetaWFManager.DeviceSelected.Mobile : YetaWFManager.DeviceSelected.Desktop;
#else
            // Check if mobile browser and redirect to mobile browser url/subdomain if needed
            HttpBrowserCapabilities caps = Manager.CurrentRequest.Browser;
            isMobile = caps.IsMobileDevice;
#endif
            if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Undecided) {
                if (isMobile) {
                    Manager.ActiveDevice = YetaWFManager.DeviceSelected.Mobile;
                    if (!string.IsNullOrEmpty(site.MobileSiteUrl)) {
                        if (uri.IsAbsoluteUri) {
                            if (!Manager.IsLocalHost && string.Compare(uri.AbsoluteUri, site.MobileSiteUrl, true) != 0) {
                                UriBuilder newUrl = new UriBuilder(site.MobileSiteUrl);
                                string logMsg = Logging.AddLog("301 Moved Permanently - {0}", newUrl.ToString()).Truncate(100);
#if MVC6
                                Manager.CurrentResponse.StatusCode = 301;
                                Manager.CurrentResponse.Headers.Add("Location", site.LockedUrl);
#else
                                Manager.CurrentResponse.Status = logMsg;
                                Manager.CurrentResponse.AddHeader("Location", newUrl.ToString());
                                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                                return new EmptyResult();
                            }
                        }
                    }
                }
                Manager.ActiveDevice = YetaWFManager.DeviceSelected.Desktop;
            } else {
                Manager.ActiveDevice = isMobile ? YetaWFManager.DeviceSelected.Mobile : YetaWFManager.DeviceSelected.Desktop;
            }

#if MVC6
            // we'll just ignore this - if desired, use some client-side method in the skin to address browser incompatibilities
#else
            // Check for browser capabilities
            if (caps != null && !caps.Crawler && !string.IsNullOrWhiteSpace(site.UnsupportedBrowserUrl) && !BrowserCaps.SupportedBrowser(caps) && string.Compare(uri.AbsolutePath, site.UnsupportedBrowserUrl, true) != 0) {
                Logging.AddLog("Unsupported browser {0}, version {1}.", caps.Browser, caps.Version);
                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", site.UnsupportedBrowserUrl).Truncate(100);
                Manager.CurrentResponse.AddHeader("Location", site.UnsupportedBrowserUrl);
                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                return new EmptyResult();
            }
#endif

            if (site.IsLockedAny && !string.IsNullOrWhiteSpace(site.GetLockedForIP()) && !string.IsNullOrWhiteSpace(site.LockedUrl) &&
                    Manager.UserHostAddress != site.GetLockedForIP() && Manager.UserHostAddress != "127.0.0.1" &&
                    string.Compare(uri.AbsolutePath, site.LockedUrl, true) != 0) {
                Logging.AddLog("302 Found - {0}", site.LockedUrl);
#if MVC6
                Manager.CurrentResponse.StatusCode = 302;
                Manager.CurrentResponse.Headers.Add("Location", site.LockedUrl);
#else
                Manager.CurrentResponse.Status = string.Format("302 Found - {0}", site.LockedUrl).Truncate(100);
                Manager.CurrentResponse.AddHeader("Location", site.LockedUrl);
                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                return new EmptyResult();
            }

            if (SiteDefinition.INITIAL_INSTALL) {
                string initPageUrl = "/YetaWF_Packages/StartupPage/Show";
                string initPageQs = "";
                string newUrl;
                if (!Manager.IsLocalHost)
                    // if we're using IIS we need to use the real domain which we haven't saved yet in Site Properties
                    newUrl = Manager.CurrentSite.MakeUrl(initPageUrl + initPageQs, RealDomain: Manager.HostUsed);
                else
                    newUrl = Manager.CurrentSite.MakeUrl(initPageUrl + initPageQs);
                if (string.Compare(uri.LocalPath, initPageUrl, true) != 0) {
#if MVC6
                    Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                    Manager.CurrentResponse.StatusCode = 302;
                    Manager.CurrentResponse.Headers.Add("Location", newUrl);
#else
                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                    Manager.CurrentResponse.AddHeader("Location", newUrl);
                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                    return new EmptyResult();
                }
            }

            // Check if site language requested using !yLang= arg
#if MVC6
            string lang = Manager.CurrentRequest.Query[Globals.Link_Language];
#else
            string lang = Manager.CurrentRequest[Globals.Link_Language];
#endif
            if (!string.IsNullOrWhiteSpace(lang))
                await Manager.SetUserLanguageAsync(lang);

            // Check if home URL is requested and matches site's desired home URL
            if (uri.AbsolutePath == "/") {
                if (Manager.CurrentSite.HomePageUrl != "/") {
                    string newUrl = Manager.CurrentSite.MakeUrl(Manager.CurrentSite.HomePageUrl);
#if MVC6
                    Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                    Manager.CurrentResponse.StatusCode = 302;
                    Manager.CurrentResponse.Headers.Add("Location", newUrl);
#else
                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                    Manager.CurrentResponse.AddHeader("Location", newUrl);
                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                    return new EmptyResult();
                }
            }

            // http/https
            if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite) {
                switch (Manager.CurrentSite.PageSecurity) {
                    default:
                    case PageSecurityType.AsProvided:
                        // it's all good
                        break;
                    case PageSecurityType.NoSSLOnly:
                        if (uri.Scheme == "https") {
                            UriBuilder newUri = new UriBuilder(uri);
                            newUri.Scheme = "http";
                            newUri.Port = Manager.CurrentSite.PortNumberEval;
#if MVC6
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.StatusCode = 302;
                        Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
#else
                            Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                            Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                            Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif

                            return new EmptyResult();
                        }
                        break;
                    case PageSecurityType.NoSSLOnlyAnonymous_LoggedOnhttps:
                        if (!Manager.HaveUser && uri.Scheme == "https") {
                            UriBuilder newUri = new UriBuilder(uri);
                            newUri.Scheme = "http";
                            newUri.Port = Manager.CurrentSite.PortNumberEval;
#if MVC6
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.StatusCode = 302;
                        Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
#else
                            Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                            Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                            Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                            return new EmptyResult();
                        } else if (Manager.HaveUser && uri.Scheme != "https") {
                            UriBuilder newUri = new UriBuilder(uri);
                            newUri.Scheme = "https";
                            newUri.Port = Manager.CurrentSite.PortNumberSSLEval;
#if MVC6
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.StatusCode = 302;
                        Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
#else
                            Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                            Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                            Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                            return new EmptyResult();
                        }
                        break;
                    case PageSecurityType.AsProvidedLoggedOn_Anonymoushttp:
                        // set later
                        break;
                    case PageSecurityType.AsProvidedAnonymous_LoggedOnhttps:
                        // set later
                        break;
                    case PageSecurityType.SSLOnly:
                        if (uri.Scheme != "https") {
                            UriBuilder newUri = new UriBuilder(uri);
                            newUri.Scheme = "https";
                            newUri.Port = Manager.CurrentSite.PortNumberSSLEval;
#if MVC6
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.StatusCode = 302;
                        Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
#else
                            Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                            Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                            Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                            return new EmptyResult();
                        }
                        break;
                    case PageSecurityType.UsePageModuleSettings:
                        // to be determined when loading page/module
                        break;
                }
            }
            // set up all info, like who is logged on, popup, origin list, etc.
            await YetaWFController.SetupEnvironmentInfoAsync();

            Logging.AddLog("Page");

            // Check if it's a built-in command (mostly for debugging and initial install) and build a page dynamically (which is not saved)
            Func<QueryHelper, Task> action = await BuiltinCommands.FindAsync(uri.LocalPath, checkAuthorization: true);
            if (action != null) {
                if (Manager.IsHeadRequest)
                    return new EmptyResult();
                Manager.CurrentPage = PageDefinition.Create();
                QueryHelper qh = QueryHelper.FromQueryString(uri.Query);
                await action(qh);
                return new EmptyResult();
            }

            // Process the page
            CanProcessAsStaticPageInfo info = await CanProcessAsStaticPageAsync(uri.LocalPath);
            if (info.Success) {
                AddXFrameOptions();
                return Content(info.Contents, "text/html");
            }

            // if this is a url with segments (like http://...local.../segment/segment/segment/segment/segment/segment)
            // rewrite it to make it a proper querystring
            PageDefinition pageFound = null;
            ModuleDefinition moduleFound = null;
            {
                string url = uri.LocalPath;
                string newUrl, newQs;
                if (url.StartsWith(Globals.ModuleUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl, out newQs);
                    if (newUrl != url) {
                        Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
#if MVC6
                        HttpContext.Request.Path = newUrl;
                        HttpContext.Request.QueryString = QueryHelper.MakeQueryString(newQs);
                        uri = new Uri(Manager.CurrentRequestUrl);
#else
                        Server.TransferRequest(QueryHelper.ToUrl(newUrl, newQs));
                        return new EmptyResult();
#endif
                    }
                    ModuleDefinition module = await ModuleDefinition.FindDesignedModuleAsync(url);
                    if (module == null)
                        module = await ModuleDefinition.LoadByUrlAsync(url);
                    moduleFound = module;
                } else if (url.StartsWith(Globals.PageUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl, out newQs);
                    if (newUrl != url) {
                        Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
#if MVC6
                        HttpContext.Request.Path = newUrl;
                        HttpContext.Request.QueryString = QueryHelper.MakeQueryString(newQs);
                        uri = new Uri(Manager.CurrentRequestUrl);
#else
                        Server.TransferRequest(QueryHelper.ToUrl(newUrl, newQs));
                        return new EmptyResult();
#endif
                    }
                    pageFound = await PageDefinition.LoadFromUrlAsync(url);
                } else {
                    PageDefinition.PageUrlInfo pageInfo = await PageDefinition.GetPageUrlFromUrlWithSegmentsAsync(url, uri.Query);
                    PageDefinition page = pageInfo.Page;
                    if (page != null) {
                        // we have a page, check if the URL was rewritten because it had human readable arguments
                        if (pageInfo.NewUrl != url) {
                            Logging.AddTraceLog("Server.TransferRequest - {0}", pageInfo.NewUrl);
#if MVC6
                            HttpContext.Request.Path = pageInfo.NewUrl;
                            HttpContext.Request.QueryString = QueryHelper.MakeQueryString(pageInfo.NewQS);
                            uri = new Uri(Manager.CurrentRequestUrl);
#else
                            Server.TransferRequest(QueryHelper.ToUrl(pageInfo.NewUrl, pageInfo.NewQS));
                            return new EmptyResult();
#endif
                        }
                        pageFound = page;
                    } else {
                        // we have a direct url, make sure it's exactly 3 segments otherwise rewrite the remaining args as non-human readable qs args
                        // don't rewrite css/scss/less file path - we handle that in a http handler
                        PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 4, uri.Query, out newUrl, out newQs);
                        if (newUrl != url) {
                            Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
#if MVC6
                            HttpContext.Request.Path = newUrl;
                            HttpContext.Request.QueryString = QueryHelper.MakeQueryString(newQs);
                            uri = new Uri(Manager.CurrentRequestUrl);
#else
                            Server.TransferRequest(QueryHelper.ToUrl(newUrl, newQs));
                            return new EmptyResult();
#endif
                        }
                    }
                }
            }

            // redirect current request to two-step authentication setup
            if (Manager.Need2FA) {
                if (Manager.Need2FARedirect) {
                    Logging.AddLog("Two-step authentication setup required");
                    ModuleAction action2FA = await Resource.ResourceAccess.GetForceTwoStepActionSetupAsync(null);
                    Manager.Need2FARedirect = false;
                    Manager.OriginList.Add(new Origin() { Url = uri.ToString() });// where to go after setup
                    Manager.OriginList.Add(new Origin() { Url = action2FA.GetCompleteUrl() }); // setup
                    return new RedirectResult(Manager.ReturnToUrl);
                }
                Resource.ResourceAccess.ShowNeed2FA();
            }
            if (Manager.NeedNewPassword) {
                Resource.ResourceAccess.ShowNeedNewPassword();
            }

            if (pageFound != null) {
                switch (await CanProcessAsDesignedPageAsync(pageFound, uri.LocalPath, uri.Query)) {
                    case ProcessingStatus.Complete:
                        return new EmptyResult();
                    case ProcessingStatus.Page:
                        if (Manager.IsHeadRequest)
                            return new EmptyResult();
                        AddXFrameOptions();
                        return new PageViewResult();
                    case ProcessingStatus.No:
                        break;
                }
            }
            if (moduleFound != null) {
                switch (CanProcessAsModule(moduleFound, uri.LocalPath, uri.Query)) {
                    case ProcessingStatus.Complete:
                        return new EmptyResult();
                    case ProcessingStatus.Page:
                        if (Manager.IsHeadRequest)
                            return new EmptyResult();
                        AddXFrameOptions();
                        return new PageViewResult();
                    case ProcessingStatus.No:
                        break;
                }
            }

            // if we got here, we shouldn't be here

            // display 404 error page if one is defined
            if (!string.IsNullOrWhiteSpace(site.NotFoundUrl)) {
                PageDefinition page = await PageDefinition.LoadFromUrlAsync(site.NotFoundUrl);
                if (page != null) {
                    Manager.CurrentPage = page;// Found It!!
                    if (Manager.IsHeadRequest) {
#if MVC6
                        return new NotFoundObjectResult(__path);
#else
                        throw new HttpException(404, "404 Not Found");
#endif
                    }
                    AddXFrameOptions();
#if MVC6
                    Logging.AddErrorLog("404 Not Found");
                    Manager.CurrentResponse.StatusCode = 404;
#else
                    Manager.CurrentResponse.Status = Logging.AddErrorLog("404 Not Found");
#endif
                    return new PageViewResult();
                }
            }
#if MVC6
            return NotFound(__path);
#else
            throw new HttpException(404, string.Format("Url {0} not found", __path));
#endif
        }

        private void AddXFrameOptions() {
            string option = null;
            if (Manager.CurrentPage != null) {
                if (Manager.CurrentPage.IFrameUse == PageDefinition.IFrameUseEnum.Default) {
                    if (Manager.CurrentSite.IFrameUse == IFrameUseEnum.No)
                        option = "deny";
                    else if (Manager.CurrentSite.IFrameUse == IFrameUseEnum.ThisSite)
                        option = "sameorigin";
                } else if (Manager.CurrentPage.IFrameUse == PageDefinition.IFrameUseEnum.No) {
                    option = "deny";
                } else if (Manager.CurrentPage.IFrameUse == PageDefinition.IFrameUseEnum.ThisSite) {
                    option = "sameorigin";
                }
                if (option != null) {
                    if (YetaWFController.GoingToPopup())
                        option = "sameorigin";// we need at least this to go into a popup
#if MVC6
                    Manager.CurrentResponse.Headers.Add("X-Frame-Options", option);
#else
                   Manager.CurrentResponse.AddHeader("X-Frame-Options", option);
#endif
                }
            }
        }

        internal enum ProcessingStatus {
            No = 0, // not processed, try something else
            Page = 1, // Page has been set up
            Complete = 2,// no more processing is needed
        }
        internal class CanProcessAsStaticPageInfo {
            public string Contents { get; set; }
            public bool Success { get; set; }
        }
        private async Task<CanProcessAsStaticPageInfo> CanProcessAsStaticPageAsync(string localUrl) {
            if (Manager.CurrentSite.StaticPages && !Manager.HaveUser && !Manager.EditMode && Manager.CurrentSite.AllowAnonymousUsers) {
                if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Desktop && YetaWFController.GoingToPopup()) {
                    // we're going into a popup for this
                    Manager.IsInPopup = true;
                }
                Support.StaticPages.StaticPageManager.GetPageInfo info = await Manager.StaticPageManager.GetPageAsync(localUrl);
                if (!string.IsNullOrWhiteSpace(info.FileContents)) {
                    Manager.CurrentResponse.Headers.Add("Last-Modified", string.Format("{0:R}", info.LastUpdate));
                    return new CanProcessAsStaticPageInfo {
                        Contents = info.FileContents,
                        Success = true,
                    };
                }
            }
            return new CanProcessAsStaticPageInfo {
                Success = false,
            };
        }
        private async Task<ProcessingStatus> CanProcessAsDesignedPageAsync(PageDefinition page, string url, string queryString) {
            // request for a designed page
            if (!Manager.EditMode) { // only redirect if we're not editing
                if (!string.IsNullOrWhiteSpace(page.RedirectToPageUrl)) {
                    if (page.RedirectToPageUrl.StartsWith("/") && page.RedirectToPageUrl.IndexOf('?') < 0) {
                        PageDefinition redirectPage = await PageDefinition.LoadFromUrlAsync(page.RedirectToPageUrl);
                        if (redirectPage != null) {
                            if (string.IsNullOrWhiteSpace(redirectPage.RedirectToPageUrl)) {
                                string redirUrl = Manager.CurrentSite.MakeUrl(page.RedirectToPageUrl + queryString);
#if MVC6
                                Logging.AddLog("302 Found - Redirect to {0}", redirUrl).Truncate(100);
                                Manager.CurrentResponse.StatusCode = 302;
                                Manager.CurrentResponse.Headers.Add("Location", redirUrl);
#else
                                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - Redirect to {0}", redirUrl).Truncate(100);
                                Manager.CurrentResponse.AddHeader("Location", redirUrl);
                                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                                return ProcessingStatus.Complete;
                            } else
                                throw new InternalError("Page {0} redirects to page {1}, which redirects to page {2}", page.Url, page.RedirectToPageUrl, redirectPage.RedirectToPageUrl);
                        }
                    } else {
#if MVC6
                        Logging.AddLog("302 Found - Redirect to {0}", page.RedirectToPageUrl).Truncate(100);
                        Manager.CurrentResponse.StatusCode = 302;
                        Manager.CurrentResponse.Headers.Add("Location", page.RedirectToPageUrl);
#else
                        Manager.CurrentResponse.Status = Logging.AddLog("302 Found - Redirect to {0}", page.RedirectToPageUrl).Truncate(100);
                        Manager.CurrentResponse.AddHeader("Location", page.RedirectToPageUrl);
                        Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                        return ProcessingStatus.Complete;
                    }
                }
            }
            if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers || string.Compare(url, Manager.CurrentSite.LoginUrl, true) == 0) && page.IsAuthorized_View()) {
                // if the requested page is for desktop but we're on a mobile device, find the correct page to display
                if (!Manager.EditMode) { // only redirect if we're not editing
                    if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Mobile && !string.IsNullOrWhiteSpace(page.MobilePageUrl)) {
                        PageDefinition mobilePage = await PageDefinition.LoadFromUrlAsync(page.MobilePageUrl);
                        if (mobilePage != null) {
                            if (string.IsNullOrWhiteSpace(mobilePage.MobilePageUrl)) {
                                string redirUrl = page.MobilePageUrl;
                                if (redirUrl.StartsWith("/"))
                                    redirUrl = Manager.CurrentSite.MakeUrl(redirUrl + queryString);
#if MVC6
                                Logging.AddLog("302 Found - {0}", redirUrl).Truncate(100);
                                Manager.CurrentResponse.StatusCode = 302;
                                Manager.CurrentResponse.Headers.Add("Location", redirUrl);
#else
                                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", redirUrl).Truncate(100);
                                Manager.CurrentResponse.AddHeader("Location", redirUrl);
                                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                                return ProcessingStatus.Complete;
                            }
#if DEBUG
                            else
                                throw new InternalError("Page {0} redirects to mobile page {1}, which redirects to mobile page {2}", page.Url, page.MobilePageUrl, mobilePage.MobilePageUrl);
#endif
                        }
                    }
                }
                Manager.CurrentPage = page;// Found It!!
                if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Desktop && YetaWFController.GoingToPopup()) {
                    // we're going into a popup for this
                    Manager.IsInPopup = true;
                }
                bool usePageSettings = false;
                if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite) {
                    switch (Manager.CurrentSite.PageSecurity) {
                        case PageSecurityType.AsProvided:
                            usePageSettings = true;
                            break;
                        case PageSecurityType.AsProvidedAnonymous_LoggedOnhttps:
                            if (!Manager.HaveUser || page.PageSecurity != PageDefinition.PageSecurityType.Any)
                                usePageSettings = true;
                            break;
                        case PageSecurityType.AsProvidedLoggedOn_Anonymoushttp:
                            if (Manager.HaveUser || page.PageSecurity != PageDefinition.PageSecurityType.Any)
                                usePageSettings = true;
                            break;
                        case PageSecurityType.NoSSLOnlyAnonymous_LoggedOnhttps:
                        case PageSecurityType.NoSSLOnly:
                        case PageSecurityType.SSLOnly:
                            break;
                        case PageSecurityType.UsePageModuleSettings:
                            usePageSettings = true;
                            break;
                    }
                } else
                    usePageSettings = true;
                if (usePageSettings) {
                    switch (page.PageSecurity) {
                        case PageDefinition.PageSecurityType.Any:
                            break;
                        case PageDefinition.PageSecurityType.httpsOnly:
                            if (Manager.HostSchemeUsed != "https") {
                                UriBuilder newUri = new UriBuilder(Manager.CurrentRequestUrl);
                                newUri.Scheme = "https";
                                newUri.Port = Manager.CurrentSite.PortNumberSSLEval;
#if MVC6
                                Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                                Manager.CurrentResponse.StatusCode = 302;
                                Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
#else
                                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                                Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                                return ProcessingStatus.Complete;
                            }
                            break;
                        case PageDefinition.PageSecurityType.httpOnly:
                            if (Manager.HostSchemeUsed != "http") {
                                UriBuilder newUri = new UriBuilder(Manager.CurrentRequestUrl);
                                newUri.Scheme = "http";
                                newUri.Port = Manager.CurrentSite.PortNumberEval;
#if MVC6
                                Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                                Manager.CurrentResponse.StatusCode = 302;
                                Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
#else
                                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                                Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                                return ProcessingStatus.Complete;
                            }
                            break;
                    }
                }
                Logging.AddTraceLog("Page {0}", page.PageGuid);
                return ProcessingStatus.Page;
            } else {
                // Send to login page with redirect (IF NOT LOGGED IN)
                if (!Manager.HaveUser) {
                    Manager.OriginList.Clear();
                    Manager.OriginList.Add(new Origin() { Url = url + queryString });
                    string loginUrl;
                    QueryHelper qh = QueryHelper.FromUrl(Manager.CurrentSite.LoginUrl, out loginUrl);
                    qh.Add("__f", "true");// add __f=true to login url so we get a 401
                    Manager.OriginList.Add(new Origin() { Url = qh.ToUrl(loginUrl) });
                    string retUrl = Manager.ReturnToUrl;
                    Logging.AddLog("Redirect - {0}", retUrl);
                    Manager.CurrentResponse.Redirect(retUrl);
                    return ProcessingStatus.Complete;
                } else {
#if MVC6
                    Logging.AddErrorLog("403 Not Authorized");
                    Manager.CurrentResponse.StatusCode = 403;
#else
                    Manager.CurrentResponse.Status = Logging.AddErrorLog("403 Not Authorized");
                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
#endif
                    return ProcessingStatus.Complete;
                }
            }
        }
        private ProcessingStatus CanProcessAsModule(ModuleDefinition module, string url, string queryString) {
            // direct request for a module without page
            if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers) && module.IsAuthorized(ModuleDefinition.RoleDefinition.View)) {
                PageDefinition page = PageDefinition.Create();
                page.AddModule(Globals.MainPane, module);
                Manager.CurrentPage = page;
                if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Desktop && YetaWFController.GoingToPopup()) {
                    // we're going into a popup for this
                    Manager.IsInPopup = true;
                    page.SelectedPopupSkin = module.SelectedPopupSkin;
                }
                Logging.AddTraceLog("Module {0}", module.ModuleGuid);
                return ProcessingStatus.Page;
            }
            return ProcessingStatus.No;
        }

        //http://stackoverflow.com/questions/32943526/asp-net-5-mvc-6-detect-mobile-browser
        //regex from http://detectmobilebrowsers.com/
        // RFFU could use http://51degrees.codeplex.com/
        private static readonly Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static bool IsMobileBrowser(HttpRequest request) {
            var userAgent = request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(userAgent)) return false;
            if ((b.IsMatch(userAgent) || v.IsMatch(userAgent.Substring(0, 4))))
                return true;
            return false;
        }
    }
}
