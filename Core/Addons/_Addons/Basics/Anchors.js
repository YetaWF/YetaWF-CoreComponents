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
                        var formJson = $YetaWF.Forms.getJSONInfo(anchor);
                        $YetaWF.postJSONIgnore(uri_1, formJson, { Url: url }, null);
                    }
                }
                var uri = $YetaWF.parseUrl(url);
                if (uri.getPath().length === 0 || (!uri.getSchema().startsWith("http:") && !uri.getSchema().startsWith("https:")))
                    return true;
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
                var target = anchor.getAttribute("target");
                if (!target || target === "" || target === "_self")
                    target = "_self";
                // add originating module guid
                if (!uri.hasSearch(YConfigs.Basics.ModuleGuid)) {
                    var formJson = $YetaWF.Forms.getJSONInfo(anchor);
                    uri.addSearch(YConfigs.Basics.ModuleGuid, formJson.ModuleGuid);
                }
                anchor.href = uri.toUrl(); // update original href in case default handling takes place
                var cookieToReturn = null;
                var post = false;
                if (anchor.getAttribute(YConfigs.Basics.CookieDoneCssAttr) != null) {
                    cookieToReturn = (new Date()).getTime();
                    uri.replaceSearch(YConfigs.Basics.CookieToReturn, JSON.stringify(cookieToReturn));
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
                        origUri.removeSearch(YConfigs.Basics.Link_InPopup); // remove popup info from current URL
                        var contentUrl = origUri.getSearch("!ContentUrl");
                        if (!contentUrl)
                            throw "In place content must have a !ContentUrl query string argument - ".concat(url);
                        // remove noise from requested url
                        // build the new requested url
                        var uriBase = $YetaWF.parseUrl(window.location.href);
                        uriBase.replaceSearch("!ContentUrl", contentUrl);
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
            var formJson = $YetaWF.Forms.getJSONInfo(tag);
            $YetaWF.postJSON($YetaWF.parseUrl(url), formJson, null, null, function (success, data) { }, anchorOwner || undefined);
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
