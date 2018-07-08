﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// Anchor handling, navigation

namespace YetaWF {

    export class Anchors {

        private cookiePattern: RegExp | null = null;
        private cookieTimer : number | null = null;

        /**
         * Handles all navigation using <a> tags.
         */
        public init(): void {

            // For an <a> link clicked, add the page we're coming from (not for popup links though)
            $("body").on("click", "a.yaction-link,area.yaction-link", (e) => {

                var $t: JQuery<HTMLAnchorElement> = $(e.currentTarget) as JQuery<HTMLAnchorElement>;

                var uri = $t.uri();
                var url = $t[0].href;

                // send tracking info
                if ($t.hasClass('yTrack')) {
                    // find the unique skinvisitor module so we have antiforgery tokens and other context info
                    var $f = $('.YetaWF_Visitors_SkinVisitor.YetaWF_Visitors.yModule form');
                    if ($f.length == 1) {
                        var data = { 'url': url };
                        var info = YetaWF_Forms.getFormInfo($f);
                        data[YConfigs.Basics.ModuleGuid] = info.ModuleGuid;
                        data[YConfigs.Forms.RequestVerificationToken] = info.RequestVerificationToken;
                        data[YConfigs.Forms.UniqueIdPrefix] = info.UniqueIdPrefix;
                        var urlTrack = $f.attr('data-track');
                        if (!urlTrack) throw "data-track not defined";/*DEBUG*/
                        $.ajax({
                            'url': urlTrack,
                            'type': 'post',
                            'data': data,
                        });
                        // no response handling
                    }
                }

                if (uri.path().length == 0 || url.startsWith('javascript:') || url.startsWith('mailto:') || url.startsWith('tel:')) return true;

                // if we're on an edit page, propagate edit to new link unless the new uri explicitly has !Noedit
                if (!uri.hasSearch(YGlobals.Link_EditMode) && !uri.hasSearch(YGlobals.Link_NoEditMode)) {
                    var currUri = new URI(window.location.href);
                    if (currUri.hasSearch(YGlobals.Link_EditMode))
                        uri.addSearch(YGlobals.Link_EditMode, 'y');
                }
                // add status/visibility of page control module
                uri.removeSearch(YGlobals.Link_PageControl);
                if (YVolatile.Basics.PageControlVisible)
                    uri.addSearch(YGlobals.Link_PageControl, 'y');

                // add our module context info (if requested)
                if ($t.attr(YConfigs.Basics.CssAddModuleContext) != undefined) {
                    if (!uri.hasSearch(YConfigs.Basics.ModuleGuid)) {
                        var guid = YetaWF_Basics.getModuleGuidFromTag($t);
                        uri.addSearch(YConfigs.Basics.ModuleGuid, guid);
                    }
                }

                // pass along the charsize
                {
                    var charSize = YetaWF_Basics.getCharSizeFromTag($t);
                    uri.removeSearch(YGlobals.Link_CharInfo);
                    uri.addSearch(YGlobals.Link_CharInfo, charSize.width + ',' + charSize.height);
                }

                // fix the url to include where we came from
                var target = $t.attr("target");
                if ((target == undefined || target == "" || target == "_self") && $t.attr(YConfigs.Basics.CssSaveReturnUrl) != undefined) {
                    // add where we currently are so we can save it in case we need to return to this page
                    var currUri = new URI(window.location.href);
                    currUri.removeSearch(YGlobals.Link_OriginList);// remove originlist from current URL
                    currUri.removeSearch(YGlobals.Link_InPopup);// remove popup info from current URL
                    // now update url (where we're going with originlist)
                    uri.removeSearch(YGlobals.Link_OriginList);
                    var originList = YVolatile.Basics.OriginList.slice(0);// copy saved originlist

                    if ($t.attr(YConfigs.Basics.CssDontAddToOriginList) == undefined) {
                        var newOrigin = { Url: currUri.toString(), EditMode: YVolatile.Basics.EditModeActive != 0, InPopup: YetaWF_Basics.isInPopup() };
                        originList.push(newOrigin);
                        if (originList.length > 5)// only keep the last 5 urls
                            originList = originList.slice(originList.length - 5);
                    }
                    uri.addSearch(YGlobals.Link_OriginList, JSON.stringify(originList));
                    target = "_self";
                }
                if (target == undefined || target == "" || target == "_self")
                    target = "_self";

                // first try to handle this as a link to the outer window (only used in a popup)
                if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined) {
                    if (YetaWF_Popups.handleOuterWindow($t))
                        return false;
                }
                // try to handle this as a popup link
                if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined) {
                    if (YetaWF_Popups.handlePopupLink($t))
                        return false;
                }

                this.cookiePattern = null;
                this.cookieTimer = null;
                var cookieToReturn: number | null = null;
                var post: boolean = false;

                if ($t.attr(YConfigs.Basics.CookieDoneCssAttr) != undefined) {
                    cookieToReturn = (new Date()).getTime();
                    uri.removeSearch(YConfigs.Basics.CookieToReturn);
                    uri.addSearch(YConfigs.Basics.CookieToReturn, JSON.stringify(cookieToReturn));
                }
                if ($t.attr(<string/*$$$*/>YConfigs.Basics.PostAttr))
                    post = true;

                if (cookieToReturn) {
                    // this is a file download
                    var confirm = $t.attr(<string /*$$$*/>YConfigs.Basics.CssConfirm);
                    if (confirm) {
                        YetaWF_Basics.Y_AlertYesNo(confirm, undefined, () => {
                            window.location.assign(url);
                            YetaWF_Basics.setLoading();
                            this.waitForCookie(cookieToReturn);
                        });
                        return false;
                    }
                    window.location.assign(url);
                } else {
                    // if a confirmation is wanted, show it
                    // this means that it's posted by definition
                    var confirm= $t.attr(<string/*$$$*/>YConfigs.Basics.CssConfirm);
                    if (confirm) {
                        YetaWF_Basics.Y_AlertYesNo(confirm, undefined, () => {
                            this.postLink(url, $t, cookieToReturn);
                            if ($t.attr(YConfigs.Basics.CssPleaseWait) != undefined)
                                YetaWF_Basics.Y_PleaseWait($t.attr(<string/*$$$$*/>YConfigs.Basics.CssPleaseWait))
                            return false;
                        });
                        return false;
                    } else if (post) {
                        if ($t.attr(YConfigs.Basics.CssPleaseWait) != undefined)
                            YetaWF_Basics.Y_PleaseWait($t.attr(<string/*$$$$*/>YConfigs.Basics.CssPleaseWait))
                        this.postLink(url, $t, cookieToReturn);
                        return false;
                    }
                }

                if (target == "_self") {
                    // add overlay if desired
                    if ($t.attr(YConfigs.Basics.CssPleaseWait) != undefined) {
                        YetaWF_Basics.Y_PleaseWait($t.attr(<string/*$$$$*/>YConfigs.Basics.CssPleaseWait))
                    }
                }
                this.waitForCookie(cookieToReturn); // if any

                // Handle unified page clicks by activating the desired pane(s) or swapping out pane contents
                if (cookieToReturn) return true; // expecting cookie return
                if (uri.domain() !== "" && uri.hostname() !== window.document.domain) return true; // wrong domain
                // if we're switching from https->http or from http->https don't use a unified page set
                if (!url.startsWith("http") || !window.document.location.href.startsWith("http")) return true; // neither http nor https
                if ((url.startsWith("http://") != window.document.location.href.startsWith("http://")) ||
                    (url.startsWith("https://") != window.document.location.href.startsWith("https://"))) return true; // switching http<>https

                if (target == "_self")
                    return !YetaWF_Basics.ContentHandling.setContent(uri, true);

                return true;
            });
        }
        private checkCookies(): boolean {
            if (this.cookiePattern == undefined) throw "cookie pattern not defined";/*DEBUG*/
            if (this.cookieTimer == undefined) throw "cookie timer not defined";/*DEBUG*/
            if (document.cookie.search(this.cookiePattern) >= 0) {
                clearInterval(this.cookieTimer);
                YetaWF_Basics.setLoading(false);// turn off loading indicator
                console.log("Download complete!!");
                return false;
            }
            console.log("File still downloading...", new Date().getTime());
            return true;
        }
        private waitForCookie(cookieToReturn: number | null) : void {
            if (cookieToReturn) {
                // check for cookie to see whether download started
                this.cookiePattern = new RegExp((YConfigs.Basics.CookieDone + "=" + cookieToReturn), "i");
                this.cookieTimer = setInterval(this.checkCookies, 500);
            }
        }
        private postLink(url: string, $elem: JQuery<HTMLAnchorElement>, cookieToReturn: number | null) : void {
            YetaWF_Basics.setLoading();
            this.waitForCookie(cookieToReturn);

            $.ajax({
                'url': url,
                type: 'post',
                data: {},
                success: function (result, textStatus, jqXHR) {
                    YetaWF_Basics.setLoading(false);
                    YetaWF_Basics.processAjaxReturn(result, textStatus, jqXHR, $elem);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    YetaWF_Basics.setLoading(false);
                    YetaWF_Basics.Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
                    debugger;
                }
            });
        }
    }
}