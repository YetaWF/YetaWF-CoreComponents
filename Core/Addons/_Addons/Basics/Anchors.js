"use strict";
/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// Anchor handling, navigation
var YetaWF;
(function (YetaWF) {
    var Anchors = /** @class */ (function () {
        function Anchors() {
        }
        /**
         * Handles all navigation using <a> tags.
         */
        Anchors.prototype.init = function () {
            var _this = this;
            // For an <a> link clicked, add the page we're coming from (not for popup links though)
            $YetaWF.registerEventHandlerBody("click", "a.yaction-link,area.yaction-link", function (ev) {
                // find the real anchor, ev.target was clicked but it may not be the anchor itself
                if (!ev.target)
                    return true;
                var anchor = $YetaWF.elementClosestCond(ev.target, "a,area");
                if (!anchor)
                    return true;
                if ($YetaWF.getAttributeCond(anchor, "data-nohref") != null)
                    return false;
                var url = anchor.href;
                // send tracking info
                if ($YetaWF.elementHasClass(anchor, "yTrack")) {
                    // find the unique skinvisitor module so we have antiforgery tokens and other context info
                    var f = $YetaWF.getElement1BySelectorCond(".YetaWF_Visitors_SkinVisitor.YetaWF_Visitors.yModule form");
                    if (f) {
                        var urlTrack = f.getAttribute("data-track");
                        if (!urlTrack)
                            throw "data-track not defined"; /*DEBUG*/
                        var uri_1 = $YetaWF.parseUrl(urlTrack);
                        var data = { "url": url };
                        uri_1.addSearchSimpleObject(data);
                        uri_1.addFormInfo(f);
                        var request = new XMLHttpRequest();
                        request.open("POST", urlTrack, true);
                        request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                        request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                        request.send(uri_1.toFormData());
                        // no response handling
                    }
                }
                var uri = $YetaWF.parseUrl(url);
                if (uri.getPath().length === 0 || (!uri.getSchema().startsWith("http:") && !uri.getSchema().startsWith("https:")))
                    return true;
                // add status/visibility of page control module
                uri.removeSearch(YConfigs.Basics.Link_PageControl);
                if (YVolatile.Basics.PageControlVisible)
                    uri.addSearch(YConfigs.Basics.Link_PageControl, "y");
                // add our module context info (if requested)
                if (anchor.getAttribute(YConfigs.Basics.CssAddModuleContext) != null) {
                    if (!uri.hasSearch(YConfigs.Basics.ModuleGuid)) {
                        var guid = $YetaWF.getModuleGuidFromTag(anchor);
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
                var target = anchor.getAttribute("target");
                if (!target || target === "" || target === "_self") {
                    if (anchor.getAttribute(YConfigs.Basics.CssSaveReturnUrl) != null) {
                        var originList = YVolatile.Basics.OriginList.slice(0); // copy saved originlist
                        // add where we currently are so we can save it in case we need to return to this page
                        var currUri = $YetaWF.parseUrl(window.location.href);
                        currUri.removeSearch(YConfigs.Basics.Link_OriginList); // remove originlist from current URL
                        currUri.removeSearch(YConfigs.Basics.Link_InPopup); // remove popup info from current URL
                        if (anchor.getAttribute(YConfigs.Basics.CssDontAddToOriginList) == null) {
                            var newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: $YetaWF.isInPopup() };
                            originList.push(newOrigin);
                            if (originList.length > 5) // only keep the last 5 urls
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
                var cookieToReturn = null;
                var post = false;
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
                    var confirm_1 = anchor.getAttribute(YConfigs.Basics.CssConfirm);
                    if (confirm_1 != null) {
                        $YetaWF.alertYesNo(confirm_1, undefined, function () {
                            window.location.assign(url);
                            $YetaWF.setLoading();
                            _this.waitForCookie(cookieToReturn);
                        });
                        return false;
                    }
                    $YetaWF.setLoading();
                }
                else {
                    // if a confirmation is wanted, show it
                    // this means that it's posted by definition
                    var confirm_2 = anchor.getAttribute(YConfigs.Basics.CssConfirm);
                    if (confirm_2) {
                        var anchorOwner_1 = $YetaWF.getOwnerFromTag(anchor) || anchor;
                        $YetaWF.alertYesNo(confirm_2, undefined, function () {
                            _this.postLink(url, anchorOwner_1, anchor, cookieToReturn);
                            var s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                            if (s)
                                $YetaWF.pleaseWait(s);
                        });
                        return false;
                    }
                    else if (post) {
                        var s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                        if (s)
                            $YetaWF.pleaseWait(s);
                        var anchorOwner = $YetaWF.getOwnerFromTag(anchor) || anchor;
                        _this.postLink(url, anchorOwner, anchor, cookieToReturn);
                        return false;
                    }
                }
                if (target === "_self") {
                    // add overlay if desired
                    var s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                    if (s)
                        $YetaWF.pleaseWait(s);
                }
                _this.waitForCookie(cookieToReturn); // if any
                if (cookieToReturn)
                    return true; // expecting cookie return
                if (uri.getHostName() !== "" && uri.getHostName() !== window.document.domain)
                    return true; // wrong domain
                // if we're switching from https->http or from http->https don't use a unified page set
                if (!window.document.location)
                    return true;
                if (!url.startsWith("http") || !window.document.location.href.startsWith("http"))
                    return true; // neither http nor https
                if ((url.startsWith("http://") !== window.document.location.href.startsWith("http://")) ||
                    (url.startsWith("https://") !== window.document.location.href.startsWith("https://")))
                    return true; // switching http<>https
                if (target === "_self") {
                    // handle inplace content replacement if requested
                    var inplace = void 0;
                    var contentTarget = $YetaWF.getAttributeCond(anchor, "data-contenttarget");
                    var contentPane = $YetaWF.getAttributeCond(anchor, "data-contentpane");
                    if (!contentPane)
                        contentPane = "MainPane";
                    if (contentTarget) {
                        // get the requested Url
                        var origUri = $YetaWF.parseUrl(url);
                        origUri.removeSearch(YConfigs.Basics.Link_OriginList); // remove originlist from current URL
                        origUri.removeSearch(YConfigs.Basics.Link_InPopup); // remove popup info from current URL
                        var contentUrl = origUri.getSearch("!ContentUrl");
                        if (!contentUrl)
                            throw "In place content must have a !ContentUrl query string argument - ".concat(url);
                        // remove noise from requested url
                        // build the new requested url
                        var uriBase = $YetaWF.parseUrl(window.location.href);
                        uriBase.removeSearch("!ContentUrl");
                        uriBase.addSearch("!ContentUrl", contentUrl);
                        inplace = { TargetTag: contentTarget, FromPane: contentPane, PageUrl: uriBase.toUrl(), ContentUrl: contentUrl };
                    }
                    if ($YetaWF.elementHasClass(anchor, "yIgnorePageChanged"))
                        return $YetaWF.ContentHandling.setContentForce(uri, true, undefined, inplace) === YetaWF.SetContentResult.NotContent;
                    else
                        return $YetaWF.ContentHandling.setContent(uri, true, undefined, inplace) === YetaWF.SetContentResult.NotContent;
                }
                return true;
            });
        };
        Anchors.prototype.waitForCookie = function (cookieToReturn) {
            if (cookieToReturn) {
                // check for cookie to see whether download started
                new CookieWait(cookieToReturn);
            }
        };
        Anchors.prototype.postLink = function (url, anchorOwner, tag, cookieToReturn) {
            this.waitForCookie(cookieToReturn);
            $YetaWF.postJSON($YetaWF.parseUrl(url), $YetaWF.Forms.getJSONInfo(tag), null, function (success, data) { }, anchorOwner || undefined);
        };
        return Anchors;
    }());
    YetaWF.Anchors = Anchors;
    var CookieWait = /** @class */ (function () {
        function CookieWait(cookieToReturn) {
            var _this = this;
            this.cookiePattern = new RegExp((YConfigs.Basics.CookieDone + "=" + cookieToReturn), "i");
            this.cookieTimer = setInterval(function () { _this.checkCookies(); }, 500);
        }
        CookieWait.prototype.checkCookies = function () {
            if (document.cookie.search(this.cookiePattern) >= 0) {
                clearInterval(this.cookieTimer);
                $YetaWF.setLoading(false); // turn off loading indicator
                console.log("Download complete!!");
                return false;
            }
            console.log("File still downloading...", new Date().getTime());
            return true;
        };
        return CookieWait;
    }());
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Anchors.js.map
