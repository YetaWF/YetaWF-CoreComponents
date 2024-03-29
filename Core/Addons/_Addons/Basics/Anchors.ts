/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// Anchor handling, navigation

namespace YetaWF {

    export class Anchors {

        public constructor() { }

        /**
         * Handles all navigation using <a> tags.
         */
        public init() : void {

            // For an <a> link clicked, add the page we're coming from (not for popup links though)
            $YetaWF.registerEventHandlerBody("click", "a.yaction-link,area.yaction-link", (ev: Event): boolean => {

                // find the real anchor, ev.target was clicked but it may not be the anchor itself
                if (!ev.target) return true;
                let anchor = $YetaWF.elementClosestCond(ev.target as HTMLElement, "a,area") as HTMLAnchorElement;
                if (!anchor) return true;
                if ($YetaWF.getAttributeCond(anchor, "data-nohref") != null) return false;

                let url = anchor.href;

                // send tracking info
                if ($YetaWF.elementHasClass(anchor, "yTrack")) {
                    // find the unique skinvisitor module so we have antiforgery tokens and other context info
                    let f = $YetaWF.getElement1BySelectorCond(".YetaWF_Visitors_SkinVisitor.YetaWF_Visitors.yModule form");
                    if (f) {

                        let urlTrack = f.getAttribute("data-track");
                        if (!urlTrack) throw "data-track not defined";/*DEBUG*/

                        let uri = $YetaWF.parseUrl(urlTrack);
                        let data = { "url": url };
                        uri.addSearchSimpleObject(data);
                        uri.addFormInfo(f);

                        let request: XMLHttpRequest = new XMLHttpRequest();
                        request.open("POST", urlTrack, true);
                        request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                        request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                        request.send(uri.toFormData());
                        // no response handling
                    }
                }

                let uri = $YetaWF.parseUrl(url);
                if (uri.getPath().length === 0 || (!uri.getSchema().startsWith("http:") && !uri.getSchema().startsWith("https:"))) return true;

                // add status/visibility of page control module
                uri.removeSearch(YConfigs.Basics.Link_PageControl);
                if (YVolatile.Basics.PageControlVisible)
                    uri.addSearch(YConfigs.Basics.Link_PageControl, "y");

                // add our module context info (if requested)
                if (anchor.getAttribute(YConfigs.Basics.CssAddModuleContext) != null) {
                    if (!uri.hasSearch(YConfigs.Basics.ModuleGuid)) {
                        let guid = $YetaWF.getModuleGuidFromTag(anchor);
                        uri.addSearch(YConfigs.Basics.ModuleGuid, guid);
                    }
                }

                // first try to handle this as a link to the outer window (only used in a popup)
                if ($YetaWF.PopupsAvailable()) {
                    if ($YetaWF.Popups.handleOuterWindow(anchor))
                        return false;
                    // try to handle this as a popup link
                    if ($YetaWF.Popups.handlePopupLink(anchor))
                        return false;
                }

                // fix the url to include where we came from
                let target = anchor.getAttribute("target");
                if (!target || target === "" || target === "_self") {

                    if (anchor.getAttribute(YConfigs.Basics.CssSaveReturnUrl) != null) {

                        let originList = YVolatile.Basics.OriginList.slice(0);// copy saved originlist

                        // add where we currently are so we can save it in case we need to return to this page
                        let currUri = $YetaWF.parseUrl(window.location.href);
                        currUri.removeSearch(YConfigs.Basics.Link_OriginList);// remove originlist from current URL
                        currUri.removeSearch(YConfigs.Basics.Link_InPopup);// remove popup info from current URL

                        if (anchor.getAttribute(YConfigs.Basics.CssDontAddToOriginList) == null) {
                            let newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: $YetaWF.isInPopup() };
                            originList.push(newOrigin);
                            if (originList.length > 5)// only keep the last 5 urls
                                originList = originList.slice(originList.length - 5);
                        }
                        // now update url (where we're going with originlist)
                        uri.removeSearch(YConfigs.Basics.Link_OriginList);
                        if (originList.length > 0)
                            uri.addSearch(YConfigs.Basics.Link_OriginList, JSON.stringify(originList));
                    }
                    target = "_self";
                }

                anchor.href = uri.toUrl(); // update original href in case default handling takes place

                let cookieToReturn: number | null = null;
                let post: boolean = false;

                if (anchor.getAttribute(YConfigs.Basics.CookieDoneCssAttr) != null) {
                    cookieToReturn = (new Date()).getTime();
                    uri.removeSearch(YConfigs.Basics.CookieToReturn);
                    uri.addSearch(YConfigs.Basics.CookieToReturn, JSON.stringify(cookieToReturn));
                }
                if (anchor.getAttribute(YConfigs.Basics.PostAttr) != null)
                    post = true;

                url = anchor.href = uri.toUrl(); // update original href in case we let default handling take place

                if (cookieToReturn) {
                    // this is a file download
                    let confirm = anchor.getAttribute(YConfigs.Basics.CssConfirm);
                    if (confirm != null) {
                        $YetaWF.alertYesNo(confirm, undefined, (): void => {
                            window.location.assign(url);
                            $YetaWF.setLoading();
                            this.waitForCookie(cookieToReturn);
                        });
                        return false;
                    }
                    $YetaWF.setLoading();
                } else {
                    // if a confirmation is wanted, show it
                    // this means that it's posted by definition
                    let confirm = anchor.getAttribute(YConfigs.Basics.CssConfirm);
                    if (confirm) {
                        let anchorOwner = $YetaWF.getOwnerFromTag(anchor) || anchor;
                        $YetaWF.alertYesNo(confirm, undefined, (): void => {
                            this.postLink(url, anchorOwner, cookieToReturn);
                            let s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                            if (s)
                                $YetaWF.pleaseWait(s);
                        });
                        return false;
                    } else if (post) {
                        let s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                        if (s)
                            $YetaWF.pleaseWait(s);
                        let anchorOwner = $YetaWF.getOwnerFromTag(anchor) || anchor;
                        this.postLink(url, anchorOwner, cookieToReturn);
                        return false;
                    }
                }

                if (target === "_self") {
                    // add overlay if desired
                    let s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                    if (s)
                        $YetaWF.pleaseWait(s);
                }
                this.waitForCookie(cookieToReturn); // if any

                if (cookieToReturn) return true; // expecting cookie return
                if (uri.getHostName() !== "" && uri.getHostName() !== window.document.domain) return true; // wrong domain
                // if we're switching from https->http or from http->https don't use a unified page set
                if (!window.document.location) return true;
                if (!url.startsWith("http") || !window.document.location.href.startsWith("http")) return true; // neither http nor https
                if ((url.startsWith("http://") !== window.document.location.href.startsWith("http://")) ||
                    (url.startsWith("https://") !== window.document.location.href.startsWith("https://"))) return true; // switching http<>https

                if (target === "_self") {
                    // handle inplace content replacement if requested
                    let inplace: YetaWF.InplaceContents | undefined;
                    let contentTarget = $YetaWF.getAttributeCond(anchor, "data-contenttarget");
                    let contentPane = $YetaWF.getAttributeCond(anchor, "data-contentpane");
                    if (!contentPane) contentPane = "MainPane";
                    if (contentTarget) {
                        // get the requested Url
                        let origUri = $YetaWF.parseUrl(url);
                        origUri.removeSearch(YConfigs.Basics.Link_OriginList);// remove originlist from current URL
                        origUri.removeSearch(YConfigs.Basics.Link_InPopup);// remove popup info from current URL
                        let contentUrl = origUri.getSearch("!ContentUrl");
                        if (!contentUrl)
                            throw `In place content must have a !ContentUrl query string argument - ${url}`;
                        // remove noise from requested url
                        // build the new requested url
                        let uriBase = $YetaWF.parseUrl(window.location.href);
                        uriBase.removeSearch("!ContentUrl");
                        uriBase.addSearch("!ContentUrl", contentUrl);
                        inplace = { TargetTag: contentTarget, FromPane: contentPane, PageUrl: uriBase.toUrl(), ContentUrl: contentUrl };
                    }
                    if ($YetaWF.elementHasClass(anchor, "yIgnorePageChanged"))
                        return $YetaWF.ContentHandling.setContentForce(uri, true, undefined, inplace) === SetContentResult.NotContent;
                    else
                        return $YetaWF.ContentHandling.setContent(uri, true, undefined, inplace) === SetContentResult.NotContent;
                }
                return true;
            });
        }

        private waitForCookie(cookieToReturn: number | null) : void {
            if (cookieToReturn) {
                // check for cookie to see whether download started
                new CookieWait(cookieToReturn);
            }
        }
        private postLink(url: string, anchorOwner: HTMLElement | null, cookieToReturn: number | null) : void {
            this.waitForCookie(cookieToReturn);
            $YetaWF.post(url, "", (success: boolean, data: any) : void => { }, anchorOwner || undefined);
        }
    }

    class CookieWait {

        private cookiePattern: RegExp;
        private cookieTimer: number;

        constructor(cookieToReturn: number) {
            this.cookiePattern = new RegExp((YConfigs.Basics.CookieDone + "=" + cookieToReturn), "i");
            this.cookieTimer = setInterval((): void => { this.checkCookies(); }, 500);
        }
        private checkCookies(): boolean {
            if (document.cookie.search(this.cookiePattern) >= 0) {
                clearInterval(this.cookieTimer);
                $YetaWF.setLoading(false);// turn off loading indicator
                console.log("Download complete!!");
                return false;
            }
            console.log("File still downloading...", new Date().getTime());
            return true;
        }
    }
}