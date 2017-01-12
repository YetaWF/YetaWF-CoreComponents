/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.Mvc;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;
using YetaWF.Core.Pages;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Support.StaticPages;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Controllers {
    public class PageController : Controller {
        // This controller is a plain MVC controller because we don't want any startup processing to take place (like authorization, etc)
        // because we handle all this here. This controller is used for page and single module display (GET requests only)

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Head)]  // HEAD is only supported here (so dumb linkcheckers can see the pages)
        public ActionResult Show(string path) {
            // We come here for ANY page request (GET only)

            if (!YetaWFManager.HaveManager)
                throw new HttpException(404, string.Format("Url {0} not found", path));

            SiteDefinition site = Manager.CurrentSite;

            // Check if mobile browser and redirect to mobile browser url/subdomain if needed
            // RFFU could use http://51degrees.codeplex.com/
            HttpBrowserCapabilities caps = Manager.CurrentRequest.Browser;
            Uri uri = Manager.CurrentRequest.Url;
            if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Undecided) {
                if (caps.IsMobileDevice) {
                    Manager.ActiveDevice = YetaWFManager.DeviceSelected.Mobile;
                    if (!string.IsNullOrEmpty(site.MobileSiteUrl)) {
                        if (uri.IsAbsoluteUri) {
                            if (string.Compare(uri.Host, "localhost", true) != 0 && string.Compare(uri.AbsoluteUri, site.MobileSiteUrl, true) != 0) {
                                UriBuilder newUrl = new UriBuilder(site.MobileSiteUrl);
                                Manager.CurrentResponse.Status = Logging.AddLog("301 Moved Permanently - {0}", newUrl.ToString()).Truncate(100);
                                Manager.CurrentResponse.AddHeader("Location", newUrl.ToString());
                                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                                return new EmptyResult();
                            }
                        }
                    }
                }
                Manager.ActiveDevice = YetaWFManager.DeviceSelected.Desktop;
            } else {
                Manager.ActiveDevice = caps.IsMobileDevice ? YetaWFManager.DeviceSelected.Mobile : YetaWFManager.DeviceSelected.Desktop;
            }

            // Check for browser capabilities
            if (caps != null && !caps.Crawler && !string.IsNullOrWhiteSpace(site.UnsupportedBrowserUrl) && !BrowserCaps.SupportedBrowser(caps) && string.Compare(uri.AbsolutePath, site.UnsupportedBrowserUrl, true) != 0) {
                Logging.AddLog("Unsupported browser {0}, version {1}.", caps.Browser, caps.Version);
                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", site.UnsupportedBrowserUrl).Truncate(100);
                Manager.CurrentResponse.AddHeader("Location", site.UnsupportedBrowserUrl);
                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                return new EmptyResult();
            }
            if (site.IsLockedAny && !string.IsNullOrWhiteSpace(site.GetLockedForIP()) && !string.IsNullOrWhiteSpace(site.LockedUrl) &&
                    Manager.UserHostAddress != site.GetLockedForIP() && Manager.UserHostAddress != "127.0.0.1" &&
                    string.Compare(uri.AbsolutePath, site.LockedUrl, true) != 0) {
                Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", site.LockedUrl).Truncate(100);
                Manager.CurrentResponse.AddHeader("Location", site.LockedUrl);
                Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                return new EmptyResult();
            }

            if (SiteDefinition.INITIAL_INSTALL) {
                string initPageUrl = "/$initall";
                string initPageQs = "?From=Data";
                string newUrl;
                if (!Manager.IsLocalHost)
                    // if we're using IIS we need to use the real domain which we haven't saved yet in Site Properties
                    newUrl = Manager.CurrentSite.MakeUrl(initPageUrl + initPageQs, RealDomain: Manager.HostUsed);
                else
                    newUrl = Manager.CurrentSite.MakeUrl(initPageUrl + initPageQs);
                if (string.Compare(Manager.CurrentRequest.Url.LocalPath, initPageUrl, true) != 0) {
                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                    Manager.CurrentResponse.AddHeader("Location", newUrl);
                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                    return new EmptyResult();
                }
            }

            // Check if site language requested using !yLang= arg
            string lang = Manager.CurrentRequest[Globals.Link_Language];
            if (!string.IsNullOrWhiteSpace(lang))
                Manager.SetUserLanguage(lang);

            // Check if home URL is requested and matches site's desired home URL
            if (uri.AbsolutePath == "/") {
                if (Manager.CurrentSite.HomePageUrl != "/") {
                    string newUrl = Manager.CurrentSite.MakeUrl(Manager.CurrentSite.HomePageUrl);
                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                    Manager.CurrentResponse.AddHeader("Location", newUrl);
                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
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
                        Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                        Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                        return new EmptyResult();
                    }
                    break;
                case PageSecurityType.NoSSLOnlyAnonymous_LoggedOnhttps:
                    if (!Manager.HaveUser && uri.Scheme == "https") {
                        UriBuilder newUri = new UriBuilder(uri);
                        newUri.Scheme = "http";
                        newUri.Port = Manager.CurrentSite.PortNumber == -1 ? 80 : Manager.CurrentSite.PortNumber;
                        Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                        Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                        return new EmptyResult();
                    } else if (Manager.HaveUser && uri.Scheme != "https") {
                        UriBuilder newUri = new UriBuilder(uri);
                        newUri.Scheme = "https";
                        newUri.Port = Manager.CurrentSite.PortNumberSSL == -1 ? 443 : Manager.CurrentSite.PortNumberSSL;
                        Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                        Manager.CurrentContext.ApplicationInstance.CompleteRequest();
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
                        Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                        Manager.CurrentContext.ApplicationInstance.CompleteRequest();
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
                string newUrl;
                if (url.StartsWith(Globals.ModuleUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl);
                    if (newUrl != url) {
                        Logging.AddLog("Server.TransferRequest - {0}", newUrl);
                        Server.TransferRequest(newUrl);
                        return new EmptyResult();
                    }
                } else if (url.StartsWith(Globals.PageUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl);
                    if (newUrl != url) {
                        Logging.AddLog("Server.TransferRequest - {0}", newUrl);
                        Server.TransferRequest(newUrl);
                        return new EmptyResult();
                    }
                } else {
                    PageDefinition page = PageDefinition.GetPageUrlFromUrlWithSegments(url, uri.Query, out newUrl);
                    if (page != null) {
                        // we have a page, check if the URL was rewritten because it had human readable arguments
                        if (newUrl != url) {
                            Logging.AddLog("Server.TransferRequest - {0}", newUrl);
                            Server.TransferRequest(newUrl);
                            return new EmptyResult();
                        }
                    } else {
                        // we have a direct url, make sure it's exactly 3 segments otherwise rewrite the remaining args as non-human readable qs args
                        // don't rewrite css/scss/less file path - we handle that in a http handler
                        PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 4, uri.Query, out newUrl);
                        if (newUrl != url) {
                            Logging.AddLog("Server.TransferRequest - {0}", newUrl);
                            Server.TransferRequest(newUrl);
                            return new EmptyResult();
                        }
                    }
                }
            }

            // set up all info, like who is logged on, popup, origin list, etc.
            YetaWFController.SetupEnvironmentInfo();

            Logging.AddLog("Request");

            // Check if it's a built-in command (mostly for debugging and initial install) and build a page dynamically (which is not saved)
            Action<NameValueCollection> action = BuiltinCommands.Find(uri.LocalPath, checkAuthorization: true);
            if (action != null) {
                if (Manager.IsHeadRequest)
                    return new EmptyResult();
                Manager.CurrentPage = PageDefinition.Create();
                NameValueCollection qs = System.Web.HttpUtility.ParseQueryString(uri.Query);
                action(qs);
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
                    return new PageViewResult(ViewData, TempData);
                case ProcessingStatus.No:
                    break;
            }
            switch (CanProcessAsModule(uri.LocalPath, uri.Query)) {
                case ProcessingStatus.Complete:
                    return new EmptyResult();
                case ProcessingStatus.Page:
                    if (Manager.IsHeadRequest)
                        return new EmptyResult();
                    return new PageViewResult(ViewData, TempData);
                case ProcessingStatus.No:
                    break;
            }

            // if we got here, we shouldn't be here

            // display 404 error page if one is defined
            if (!string.IsNullOrWhiteSpace(site.NotFoundUrl)) {
                PageDefinition page = PageDefinition.LoadFromUrl(site.NotFoundUrl);
                if (page != null) {
                    Manager.CurrentPage = page;// Found It!!
                    if (Manager.IsHeadRequest)
                        throw new HttpException(404, "404 Not Found");
                    Manager.CurrentResponse.Status = Logging.AddErrorLog("404 Not Found");
                    return new PageViewResult(ViewData, TempData);
                }
            }
            throw new HttpException(404, Logging.AddErrorLog("404 Not Found"));
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
                pageData = Manager.StaticPageManager.GetPage(localUrl);
                return !string.IsNullOrWhiteSpace(pageData);
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
                                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - Redirect to {0}", redirUrl).Truncate(100);
                                    Manager.CurrentResponse.AddHeader("Location", redirUrl);
                                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                                    return ProcessingStatus.Complete;
                                } else
                                    throw new InternalError("Page {0} redirects to page {1}, which redirects to page {2}", page.Url, page.RedirectToPageUrl, redirectPage.MobilePageUrl);
                            }
                        } else {
                            Manager.CurrentResponse.Status = Logging.AddLog("302 Found - Redirect to {0}", page.RedirectToPageUrl).Truncate(100);
                            Manager.CurrentResponse.AddHeader("Location", page.RedirectToPageUrl);
                            Manager.CurrentContext.ApplicationInstance.CompleteRequest();
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
                                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", redirUrl).Truncate(100);
                                    Manager.CurrentResponse.AddHeader("Location", redirUrl);
                                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
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
                                if (Manager.CurrentRequest.Url.Scheme != "https") {
                                    UriBuilder newUri = new UriBuilder(Manager.CurrentRequest.Url);
                                    newUri.Scheme = "https";
                                    newUri.Port = Manager.CurrentSite.PortNumberSSL == -1 ? 443 : Manager.CurrentSite.PortNumberSSL;
                                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                                    Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                                    return ProcessingStatus.Complete;
                                }
                                break;
                            case PageDefinition.PageSecurityType.httpOnly:
                                if (Manager.CurrentRequest.Url.Scheme != "http") {
                                    UriBuilder newUri = new UriBuilder(Manager.CurrentRequest.Url);
                                    newUri.Scheme = "http";
                                    newUri.Port = Manager.CurrentSite.PortNumber == -1 ? 80 : Manager.CurrentSite.PortNumber;
                                    Manager.CurrentResponse.Status = Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                                    Manager.CurrentResponse.AddHeader("Location", newUri.ToString());
                                    Manager.CurrentContext.ApplicationInstance.CompleteRequest();
                                    return ProcessingStatus.Complete;
                                }
                                break;
                        }
                    }
                    Logging.AddLog("Page {0}", page.PageGuid);
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
                        Manager.CurrentResponse.Status = Logging.AddErrorLog("403 Not Authorized");
                        Manager.CurrentContext.ApplicationInstance.CompleteRequest();
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
                    Logging.AddLog("Module {0}", module.ModuleGuid);
                    return ProcessingStatus.Page;
                }
            }
            return ProcessingStatus.No;
        }
    }


    public class PageViewResult : ActionResult {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public PageViewResult(ViewDataDictionary viewData, TempDataDictionary tempData) {
            TempData = tempData;
            ViewData = viewData;
        }

        public override void ExecuteResult(ControllerContext context) {
            if (context == null)
                throw new ArgumentNullException("context");

            bool staticPage = Manager.CurrentPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && !Manager.HaveUser;
            Manager.RenderStaticPage = staticPage;

            Manager.PageTitle = Manager.CurrentPage.Title;

            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);

            View = new PageView();
            StringWriter writer = new StringWriter();
            ViewContext viewContext = new ViewContext(context, View, ViewData, TempData, writer);
            View.Render(viewContext, writer);

            string pageHtml = writer.ToString();

            PageProcessing pageProc = new PageProcessing(Manager);
            pageHtml = pageProc.PostProcessHtml(pageHtml);
            if (!Manager.CurrentSite.DEBUGMODE && Manager.CurrentSite.Compression)
                pageHtml = WhiteSpaceResponseFilter.Compress(Manager, pageHtml);

            if (staticPage)
                Manager.StaticPageManager.AddPage(Manager.CurrentPage.Url, Manager.CurrentPage.StaticPage == PageDefinition.StaticPageEnum.YesMemory, pageHtml);

            context.HttpContext.Response.Output.Write(pageHtml);
        }

        public TempDataDictionary TempData { get; set; }
        public IView View { get; set; }
        public ViewDataDictionary ViewData { get; set; }

    }

    public class PageView : IView {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public PageView() { }

        public void Render(ViewContext viewContext, TextWriter writer) {

            PageDefinition currPage = Manager.CurrentPage;
            SkinAccess skinAccess = new SkinAccess();
            SkinDefinition skin = SkinDefinition.EvaluatedSkin(currPage, Manager.IsInPopup);
            string virtPath = skinAccess.PhysicalPageUrl(skin, Manager.IsInPopup);
            if (!File.Exists(YetaWFManager.UrlToPhysical(virtPath)))
                throw new InternalError("No page skin available");

            // set new character dimensions
            int charWidth, charHeight;
            skinAccess.GetPageCharacterSizes(out charWidth, out charHeight);
            Manager.NewCharSize(charWidth, charHeight);
            // add char dimensions to config options
            Manager.ScriptManager.AddVolatileOption("Basics", "CharWidthAvg", charWidth);
            Manager.ScriptManager.AddVolatileOption("Basics", "CharHeight", charHeight);

            Manager.AddOnManager.AddStandardAddOns();
            Manager.AddOnManager.AddSkin(skin.Collection);

            Manager.AddOnManager.AddAddOn("YetaWF", "Core", "Basics");
            Manager.ScriptManager.AddLast("YetaWF_Basics", "YetaWF_Basics.initPage();");// end of page initialization
            if (Manager.IsInPopup)
                Manager.AddOnManager.AddAddOn("YetaWF", "Core", "Popups");

            RazorPage razorPage = (RazorPage)RazorPage.CreateInstanceFromVirtualPath(virtPath);

            //razorPage.RazorPageFile = realFile;
            razorPage.ViewContext = viewContext;
            razorPage.ViewData = new ViewDataDictionary<object>(viewContext.ViewData);
            razorPage.InitHelpers();

            razorPage.RenderView(viewContext);

            Manager.AddOnManager.AddSkinCustomization(skin.Collection);

            Manager.PopCharSize();
        }
    }
}
