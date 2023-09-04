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
                        let formJson: FormInfoJSON|null = null;
                        if ($YetaWF.FormsAvailable())
                            formJson = $YetaWF.Forms.getJSONInfo(anchor);
                        $YetaWF.postJSONIgnore(uri, formJson, { Url: url }, null);
                    }
                }

                let uri = $YetaWF.parseUrl(url);
                if (uri.getPath().length === 0 || (!uri.getSchema().startsWith("http:") && !uri.getSchema().startsWith("https:"))) return true;

                if (uri.getHostName() !== "" && uri.getHostName() !== window.document.domain) return true; // not for this domain

                // add status/visibility of page control module
                uri.removeSearch(YConfigs.Basics.Link_PageControl);
                if (YVolatile.Basics.PageControlVisible)
                    uri.replaceSearch(YConfigs.Basics.Link_PageControl, "y");
                uri.replaceSearch(YConfigs.Basics.Link_CurrentUrl, window.document.location.href);

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
                if (!target || target === "" || target === "_self")
                    target = "_self";

                // add originating module guid

                if (!uri.hasSearch("__ModuleGuid")) {
                    if ($YetaWF.FormsAvailable()) {
                        const formJson = $YetaWF.Forms.getJSONInfo(anchor);
                        uri.addSearch("__ModuleGuid", formJson.ModuleGuid);
                    }
                }

                anchor.href = uri.toUrl(); // update original href in case default handling takes place

                let cookieToReturn: number | null = null;
                let post: boolean = false;

                if (anchor.getAttribute(YConfigs.Basics.CookieDoneCssAttr) != null) {
                    cookieToReturn = (new Date()).getTime();
                    uri.replaceSearch(YConfigs.Basics.CookieToReturn, JSON.stringify(cookieToReturn));
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
                            this.postLink(url, anchorOwner, anchor, cookieToReturn);
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
                        this.postLink(url, anchorOwner, anchor, cookieToReturn);
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
                        origUri.removeSearch(YConfigs.Basics.Link_InPopup);// remove popup info from current URL
                        let contentUrl = origUri.getSearch("!ContentUrl");
                        if (!contentUrl)
                            throw `In place content must have a !ContentUrl query string argument - ${url}`;
                        // remove noise from requested url
                        // build the new requested url
                        let uriBase = $YetaWF.parseUrl(window.location.href);
                        uriBase.replaceSearch("!ContentUrl", contentUrl);
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
        private postLink(url: string, anchorOwner: HTMLElement | null, tag: HTMLElement, cookieToReturn: number | null) : void {
            this.waitForCookie(cookieToReturn);
            let formJson: FormInfoJSON|null = null;
            if ($YetaWF.FormsAvailable())
                formJson = $YetaWF.Forms.getJSONInfo(tag);
            $YetaWF.postJSON($YetaWF.parseUrl(url), formJson, null, null, (success: boolean, data: any) : void => { }, anchorOwner || undefined);
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