"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// Anchor handling, navigation
var YetaWF;
(function (YetaWF) {
    var Anchors = /** @class */ (function () {
        /**
         * Handles all navigation using <a> tags.
         */
        function Anchors() {
            var _this = this;
            this.cookiePattern = null;
            this.cookieTimer = null;
            // For an <a> link clicked, add the page we're coming from (not for popup links though)
            YetaWF_Basics.registerEventHandlerBody("click", "a.yaction-link,area.yaction-link", function (ev) {
                // find the real anchor, ev.srcElement was clicked but it may not be the anchor itself
                if (!ev.srcElement)
                    return true;
                var anchor = YetaWF_Basics.elementClosest(ev.srcElement, "a");
                if (!anchor)
                    return true;
                var url = anchor.href;
                // send tracking info
                if (YetaWF_Basics.elementHasClass(anchor, 'yTrack')) {
                    // find the unique skinvisitor module so we have antiforgery tokens and other context info
                    var f = YetaWF_Basics.getElement1BySelectorCond('.YetaWF_Visitors_SkinVisitor.YetaWF_Visitors.yModule form');
                    if (f) {
                        var data = { 'url': url };
                        var info = YetaWF_Forms.getFormInfo(f);
                        data[YConfigs.Basics.ModuleGuid] = info.ModuleGuid;
                        data[YConfigs.Forms.RequestVerificationToken] = info.RequestVerificationToken;
                        data[YConfigs.Forms.UniqueIdPrefix] = info.UniqueIdPrefix;
                        var urlTrack = f.getAttribute('data-track');
                        if (!urlTrack)
                            throw "data-track not defined"; /*DEBUG*/
                        $.ajax({
                            'url': urlTrack,
                            'type': 'post',
                            'data': data,
                        });
                        // no response handling
                    }
                }
                var uri = YetaWF_Basics.parseUrl(url);
                if (uri.getPath().length == 0 || (!uri.getSchema().startsWith('http:') && !uri.getSchema().startsWith('https:')))
                    return true;
                // if we're on an edit page, propagate edit to new link unless the new uri explicitly has !Noedit
                if (!uri.hasSearch(YConfigs.Basics.Link_EditMode) && !uri.hasSearch(YConfigs.Basics.Link_NoEditMode)) {
                    var currUri = YetaWF_Basics.parseUrl(window.location.href);
                    if (currUri.hasSearch(YConfigs.Basics.Link_EditMode))
                        uri.addSearch(YConfigs.Basics.Link_EditMode, 'y');
                }
                // add status/visibility of page control module
                uri.removeSearch(YConfigs.Basics.Link_PageControl);
                if (YVolatile.Basics.PageControlVisible)
                    uri.addSearch(YConfigs.Basics.Link_PageControl, 'y');
                // add our module context info (if requested)
                if (anchor.getAttribute(YConfigs.Basics.CssAddModuleContext) != null) {
                    if (!uri.hasSearch(YConfigs.Basics.ModuleGuid)) {
                        var guid = YetaWF_Basics.getModuleGuidFromTag(anchor);
                        uri.addSearch(YConfigs.Basics.ModuleGuid, guid);
                    }
                }
                // pass along the charsize
                {
                    var charSize = YetaWF_Basics.getCharSizeFromTag(anchor);
                    uri.removeSearch(YConfigs.Basics.Link_CharInfo);
                    uri.addSearch(YConfigs.Basics.Link_CharInfo, charSize.width + ',' + charSize.height);
                }
                // fix the url to include where we came from
                var target = anchor.getAttribute("target");
                if ((!target || target == "" || target == "_self") && anchor.getAttribute(YConfigs.Basics.CssSaveReturnUrl) != null) {
                    // add where we currently are so we can save it in case we need to return to this page
                    var currUri = YetaWF_Basics.parseUrl(window.location.href);
                    currUri.removeSearch(YConfigs.Basics.Link_OriginList); // remove originlist from current URL
                    currUri.removeSearch(YConfigs.Basics.Link_InPopup); // remove popup info from current URL
                    // now update url (where we're going with originlist)
                    uri.removeSearch(YConfigs.Basics.Link_OriginList);
                    var originList = YVolatile.Basics.OriginList.slice(0); // copy saved originlist
                    if (anchor.getAttribute(YConfigs.Basics.CssDontAddToOriginList) == null) {
                        var newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: YetaWF_Basics.isInPopup() };
                        originList.push(newOrigin);
                        if (originList.length > 5) // only keep the last 5 urls
                            originList = originList.slice(originList.length - 5);
                    }
                    uri.addSearch(YConfigs.Basics.Link_OriginList, JSON.stringify(originList));
                    target = "_self";
                }
                if (!target || target == "" || target == "_self")
                    target = "_self";
                anchor.href = uri.toUrl(); // update original href in case let default handling take place
                // first try to handle this as a link to the outer window (only used in a popup)
                if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined) {
                    if (YetaWF_Popups.handleOuterWindow(anchor))
                        return false;
                }
                // try to handle this as a popup link
                if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined) {
                    if (YetaWF_Popups.handlePopupLink(anchor))
                        return false;
                }
                _this.cookiePattern = null;
                _this.cookieTimer = null;
                var cookieToReturn = null;
                var post = false;
                if (anchor.getAttribute(YConfigs.Basics.CookieDoneCssAttr) != null) {
                    cookieToReturn = (new Date()).getTime();
                    uri.removeSearch(YConfigs.Basics.CookieToReturn);
                    uri.addSearch(YConfigs.Basics.CookieToReturn, JSON.stringify(cookieToReturn));
                }
                if (anchor.getAttribute(YConfigs.Basics.PostAttr) != null)
                    post = true;
                anchor.href = uri.toUrl(); // update original href in case let default handling take place
                if (cookieToReturn) {
                    // this is a file download
                    var confirm = anchor.getAttribute(YConfigs.Basics.CssConfirm);
                    if (confirm != null) {
                        YetaWF_Basics.alertYesNo(confirm, undefined, function () {
                            window.location.assign(url);
                            YetaWF_Basics.setLoading();
                            _this.waitForCookie(cookieToReturn);
                        });
                        return false;
                    }
                    window.location.assign(url);
                }
                else {
                    // if a confirmation is wanted, show it
                    // this means that it's posted by definition
                    var confirm = anchor.getAttribute(YConfigs.Basics.CssConfirm);
                    if (confirm) {
                        YetaWF_Basics.alertYesNo(confirm, undefined, function () {
                            _this.postLink(url, anchor, cookieToReturn);
                            var s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                            if (s)
                                YetaWF_Basics.pleaseWait(s);
                            return false;
                        });
                        return false;
                    }
                    else if (post) {
                        var s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                        if (s)
                            YetaWF_Basics.pleaseWait(s);
                        _this.postLink(url, anchor, cookieToReturn);
                        return false;
                    }
                }
                if (target == "_self") {
                    // add overlay if desired
                    var s = anchor.getAttribute(YConfigs.Basics.CssPleaseWait);
                    if (s)
                        YetaWF_Basics.pleaseWait(s);
                }
                _this.waitForCookie(cookieToReturn); // if any
                // Handle unified page clicks by activating the desired pane(s) or swapping out pane contents
                if (cookieToReturn)
                    return true; // expecting cookie return
                if (uri.getDomain() !== "" && uri.getDomain() !== window.document.domain)
                    return true; // wrong domain
                // if we're switching from https->http or from http->https don't use a unified page set
                if (!url.startsWith("http") || !window.document.location.href.startsWith("http"))
                    return true; // neither http nor https
                if ((url.startsWith("http://") != window.document.location.href.startsWith("http://")) ||
                    (url.startsWith("https://") != window.document.location.href.startsWith("https://")))
                    return true; // switching http<>https
                if (target == "_self")
                    return !YetaWF_Basics.ContentHandling.setContent(uri, true);
                return true;
            });
        }
        Anchors.prototype.checkCookies = function () {
            if (this.cookiePattern == undefined)
                throw "cookie pattern not defined"; /*DEBUG*/
            if (this.cookieTimer == undefined)
                throw "cookie timer not defined"; /*DEBUG*/
            if (document.cookie.search(this.cookiePattern) >= 0) {
                clearInterval(this.cookieTimer);
                YetaWF_Basics.setLoading(false); // turn off loading indicator
                console.log("Download complete!!");
                return false;
            }
            console.log("File still downloading...", new Date().getTime());
            return true;
        };
        Anchors.prototype.waitForCookie = function (cookieToReturn) {
            if (cookieToReturn) {
                // check for cookie to see whether download started
                this.cookiePattern = new RegExp((YConfigs.Basics.CookieDone + "=" + cookieToReturn), "i");
                this.cookieTimer = setInterval(this.checkCookies, 500);
            }
        };
        Anchors.prototype.postLink = function (url, elem, cookieToReturn) {
            YetaWF_Basics.setLoading();
            this.waitForCookie(cookieToReturn);
            var request = new XMLHttpRequest();
            request.open("POST", url, true);
            request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            request.onreadystatechange = function (ev) {
                var req = this;
                if (req.readyState === 4 /*DONE*/) {
                    YetaWF_Basics.setLoading(false);
                    YetaWF_Basics.processAjaxReturn(req.responseText, req.statusText, req, elem);
                }
            };
            request.send("");
        };
        return Anchors;
    }());
    YetaWF.Anchors = Anchors;
})(YetaWF || (YetaWF = {}));
