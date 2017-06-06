/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Extensions;
using YetaWF.Core.Log;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Skins;
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

    /// <summary>
    /// Controller for all page requests within YetaWF that only need the content rendered (used client-side to replace module content only).
    /// </summary>
    /// <remarks>This controller is a plain MVC controller because we don't want any startup processing to take place (like authorization, etc.)
    /// because we handle all this here.</remarks>
    public class PageContentController : Controller {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public class PageContentResult : YJsonResult {
            public PageContentResult() {
#if MVC6
#else
                JsonRequestBehavior = JsonRequestBehavior.AllowGet;
#endif
                Result = new PageContentData();
                Data = Result;
            }
            public PageContentData Result { get; set; }
        }
        public class PageContentData {
            public PageContentData() {
                Content = new List<PaneContent>();
                ScriptFiles = new List<string>();
                CssFiles = new List<string>();
            }
            /// <summary>
            /// The status of the request.
            /// </summary>
            /// <remarks>A null or empty string means success. Otherwise an error message is provided.</remarks>
            public string Status { get; internal set; }

            /// <summary>
            /// Returns a new Url if the request cannot be answered with page content.
            /// </summary>
            /// <remarks>The client will redirect the entire page to this new Url.</remarks>
            public string Redirect { get; set; }

            /// <summary>
            /// Returns the content for all panes.
            /// </summary>
            public List<PaneContent> Content { get; set; }
            /// <summary>
            /// The requested page's title.
            /// </summary>
            public string PageTitle { get; set; }
            /// <summary>
            /// The requested page's Css classes (defined using the page's Page Settings), unique to this page.
            /// </summary>
            public string PageCssClasses { get; set; }
            /// <summary>
            /// The requested page's Canonical Url.
            /// </summary>
            public string CanonicalUrl { get; set; }
            /// <summary>
            /// The requested page's local Url (without querystring)
            /// </summary>
            public string LocalUrl { get; set; }
            /// <summary>
            /// Inline script snippets generated for this page.
            /// </summary>
            public string Scripts { get; internal set; }
            /// <summary>
            /// End of page script snippets generated for this page.
            /// </summary>
            public string EndOfPageScripts { get; internal set; }
            /// <summary>
            /// Script files to include for this page.
            /// </summary>
            public List<string> ScriptFiles { get; internal set; }
            /// <summary>
            /// Css files to include for this page.
            /// </summary>
            public List<string> CssFiles { get; internal set; }
        }
        public class PaneContent {
            public string Pane { get; set; }
            public string HTML { get; set; }
        }

        public class DataIn {
            public string Path { get; set; }
            public string UnifiedSetGuid { get; set; }
            public PageDefinition.UnifiedModeEnum UnifiedMode { get; set; }
            public int UniqueIdPrefixCounter { get; set; }
            public string UnifiedSkinCollection { get; set; }
            public string UnifiedSkinFileName { get; set; }
            public string QueryString { get; set; }
            public List<string> Panes { get; set; }
            public List<string> KnownCss { get; set; }
            public List<string> KnownScripts { get; set; }
        }
#if MVC6
        /// <summary>
        /// Constructor.
        /// </summary>
        public PageContentController(IViewRenderService viewRenderService) {
            _viewRenderService = viewRenderService;
        }
        private readonly IViewRenderService _viewRenderService;
#else
#endif

        /// <summary>
        /// The Show action handles all page content requests within YetaWF.
        /// </summary>
        /// <param name="path">The local Url requested.</param>
        /// <returns></returns>
        [HttpGet] // MUST be a GET request because we are invoking additional controller actions that also require GET
        //$$$ don't come here if there is a new !yLang= querystring arg (means we're switching languages)
        //$$$$ don't come here if http<>https changed - or we don't care about page security?
        //$$$ don't come here in edit mode
        //$$$ provide info whether this is desktop/mobile
        //$$$ don't come here if we're going into a popup
        public ActionResult Show(DataIn dataIn) {

            if (!YetaWFManager.HaveManager) {
#if MVC6
                return new NotFoundObjectResult(dataIn.Path);
#else
                throw new HttpException(404, string.Format("Url {0} not found", dataIn.Path));
#endif
            }

            SiteDefinition site = Manager.CurrentSite;

            //if (site.IsLockedAny && !string.IsNullOrWhiteSpace(site.GetLockedForIP()) && !string.IsNullOrWhiteSpace(site.LockedUrl) &&
            //        Manager.UserHostAddress != site.GetLockedForIP() && Manager.UserHostAddress != "127.0.0.1" &&
            //        string.Compare(uri.AbsolutePath, site.LockedUrl, true) != 0) {
            //    Logging.AddLog("302 Found - {0}", site.LockedUrl).Truncate(100);
            //    PageContentResult cr = new PageContentResult();
            //    cr.Result.Redirect = site.LockedUrl;
            //    return cr;
            //}

            // Check if site language requested using !yLang= arg
#if MVC6
            string lang = Manager.CurrentRequest.Query[Globals.Link_Language];
#else
            string lang = Manager.CurrentRequest[Globals.Link_Language];
#endif
            // set the unique id prefix so all generated ids start where the main page left off
            Manager.UniqueIdPrefixCounter = dataIn.UniqueIdPrefixCounter;

            if (!string.IsNullOrWhiteSpace(lang))
                Manager.SetUserLanguage(lang);

            // if this is a url with segments (like http://...local.../segment/segment/segment/segment/segment/segment)
            // rewrite it to make it a proper querystring
            {
                string url = dataIn.Path;
                string[] segments = url.Split(new char[] { '/' });
                string newUrl, newQs;
                if (url.StartsWith(Globals.ModuleUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, segments, 3, dataIn.QueryString, out newUrl, out newQs);
                    if (newUrl != url) {
                        dataIn.Path = newUrl;
                        dataIn.QueryString = newQs;
                    }
                } else if (url.StartsWith(Globals.PageUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    PageDefinition.GetUrlFromUrlWithSegments(url, segments, 3, dataIn.QueryString, out newUrl, out newQs);
                    if (newUrl != url) {
                        dataIn.Path = newUrl;
                        dataIn.QueryString = newQs;
                    }
                } else {
                    PageDefinition page = PageDefinition.GetPageUrlFromUrlWithSegments(url, dataIn.QueryString, out newUrl, out newQs);
                    if (page != null) {
                        // we have a page, check if the URL was rewritten because it had human readable arguments
                        if (newUrl != url) {
                            dataIn.Path = newUrl;
                            dataIn.QueryString = newQs;
                        }
                    } else {
                        // we have a direct url, make sure it's exactly 3 segments otherwise rewrite the remaining args as non-human readable qs args
                        // don't rewrite css/scss/less file path - we handle that in a http handler
                        PageDefinition.GetUrlFromUrlWithSegments(url, segments, 4, dataIn.QueryString, out newUrl, out newQs);
                        if (newUrl != url) {
                            dataIn.Path = newUrl;
                            dataIn.QueryString = newQs;
                        }
                    }
                }
            }
            Manager.OverrideQueryString(dataIn.QueryString);// set new query string in case it was rewritten

            // set up all info, like who is logged on, popup, origin list, etc.
            YetaWFController.SetupEnvironmentInfo();

            //// redirect current request to two-step authentication setup
            //if (Manager.Need2FA) {
            //    if (Manager.Need2FARedirect) {
            //        Logging.AddLog("Two-step authentication setup required");
            //        ModuleAction action2FA = Resource.ResourceAccess.GetForceTwoStepActionSetup(null);
            //        Manager.Need2FARedirect = false;
            //        Manager.OriginList.Add(new Origin() { Url = uri.ToString() });// where to go after setup
            //        Manager.OriginList.Add(new Origin() { Url = action2FA.GetCompleteUrl() }); // setup
            //        PageContentResult cr = new PageContentResult();
            //        cr.Result.Redirect = Manager.ReturnToUrl;//$$LOCAL
            //        return cr;
            //    }
            //    Resource.ResourceAccess.ShowNeed2FA();//$$$ TEST
            //}

            Logging.AddLog("Page Content");

            // Check if this is a static page
            if (CanProcessAsStaticPage(dataIn.Path)) { // if this is a static page, render as complete static page
                PageContentResult cr = new PageContentResult();
                cr.Result.Redirect = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
                return cr;
            }

            // Process the page
            {
                PageContentResult cr = new PageContentResult();
                switch (CanProcessAsDesignedPage(dataIn, cr)) {
                    case ProcessingStatus.Complete:
                        return cr;
                    case ProcessingStatus.Page:
#if MVC6
                        return new PageContentViewResult(_viewRenderService, ViewData, TempData, dataIn);
#else
                        return new PageContentViewResult(ViewData, TempData, dataIn);
#endif
                    default:
                    case ProcessingStatus.No:
                        // if we got here, we shouldn't be here - probably requesting a page outside of unified page set
                        cr.Result.Redirect = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
                        return cr;
                }
            }
        }

        protected enum ProcessingStatus {
            No = 0, // not processed, try something else
            Page = 1, // Page has been set up
            Complete = 2,// no more processing is needed
        }
        private bool CanProcessAsStaticPage(string localUrl) {
            if (Manager.CurrentSite.StaticPages && !Manager.HaveUser && !Manager.EditMode && Manager.CurrentSite.AllowAnonymousUsers) {
                if (Manager.StaticPageManager.HavePage(localUrl))
                    return true;
            }
            return false;
        }
        private ProcessingStatus CanProcessAsDesignedPage(DataIn dataIn, PageContentResult cr) {

            // request for a designed page
            PageDefinition page = PageDefinition.LoadFromUrl(dataIn.Path);
            if (page != null) {
                if (dataIn.UnifiedMode == PageDefinition.UnifiedModeEnum.SkinDynamicContent) {
                    if (page.UnifiedSetGuid != null) {
                        // this page is part of another unified page set
                        string redirUrl = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
                        cr.Result.Redirect = redirUrl;
                        return ProcessingStatus.Complete;
                    }
                    // make sure it's the same skin
                    // some pages (created with earlier versions of YetaWF) have a null skin name, which defaults to SkinAccess.FallbackPageFileName
                    if (string.IsNullOrWhiteSpace(page.SelectedSkin.FileName))
                        page.SelectedSkin.FileName = SkinAccess.FallbackPageFileName;
                    if (page.SelectedSkin.Collection != dataIn.UnifiedSkinCollection || page.SelectedSkin.FileName != dataIn.UnifiedSkinFileName) {
                        // this page is part of another skin
                        string redirUrl = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
                        cr.Result.Redirect = redirUrl;
                        return ProcessingStatus.Complete;
                    }
                } else {
                    if (page.UnifiedSetGuid != new Guid(dataIn.UnifiedSetGuid)) {
                        // this page isn't part of this unified set (not listed)
                        string redirUrl = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
                        cr.Result.Redirect = redirUrl;
                        return ProcessingStatus.Complete;
                    }
                }
                if (!Manager.EditMode) { // only redirect if we're not editing
                    if (!string.IsNullOrWhiteSpace(page.RedirectToPageUrl)) {
                        if (page.RedirectToPageUrl.StartsWith("/") && page.RedirectToPageUrl.IndexOf('?') < 0) {
                            PageDefinition redirectPage = PageDefinition.LoadFromUrl(page.RedirectToPageUrl);
                            if (redirectPage != null) {
                                if (string.IsNullOrWhiteSpace(redirectPage.RedirectToPageUrl)) {
                                    string redirUrl = Manager.CurrentSite.MakeUrl(QueryHelper.ToUrl(page.RedirectToPageUrl, dataIn.QueryString));
                                    Logging.AddLog("302 Found - Redirect to {0}", redirUrl).Truncate(100);
                                    cr.Result.Redirect = redirUrl;
                                    return ProcessingStatus.Complete;
                                } else
                                    throw new InternalError("Page {0} redirects to page {1}, which redirects to page {2}", page.Url, page.RedirectToPageUrl, redirectPage.RedirectToPageUrl);
                            }
                        } else {
                            // redirect elsewhere
                            Logging.AddLog("302 Found - Redirect to {0}", page.RedirectToPageUrl).Truncate(100);
                            cr.Result.Redirect = page.RedirectToPageUrl;
                            return ProcessingStatus.Complete;
                        }
                    }
                }
                if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers || string.Compare(dataIn.Path, Manager.CurrentSite.LoginUrl, true) == 0) && page.IsAuthorized_View()) {
                    // if the requested page is for desktop but we're on a mobile device, find the correct page to display
                    if (!Manager.EditMode) { // only redirect if we're not editing
                        if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Mobile && !string.IsNullOrWhiteSpace(page.MobilePageUrl)) {
                            PageDefinition mobilePage = PageDefinition.LoadFromUrl(page.MobilePageUrl);
                            if (mobilePage != null) {
                                if (string.IsNullOrWhiteSpace(mobilePage.MobilePageUrl)) {
                                    string redirUrl = page.MobilePageUrl;
                                    Logging.AddLog("302 Found - {0}", redirUrl).Truncate(100);
                                    if (redirUrl.StartsWith("/")) {
                                        redirUrl = Manager.CurrentSite.MakeUrl(QueryHelper.ToUrl(redirUrl, dataIn.QueryString));
                                        cr.Result.Redirect = redirUrl;//$$LOCAL
                                    } else {
                                        cr.Result.Redirect = redirUrl;
                                    }
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
                    Logging.AddTraceLog("Page {0}", page.PageGuid);
                    return ProcessingStatus.Page;
                } else {
                    // Send to login page with redirect (IF NOT LOGGED IN)
                    if (!Manager.HaveUser) {
                        Manager.OriginList.Clear();
                        Manager.OriginList.Add(new Origin() { Url = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString) });
                        Manager.OriginList.Add(new Origin() { Url = Manager.CurrentSite.LoginUrl });
                        string retUrl = Manager.ReturnToUrl;
                        Logging.AddLog("Redirect - {0}", retUrl);
                        if (retUrl.StartsWith("/")) {
                            cr.Result.Redirect = retUrl;//$$LOCAL
                        } else {
                            cr.Result.Redirect = retUrl;
                        }
                        return ProcessingStatus.Complete;
                    } else {
                        cr.Result.Status = Logging.AddErrorLog("403 Not Authorized");
                        return ProcessingStatus.Complete;
                    }
                }
            }
            return ProcessingStatus.No;
        }
    }
}
