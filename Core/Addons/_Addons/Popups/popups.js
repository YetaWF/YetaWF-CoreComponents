/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

/* Popup */

var YetaWF_Popup = {};
var _YetaWF_Popup = {};

// inline - as soon as we're loading, resize the popup window, if we're in a popup
if (YVolatile.Basics.IsInPopup) {

    var popup = window.parent.document.YPopupWindowActive;

    // get the popup window height
    var width = YVolatile.Skin.PopupWidth;
    var height = YVolatile.Skin.PopupHeight;

    popup.setOptions({
        width: width == undefined ? YConfigs.Popups.DefaultPopupWidth : width,
        height: height == undefined ? YConfigs.Popups.DefaultPopupHeight : height,
    });
    popup.center().open();

    // show/hide the maximize button (not directly supported so we'll do it manually)
    var $popupwin = $("#ypopup", $(window.parent.document));
    if ($popupwin.length == 0) throw "Couldn't find popup window";/*DEBUG*/
    var $popWindow = $popupwin.closest('.k-widget.k-window');
    if ($popWindow.length == 0) throw "Couldn't find enclosing popup window";/*DEBUG*/
    if (YVolatile.Skin.PopupMaximize)
        $('.k-window-action.k-link', $popWindow).eq(0).show();// show the maximize button
    else
        $('.k-window-action.k-link', $popWindow).eq(0).hide();// hide the maximize button

}

// close a popup (if there is one)
YetaWF_Popup.closePopup = function (forceReload) {
    if (YVolatile.Basics.IsInPopup) {
        var forced = (forceReload === true);
        if (forced)
            Y_ReloadWindowPage(window.parent, true)
        var popup = window.parent.document.YPopupWindowActive;
        popup.close();
        popup.destroy();
    }
}

// Use this in a popup to set link to a url in the outer parent (main) window
YetaWF_Popup.handleOuterWindow = function ($this) {
    // check if this is a popup link
    if ($this.attr(YConfigs.Basics.CssOuterWindow)==undefined)
        return false;
    if (!Y_InPopup()) return false; // this shouldn't really happen
    Y_Loading(true);
    window.parent.location = $this[0].href;
    return true;
};

// Handles links that invoke a popup window
YetaWF_Popup.handlePopupLink = function ($this) {

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

    // build a url that has a random portion so the page is not cached
    // this is so we can have the same page nested within itself
    if (url.indexOf('?') < 0)
        url += '?';
    else
        url += "&";
    url += new Date().getUTCMilliseconds();
    url += "&" + YGlobals.Link_ToPopup + "=y";// we're now going into a popup

    Y_Loading(true);

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
    // this is automaticaly removed when destroy() is called
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
            document.YPopupWindowActive = null;
            popup = null;
        },
        //animation: {
        //    open: false
        //},
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

    return true; // we handled this as a popup
};



