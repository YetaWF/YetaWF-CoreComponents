﻿/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Extensions;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Endpoints;

/// <summary>
/// Endpoint for all page requests within YetaWF that only need the content rendered (used client-side to replace module content only).
/// </summary>
public class PageContentEndpoints : YetaWFEndpoints {

    public static void RegisterEndpoints(IEndpointRouteBuilder endpoints, Package package, string areaName) {
        endpoints.MapPost(GetPackageApiEndpoint(package, typeof(PageContentEndpoints), nameof(Show)), async (HttpContext context, [FromBody] DataIn dataIn) => {
            return await Show(context, dataIn);
        });
    }

    internal class PageContentData {
        public PageContentData() {
            Content = new List<PaneContent>();
            ScriptFiles = new List<UrlEntry>();
            ScriptFilesPayload = new List<Payload>();
            CssFiles = new List<UrlEntry>();
            CssFilesPayload = new List<Payload>();
            ScriptBundleFiles = new List<string>();
            CssBundleFiles = new List<string>();
        }
        /// <summary>
        /// The status of the request.
        /// </summary>
        /// <remarks>A null or empty string means success. Otherwise an error message is provided.</remarks>
        public string? Status { get; internal set; }

        /// <summary>
        /// Returns a new Url if the request cannot be answered with page content.
        /// </summary>
        /// <remarks>The client will redirect the entire page to this new Url.</remarks>
        public string? Redirect { get; set; }
        /// <summary>
        /// Returns a new Url if the request can only be answered with different page content.
        /// </summary>
        /// <remarks>The client will redirect the page content to this new Url.</remarks>
        public string? RedirectContent { get; set; }

        /// <summary>
        /// Returns the content for all panes.
        /// </summary>
        public List<PaneContent> Content { get; set; }
        /// <summary>
        /// Returns the addon html.
        /// </summary>
        public string? Addons { get; set; }
        /// <summary>
        /// The requested page's title.
        /// </summary>
        public string PageTitle { get; set; } = null!;
        /// <summary>
        /// The requested page's Css classes (defined using the page's Page Settings), unique to this page.
        /// </summary>
        public string PageCssClasses { get; set; } = null!;
        /// <summary>
        /// The requested page's Canonical Url.
        /// </summary>
        public string? CanonicalUrl { get; set; }
        /// <summary>
        /// The requested page's local Url (without querystring)
        /// </summary>
        public string? LocalUrl { get; set; }
        /// <summary>
        /// Inline script snippets generated for this page.
        /// </summary>
        public string Scripts { get; internal set; } = null!;
        /// <summary>
        /// End of page script snippets generated for this page.
        /// </summary>
        public string EndOfPageScripts { get; internal set; } = null!;
        /// <summary>
        /// Script files to include for this page.
        /// </summary>
        public List<UrlEntry> ScriptFiles { get; internal set; } = null!;
        /// <summary>
        /// Script file payload (inline script) to include for this page.
        /// </summary>
        public List<Payload> ScriptFilesPayload { get; internal set; }
        /// <summary>
        /// Css files to include for this page.
        /// </summary>
        public List<UrlEntry> CssFiles { get; internal set; }
        /// <summary>
        /// Css files (inline) to include for this page.
        /// </summary>
        public List<Payload> CssFilesPayload { get; internal set; }
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
        public string AnalyticsContent { get; internal set; } = null!;
    }
    internal class PaneContent {
        public string Pane { get; set; } = null!;
        public string HTML { get; set; } = null!;
    }
    internal class Payload {
        public string Name { get; set; } = null!;
        public string Text { get; set; } = null!;
    }
    internal class UrlEntry {
        public string Name { get; set; } = null!;
        public string Url { get; set; } = null!;
        /// <summary>
        /// Dictionary of HTML attributes. Only used for external scripts.
        /// </summary>
        public IDictionary<string, object?> Attributes { get; set; } = null!;
    }

