/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.Extensions;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.ResponseFilter;
using YetaWF.Core.Site;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Support.UrlHistory;

namespace YetaWF.Core.Endpoints;

/// <summary>
/// Endpoint for all page requests within YetaWF that only need the content rendered (used client-side to replace module content only).
/// </summary>
public class PageEndpoints : YetaWFEndpoints {

    public static void RegisterEndpoints(IEndpointRouteBuilder endpoints, Package package, string areaName) {
        endpoints.MapMethods("/", new List<string> { "GET", "HEAD" }, async (HttpContext context) => {
            return await Show(context);
        });

        endpoints.MapMethods("/{*rest}", new List<string> { "GET", "HEAD" }, async (HttpContext context, string rest) => {
            return await Show(context);
        });
    }

    private static async Task<IResult> Show(HttpContext context) {
        // We come here for ANY page request (GET, HEAD only)

        if (!YetaWFManager.HaveManager)
            throw new InternalError("No manager available");

        HttpRequest request = Manager.CurrentContext.Request;
        Manager.CurrentUrl = QueryHelper.ToUrl(request.Path, request.QueryString.Value);

        SiteDefinition site = Manager.CurrentSite;
        Uri uri = new Uri(Manager.CurrentRequestUrl);
        Manager.RenderContentOnly = false;

        // process logging type callbacks
        await PageLogging.HandleCallbacksAsync(Manager.CurrentRequestUrl, false);

        // Legacy browser
        if (CssLegacy.IsLegacyBrowser()) {
            if (!CssLegacy.SupportLegacyBrowser()) {
                string url = Manager.CurrentSite.UnsupportedBrowserUrl ?? Manager.CurrentSite.HomePageUrl;
                Logging.AddLog("302 Found - Legacy {0}",url).Truncate(100);
                return Results.Redirect(url);
            }
        }

        // Mobile detection
        bool isMobile;
        // The reason we need to do this is because we may or may not want to go into a popup
        // The only time we go into a popup without knowing whether we're on a mobile device is
        // through a client-side action request (popup.js). The CLIENT should determine whether we're in a mobile
        // device. Popups are limited by screen size so a simple screen size check would suffice.
        // However, if we want to redirect to a mobile site (different Url) for a first-time request, we need to
        // analyze headers (user-agent).
        isMobile = IsMobileBrowser(Manager.CurrentRequest);
        //Manager.ActiveDevice = IsMobileBrowser(Manager.CurrentRequest) ? YetaWFManager.DeviceSelected.Mobile : YetaWFManager.DeviceSelected.Desktop;
        if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Undecided) {
            if (isMobile) {
                Manager.ActiveDevice = YetaWFManager.DeviceSelected.Mobile;
                if (!string.IsNullOrEmpty(site.MobileSiteUrl)) {
                    if (uri.IsAbsoluteUri) {
                        if (!Manager.IsLocalHost && string.Compare(uri.AbsoluteUri, site.MobileSiteUrl, true) != 0) {
                            Logging.AddLog("301 Moved Permanently - {0}", site.MobileSiteUrl).Truncate(100);
                            return Results.Redirect(site.MobileSiteUrl, permanent: true);
                        }
                    }
                }
            }
            Manager.ActiveDevice = YetaWFManager.DeviceSelected.Desktop;
        } else {
            Manager.ActiveDevice = isMobile ? YetaWFManager.DeviceSelected.Mobile : YetaWFManager.DeviceSelected.Desktop;
        }

        if (site.IsLockedAny && !string.IsNullOrWhiteSpace(site.GetLockedForIP()) && !string.IsNullOrWhiteSpace(site.LockedUrl) &&
                Manager.UserHostAddress != site.GetLockedForIP() && Manager.UserHostAddress != "127.0.0.1" &&
                string.Compare(uri.AbsolutePath, site.LockedUrl, true) != 0) {
            Logging.AddLog("302 Found - {0}", site.LockedUrl);
            return Results.Redirect(site.LockedUrl);
        }

        if (SiteDefinition.INITIAL_INSTALL) {
            string initPageUrl = "/!api/YetaWF_Packages/StartupPage/Show";
            string initPageQs = "";
            string newUrl;
            if (!Manager.IsLocalHost)
                // if we're using IIS we need to use the real domain which we haven't saved yet in Site Properties
                newUrl = Manager.CurrentSite.MakeUrl(initPageUrl + initPageQs, RealDomain: Manager.HostUsed);
            else
                newUrl = Manager.CurrentSite.MakeUrl(initPageUrl + initPageQs);
            if (string.Compare(uri.LocalPath, initPageUrl, true) != 0) {
                Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                return Results.Redirect(newUrl);
            }
        }

