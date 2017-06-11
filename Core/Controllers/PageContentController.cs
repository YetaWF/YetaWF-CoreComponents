﻿/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
using YetaWF.Core.Identity;
using YetaWF.Core.Modules;
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
                ScriptBundleFiles = new List<string>();
                CssBundleFiles = new List<string>();
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
            /// Returns a new Url if the request can only be answered with page different content.
            /// </summary>
            /// <remarks>The client will redirect the page content to this new Url.</remarks>
            public string RedirectContent { get; set; }

            /// <summary>
            /// Returns the content for all panes.
            /// </summary>
            public List<PaneContent> Content { get; set; }
            /// <summary>
            /// Returns the addon html.
            /// </summary>
            public string Addons { get; set; }
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
            /// <summary>
            /// Javascript files that are included in this page with a bundled Javascript file.
            /// </summary>
            /// <remarks>The bundled file is listed in ScriptFiles.</remarks>
            public List<string> ScriptBundleFiles { get; internal set; }
            /// <summary>
            /// Css files that are included in this page with a bundled css file.
            /// </summary>
            /// <remarks>The bundled file is listed in CssFiles.</remarks>
            public List<string> CssBundleFiles { get; internal set; }
            /// <summary>
            /// Analytics javascript code executed when a new page becomes active in an active Unified Page Set.
            /// </summary>
            public string AnalyticsContent { get; internal set; }
        }
        public class PaneContent {
            public string Pane { get; set; }
            public string HTML { get; set; }
        }

        /// <summary>
        /// Data received from client side for the requested page.
        /// </summary>
        /// <remarks>Because we use an Ajax GET request, these are encoded as query string.
        /// To avoid collisions all properties are prefixed with "__".</remarks>
        public class DataIn {
            public string __Path { get; set; }
            public string __QueryString { get; set; }
            public string __UnifiedSetGuid { get; set; }
            public PageDefinition.UnifiedModeEnum __UnifiedMode { get; set; }
            public List<Guid> __UnifiedAddonMods { get; set; }
            public int __UniqueIdPrefixCounter { get; set; }
            public bool __IsMobile { get; set; }
            public string __UnifiedSkinCollection { get; set; }
            public string __UnifiedSkinFileName { get; set; }
            public List<string> __Panes { get; set; }
            public List<string> __KnownCss { get; set; }
            public List<string> __KnownScripts { get; set; }
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
        public ActionResult Show(DataIn dataIn) {

            dataIn.__Path = YetaWFManager.UrlDecodePath(dataIn.__Path);
            if (!YetaWFManager.HaveManager) {
#if MVC6
                return new NotFoundObjectResult(dataIn.__Path);
#else
                throw new HttpException(404, string.Format("Url {0} not found", dataIn.__Path));
#endif
            }
            if (Manager.EditMode) throw new InternalError("Unified Page Sets can't be used in Site Edit Mode");
            if (Manager.IsInPopup) throw new InternalError("Unified Page Sets can't be used in popup windows");

            Uri uri = new Uri(Manager.CurrentRequestUrl);

            SiteDefinition site = Manager.CurrentSite;

            if (site.IsLockedAny && !string.IsNullOrWhiteSpace(site.GetLockedForIP()) && !string.IsNullOrWhiteSpace(site.LockedUrl) &&
                    Manager.UserHostAddress != site.GetLockedForIP() && Manager.UserHostAddress != "127.0.0.1" &&
                    string.Compare(uri.AbsolutePath, site.LockedUrl, true) != 0) {
                Logging.AddLog("302 Found - {0}", site.LockedUrl).Truncate(100);
                PageContentResult cr = new PageContentResult();
                if (site.LockedUrl.StartsWith("/"))
                    cr.Result.RedirectContent = site.LockedUrl;
                else
                    cr.Result.Redirect = site.LockedUrl;
                return cr;
            }

            // Check if site language requested using !yLang= arg
#if MVC6
            string lang = Manager.CurrentRequest.Query[Globals.Link_Language];
#else
            string lang = Manager.CurrentRequest[Globals.Link_Language];
#endif
            if (!string.IsNullOrWhiteSpace(lang)) {
                // !yLang= is only used in <link rel='alternate' href='{0}' hreflang='{1}' /> to indicate multi-language support for pages, so we just redirect to that page
                // we need the entire page, content is not sufficient
                PageContentResult cr = new PageContentResult();
                cr.Result.Redirect = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
                return cr;
            }

            // set the unique id prefix so all generated ids start where the main page left off
            Manager.UniqueIdPrefixCounter = dataIn.__UniqueIdPrefixCounter;

            // if this is a url with segments (like http://...local.../segment/segment/segment/segment/segment/segment)
            // rewrite it to make it a proper querystring
            {
                string url = dataIn.__Path;
                string[] segments = url.Split(new char[] { '/' });
                string newUrl, newQs;
                if (url.StartsWith(Globals.ModuleUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    // direct module references don't participate in unified page sets
                    PageContentResult cr = new PageContentResult();
                    cr.Result.Redirect = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
                    return cr;
                } else if (url.StartsWith(Globals.PageUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    // direct page references don't participate in unified page sets
                    PageContentResult cr = new PageContentResult();
                    cr.Result.Redirect = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
                    return cr;
                } else {
                    PageDefinition page = PageDefinition.GetPageUrlFromUrlWithSegments(url, dataIn.__QueryString, out newUrl, out newQs);
                    if (page != null) {
                        // we have a page, check if the URL was rewritten because it had human readable arguments
                        if (newUrl != url) {
                            PageContentResult cr = new PageContentResult();
                            cr.Result.RedirectContent = QueryHelper.ToUrl(newUrl, newQs);
                            return cr;
                        }
                    } else {
                        // direct urls don't participate in unified page sets
                        PageContentResult cr = new PageContentResult();
                        cr.Result.Redirect = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
                        return cr;
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
                    PageContentResult cr = new PageContentResult();
                    string returnUrl = Manager.ReturnToUrl;
                    if (returnUrl.StartsWith("/"))
                        cr.Result.RedirectContent = returnUrl;
                    else
                        cr.Result.Redirect = returnUrl;
                    return cr;
                }
                // this shouldn't be necessary because the first page shown in the unified page set would have generated this
                //Resource.ResourceAccess.ShowNeed2FA();
            }

            Logging.AddLog("Page Content");

            // Check if this is a static page
            if (CanProcessAsStaticPage(dataIn.__Path)) { // if this is a static page, render as complete static page
                PageContentResult cr = new PageContentResult();
                cr.Result.Redirect = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
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
                        // if we got here, we shouldn't be here - we're requesting a page outside of unified page set
                        cr.Result.Redirect = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
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
            PageDefinition page = PageDefinition.LoadFromUrl(dataIn.__Path);
            if (page != null) {
                if (dataIn.__UnifiedMode == PageDefinition.UnifiedModeEnum.SkinDynamicContent) {
                    if (page.UnifiedSetGuid != null) {
                        // this page is part of another unified page set
                        string redirUrl = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
                        cr.Result.Redirect = redirUrl;
                        return ProcessingStatus.Complete;
                    }
                    // make sure it's the same skin
                    // some pages (created with earlier versions of YetaWF) have a null skin name, which defaults to SkinAccess.FallbackPageFileName
                    if (string.IsNullOrWhiteSpace(page.SelectedSkin.FileName))
                        page.SelectedSkin.FileName = SkinAccess.FallbackPageFileName;
                    if (page.SelectedSkin.Collection != dataIn.__UnifiedSkinCollection || page.SelectedSkin.FileName != dataIn.__UnifiedSkinFileName) {
                        // this page is part of another skin
                        string redirUrl = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
                        cr.Result.Redirect = redirUrl;
                        return ProcessingStatus.Complete;
                    }
                } else {
                    if (page.UnifiedSetGuid != new Guid(dataIn.__UnifiedSetGuid)) {
                        // this page isn't part of this unified set (not listed)
                        string redirUrl = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString);
                        cr.Result.Redirect = redirUrl;
                        return ProcessingStatus.Complete;
                    }
                }
                if (!string.IsNullOrWhiteSpace(page.RedirectToPageUrl)) {
                    if (page.RedirectToPageUrl.StartsWith("/") && page.RedirectToPageUrl.IndexOf('?') < 0) {
                        PageDefinition redirectPage = PageDefinition.LoadFromUrl(page.RedirectToPageUrl);
                        if (redirectPage != null) {
                            if (string.IsNullOrWhiteSpace(redirectPage.RedirectToPageUrl)) {
                                string redirUrl = Manager.CurrentSite.MakeUrl(QueryHelper.ToUrl(page.RedirectToPageUrl, dataIn.__QueryString));
                                Logging.AddLog("302 Found - Redirect to {0}", redirUrl).Truncate(100);
                                cr.Result.RedirectContent = redirUrl;
                                return ProcessingStatus.Complete;
                            } else
                                throw new InternalError("Page {0} redirects to page {1}, which redirects to page {2}", page.Url, page.RedirectToPageUrl, redirectPage.RedirectToPageUrl);
                        }
                    } else {
                        // redirect elsewhere
                        Logging.AddLog("302 Found - Redirect to {0}", page.RedirectToPageUrl).Truncate(100);
                        if (page.RedirectToPageUrl.StartsWith("/"))
                            cr.Result.RedirectContent = page.RedirectToPageUrl;
                        else
                            cr.Result.Redirect = page.RedirectToPageUrl;
                        return ProcessingStatus.Complete;
                    }
                }
                if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers || string.Compare(dataIn.__Path, Manager.CurrentSite.LoginUrl, true) == 0) && page.IsAuthorized_View()) {
                    // if the requested page is for desktop but we're on a mobile device, find the correct page to display
                    if (dataIn.__IsMobile && !string.IsNullOrWhiteSpace(page.MobilePageUrl)) {
                        PageDefinition mobilePage = PageDefinition.LoadFromUrl(page.MobilePageUrl);
                        if (mobilePage != null) {
                            if (string.IsNullOrWhiteSpace(mobilePage.MobilePageUrl)) {
                                string redirUrl = page.MobilePageUrl;
                                Logging.AddLog("302 Found - {0}", redirUrl).Truncate(100);
                                redirUrl = QueryHelper.ToUrl(redirUrl, dataIn.__QueryString);
                                cr.Result.RedirectContent = redirUrl;
                                return ProcessingStatus.Complete;
                            }
#if DEBUG
                            else
                                throw new InternalError("Page {0} redirects to mobile page {1}, which redirects to mobile page {2}", page.Url, page.MobilePageUrl, mobilePage.MobilePageUrl);
#endif
                        }
                    }
                    Manager.CurrentPage = page;// Found It!!
                    Logging.AddTraceLog("Page {0}", page.PageGuid);
                    return ProcessingStatus.Page;
                } else {
                    // Send to login page with redirect (IF NOT LOGGED IN)
                    if (!Manager.HaveUser) {
                        Manager.OriginList.Clear();
                        Manager.OriginList.Add(new Origin() { Url = QueryHelper.ToUrl(dataIn.__Path, dataIn.__QueryString) });
                        Manager.OriginList.Add(new Origin() { Url = Manager.CurrentSite.LoginUrl });
                        string retUrl = Manager.ReturnToUrl;
                        Logging.AddLog("Redirect - {0}", retUrl);
                        if (retUrl.StartsWith("/"))
                            cr.Result.RedirectContent = retUrl;
                        else
                            cr.Result.Redirect = retUrl;
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