    /// <summary>
    /// Data received from the client for the requested page.
    /// </summary>
    /// <remarks>An instance of this class is sent from the client to request a "Single Page Application" update to change from the current page URL to the requested URL.</remarks>
    public class DataIn {
        /// <summary>
        /// The current's page version. If there is a version mismatch, the site has been restarted and a full page is returned instead.
        /// </summary>
        public string CacheVersion { get; set; } = null!;
        /// <summary>
        /// If the site was restarted (based on CacheVersion) redirect to this page. May be null.
        /// </summary>
        public string? CacheFailUrl { get; set; }
        /// <summary>
        /// The path of the requested page.
        /// </summary>
        public string Path { get; set; } = null!;
        /// <summary>
        /// The query string of the requested page.
        /// </summary>
        public string QueryString { get; set; } = null!;
        /// <summary>
        /// A list of "Referenced Modules" that have been loaded by the current page.
        /// </summary>
        public List<Guid>? UnifiedAddonMods { get; set; }
        /// <summary>
        /// The unique id prefix counter used by the current page. This value is used to prevent collisions when generating unique HTML tag ids.
        /// </summary>
        public YetaWFManager.UniqueIdInfo UniqueIdCounters { get; set; } = null!;
        /// <summary>
        /// Defines whether the current page was rendered on a mobile device.
        /// </summary>
        public bool IsMobile { get; set; }
        /// <summary>
        /// Defines the skin collection used by the current page.
        /// </summary>
        public string UnifiedSkinCollection { get; set; } = null!;
        /// <summary>
        /// Defines the skin collection's file used by the current page.
        /// </summary>
        public string UnifiedSkinFileName { get; set; } = null!;
        /// <summary>
        /// The collection of pages requested.
        /// </summary>
        public List<string> Panes { get; set; } = null!;
        /// <summary>
        /// A collection of all CSS files the client has already loaded.
        /// </summary>
        public List<string> KnownCss { get; set; } = null!;
        /// <summary>
        /// A collection of all JavaScript files the client has already loaded.
        /// </summary>
        public List<string> KnownScripts { get; set; } = null!;
    }

    /// <summary>
    /// Handles all page content requests within YetaWF.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    /// <param name="dataIn">Current data provided by front-end.</param>
    /// <returns></returns>
    private static async Task<IResult> Show(HttpContext context, DataIn dataIn) {
        dataIn.Path = Utility.UrlDecodePath(dataIn.Path);
        if (!YetaWFManager.HaveManager || string.IsNullOrWhiteSpace(dataIn.Path) || Manager.CurrentRequest.Headers == null || Manager.CurrentRequest.Headers["X-Requested-With"] != "XMLHttpRequest")
            return Results.NotFound(dataIn.Path);

        Manager.CurrentUrl = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);

        Uri uri = new Uri(Manager.CurrentRequestUrl);
        SiteDefinition site = Manager.CurrentSite;
        Manager.RenderContentOnly = true;

        Manager.UniqueIdCounters = dataIn.UniqueIdCounters;

        // process logging type callbacks
        await PageLogging.HandleCallbacksAsync(dataIn.Path, false);

        if (Manager.EditMode) throw new InternalError("Unified Page Sets can't be used in Site Edit Mode");

        if (site.IsLockedAny && !string.IsNullOrWhiteSpace(site.GetLockedForIP()) && !string.IsNullOrWhiteSpace(site.LockedUrl) &&
                Manager.UserHostAddress != site.GetLockedForIP() && Manager.UserHostAddress != "127.0.0.1" &&
                string.Compare(uri.AbsolutePath, site.LockedUrl, true) != 0) {
            Logging.AddLog("302 Found - {0}", site.LockedUrl).Truncate(100);
            PageContentData cr = new PageContentData();
            if (site.LockedUrl.StartsWith("/"))
                cr.RedirectContent = site.LockedUrl;
            else
                cr.Redirect = site.LockedUrl;
            return Results.Ok(cr);
        }