        // Check if site language requested using !yLang= arg
        string? lang = Manager.CurrentRequest.Query[Globals.Link_Language];
        if (!string.IsNullOrWhiteSpace(lang))
            await Manager.SetUserLanguageAsync(lang);

        // Check if home URL is requested and matches site's desired home URL
        if (uri.AbsolutePath == "/") {
            if (Manager.CurrentSite.HomePageUrl != "/") {
                string newUrl = Manager.CurrentSite.MakeUrl(Manager.CurrentSite.HomePageUrl);
                Logging.AddLog("302 Found - {0}", newUrl).Truncate(100);
                return Results.Redirect(newUrl);
            }
        }

        // http/https
        if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite) {
            switch (Manager.CurrentSite.EvaluatedPageSecurity) {
                default:
                case PageSecurityType.AsProvided:
                    // it's all good
                    break;
                case PageSecurityType.NoSSLOnly:
                    if (uri.Scheme == "https") {
                        UriBuilder newUri = new UriBuilder(uri);
                        newUri.Scheme = "http";
                        newUri.Port = Manager.CurrentSite.PortNumberEval;
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        return Results.Redirect(newUri.ToString());
                    }
                    break;
                case PageSecurityType.NoSSLOnlyAnonymous_LoggedOnhttps:
                    if (!Manager.HaveUser && uri.Scheme == "https") {
                        UriBuilder newUri = new UriBuilder(uri);
                        newUri.Scheme = "http";
                        newUri.Port = Manager.CurrentSite.PortNumberEval;
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        return Results.Redirect(newUri.ToString());
                    } else if (Manager.HaveUser && uri.Scheme != "https") {
                        UriBuilder newUri = new UriBuilder(uri);
                        newUri.Scheme = "https";
                        newUri.Port = Manager.CurrentSite.PortNumberSSLEval;
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        return Results.Redirect(newUri.ToString());
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
                        Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                        return Results.Redirect(newUri.ToString());
                    }
                    break;
                case PageSecurityType.UsePageModuleSettings:
                    // to be determined when loading page/module
                    break;
            }
        }

        Logging.AddLog("Page");

        // Check if it's a built-in command (mostly for debugging and initial install) and build a page dynamically (which is not saved)
        Func<QueryHelper, Task>? action = await BuiltinCommands.FindAsync(uri.LocalPath, checkAuthorization: true);
        if (action != null) {
            if (Manager.IsHeadRequest)
                return Results.Empty;
            Manager.CurrentPage = PageDefinition.Create();
            QueryHelper qh = QueryHelper.FromQueryString(uri.Query);
            await action(qh);
            return Results.Empty;
        }

        // Process the page
        CanProcessAsStaticPageInfo info = await CanProcessAsStaticPageAsync(uri);
        if (info.Success) {
            AddStandardHeaders();
            return Results.Text(info.Contents, "text/html");
        }

        // if this is a url with segments (like http://...local.../segment/segment/segment/segment/segment/segment)
        // rewrite it to make it a proper querystring
        PageDefinition? pageFound = null;
        ModuleDefinition? moduleFound = null;
        {
            string url = uri.LocalPath;
            string newUrl, newQs;
            if (url.StartsWith(Globals.ModuleUrl, StringComparison.InvariantCultureIgnoreCase)) {
                PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl, out newQs);
                if (newUrl != url) {
                    Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
                    context.Request.Path = newUrl;
                    context.Request.QueryString = QueryHelper.MakeQueryString(newQs);
                    uri = new Uri(Manager.CurrentRequestUrl);
                }
                ModuleDefinition? module = await ModuleDefinition.FindDesignedModuleAsync(url);
                if (module == null)
                    module = await ModuleDefinition.LoadByUrlAsync(url);
                moduleFound = module;
            } else if (url.StartsWith(Globals.PageUrl, StringComparison.InvariantCultureIgnoreCase)) {
                PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 3, uri.Query, out newUrl, out newQs);
                if (newUrl != url) {
                    Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
                    context.Request.Path = newUrl;
                    context.Request.QueryString = QueryHelper.MakeQueryString(newQs);
                    uri = new Uri(Manager.CurrentRequestUrl);
                }
                pageFound = await PageDefinition.LoadFromUrlAsync(url);
            } else {
                PageDefinition.PageUrlInfo pageInfo = await PageDefinition.GetPageUrlFromUrlWithSegmentsAsync(url, uri.Query);
                PageDefinition? page = pageInfo.Page;
                if (page != null) {
                    // we have a page, check if the URL was rewritten because it had human readable arguments
                    if (pageInfo.NewUrl != url) {
                        Logging.AddTraceLog("Server.TransferRequest - {0}", pageInfo.NewUrl);
                        context.Request.Path = pageInfo.NewUrl;
                        context.Request.QueryString = QueryHelper.MakeQueryString(pageInfo.NewQS);
                        uri = new Uri(Manager.CurrentRequestUrl);
                    }
                    pageFound = page;
                } else {
                    // we have a direct url, make sure it's exactly 4 segments otherwise rewrite the remaining args as non-human readable qs args
                    // don't rewrite css/scss/less file path - we handle that in a http handler
                    PageDefinition.GetUrlFromUrlWithSegments(url, uri.Segments, 4, uri.Query, out newUrl, out newQs);
                    if (newUrl != url) {
                        Logging.AddTraceLog("Server.TransferRequest - {0}", newUrl);
                        context.Request.Path = newUrl;
                        context.Request.QueryString = QueryHelper.MakeQueryString(newQs);
                        uri = new Uri(Manager.CurrentRequestUrl);
                    }
                }
            }
        }

        // redirect current request to two-step authentication setup
        if (Manager.Need2FA) {
            if (Manager.Need2FARedirect) {
                Logging.AddLog("Two-step authentication setup required");
                ModuleAction action2FA = await Resource.ResourceAccess.GetForceTwoStepActionSetupAsync(null, uri.ToString());
                Manager.Need2FARedirect = false;
                return Results.Redirect(action2FA.GetCompleteUrl());
            }
            Resource.ResourceAccess.ShowNeed2FA();
        }
        if (Manager.NeedNewPassword) {
            Resource.ResourceAccess.ShowNeedNewPassword();
        }

        if (pageFound != null) {
            switch (await CanProcessAsDesignedPageAsync(pageFound, uri)) {
                case ProcessingStatus.Complete:
                    return Results.Empty;
                case ProcessingStatus.Page:
                    if (Manager.IsHeadRequest)
                        return Results.Empty;
                    AddStandardHeaders();
                    return await GetPageResultAsync(context);
                case ProcessingStatus.No:
                    break;
            }
        }
        if (moduleFound != null) {
            switch (CanProcessAsModule(moduleFound, uri.LocalPath, uri.Query)) {
                case ProcessingStatus.Complete:
                    return Results.Empty;
                case ProcessingStatus.Page:
                    if (Manager.IsHeadRequest)
                        return Results.Empty;
                    AddStandardHeaders();
                    return await GetPageResultAsync(context);
                case ProcessingStatus.No:
                    break;
            }
        }

        // if we got here, we shouldn't be here

        // display 404 error page if one is defined
        if (!string.IsNullOrWhiteSpace(site.NotFoundUrl)) {
            PageDefinition? page = await PageDefinition.LoadFromUrlAsync(site.NotFoundUrl);
            if (page != null) {
                Manager.CurrentPage = page;
                if (Manager.IsHeadRequest)
                    return Results.NotFound(Manager.CurrentUrl);
                AddStandardHeaders();
                Logging.AddErrorLog("404 Not Found");
                Manager.CurrentResponse.StatusCode = StatusCodes.Status404NotFound;
                return await GetPageResultAsync(context);
            }
        }
        return Results.NotFound(Manager.CurrentUrl);
    }

    private static void AddStandardHeaders() {

        // X-Frame-Options

        string? option = null;
        if (Manager.CurrentPage != null) {
            if (Manager.CurrentPage.IFrameUse == PageDefinition.IFrameUseEnum.Default) {
                if (Manager.CurrentSite.IFrameUse == IFrameUseEnum.No)
                    option = "DENY";
                else if (Manager.CurrentSite.IFrameUse == IFrameUseEnum.ThisSite)
                    option = "SAMEORIGIN";
            } else if (Manager.CurrentPage.IFrameUse == PageDefinition.IFrameUseEnum.No) {
                option = "DENY";
            } else if (Manager.CurrentPage.IFrameUse == PageDefinition.IFrameUseEnum.ThisSite) {
                option = "SAMEORIGIN";
            }
            if (option != null) {
                if (PageContentEndpoints.GoingToPopup())
                    option = "SAMEORIGIN";// we need at least this to go into a popup
                Manager.CurrentResponse.Headers.Add("X-Frame-Options", option);
            }
        }

        // X-Content-Type-Options

        if (Manager.CurrentSite.ContentTypeOptions == ContentTypeEnum.NoSniff)
            Manager.CurrentResponse.Headers.Add("X-Content-Type-Options", "nosniff");

        // Strict-Transport-Security (HSTS)
        if (YetaWFManager.Deployed) {
            if (Manager.CurrentSite.StrictTransportSecurity == StrictTransportSecurityEnum.All) {
                Manager.CurrentResponse.Headers.Add("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
            }
        }
    }

    internal enum ProcessingStatus {
        No = 0, // not processed, try something else
        Page = 1, // Page has been set up
        Complete = 2,// no more processing is needed
    }
    internal class CanProcessAsStaticPageInfo {
        public string? Contents { get; set; }
        public bool Success { get; set; }
    }
    private static async Task<CanProcessAsStaticPageInfo> CanProcessAsStaticPageAsync(Uri uri) {
        if (Manager.CurrentSite.StaticPages && !Manager.HaveUser && !PageContentEndpoints.GetTempEditMode() && Manager.CurrentSite.AllowAnonymousUsers && string.Compare(Manager.HostUsed, Manager.CurrentSite.SiteDomain, true) == 0) {
            // support static pages for exact domain match only (so other sites, like blue/green don't use static pages)
            string localUrl = uri.LocalPath;
            if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Desktop && PageContentEndpoints.GoingToPopup()) {
                // we're going into a popup for this
                Manager.IsInPopup = true;
            }
            Core.Support.StaticPages.StaticPageManager.GetPageInfo info = await Manager.StaticPageManager.GetPageAsync(localUrl);
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
    private static async Task<ProcessingStatus> CanProcessAsDesignedPageAsync(PageDefinition page, Uri uri) {
        // request for a designed page
        if (!PageContentEndpoints.GetTempEditMode()) { // only redirect if we're not editing
            if (!string.IsNullOrWhiteSpace(page.RedirectToPageUrl)) {
                if (page.RedirectToPageUrl.StartsWith("/") && page.RedirectToPageUrl.IndexOf('?') < 0) {
                    PageDefinition? redirectPage = await PageDefinition.LoadFromUrlAsync(page.RedirectToPageUrl);
                    if (redirectPage != null) {
                        if (string.IsNullOrWhiteSpace(redirectPage.RedirectToPageUrl)) {
                            string redirUrl = Manager.CurrentSite.MakeUrl(page.RedirectToPageUrl + uri.Query);
                            Logging.AddLog("302 Found - Redirect to {0}", redirUrl).Truncate(100);
                            Manager.CurrentResponse.StatusCode = StatusCodes.Status302Found;
                            Manager.CurrentResponse.Headers.Add("Location", redirUrl);
                            return ProcessingStatus.Complete;
                        } else
                            throw new InternalError("Page {0} redirects to page {1}, which redirects to page {2}", page.Url, page.RedirectToPageUrl, redirectPage.RedirectToPageUrl);
                    }
                } else {
                    Logging.AddLog("302 Found - Redirect to {0}", page.RedirectToPageUrl).Truncate(100);
                    Manager.CurrentResponse.StatusCode = StatusCodes.Status302Found;
                    Manager.CurrentResponse.Headers.Add("Location", page.RedirectToPageUrl);
                    return ProcessingStatus.Complete;
                }
            }
        }
        if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers || string.Compare(uri.LocalPath, Manager.CurrentSite.LoginUrl, true) == 0) && page.IsAuthorized_View()) {
            // if the requested page is for desktop but we're on a mobile device, find the correct page to display
            if (!PageContentEndpoints.GetTempEditMode()) { // only redirect if we're not editing
                if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Mobile && !string.IsNullOrWhiteSpace(page.MobilePageUrl)) {
                    PageDefinition? mobilePage = await PageDefinition.LoadFromUrlAsync(page.MobilePageUrl);
                    if (mobilePage != null) {
                        if (string.IsNullOrWhiteSpace(mobilePage.MobilePageUrl)) {
                            string redirUrl = page.MobilePageUrl;
                            if (redirUrl.StartsWith("/"))
                                redirUrl = Manager.CurrentSite.MakeUrl(redirUrl + uri.Query);
                            Logging.AddLog("302 Found - {0}", redirUrl).Truncate(100);
                            Manager.CurrentResponse.StatusCode = StatusCodes.Status302Found;
                            Manager.CurrentResponse.Headers.Add("Location", redirUrl);
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
            if (PageContentEndpoints.GetTempEditMode() && Manager.CurrentPage.IsAuthorized_Edit())
                Manager.EditMode = true;

            if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Desktop && PageContentEndpoints.GoingToPopup()) {
                // we're going into a popup for this
                Manager.IsInPopup = true;
            }
            bool usePageSettings = false;
            if (!Manager.IsTestSite && !Manager.IsLocalHost && !YetaWFManager.IsHTTPSite) {
                switch (Manager.CurrentSite.EvaluatedPageSecurity) {
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
                            Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                            Manager.CurrentResponse.StatusCode = StatusCodes.Status302Found;
                            Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
                            return ProcessingStatus.Complete;
                        }
                        break;
                    case PageDefinition.PageSecurityType.httpOnly:
                        if (Manager.HostSchemeUsed != "http") {
                            UriBuilder newUri = new UriBuilder(Manager.CurrentRequestUrl);
                            newUri.Scheme = "http";
                            newUri.Port = Manager.CurrentSite.PortNumberEval;
                            Logging.AddLog("302 Found - {0}", newUri.ToString()).Truncate(100);
                            Manager.CurrentResponse.StatusCode = StatusCodes.Status302Found;
                            Manager.CurrentResponse.Headers.Add("Location", newUri.ToString());
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
                QueryHelper qh = QueryHelper.FromUrl(Manager.CurrentSite.LoginUrl, out string loginUrl);
                qh.Add("__f", "true");// add __f=true to login url so we get a 401
                qh.Add("returnUrl", uri.ToString());
                string retUrl = qh.ToUrl(loginUrl);
                Logging.AddLog("Redirect - {0}", retUrl);
                Manager.CurrentResponse.Redirect(retUrl);
                return ProcessingStatus.Complete;
            } else {
                return ProcessingStatus.No;// end up as 404 not found
            }
        }
    }
    private static ProcessingStatus CanProcessAsModule(ModuleDefinition module, string url, string queryString) {
        // direct request for a module without page
        if ((Manager.HaveUser || Manager.CurrentSite.AllowAnonymousUsers) && module.IsAuthorized(ModuleDefinition.RoleDefinition.View)) {
            PageDefinition page = PageDefinition.Create();
            page.AddModule(Globals.MainPane, module);
            Manager.CurrentPage = page;
            if (PageContentEndpoints.GetTempEditMode() && Manager.CurrentPage.IsAuthorized_Edit())
                Manager.EditMode = true;
            if (Manager.ActiveDevice == YetaWFManager.DeviceSelected.Desktop && PageContentEndpoints.GoingToPopup()) {
                // we're going into a popup for this
                Manager.IsInPopup = true;
                page.PopupPage = module.PopupPage;
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

    private static async Task<IResult> GetPageResultAsync(HttpContext context) {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        PageDefinition requestedPage = Manager.CurrentPage;
        Manager.PageTitle = requestedPage.Title;

        Manager.ScriptManager.AddVolatileOption("Basics", "PageGuid", requestedPage.PageGuid);
        Manager.ScriptManager.AddVolatileOption("Basics", "TemporaryPage", requestedPage.Temporary);
        Manager.ScriptManager.AddVolatileOption("Basics", Basics.AntiforgeryRequestToken, Manager.AntiforgeryRequestToken);

        bool staticPage = false;
        if (YetaWFManager.Deployed)
            staticPage = requestedPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && !Manager.HaveUser && string.Compare(Manager.HostUsed, Manager.CurrentSite.SiteDomain, true) == 0;
        Manager.RenderStaticPage = staticPage;

        SkinAccess skinAccess = new SkinAccess();
        string pageViewName = skinAccess.GetViewName(requestedPage.PopupPage);
        SkinDefinition skin = Manager.CurrentSite.Skin;
        string skinCollection = skin.Collection!;

        Manager.AddOnManager.AddExplicitlyInvokedModules(Manager.CurrentSite.ReferencedModules);
        Manager.AddOnManager.AddExplicitlyInvokedModules(requestedPage.ReferencedModules);


        // set new character dimensions and popup info
        PageSkinEntry pageSkin = skinAccess.GetPageSkinEntry();
        if (Manager.IsInPopup) {
            Manager.ScriptManager.AddVolatileOption("Skin", "PopupWidth", pageSkin.Width);// Skin size in a popup window
            Manager.ScriptManager.AddVolatileOption("Skin", "PopupHeight", pageSkin.Height);
            Manager.ScriptManager.AddVolatileOption("Skin", "PopupMaximize", pageSkin.MaximizeButton);
        }
        Manager.LastUpdated = requestedPage.Updated;

        // Skins first. Skins can/should really only add CSS files.
        await Manager.AddOnManager.AddSkinAsync(skinCollection, Manager.CurrentSite.Theme ?? SiteDefinition.DefaultTheme);
        await YetaWFCoreRendering.Render.AddStandardAddOnsAsync();

        Manager.ScriptManager.AddVolatileOption("Skin", "MinWidthForPopups", Manager.SkinInfo.MinWidthForPopups);
        Manager.ScriptManager.AddVolatileOption("Skin", "MinWidthForCondense", Manager.SkinInfo.MinWidthForCondense);

        await YetaWFCoreRendering.Render.AddSkinAddOnsAsync();
        await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF_Core", "SkinBasics");

        YHtmlHelper htmlHelper = new YHtmlHelper(null);
        string pageHtml = await htmlHelper.ForPageAsync(pageViewName);

        Manager.ScriptManager.AddLast("$YetaWF", "$YetaWF.initPage();");// end of page, initialization - this is the first thing that runs
        pageHtml = ProcessInlineScripts(pageHtml);

        await Manager.AddOnManager.AddSkinCustomizationAsync(skinCollection);

        if (Manager.UniqueIdCounters.IsTracked)
            Manager.ScriptManager.AddVolatileOption("Basics", "UniqueIdCounters", Manager.UniqueIdCounters);
        Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedCssBundleFiles", Manager.CssManager.GetBundleFiles());
        Manager.ScriptManager.AddVolatileOption("Basics", "UnifiedScriptBundleFiles", Manager.ScriptManager.GetBundleFiles());
        ModuleDefinitionExtensions.AddVolatileOptionsUniqueModuleAddOns(MarkPrevious: true);

        PageProcessing pageProc = new PageProcessing(Manager);
        pageHtml = await pageProc.PostProcessHtmlAsync(pageHtml);
        pageHtml = WhiteSpaceResponseFilter.Compress(pageHtml);

        if (staticPage) {
            await Manager.StaticPageManager.AddPageAsync(requestedPage.Url, requestedPage.StaticPage == PageDefinition.StaticPageEnum.YesMemory, pageHtml, Manager.LastUpdated);
            // Last-Modified is dependent on which user is logged on (if any) and any module that generates data which changes each time will defeat last-modified
            // so is only helpful for static pages and can't be used for dynamic pages
            context.Response.Headers.Add("Last-Modified", string.Format("{0:R}", Manager.LastUpdated));
        } else if (Manager.HaveUser && requestedPage.StaticPage != PageDefinition.StaticPageEnum.No && Manager.CurrentSite.StaticPages && Manager.HostUsed.ToLower() == Manager.CurrentSite.SiteDomain.ToLower()) {
            // if we have a user for what would be a static page, we have to make sure the last modified date is set to override any previously
            // served page to the then anonymous user before he/she logged on.
            context.Response.Headers.Add("Last-Modified", string.Format("{0:R}", DateTime.UtcNow));
        }
        return Results.Text(pageHtml, "text/html");
    }

    /// <summary>
    /// Moves all &lt;script&gt;&lt;/script&gt; snippets to the end of the page.
    /// </summary>
    /// <param name="viewHtml">The contents of the view.</param>
    /// <returns>The contents of the view with all &lt;script&gt;&lt;/script&gt; snippets removed.</returns>
    /// <remarks>Components and views do NOT generate &lt;script&gt;&lt;/script&gt; tags. They must use Manager.ScriptManager.AddLast instead.
    /// This is only used to move &lt;script&gt;&lt;/script&gt; sections that were added in YetaWF.Text modules.
    /// </remarks>
    internal static string ProcessInlineScripts(string viewHtml) {
        // code snippets must use <script></script> (without any attributes)
        int pos = 0;
        for (; ; ) {
            int index = viewHtml.IndexOf("<script>", pos, StringComparison.Ordinal);
            if (index < 0)
                break;
            int endIndex = viewHtml.IndexOf("</script>", index + 8, StringComparison.Ordinal);
            if (endIndex < 0)
                throw new InternalError("Missing </script> in view");
            YetaWFManager.Manager.ScriptManager.AddLast(viewHtml.Substring(index + 8, endIndex - index - 8));
            viewHtml = viewHtml.Remove(index, endIndex + 9 - index);
            pos = index;
        }
        return viewHtml;
    }
}