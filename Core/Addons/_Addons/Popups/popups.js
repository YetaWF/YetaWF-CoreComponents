/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Popup */

var YetaWF_Popup = {};
var _YetaWF_Popup = {};

document.YPopupWindowActive = null;

// inline - as soon as we're loading, resize the popup window, if we're in a popup
// this is only used by full page loads (i.e., a popup using an iframe)
if (YVolatile.Basics.IsInPopup) {
    'use strict';

    var $popupwin = $("#ypopup", $(window.parent.document));
    var popup = window.parent.document.YPopupWindowActive;

    // get the popup window height
    var width = YVolatile.Skin.PopupWidth;
    var height = YVolatile.Skin.PopupHeight;

    popup.setOptions({
        width: width,
        height: height,
    });
    YetaWF_Basics.setCondense($popupwin, width);
    popup.center().open();

    // show/hide the maximize button (not directly supported so we'll do it manually)
    if ($popupwin.length == 0) throw "Couldn't find popup window";/*DEBUG*/
    var $popWindow = $popupwin.closest('.k-widget.k-window');
    if ($popWindow.length == 0) throw "Couldn't find enclosing popup window";/*DEBUG*/
    if (YVolatile.Skin.PopupMaximize)
        $('.k-window-action.k-button', $popWindow).eq(0).show();// show the maximize button
    else
        $('.k-window-action.k-button', $popWindow).eq(0).hide();// hide the maximize button
}

// Close the popup - this can only be used by code that is running within the popup (not the parent document/page)
YetaWF_Popup.closePopup = function (forceReload) {
    'use strict';
    if (YVolatile.Basics.IsInPopup) {
        var forced = (forceReload === true);
        if (forced)
            Y_ReloadWindowPage(window.parent, true)
        // with unified page sets there may actually not be a parent, but window.parent returns itself in this case anyway
        var popup = window.parent.document.YPopupWindowActive;
        if (popup != null) {
            popup.close();
            popup.destroy();
        }
        YVolatile.Basics.IsInPopup = false; // we're no longer in a popup
    }
}
// Close the popup - this can only be used by code that is running on the main page (not within the popup)
YetaWF_Popup.closeInnerPopup = function () {
    'use strict';
    var popup = document.YPopupWindowActive;
    if (popup != null) {
        popup.close();
        popup.destroy();
        document.YPopupWindowActive = null;
    }
    YVolatile.Basics.IsInPopup = false; // we're no longer in a popup
}

// Use this in a popup to set the link to a url in the outer parent (main) window
YetaWF_Popup.handleOuterWindow = function ($this) {
    'use strict';
    // check if this is a popup link
    if ($this.attr(YConfigs.Basics.CssOuterWindow)==undefined)
        return false;
    if (!Y_InPopup()) return false; // this shouldn't really happen
    Y_Loading(true);
    if (!window.parent._YetaWF_Basics.setContent(new URI($this[0].href), true))
        window.parent.location.assign($this[0].href);
    return true;
};

// Handles links that invoke a popup window
YetaWF_Popup.handlePopupLink = function ($this) {
    'use strict';

    var url = $this[0].href;

    // check if this is a popup link
    if (!$this.hasClass(YConfigs.Basics.CssPopupLink))
        return false;
    // check whether we allow popups at all
    if (!YVolatile.Popups.AllowPopups)
        return false;
    if (YVolatile.Skin.MinWidthForPopups > window.outerWidth) // the screen is too small for a popup
        return false;
    // if we're switching from https->http or from http->https don't use a popup
    if (!url.startsWith("http") || !window.document.location.href.startsWith("http"))
        return false;
    if ((url.startsWith("http://") != window.document.location.href.startsWith("http://")) ||
         (url.startsWith("https://") != window.document.location.href.startsWith("https://")))
        return false;
    if (YVolatile.Basics.EditModeActive || YVolatile.Basics.PageControlVisible) {
        //if we're in edit mode or the page control module is visible, all links bring up a page (no popups) except for modules with the PopupEdit style
        if ($this.attr(YConfigs.Basics.CssAttrDataSpecialEdit) == undefined)
            return false;
    }

    return YetaWF_Popup.openPopup(url);
};

