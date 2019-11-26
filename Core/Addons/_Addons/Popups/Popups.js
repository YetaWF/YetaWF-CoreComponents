"use strict";
/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
/**
 * Class implementing popup services used throughout YetaWF.
 */
var YetaWF;
(function (YetaWF) {
    var Popups = /** @class */ (function () {
        function Popups() {
        }
        // Implemented by renderer
        // Implemented by renderer
        // Implemented by renderer
        /**
         * Close the popup - this can only be used by code that is running within the popup (not the parent document/page)
         */
        Popups.prototype.closePopup = function (forceReload) {
            YetaWF_PopupsImpl.closePopup(forceReload);
        };
        /**
         * Close the popup - this can only be used by code that is running on the main page (not within the popup)
         */
        Popups.prototype.closeInnerPopup = function () {
            YetaWF_PopupsImpl.closeInnerPopup();
        };
        // Implemented by YetaWF
        // Implemented by YetaWF
        // Implemented by YetaWF
        /**
         * opens a popup given a url
         */
        Popups.prototype.openPopup = function (url, forceIframe) {
            $YetaWF.setLoading(true);
            // build a url that has a random portion so the page is not cached - this is so we can have the same page nested within itself
            if (url.indexOf("?") < 0)
                url += "?";
            else
                url += "&";
            url += new Date().getUTCMilliseconds();
            url += "&" + YConfigs.Basics.Link_ToPopup + "=y"; // we're now going into a popup
            if (!forceIframe) {
                if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl(url), false, YetaWF_PopupsImpl.openDynamicPopup)) {
                    // contents set in dynamic popup
                    return true;
                }
            }
            YetaWF_PopupsImpl.openStaticPopup(url);
            return true;
        };
        /**
         * Handles links that invoke a popup window.
         */
        Popups.prototype.handlePopupLink = function (elem) {
            var url = elem.href;
            // check if this is a popup link
            if (!$YetaWF.elementHasClass(elem, YConfigs.Basics.CssPopupLink))
                return false;
            // check whether we allow popups at all
            if (!YVolatile.Popups.AllowPopups)
                return false;
            if (YVolatile.Skin.MinWidthForPopups > window.outerWidth) // the screen is too small for a popup
                return false;
            // if we're switching from https->http or from http->https don't use a popup
            if (!window.document.location)
                return false;
            if (!url.startsWith("http") || !window.document.location.href.startsWith("http"))
                return false;
            if ((url.startsWith("http://") !== window.document.location.href.startsWith("http://")) ||
                (url.startsWith("https://") !== window.document.location.href.startsWith("https://")))
                return false;
            if (YVolatile.Basics.EditModeActive || YVolatile.Basics.PageControlVisible) {
                //if we're in edit mode or the page control module is visible, all links bring up a page (no popups) except for modules with the PopupEdit style
                if (elem.getAttribute(YConfigs.Basics.CssAttrDataSpecialEdit) == null)
                    return false;
            }
            return this.openPopup(url, false);
        };
        /**
         * Handles links in a popup that link to a url in the outer parent (main) window.
         */
        Popups.prototype.handleOuterWindow = function (elem) {
            // check if this is a popup link
            if (!elem.getAttribute(YConfigs.Basics.CssOuterWindow))
                return false;
            if (!$YetaWF.isInPopup())
                return false; // this shouldn't really happen
            $YetaWF.setLoading(true);
            window.parent.$YetaWF.ContentHandling.setNewUri($YetaWF.parseUrl(elem.href));
            return true;
        };
        Popups.prototype.init = function () { };
        return Popups;
    }());
    YetaWF.Popups = Popups;
    // tslint:disable-next-line:no-unused-expression
    $YetaWF.Popups; // need to evaluate for side effect to initialize popups
})(YetaWF || (YetaWF = {}));