        // Check if site language requested using !yLang= arg
        string? lang = Manager.CurrentRequest.Query[Globals.Link_Language];
        if (dataIn.CacheVersion != YetaWFManager.CacheBuster || !string.IsNullOrWhiteSpace(lang)) {
            // If the cache version doesn't match, client is using an "old" site which was restarted, so we need to redirect to reload the entire page
            // !yLang= is only used in <link rel='alternate' href='{0}' hreflang='{1}' /> to indicate multi-language support for pages, so we just redirect to that page
            // we need the entire page, content is not sufficient
            PageContentData cr = new PageContentData();
            if (dataIn.CacheFailUrl != null)
                cr.Redirect = dataIn.CacheFailUrl;
            else
                cr.Redirect = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
            return Results.Ok(cr);
        }

        // set the unique id prefix so all generated ids start where the main page left off
        Manager.NextUniqueIdPrefix();

        // Check if this is a static page
        // It seems if we can handle a page as a content replacement, that's better than a static page, which reruns all javascript
        // If it turns out it's not a content page, we'll redirect to the static page
        //if (CanProcessAsStaticPage(dataIn.Path)) { // if this is a static page, render as complete static page
        //    PageContentData cr = new PageContentData();
        //    cr.Redirect = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
        //    return cr;
        //}

