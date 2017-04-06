/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
using YetaWF.Core.Identity;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewEngines;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Controllers {

    public class PageController : Controller {
        // This controller is a plain MVC controller because we don't want any startup processing to take place (like authorization, etc)
        // because we handle all this here. This controller is used for page and single module display (GET/HEAD requests only)

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

#if MVC6
        public PageController(IViewRenderService viewRenderService) {
            _viewRenderService = viewRenderService;
        }
        private readonly IViewRenderService _viewRenderService;
#else
#endif

        [AcceptVerbs("GET", "HEAD")]  // HEAD is only supported here (so dumb linkcheckers can see the pages)
        public ActionResult Show(string path) {
            // We come here for ANY page request (GET only)

            if (!YetaWFManager.HaveManager) {
#if MVC6
                return new NotFoundObjectResult(path);
#else
                throw new HttpException(404, string.Format("Url {0} not found", path));
#endif
            }
            SiteDefinition site = Manager.CurrentSite;
            Uri uri = new Uri(Manager.CurrentRequestUrl);

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
                            if (string.Compare(uri.Host, "localhost", true) != 0 && string.Compare(uri.AbsoluteUri, site.MobileSiteUrl, true) != 0) {
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
#if MVC6
                Logging.AddLog("302 Found - {0}", site.LockedUrl).Truncate(100);
                Manager.CurrentResponse.StatusCode = 302;
                Manager.CurrentResponse.Headers.Add("Location", site.LockedUrl);
#else
                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", site.LockedUrl).Truncate(100);
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
                Manager.SetUserLanguage(lang);

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
            switch (Manager.CurrentSite.PageSecurity) {
                default:
                case PageSecurityType.AsProvided:
                    // it's all good
                    break;
                case PageSecurityType.NoSSLOnly:
                    if (uri.Scheme == "https") {
                        UriBuilder newUri = new UriBuilder(uri);
                        newUri.Scheme = "http";
                        newUri.Port = Manager.CurrentSite.PortNumber == -1 ? 80 : Manager.CurrentSite.PortNumber;
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
                        newUri.Port = Manager.CurrentSite.PortNumber == -1 ? 80 : Manager.CurrentSite.PortNumber;
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
                        newUri.Port = Manager.CurrentSite.PortNumberSSL == -1 ? 443 : Manager.CurrentSite.PortNumberSSL;
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
                        newUri.Port = Manager.CurrentSite.PortNumberSSL == -1 ? 443 : Manager.CurrentSite.PortNumberSSL;
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

            // if this is a url with segments (like http://...local.../segment/segment/segment/segment/segment/segment)
            // rewrite it to make it a proper querystring
            {
                string url = uri.LocalPath;
                string newUrl, newQs;
                if (url.StartsWith(Globals.ModuleUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl, out newQs);
                    if (newUrl != url) {
                        Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
#if MVC6
                        HttpContext.Request.Path = newUrl;
                        HttpContext.Request.QueryString = new QueryString(newQs);
                        uri = new Uri(Manager.CurrentRequestUrl);
#else
                        Server.TransferRequest(newUrl + newQs);
                        return new EmptyResult();
#endif
                    }
                } else if (url.StartsWith(Globals.PageUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl, out newQs);
                    if (newUrl != url) {
                        Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
#if MVC6
                        HttpContext.Request.Path = newUrl;
                        HttpContext.Request.QueryString = new QueryString(newQs);
                        uri = new Uri(Manager.CurrentRequestUrl);
#else
                        Server.TransferRequest(newUrl + newQs);
                        return new EmptyResult();
#endif
                    }
                } else {
                    PageDefinition page = PageDefinition.GetPageUrlFromUrlWithSegments(url, uri.Query, out newUrl, out newQs);
                    if (page != null) {
                        // we have a page, check if the URL was rewritten because it had human readable arguments
                        if (newUrl != url) {
                            Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
#if MVC6
                            HttpContext.Request.Path = newUrl;
                            HttpContext.Request.QueryString = new QueryString(newQs);
                            uri = new Uri(Manager.CurrentRequestUrl);
#else
                            Server.TransferRequest(newUrl + newQs);
                            return new EmptyResult();
#endif
                        }
                    } else {
                        // we have a direct url, make sure it's exactly 3 segments otherwise rewrite the remaining args as non-human readable qs args
                        // don't rewrite css/scss/less file path - we handle that in a http handler
                        PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 4, uri.Query, out newUrl, out newQs);
                        if (newUrl != url) {
                            Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
#if MVC6
                            HttpContext.Request.Path = newUrl;
                            HttpContext.Request.QueryString = new QueryString(newQs);
                            uri = new Uri(Manager.CurrentRequestUrl);
#else
                            Server.TransferRequest(newUrl + newQs);
                            return new EmptyResult();
#endif
                        }
                    }
                }
            }

            // set up all info, like who is logged on, popup, origin list, etc.
            YetaWFController.SetupEnvironmentInfo();

            // redirect current request to two-step authentication setup
            if (Manager.Need2FA) {
                if (Manager.Need2FARedirect) {
                    Logging.AddLog("Two-step authentication setup required");
                    ModuleAction action2FA = Resource.ResourceAccess.GetForceTwoStepActionSetup(null);
                    Manager.Need2FARedirect = false;
                    Manager.OriginList.Add(new Origin() { Url = uri.ToString() });// where to go after setup
                    Manager.OriginList.Add(new Origin() { Url = action2FA.GetCompleteUrl() }); // setup
                    return new RedirectResult(Manager.ReturnToUrl);
                }
                Resource.ResourceAccess.ShowNeed2FA();
            }

            Logging.AddTraceLog("Request");
            // Check if it's a built-in command (mostly for debugging and initial install) and build a page dynamically (which is not saved)
            Action<QueryHelper> action = BuiltinCommands.Find(uri.LocalPath, checkAuthorization: true);
            if (action != null) {
                if (Manager.IsHeadRequest)
                    return new EmptyResult();
                Manager.CurrentPage = PageDefinition.Create();
                QueryHelper qh = QueryHelper.FromQueryString(uri.Query);
                action(qh);
                return new EmptyResult();
            }
            // Process the page
            string pageData = null;
            if (CanProcessAsStaticPage(uri.LocalPath, out pageData)) {
                return Content(pageData, "text/html");
            }
            switch (CanProcessAsDesignedPage(uri.LocalPath, uri.Query)) {
                case ProcessingStatus.Complete:
                    return new EmptyResult();
                case ProcessingStatus.Page:
                    if (Manager.IsHeadRequest)
                        return new EmptyResult();
#if MVC6
                    return new PageViewResult(_viewRenderService, ViewData, TempData);
#else
                    return new PageViewResult(ViewData, TempData);
#endif
                case ProcessingStatus.No:
                    break;
            }
            switch (CanProcessAsModule(uri.LocalPath, uri.Query)) {
                case ProcessingStatus.Complete:
                    return new EmptyResult();
                case ProcessingStatus.Page:
                    if (Manager.IsHeadRequest)
                        return new EmptyResult();
#if MVC6
                    return new PageViewResult(_viewRenderService, ViewData, TempData);
#else
                    return new PageViewResult(ViewData, TempData);
#endif
                case ProcessingStatus.No:
                    break;
            }

            // if we got here, we shouldn't be here

            // display 404 error page if one is defined
            if (!string.IsNullOrWhiteSpace(site.NotFoundUrl)) {
                PageDefinition page = PageDefinition.LoadFromUrl(site.NotFoundUrl);
                if (page != null) {
                    Manager.CurrentPage = page;// Found It!!
                    if (Manager.IsHeadRequest) {
#if MVC6
                        return new NotFoundObjectResult(path);
#else
                        throw new HttpException(404, "404 Not Found");
#endif
                    }
#if MVC6
                    Logging.AddErrorLog("404 Not Found");
                    Manager.CurrentResponse.StatusCode = 404;
                    return new PageViewResult(_viewRenderService, ViewData, TempData);
#else
                    Manager.CurrentResponse.Status = Logging.AddErrorLog("404 Not Found");
                    return new PageViewResult(ViewData, TempData);
#endif
                }
            }
#if MVC6
            return NotFound(path);
#else
            throw new HttpException(404, string.Format("Url {0} not found", path));
#endif
        }

        protected enum ProcessingStatus {
            No = 0, // not processed, try something else
            Page = 1, // Page has been set up
            Complete = 2,// no more processing is needed
        }
        private bool CanProcessAsStaticPage(string localUrl, out string pageData) {
            pageData = null;
            if (Manager.CurrentSite.StaticPages && !Manager.HaveUser && !Manager.EditMode && Manager.CurrentSite.AllowAnonymousUsers) {
                if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Desktop && YetaWFController.GoingToPopup()) {
                    // we're going into a popup for this
                    Manager.IsInPopup = true;
                }
                DateTime lastUpdate;
                pageData = Manager.StaticPageManager.GetPage(localUrl, out lastUpdate);
                if (!string.IsNullOrWhiteSpace(pageData)) {
                    Manager.CurrentResponse.Headers.Add("Last-Modified", string.Format("{0:R}", lastUpdate));
                    return true;
                }
            }
            return false;
        }
        private ProcessingStatus CanProcessAsDesignedPage(string url, string queryString) {
            // request for a designed page
            PageDefinition page = PageDefinition.LoadFromUrl(url);
            if (page != null) {
                if (!Manager.EditMode) { // only redirect if we're not editing
                    if (!string.IsNullOrWhiteSpace(page.RedirectToPageUrl)) {
                        if (page.RedirectToPageUrl.StartsWith("/") && page.RedirectToPageUrl.IndexOf('?') < 0) {
                            PageDefinition redirectPage = PageDefinition.LoadFromUrl(page.RedirectToPageUrl);
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
                                    throw new InternalError("Page {0} redirects to page {1}, which redirects to page {2}", page.Url, page.RedirectToPageUrl, redirectPage.MobilePageUrl);
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
                            PageDefinition mobilePage = PageDefinition.LoadFromUrl(page.MobilePageUrl);
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
                    if (usePageSettings) {
                        switch (page.PageSecurity) {
                            case PageDefinition.PageSecurityType.Any:
                                break;
                            case PageDefinition.PageSecurityType.httpsOnly:
#if MVC6
                                if (Manager.CurrentRequest.Scheme != "https") {
#else
                                if (Manager.CurrentRequest.Url.Scheme != "https") {
#endif
                                    UriBuilder newUri = new UriBuilder(Manager.CurrentRequestUrl);
                                    newUri.Scheme = "https";
                                    newUri.Port = Manager.CurrentSite.PortNumberSSL == -1 ? 443 : Manager.CurrentSite.PortNumberSSL;
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
#if MVC6
                                if (Manager.CurrentRequest.Scheme != "http") {
#else
                                if (Manager.CurrentRequest.Url.Scheme != "http") {
#endif
                                    UriBuilder newUri = new UriBuilder(Manager.CurrentRequestUrl);
                                    newUri.Scheme = "http";
                                    newUri.Port = Manager.CurrentSite.PortNumber == -1 ? 80 : Manager.CurrentSite.PortNumber;
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
                        Manager.OriginList.Add(new Origin() { Url = Manager.CurrentSite.LoginUrl });
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
            return ProcessingStatus.No;
        }
        private ProcessingStatus CanProcessAsModule(string url, string queryString) {
            // direct request for a module without page
            ModuleDefinition module = ModuleDefinition.FindDesignedModule(url);
            if (module == null)
                module = ModuleDefinition.LoadByUrl(url);
            if (module != null) {
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

    public class PageViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        private IViewRenderService _viewRenderService;

        public PageViewResult(IViewRenderService _viewRenderService, ViewDataDictionary viewData, ITempDataDictionary tempData) {
            this._viewRenderService = _viewRenderService;
#else
        public PageViewResult(ViewDataDictionary viewData, TempDataDictionary tempData) {
#endif
            TempData = tempData;
            ViewData = viewData;
        }
#if MVC6
        public ITempDataDictionary TempData { get; set; }
#else
        public TempDataDictionary TempData { get; set; }
#endif
        public IView View { get; set; }
        public ViewDataDictionary ViewData { get; set; }

#if MVC6
        public override async Task ExecuteResultAsync(ActionContext context) {
#else
        public override void ExecuteResult(ControllerContext context) {
#endif
            if (context == null)
                throw new ArgumentNullException("context");

            bool staticPage = false;
            if (Manager.Deployed)
                staticPage = Manager.CurrentPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && !Manager.HaveUser;
            Manager.RenderStaticPage = staticPage;

            Manager.PageTitle = Manager.CurrentPage.Title;

            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);

            PageDefinition currPage = Manager.CurrentPage;
            SkinAccess skinAccess = new SkinAccess();
            SkinDefinition skin = SkinDefinition.EvaluatedSkin(currPage, Manager.IsInPopup);
            string skinCollection = skin.Collection;
            string virtPath = skinAccess.PhysicalPageUrl(skin, Manager.IsInPopup);
            if (!File.Exists(YetaWFManager.UrlToPhysical(virtPath)))
                throw new InternalError("No page skin available");

            // set new character dimensions
            int charWidth, charHeight;
            skinAccess.GetPageCharacterSizes(out charWidth, out charHeight);
            Manager.NewCharSize(charWidth, charHeight);
            Manager.LastUpdated = Manager.CurrentPage.Updated;
            Manager.ScriptManager.AddVolatileOption("Basics", "CharWidthAvg", charWidth);
            Manager.ScriptManager.AddVolatileOption("Basics", "CharHeight", charHeight);

            Manager.AddOnManager.AddStandardAddOns();
            Manager.AddOnManager.AddSkin(skinCollection);

            Manager.AddOnManager.AddAddOn("YetaWF", "Core", "Basics");
            Manager.ScriptManager.AddLast("YetaWF_Basics", "YetaWF_Basics.initPage();");// end of page initialization
            if (Manager.IsInPopup)
                Manager.AddOnManager.AddAddOn("YetaWF", "Core", "Popups");

            string pageHtml;
#if MVC6
            pageHtml = await _viewRenderService.RenderToStringAsync(context, "~/wwwroot" + virtPath, ViewData);
#else
            View = new PageView(virtPath);
            StringWriter writer = new StringWriter();

            ViewContext viewContext = new ViewContext(context, View, ViewData, TempData, writer);
            View.Render(viewContext, writer);

            pageHtml = writer.ToString();
#endif
            Manager.AddOnManager.AddSkinCustomization(skinCollection);
            Manager.PopCharSize();

            PageProcessing pageProc = new PageProcessing(Manager);
            pageHtml = pageProc.PostProcessHtml(pageHtml);
            if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.Compression)
                pageHtml = WhiteSpaceResponseFilter.Compress(Manager, pageHtml);

            if (staticPage) {
                Manager.StaticPageManager.AddPage(Manager.CurrentPage.Url, Manager.CurrentPage.StaticPage == PageDefinition.StaticPageEnum.YesMemory, pageHtml, Manager.LastUpdated);
                // Last-Modified is dependent on which user is logged on (if any) and any module that generates data which changes each time will defeat last-modified
                // so is only helpful for static pages and can't be used for dynamic pages
                context.HttpContext.Response.Headers.Add("Last-Modified", string.Format("{0:R}", Manager.LastUpdated));
            } else if (Manager.HaveUser && Manager.CurrentPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages) {
                // if we have a user for what would be a static page, we have to make sure the last modified date is set to override any perviously
                // served page to the then anonymous user before he/she logged on.
                context.HttpContext.Response.Headers.Add("Last-Modified", string.Format("{0:R}", DateTime.UtcNow));
            }
#if MVC6
            byte[] btes = Encoding.ASCII.GetBytes(pageHtml);
            await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
#else
            context.HttpContext.Response.Output.Write(pageHtml);
#endif
        }
    }

#if MVC6
#else
    public class PageView : IView {

        public PageView(string virtPath) {
            VirtualPath = virtPath;
        }
        private string VirtualPath { get; set; }

        public void Render(ViewContext viewContext, TextWriter writer) {

            RazorPage razorPage = (RazorPage)RazorPage.CreateInstanceFromVirtualPath(VirtualPath);

            razorPage.ViewContext = viewContext;
            razorPage.ViewData = new ViewDataDictionary<object>(viewContext.ViewData);
            razorPage.InitHelpers();

            razorPage.RenderView(viewContext);
        }
    }
#endif
}