// opens a popup with dynamic content (unified page sets)
_YetaWF_Popup.openDynamicPopup = function(result) {

    function closePopup() {
        var popup = $("#ypopup").data("kendoWindow");
        popup.destroy();
        popup = null;
        document.YPopupWindowActive = null;
        YVolatile.Basics.IsInPopup = false; // we're no longer in a popup
    }

    // we're already in a popup
    if (Y_InPopup())
        closePopup();

    // insert <div id="ypopup" class='yPopupDyn'></div> at top of page for the popup window
    // this is automatically removed when destroy() is called
    $("body").prepend("<div id='ypopup' class='yPopupDyn'></div>");
    var $popupwin = $("#ypopup");
    $popupwin.addClass(YVolatile.Skin.PopupCss);

    // add pane content
    var contentLength = result.Content.length;
    for (var i = 0; i < contentLength; i++) {
        // add the pane
        var $pane = $("<div class='yPane'></div>").addClass(result.Content[i].Pane);
        $pane.append(result.Content[i].HTML);
        $popupwin.append($pane);
    }

    var popup = null;

    var acts = [];
    if (YVolatile.Skin.PopupMaximize)
        acts.push("Maximize");
    acts.push("Close");

    // Create the window
    $popupwin.kendoWindow({
        actions: acts,
        width: YVolatile.Skin.PopupWidth,
        height: YVolatile.Skin.PopupHeight,
        draggable: true,
        iframe: false,
        modal: true,
        resizable: false,
        title: result.PageTitle,
        visible: false,
        close: function () {
            closePopup();
        },
        animation: {
            open: false
        },
        refresh: function () { // page complete
            Y_Loading(false);
        },
        error: function (e) {
            Y_Loading(false);
            Y_Error("Request failed with status " + e.status);
        }
    });

    // show and center the window
    popup = $popupwin.data("kendoWindow");
    popup.center().open();

    // mark that a popup is active
    document.expando = true;
    document.YPopupWindowActive = popup;
    YVolatile.Basics.IsInPopup = true; // we're in a popup

    YetaWF_Basics.setCondense($popupwin, YVolatile.Skin.PopupWidth);

    return $popupwin;
}

// opens a popup given a url
YetaWF_Popup.openPopup = function(url, forceIframe) {
    'use strict';

    Y_Loading(true);

    // build a url that has a random portion so the page is not cached
    // this is so we can have the same page nested within itself
    if (url.indexOf('?') < 0)
        url += '?';
    else
        url += "&";
    url += new Date().getUTCMilliseconds();
    url += "&" + YGlobals.Link_ToPopup + "=y";// we're now going into a popup

    if (!forceIframe && _YetaWF_Basics.setContent(new URI(url), false, _YetaWF_Popup.openDynamicPopup))
        return true;

    // we're already in a popup
    if (Y_InPopup()) {
        // we handle links within a popup by replacing the current popup page with the new page
        Y_Loading(true);
        var $popupwin = $("#ypopup", $(window.parent.document));
        if ($popupwin.length == 0) throw "Couldn't find popup window";/*DEBUG*/
        var iframeDomElement = $popupwin.children("iframe")[0];
        iframeDomElement.src = url;
        return true;
    }

    // insert <div id="ypopup"></div> at top of page for the popup window
    // this is automatically removed when destroy() is called
    $("body").prepend("<div id='ypopup'></div>");
    var $popupwin = $("#ypopup");
    var popup = null;

    var acts = [];
    acts.push("Maximize");// always show the maximize button - hide it based on popup skin options
    acts.push("Close");

    // Create the window
    $popupwin.kendoWindow({
        actions: acts,
        width: YConfigs.Popups.DefaultPopupWidth,
        height: YConfigs.Popups.DefaultPopupHeight,
        draggable: true,
        iframe: true,
        modal: true,
        resizable: false,
        title: " ", //title is set later once contents are available
        visible: false,
        content: url,
        close: function () {
            var popup = $popupwin.data("kendoWindow");
            popup.destroy();
            popup = null;
            document.YPopupWindowActive = null;
            YVolatile.Basics.IsInPopup = false;
        },
        animation: {
            open: false
        },
        refresh: function () { // page complete
            var iframeDomElement = $popupwin.children("iframe")[0];
            var iframeDocumentObject = iframeDomElement.contentDocument;
            popup.title(iframeDocumentObject.title);
            Y_Loading(false);
        },
        error: function (e) {
            Y_Loading(false);
            Y_Error("Request failed with status " + e.status);
        }
    });

    // show and center the window
    popup = $popupwin.data("kendoWindow");
    //do not open the window here - the loaded content opens it because it knows the desired size
    //popup.center().open();

    // mark that a popup is active
    document.expando = true;
    document.YPopupWindowActive = popup;
    YVolatile.Basics.IsInPopup = true; // we're in a popup

    return true; // we handled this as a popup
};