        // if this is a url with segments (like http://...local.../segment/segment/segment/segment/segment/segment)
        // rewrite it to make it a proper querystring
        PageDefinition? pageFound = null;
        ModuleDefinition? moduleFound = null;
        {
            string url = dataIn.Path;
            string? newUrl, newQs;
            if (url.StartsWith(Globals.ModuleUrl, StringComparison.InvariantCultureIgnoreCase)) {
                PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 5, uri.Query, out newUrl, out newQs);
                if (newUrl != url) {
                    PageContentData cr = new PageContentData();
                    cr.RedirectContent = QueryHelper.ToUrl(newUrl, newQs);
                    return Results.Ok(cr);
                }
                ModuleDefinition? module = await ModuleDefinition.FindDesignedModuleAsync(dataIn.Path);
                if (module == null)
                    module = await ModuleDefinition.LoadByUrlAsync(dataIn.Path);
                moduleFound = module;
            } else if (url.StartsWith(Globals.PageUrl, StringComparison.InvariantCultureIgnoreCase)) {
                PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 5, uri.Query, out newUrl, out newQs);
                if (newUrl != url) {
                    PageContentData cr = new PageContentData();
                    cr.RedirectContent = QueryHelper.ToUrl(newUrl, newQs);
                    return Results.Ok(cr);
                }
                pageFound = await PageDefinition.LoadFromUrlAsync(url);
            } else {
                PageDefinition.PageUrlInfo pageInfo = await PageDefinition.GetPageUrlFromUrlWithSegmentsAsync(url, dataIn.QueryString);
                PageDefinition? page = pageInfo.Page;
                if (page != null) {
                    // we have a page, check if the URL was rewritten because it had human readable arguments
                    if (pageInfo.NewUrl != url) {
                        PageContentData cr = new PageContentData();
                        cr.RedirectContent = QueryHelper.ToUrl(pageInfo.NewUrl, pageInfo.NewQS);
                        return Results.Ok(cr);
                    }
                    pageFound = page;
                } else {
                    // direct urls don't participate in unified page sets
                    PageContentData cr = new PageContentData();
                    cr.Redirect = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
                    return Results.Ok(cr);
                }
            }
        }

        Logging.AddLog("Page Content");

        // redirect current request to two-step authentication setup
        if (Manager.Need2FA) {
            if (Manager.Need2FARedirect) {
                Logging.AddLog("Two-step authentication setup required");
                ModuleAction action2FA = await Resource.ResourceAccess.GetForceTwoStepActionSetupAsync(null);
                Manager.Need2FARedirect = false;
                Manager.OriginList.Add(new Origin() { Url = uri.ToString() });// where to go after setup
                Manager.OriginList.Add(new Origin() { Url = action2FA.GetCompleteUrl() }); // setup
                PageContentData cr = new PageContentData();
                string returnUrl = Manager.ReturnToUrl;
                if (returnUrl.StartsWith("/"))
                    cr.RedirectContent = returnUrl;
                else
                    cr.Redirect = returnUrl;
                return Results.Ok(cr);
            }
            // this shouldn't be necessary because the first page shown in the unified page set would have generated this
            //Resource.ResourceAccess.ShowNeed2FA();
        }
        if (Manager.NeedNewPassword) {
            // this shouldn't be necessary because the first page shown in the unified page set would have generated this
            //Resource.ResourceAccess.ShowNeedNewPassword();
        }

        // Process the page
        if (pageFound != null) {
            PageContentData cr = new PageContentData();
            switch (await CanProcessAsDesignedPageAsync(pageFound, dataIn, cr)) {
                case ProcessingStatus.Complete:
                    return Results.Ok(cr);
                case ProcessingStatus.Page:
                    return await GetPageResultAsync(context, dataIn);
                default:
                case ProcessingStatus.No:
                    break;
            }
        }
        if (moduleFound != null) {
            PageContentData cr = new PageContentData();
            switch (CanProcessAsModule(moduleFound)) {
                case ProcessingStatus.Complete:
                    return Results.Ok(cr);
                case ProcessingStatus.Page:
                    return await GetPageResultAsync(context, dataIn);
                default:
                case ProcessingStatus.No:
                    break;
            }
        }
        // if we got here, we shouldn't be here - we're requesting a page outside of unified page set
        {
            PageContentData cr = new PageContentData();
            cr.Redirect = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString);
            return Results.Ok(cr);
        }
    }

    internal enum ProcessingStatus {
        No = 0, // not processed, try something else
        Page = 1, // Page has been set up
        Complete = 2,// no more processing is needed
    }
    //private bool CanProcessAsStaticPage(string localUrl) {
    //    if (Manager.CurrentSite.StaticPages && !Manager.HaveUser && !Manager.EditMode && Manager.CurrentSite.AllowAnonymousUsers) {
    //        if (Manager.StaticPageManager.HavePage(localUrl))
    //            return true;
    //    }
    //    return false;
    //}
    private static async Task<ProcessingStatus> CanProcessAsDesignedPageAsync(PageDefinition page, DataIn dataIn, PageContentData cr) {
        // request for a designed page
        if (!string.IsNullOrWhiteSpace(page.RedirectToPageUrl)) {
            if (page.RedirectToPageUrl.StartsWith("/") && page.RedirectToPageUrl.IndexOf('?') < 0) {
                PageDefinition? redirectPage = await PageDefinition.LoadFromUrlAsync(page.RedirectToPageUrl);
                if (redirectPage != null) {
                    if (string.IsNullOrWhiteSpace(redirectPage.RedirectToPageUrl)) {
                        string redirUrl = Manager.CurrentSite.MakeUrl(QueryHelper.ToUrl(page.RedirectToPageUrl, dataIn.QueryString));
                        Logging.AddLog("302 Found - Redirect to {0}", redirUrl).Truncate(100);
                        cr.RedirectContent = redirUrl;
                        return ProcessingStatus.Complete;
                    } else
                        throw new InternalError("Page {0} redirects to page {1}, which redirects to page {2}", page.Url, page.RedirectToPageUrl, redirectPage.RedirectToPageUrl);
                }
            } else {
                // redirect elsewhere
                Logging.AddLog("302 Found - Redirect to {0}", page.RedirectToPageUrl).Truncate(100);
                if (page.RedirectToPageUrl.StartsWith("/"))
                    cr.RedirectContent = page.RedirectToPageUrl;
                else
                    cr.Redirect = page.RedirectToPageUrl;
                return ProcessingStatus.Complete;
            }
        }
        if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers || string.Compare(dataIn.Path, Manager.CurrentSite.LoginUrl, true) == 0) && page.IsAuthorized_View()) {
            // if the requested page is for desktop but we're on a mobile device, find the correct page to display
            if (dataIn.IsMobile && !string.IsNullOrWhiteSpace(page.MobilePageUrl)) {
                PageDefinition? mobilePage = await PageDefinition.LoadFromUrlAsync(page.MobilePageUrl);
                if (mobilePage != null) {
                    if (string.IsNullOrWhiteSpace(mobilePage.MobilePageUrl)) {
                        string redirUrl = page.MobilePageUrl;
                        Logging.AddLog("302 Found - {0}", redirUrl).Truncate(100);
                        redirUrl = QueryHelper.ToUrl(redirUrl, dataIn.QueryString);
                        cr.RedirectContent = redirUrl;
                        return ProcessingStatus.Complete;
                    }
#if DEBUG
                    else
                        throw new InternalError("Page {0} redirects to mobile page {1}, which redirects to mobile page {2}", page.Url, page.MobilePageUrl, mobilePage.MobilePageUrl);
#endif
                }
            }
            Manager.CurrentPage = page;// Found It!!
            if (PageContentEndpoints.GoingToPopup()) {
                // this is a popup request
                Manager.IsInPopup = true;
            }
            Logging.AddTraceLog("Page {0}", page.PageGuid);
            return ProcessingStatus.Page;
        } else {
            // Send to login page with redirect (IF NOT LOGGED IN)
            if (!Manager.HaveUser) {
                Manager.OriginList.Clear();
                Manager.OriginList.Add(new Origin() { Url = QueryHelper.ToUrl(dataIn.Path, dataIn.QueryString) });
                QueryHelper qh = QueryHelper.FromUrl(Manager.CurrentSite.LoginUrl, out string loginUrl);
                qh.Add("__f", "true");// add __f=true to login url so we get a 401
                Manager.OriginList.Add(new Origin() { Url = qh.ToUrl(loginUrl) });
                string retUrl = Manager.ReturnToUrl;
                Logging.AddLog("Redirect - {0}", retUrl);
                if (retUrl.StartsWith("/"))
                    cr.RedirectContent = retUrl;
                else
                    cr.Redirect = retUrl;
                return ProcessingStatus.Complete;
            } else {
                cr.Status = Logging.AddErrorLog("403 Not Authorized");
                return ProcessingStatus.Complete;
            }
        }
    }
    private static ProcessingStatus CanProcessAsModule(ModuleDefinition module) {
        // direct request for a module without page
        if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers) && module.IsAuthorized(ModuleDefinition.RoleDefinition.View)) {
            PageDefinition page = PageDefinition.Create();
            page.AddModule(Globals.MainPane, module);
            Manager.CurrentPage = page;
            if (PageContentEndpoints.GoingToPopup()) {
                // we're going into a popup for this
                Manager.IsInPopup = true;
                page.PopupPage = module.PopupPage;
            }
            Logging.AddTraceLog("Module {0}", module.ModuleGuid);
            return ProcessingStatus.Page;
        }
        return ProcessingStatus.No;
    }

    private static async Task<IResult> GetPageResultAsync(HttpContext context, DataIn dataIn) {
        PageContentData cr = new PageContentData();

        try {

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Manager.PageTitle = Manager.CurrentPage.Title;

            SkinAccess skinAccess = new SkinAccess();

            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
            Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentPage.ReferencedModules);

            // set popup info
            PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
            if (Manager.IsInPopup) {
                Manager.ScriptManager.AddVolatileOption("Skin", "PopupWidth", pageSkin.Width);// Skin size in a popup window
                Manager.ScriptManager.AddVolatileOption("Skin", "PopupHeight", pageSkin.Height);
                Manager.ScriptManager.AddVolatileOption("Skin", "PopupMaximize", pageSkin.MaximizeButton);
                Manager.ScriptManager.AddVolatileOption("Skin", "PopupCss", pageSkin.CSS);
            }

            YHtmlHelper htmlHelper = new YHtmlHelper(new Microsoft.AspNetCore.Mvc.ActionContext(), null);//$$$$$ remove this garbage
            await Manager.CurrentPage.RenderPaneContentsAsync(htmlHelper, dataIn, cr);

            //Manager.PopCharSize();

            if (Manager.UniqueIdCounters.IsTracked)
                Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdCounters", Manager.UniqueIdCounters);

            Manager.ScriptManager.AddVolatileOption("Basics", "OriginList", Manager.OriginList ?? new List<Origin>());

            Manager.ScriptManager.AddVolatileOption("Basics", "PageGuid", Manager.CurrentPage.PageGuid);
            Manager.ScriptManager.AddVolatileOption("Basics", "TemporaryPage", Manager.CurrentPage.Temporary);
            ModuleDefinitionExtensions.AddVolatileOptionsUniqueModuleAddOns();

            await Manager.CssManager.RenderAsync(cr, dataIn.KnownCss);
            await Manager.ScriptManager.RenderAsync(cr, dataIn.KnownScripts);
            Manager.ScriptManager.RenderEndofPageScripts(cr);

            if (YetaWFManager.Deployed) {
                if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.AnalyticsContent))
                    cr.AnalyticsContent = Manager.CurrentPage.AnalyticsContent;
                else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.AnalyticsContent))
                    cr.AnalyticsContent = Manager.CurrentSite.AnalyticsContent;
                if (!string.IsNullOrWhiteSpace(cr.AnalyticsContent))
                    cr.AnalyticsContent = cr.AnalyticsContent.Replace("<<Url>>", Utility.JserEncode(Manager.CurrentPage.EvaluatedCanonicalUrl));
            }
            cr.PageTitle = Manager.PageTitle.ToString();
            cr.PageCssClasses = Manager.CurrentPage.GetCssClass();
            cr.CanonicalUrl = Manager.CurrentPage.EvaluatedCanonicalUrl;
            if (cr.CanonicalUrl != null) {
                UriBuilder ub = new UriBuilder(cr.CanonicalUrl);
                cr.LocalUrl = QueryHelper.ToUrl(ub.Path, ub.Query);
            }
        } catch (Exception exc) {
            cr.Status = Logging.AddErrorLog(ErrorHandling.FormatExceptionMessage(exc));
        }
        return Results.Ok(cr);
    }

    public static async Task SetupEnvironmentInfoAsync() {

        if (!Manager.LocalizationSupportEnabled) {// this only needs to be done once, so we gate on LocalizationSupportEnabled
            Manager.IsInPopup = InPopup();
            Manager.OriginList = GetOriginList();
            Manager.PageControlShown = PageControlShown();

            // determine user identity - authentication provider updates Manager with user information
            await Resource.ResourceAccess.ResolveUserAsync();

            // get user's default language
            Manager.GetUserLanguage();
            // only now can we enable resource loading
            Manager.LocalizationSupportEnabled = true;
        }
    }

    internal static bool GoingToPopup() {
        string? toPopup = null;
        try {
            toPopup = Manager.RequestQueryString[Globals.Link_ToPopup];
        } catch (Exception) { }
        return toPopup != null;
    }
    internal static bool InPopup() {
        string? inPopup = null;
        try {
            inPopup = Manager.RequestQueryString[Globals.Link_InPopup];
        } catch (Exception) { }
        return inPopup != null;
    }
    internal static bool PageControlShown() {
        string? pageControlShown = null;
        try {
            pageControlShown = Manager.RequestQueryString[Globals.Link_PageControl];
        } catch (Exception) { }
        return pageControlShown != null;
    }
    internal static bool GetTempEditMode() {
        if (!Manager.HaveUser)
            return false;
        try {
            string? editMode = Manager.RequestQueryString[Globals.Link_EditMode];
            if (editMode != null)
                return true;
        } catch (Exception) { }
        return false;
    }
    internal static List<Origin> GetOriginList() {

        // Get info where we came from for return handling. We append the originlist when we
        // use links within our site. (We don't use UrlReferrer or the browser's history).
        // We're saving the origin list so we can return there once a form is completed (saved)
        // Because it relies on our own information it only works if we're navigating within our site.
        // If the user enters a direct Url or we can't determine where we're coming from, we usually use
        // the home page to return to.
        string? originList = null;
        try {
            originList = Manager.RequestForm[Globals.Link_OriginList];
            if (originList == null)
                originList = Manager.RequestQueryString[Globals.Link_OriginList];
        } catch (Exception) { }
        if (!string.IsNullOrWhiteSpace(originList)) {
            try {
                return Utility.JsonDeserialize<List<Origin>>(originList);
            } catch (Exception) {
                throw new InternalError("Invalid Url arguments");
            }
        } else
            return new List<Origin>();
    }
}
