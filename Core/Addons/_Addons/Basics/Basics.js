"use strict";
/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
/**
 * Class implementing basic services used throughout YetaWF.
 */
var YetaWF;
(function (YetaWF) {
    var BasicsServices = /** @class */ (function () {
        function BasicsServices() {
            // Form handling
            this.forms = null;
            // Popup handling
            this.popups = null;
            // Page
            /**
             * currently loaded addons
             */
            this.UnifiedAddonModsLoaded = [];
            // Navigation
            this.suppressPopState = 0;
            this.reloadingModuleTagInModule = null;
            this.reloadInfo = [];
            this.escElement = document.createElement("div");
            // WhenReadyOnce
            // Usage:
            // $YetaWF.addWhenReadyOnce((tag) => {})    // function to be called
            this.whenReadyOnce = [];
            // ClearDiv
            this.ClearDivHandlers = [];
            this.DataObjectCache = [];
            this._pageChanged = false;
        }
        // Implemented by renderer
        // Implemented by renderer
        // Implemented by renderer
        /** Called when a new full page has been loaded and needs to be initialized */
        BasicsServices.prototype.initFullPage = function () {
            YetaWF_BasicsImpl.initFullPage();
        };
        Object.defineProperty(BasicsServices.prototype, "isLoading", {
            /** Returns whether the loading indicator is on or off */
            get: function () {
                return YetaWF_BasicsImpl.isLoading;
            },
            enumerable: false,
            configurable: true
        });
        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        BasicsServices.prototype.setLoading = function (on) {
            YetaWF_BasicsImpl.setLoading(on);
            if (on === false)
                this.pleaseWaitClose();
        };
        /**
         * Displays an informational message.
         */
        BasicsServices.prototype.message = function (message, title, onOK, options) { YetaWF_BasicsImpl.message(message, title, onOK, options); };
        /**
         * Displays an error message.
         */
        BasicsServices.prototype.warning = function (message, title, onOK, options) { YetaWF_BasicsImpl.warning(message, title, onOK, options); };
        /**
         * Displays an error message.
         */
        BasicsServices.prototype.error = function (message, title, onOK, options) { YetaWF_BasicsImpl.error(message, title, onOK, options); };
        /**
         * Displays a confirmation message.
         */
        BasicsServices.prototype.confirm = function (message, title, onOK, options) { YetaWF_BasicsImpl.confirm(message, title, onOK, options); };
        /**
         * Displays an alert message with Yes/No buttons.
         */
        BasicsServices.prototype.alertYesNo = function (message, title, onYes, onNo, options) { YetaWF_BasicsImpl.alertYesNo(message, title, onYes, onNo, options); };
        /**
         * Displays a "Please Wait" message
         */
        BasicsServices.prototype.pleaseWait = function (message, title) { YetaWF_BasicsImpl.pleaseWait(message, title); };
        /**
         * Closes the "Please Wait" message (if any).
         */
        BasicsServices.prototype.pleaseWaitClose = function () { YetaWF_BasicsImpl.pleaseWaitClose(); };
        /**
         * Closes any open overlays, menus, dropdownlists, tooltips, etc. (Popup windows are not handled and are explicitly closed using $YetaWF.Popups)
         */
        BasicsServices.prototype.closeOverlays = function () { YetaWF_BasicsImpl.closeOverlays(); };
        Object.defineProperty(BasicsServices.prototype, "Forms", {
            get: function () {
                if (!this.forms) {
                    this.forms = new YetaWF.Forms(); // if this fails, forms.*.js was not included automatically
                    this.forms.init();
                }
                return this.forms;
            },
            enumerable: false,
            configurable: true
        });
        BasicsServices.prototype.FormsAvailable = function () {
            return this.forms != null;
        };
        Object.defineProperty(BasicsServices.prototype, "Popups", {
            get: function () {
                if (!this.popups) {
                    this.popups = new YetaWF.Popups(); // if this fails, popups.*.js was not included automatically
                    this.popups.init();
                }
                return this.popups;
            },
            enumerable: false,
            configurable: true
        });
        BasicsServices.prototype.PopupsAvailable = function () {
            return this.popups != null;
        };
        // Url parsing
        BasicsServices.prototype.parseUrl = function (url) {
            var uri = new YetaWF.Url();
            uri.parse(url);
            return uri;
        };
        // Focus
        /**
         * Set focus to a suitable field within the specified elements.
         */
        BasicsServices.prototype.setFocus = function (tags) {
            // if we have a dialog popup, don't set the focus
            if (YetaWF_BasicsImpl.messagePopupActive())
                return;
            //TODO: this should also consider input fields with validation errors (although that seems to magically work right now)
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            // if the page as a yFocusOnMe css class, ignore element focus requests
            if ($YetaWF.elementHasClass(document.body, "yFocusOnMe"))
                return;
            var f = null;
            var items = this.getElementsBySelector(".yFocusOnMe", tags);
            items = this.limitToVisibleOnly(items); //:visible
            for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
                var item = items_1[_i];
                if (item.tagName === "DIV") { // if we found a div, find the edit element instead
                    var i = this.getElementsBySelector("input,select,textarea,.yt_dropdownlist_base", [item]);
                    i = this.limitToNotTypeHidden(i); // .not("input[type='hidden']")
                    i = this.limitToVisibleOnly(i); // :visible
                    if (i.length > 0) {
                        f = i[0];
                        break;
                    }
                }
            }
            if (f != null) {
                try {
                    f.focus();
                }
                catch (e) { }
            }
        };
        // Screen size
        /**
         * Sets yCondense/yNoCondense css class on popup or body to indicate screen size.
         * Sets rendering mode based on window size
         * we can't really use @media (max-width:...) in css because popups (in Unified Page Sets) don't use iframes so their size may be small but
         * doesn't match @media screen (ie. the window). So, instead we add the css class yCondense to the <body> or popup <div> to indicate we want
         * a more condensed appearance.
         */
        BasicsServices.prototype.setCondense = function (tag, width) {
            if (width < YVolatile.Skin.MinWidthForCondense) {
                this.elementAddClass(tag, "yCondense");
                this.elementRemoveClass(tag, "yNoCondense");
            }
            else {
                this.elementAddClass(tag, "yNoCondense");
                this.elementRemoveClass(tag, "yCondense");
            }
        };
        // Popup
        /**
         * Returns whether a popup is active
         */
        BasicsServices.prototype.isInPopup = function () {
            return YVolatile.Basics.IsInPopup;
        };
        //
        /**
         * Close any popup window.
         */
        BasicsServices.prototype.closePopup = function (forceReload) {
            if (this.PopupsAvailable())
                this.Popups.closePopup(forceReload);
        };
        // Scrolling
        BasicsServices.prototype.setScrollPosition = function () {
            // positioning isn't exact. For example, TextArea (i.e. CKEditor) will expand the window size which may happen later.
            var uri = this.parseUrl(window.location.href);
            var left = uri.getSearch(YConfigs.Basics.Link_ScrollLeft);
            var top = uri.getSearch(YConfigs.Basics.Link_ScrollTop);
            if (left || top) {
                window.scroll(left ? parseInt(left, 10) : 0, top ? parseInt(top, 10) : 0);
                return true;
            }
            else
                return false;
        };
        /**
         * Initialize the current page (full page load) - runs during page load, before document ready
         */
        BasicsServices.prototype.initPage = function () {
            var _this = this;
            this.initFullPage();
            this.init();
            // page position
            var scrolled = this.setScrollPosition();
            // FOCUS
            // FOCUS
            // FOCUS
            this.registerDocumentReady(function () {
                if (!scrolled && location.hash.length <= 1)
                    _this.setFocus();
                else {
                    var hash = location.hash;
                    if (hash && hash.length > 1) {
                        var target = null;
                        try { // handle invalid id
                            target = $YetaWF.getElement1BySelectorCond(hash);
                        }
                        catch (e) { }
                        if (target) {
                            target.scrollIntoView();
                        }
                    }
                }
            });
            // content navigation
            this.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious; // save loaded addons
        };
        // Panes
        BasicsServices.prototype.showPaneSet = function (id, editMode, equalHeights) {
            var _this = this;
            var div = this.getElementById(id);
            var shown = false;
            if (editMode) {
                div.style.display = "block";
                shown = true;
            }
            else {
                // show the pane if it has modules
                var mod = this.getElement1BySelectorCond("div.yModule", [div]);
                if (mod) {
                    div.style.display = "block";
                    shown = true;
                }
            }
            if (shown && equalHeights) {
                // make all panes the same height
                // this should happen late in case the content is changed dynamically (use with caution)
                // if it does, the pane will still expand because we're only setting the minimum height
                this.registerDocumentReady(function () {
                    var panes = _this.getElementsBySelector("#" + id + " > div"); // get all immediate child divs (i.e., the panes)
                    panes = _this.limitToVisibleOnly(panes); //:visible
                    // exclude panes that have .y_cleardiv
                    var newPanes = [];
                    for (var _i = 0, panes_1 = panes; _i < panes_1.length; _i++) {
                        var pane = panes_1[_i];
                        if (!_this.elementHasClass(pane, "y_cleardiv"))
                            newPanes.push(pane);
                    }
                    panes = newPanes;
                    var height = 0;
                    // calc height
                    for (var _a = 0, panes_2 = panes; _a < panes_2.length; _a++) {
                        var pane = panes_2[_a];
                        var h = pane.clientHeight;
                        if (h > height)
                            height = h;
                    }
                    // set each pane's height
                    for (var _b = 0, panes_3 = panes; _b < panes_3.length; _b++) {
                        var pane = panes_3[_b];
                        pane.style.minHeight = height + "px";
                    }
                });
            }
        };
        BasicsServices.prototype.setUrl = function (url) {
            try {
                var stateObj = {};
                history.pushState(stateObj, "", url);
            }
            catch (err) { }
        };
        BasicsServices.prototype.loadUrl = function (url) {
            var uri = $YetaWF.parseUrl(url);
            var result = $YetaWF.ContentHandling.setContent(uri, true);
            if (result !== YetaWF.SetContentResult.ContentReplaced)
                window.location.assign(url);
        };
        // Reload, refresh
        /**
         * Reloads the current page - in its entirety (full page load)
         */
        BasicsServices.prototype.reloadPage = function (keepPosition, w) {
            if (!w)
                w = window;
            if (!keepPosition)
                keepPosition = false;
            var uri = this.parseUrl(w.location.href);
            uri.removeSearch(YConfigs.Basics.Link_ScrollLeft);
            uri.removeSearch(YConfigs.Basics.Link_ScrollTop);
            if (keepPosition) {
                var left = (document.documentElement && document.documentElement.scrollLeft) || document.body.scrollLeft;
                if (left)
                    uri.addSearch(YConfigs.Basics.Link_ScrollLeft, left.toString());
                var top_1 = (document.documentElement && document.documentElement.scrollTop) || document.body.scrollTop;
                if (top_1)
                    uri.addSearch(YConfigs.Basics.Link_ScrollTop, top_1.toString());
            }
            uri.removeSearch("!rand");
            uri.addSearch("!rand", ((new Date()).getTime()).toString()); // cache buster
            if (this.ContentHandling.setContent(uri, true) !== YetaWF.SetContentResult.NotContent)
                return;
            if (keepPosition) {
                w.location.assign(uri.toUrl());
                return;
            }
            w.location.reload();
        };
        /**
         * Reloads a module in place, defined by the specified tag (any tag within the module).
         */
        BasicsServices.prototype.reloadModule = function (tag) {
            if (!tag) {
                if (!this.reloadingModuleTagInModule)
                    throw "No module found"; /*DEBUG*/
                tag = this.reloadingModuleTagInModule;
            }
            var mod = YetaWF.ModuleBase.getModuleDivFromTag(tag);
            var form = this.getElement1BySelector("form", [mod]);
            this.Forms.submit(form, false, YConfigs.Basics.Link_SubmitIsApply + "=y"); // the form must support a simple Apply
        };
        BasicsServices.prototype.refreshModule = function (mod) {
            if (!this.getElementByIdCond(mod.id))
                throw "Module with id " + mod.id + " not found"; /*DEBUG*/
            this.processReloadInfo(mod.id);
        };
        BasicsServices.prototype.refreshModuleByAnyTag = function (elem) {
            var mod = YetaWF.ModuleBase.getModuleDivFromTag(elem);
            this.processReloadInfo(mod.id);
        };
        BasicsServices.prototype.processReloadInfo = function (moduleId) {
            var len = this.reloadInfo.length;
            for (var i = 0; i < len; ++i) {
                var entry = this.reloadInfo[i];
                if (entry.module.id === moduleId) {
                    if (this.getElementByIdCond(entry.tagId)) {
                        // call the reload callback
                        entry.callback(entry.module);
                    }
                    else {
                        // the tag requesting the callback no longer exists
                        this.reloadInfo.splice(i, 1);
                        --len;
                        --i;
                    }
                }
            }
        };
        BasicsServices.prototype.refreshPage = function () {
            var len = this.reloadInfo.length;
            for (var i = 0; i < len; ++i) {
                var entry = this.reloadInfo[i];
                if (this.getElementByIdCond(entry.module.id)) { // the module exists
                    if (this.getElementByIdCond(entry.tagId)) {
                        // the tag requesting the callback still exists
                        if (!this.elementClosestCond(entry.module, ".yPopup, .yPopupDyn")) // don't refresh modules within popups when refreshing the page
                            entry.callback(entry.module);
                    }
                    else {
                        // the tag requesting the callback no longer exists
                        this.reloadInfo.splice(i, 1);
                        --len;
                        --i;
                    }
                }
                else {
                    // the module no longer exists
                    this.reloadInfo.splice(i, 1);
                    --len;
                    --i;
                }
            }
        };
        /**
         * Registers a callback that is called when a module is to be refreshed/reloaded.
         * @param tag Defines the tag that is requesting the callback when the containing module is refreshed.
         * @param callback Defines the callback to be called.
         * The element defined by tag may no longer exist when a module is refreshed in which case the callback is not called (and removed).
         */
        BasicsServices.prototype.registerModuleRefresh = function (tag, callback) {
            var module = YetaWF.ModuleBase.getModuleDivFromTag(tag); // get the containing module
            if (!tag.id || tag.id.length === 0)
                throw "No id defined for " + tag.outerHTML;
            // reuse existing entry if this id is already registered
            for (var _i = 0, _a = this.reloadInfo; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (entry.tagId === tag.id) {
                    entry.callback = callback;
                    return;
                }
            }
            // new id
            this.reloadInfo.push({ module: module, tagId: tag.id, callback: callback });
        };
        // Module locator
        BasicsServices.prototype.getModuleGuidFromTag = function (tag) {
            var mod = YetaWF.ModuleBase.getModuleDivFromTag(tag);
            var guid = mod.getAttribute("data-moduleguid");
            if (!guid)
                throw "Can't find module guid"; /*DEBUG*/
            return guid;
        };
        // Utility functions
        BasicsServices.prototype.htmlEscape = function (s, preserveCR) {
            var pre = preserveCR ? "&#13;" : "\n";
            return ("" + s) /* Forces the conversion to string. */
                .replace(/&/g, "&amp;") /* This MUST be the 1st replacement. */
                .replace(/'/g, "&apos;") /* The 4 other predefined entities, required. */
                .replace(/"/g, "&quot;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                /*
                You may add other replacements here for HTML only
                (but it's not necessary).
                Or for XML, only if the named entities are defined in its DTD.
                */
                .replace(/\r\n/g, pre) /* Must be before the next replacement. */
                .replace(/[\r\n]/g, pre);
        };
        BasicsServices.prototype.htmlAttrEscape = function (s) {
            this.escElement.textContent = s;
            s = this.escElement.innerHTML;
            return s.replace(/'/g, "&apos;").replace(/"/g, "&quot;");
        };
        /**
         * string compare that considers null == ""
         */
        BasicsServices.prototype.stringCompare = function (str1, str2) {
            if (!str1 && !str2)
                return true;
            return str1 === str2;
        };
        /** Send a GET/POST/... request to the specified URL, expecting a JSON response. Errors are automatically handled. The callback is called once the POST response is available.
         * @param url The URL used for the POST request.
         * @param data The data to send as form data with the POST request.
         * @param callback The callback to call when the POST response is available. Errors are automatically handled.
         * @param tagInModule The optional tag in a module to refresh when AjaxJavascriptReloadModuleParts is returned.
         */
        BasicsServices.prototype.send = function (method, url, data, callback, tagInModule) {
            this.setLoading(true);
            var request = new XMLHttpRequest();
            request.open(method, url, true);
            if (method.toLowerCase() === "post")
                request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            $YetaWF.handleReadyStateChange(request, callback, tagInModule);
            request.send(data);
        };
        /** POST form data to the specified URL, expecting a JSON response. Errors are automatically handled. The callback is called once the POST response is available.
         * @param url The URL used for the POST request.
         * @param data The data to send as form data with the POST request.
         * @param callback The callback to call when the POST response is available. Errors are automatically handled.
         * @param tagInModule The optional tag in a module to refresh when AjaxJavascriptReloadModuleParts is returned.
         */
        BasicsServices.prototype.post = function (url, data, callback, tagInModule) {
            this.setLoading(true);
            var request = new XMLHttpRequest();
            request.open("POST", url, true);
            request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            $YetaWF.handleReadyStateChange(request, callback, tagInModule);
            request.send(data);
        };
        /** POST JSON data to the specified URL, expecting a JSON response. Errors are automatically handled. The callback is called once the POST response is available.
         * @param url The URL used for the POST request.
         * @param data The data to send as form data with the POST request.
         * @param callback The callback to call when the POST response is available. Errors are automatically handled.
         */
        BasicsServices.prototype.postJSON = function (url, data, callback) {
            this.setLoading(true);
            var request = new XMLHttpRequest();
            request.open("POST", url, true);
            request.setRequestHeader("Content-Type", "application/json");
            $YetaWF.handleReadyStateChange(request, callback);
            request.send(JSON.stringify(data));
        };
        BasicsServices.prototype.handleReadyStateChange = function (request, callback, tagInModule) {
            var _this = this;
            request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            request.onreadystatechange = function (ev) {
                if (request.readyState === XMLHttpRequest.DONE) {
                    _this.setLoading(false);
                    if (request.status === 200) {
                        var result = null;
                        if (request.responseText && !request.responseText.startsWith("<"))
                            result = JSON.parse(request.responseText);
                        else
                            result = request.responseText;
                        if (typeof result === "string") {
                            if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                                var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                                if (script.length > 0) {
                                    // eslint-disable-next-line no-eval
                                    eval(script);
                                }
                                callback(true, null);
                                return;
                            }
                            else if (result.startsWith(YConfigs.Basics.AjaxJSONReturn)) {
                                var json = result.substring(YConfigs.Basics.AjaxJSONReturn.length);
                                callback(true, JSON.parse(json));
                                return;
                            }
                            else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                                var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                                // eslint-disable-next-line no-eval
                                eval(script);
                                callback(false, null);
                                return;
                            }
                            else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadPage)) {
                                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadPage.length);
                                // eslint-disable-next-line no-eval
                                eval(script); // if this uses $YetaWF.message or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explicitly reload page in your javascript)
                                _this.reloadPage(true);
                                callback(true, null);
                                return;
                            }
                            else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
                                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
                                // eslint-disable-next-line no-eval
                                eval(script); // if this uses $YetaWF.message or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explicitly reload module in your javascript)
                                _this.reloadModule();
                                callback(true, null);
                                return;
                            }
                            else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
                                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
                                // eslint-disable-next-line no-eval
                                eval(script);
                                if (tagInModule)
                                    _this.refreshModuleByAnyTag(tagInModule);
                                return true;
                            }
                            else {
                                callback(true, result);
                                return;
                            }
                        }
                        callback(true, result);
                    }
                    else if (request.status >= 400 && request.status <= 499) {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, YLocs.Forms.AjaxNotAuth), YLocs.Forms.AjaxErrorTitle);
                        callback(false, null);
                    }
                    else if (request.status === 0) {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, YLocs.Forms.AjaxConnLost), YLocs.Forms.AjaxErrorTitle);
                        callback(false, null);
                    }
                    else {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, request.responseText), YLocs.Forms.AjaxErrorTitle);
                        callback(false, null);
                    }
                }
            };
        };
        // JSX
        /**
         * React-like createElement function so we can use JSX in our TypeScript/JavaScript code.
         */
        BasicsServices.prototype.createElement = function (tag, attrs, children) {
            var element = document.createElement(tag);
            for (var name_1 in attrs) {
                if (name_1 && attrs.hasOwnProperty(name_1)) {
                    var value = attrs[name_1];
                    if (value === true) {
                        element.setAttribute(name_1, name_1);
                    }
                    else if (value !== false && value != null) {
                        element.setAttribute(name_1, value.toString());
                    }
                }
            }
            for (var i = 2; i < arguments.length; i++) {
                var child = arguments[i];
                element.appendChild(!child.nodeType ? document.createTextNode(child.toString()) : child);
            }
            return element;
        };
        // Global script eval
        BasicsServices.prototype.runGlobalScript = function (script) {
            var elem = document.createElement("script");
            elem.text = script;
            var newElem = document.head.appendChild(elem); // add to execute script
            newElem.parentNode.removeChild(newElem); // and remove - we're done with it
        };
        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor tag/elem and only manipulate child objects.
         * THIS IS FOR INTERNAL USE ONLY and is not intended for application use.
         * The callback is called ONCE. Then the callback is removed.
         * @param def
         */
        BasicsServices.prototype.addWhenReadyOnce = function (callback) {
            this.whenReadyOnce.push({ callback: callback });
        };
        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        BasicsServices.prototype.processAllReadyOnce = function (tags) {
            var dummyEntry = null;
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            if (tags.length === 0) {
                // it may happen that new content becomes available without any tags to update.
                // in that case create a dummy tag so all handlers are called. Some handlers don't use the tag and just need to be notified that "something" changed.
                dummyEntry = document.createElement("div");
                tags.push(dummyEntry); // dummy element
            }
            for (var _i = 0, _a = this.whenReadyOnce; _i < _a.length; _i++) {
                var entry = _a[_i];
                try { // catch errors to insure all callbacks are called
                    for (var _b = 0, tags_1 = tags; _b < tags_1.length; _b++) {
                        var tag = tags_1[_b];
                        entry.callback(tag);
                    }
                }
                catch (err) {
                    console.error(err.message);
                }
            }
            this.whenReadyOnce = [];
            if (dummyEntry)
                dummyEntry.remove();
        };
        /**
         * Registers a callback that is called when a <div> is cleared. This is used so templates can register a cleanup
         * callback so elements can be destroyed when a div is emptied (used by UPS).
         * @param autoRemove Set to true to remove the entry when the callback is called and returns true.
         * @param callback The callback to be called when a div is cleared. The callback returns true if the callback performed cleanup processing, false otherwise.
         */
        BasicsServices.prototype.registerClearDiv = function (autoRemove, callback) {
            this.ClearDivHandlers.push({ callback: callback, autoRemove: autoRemove });
        };
        /**
         * Process all callbacks for the specified element being cleared.
         * @param elem The element being cleared.
         */
        BasicsServices.prototype.processClearDiv = function (tag) {
            var newList = [];
            for (var _i = 0, _a = this.ClearDivHandlers; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (entry.callback != null) {
                    try { // catch errors to insure all callbacks are called
                        entry.callback(tag);
                    }
                    catch (err) {
                        console.error(err.message || err);
                    }
                    if (!entry.autoRemove)
                        newList.push(entry);
                }
            }
            // save new list without removed entries
            this.ClearDivHandlers = newList;
            // also release any attached objects
            for (var i = 0; i < this.DataObjectCache.length;) {
                var doe = this.DataObjectCache[i];
                if (this.getElement1BySelectorCond("#" + doe.DivId, [tag])) {
                    console.log("Element #" + doe.DivId + " is being removed but still has a data object - forced cleanup");
                    if (YConfigs.Basics.DEBUGBUILD) {
                        // eslint-disable-next-line no-debugger
                        debugger; // if we hit this, there is an object that's not cleaned up by handling processClearDiv in a component specific way
                    }
                    this.DataObjectCache.splice(i, 1);
                    continue;
                }
                ++i;
            }
        };
        BasicsServices.prototype.validateObjectCache = function () {
            if (YConfigs.Basics.DEBUGBUILD) {
                //DEBUG ONLY
                for (var _i = 0, _a = this.DataObjectCache; _i < _a.length; _i++) {
                    var doe = _a[_i];
                    if (!this.getElement1BySelectorCond("#" + doe.DivId)) {
                        console.log("Element #" + doe.DivId + " no longer exists but still has a data object");
                        // eslint-disable-next-line no-debugger
                        debugger; // if we hit this, there is an object that has no associated dom element
                    }
                }
            }
        };
        /**
         * Adds an object (a Typescript class) to a tag. Used for cleanup when a parent div is removed.
         * Typically used by templates.
         * Objects attached to divs are terminated by processClearDiv which calls any handlers that registered a
         * template class using addObjectDataById.
         * @param tagId - The element id (DOM) where the object is attached
         * @param obj - the object to attach
         */
        BasicsServices.prototype.addObjectDataById = function (tagId, obj) {
            this.validateObjectCache();
            this.getElementById(tagId); // used to validate the existence of the element
            var doe = this.DataObjectCache.filter(function (entry) { return entry.DivId === tagId; });
            if (doe.length > 0)
                throw "addObjectDataById - tag with id " + tagId + " already has data"; /*DEBUG*/
            this.DataObjectCache.push({ DivId: tagId, Data: obj });
        };
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param tagId - The element id (DOM) where the object is attached
         */
        BasicsServices.prototype.getObjectDataByIdCond = function (tagId) {
            var doe = this.DataObjectCache.filter(function (entry) { return entry.DivId === tagId; });
            if (doe.length === 0)
                return null;
            return doe[0].Data;
        };
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param tagId - The element id (DOM) where the object is attached
         */
        BasicsServices.prototype.getObjectDataById = function (tagId) {
            var data = this.getObjectDataByIdCond(tagId);
            if (!data)
                throw "getObjectDataById - tag with id " + tagId + " doesn't have any data"; /*DEBUG*/
            return data;
        };
        /**
         * Retrieves a data object (a Typescript class) from a tag. The data object may not be available.
         * @param tagId - The element id (DOM) where the object is attached
         */
        BasicsServices.prototype.getObjectDataCond = function (element) {
            if (!element.id)
                throw "element without id - " + element.outerHTML;
            return this.getObjectDataByIdCond(element.id);
        };
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param tagId - The element id (DOM) where the object is attached
         */
        BasicsServices.prototype.getObjectData = function (element) {
            if (!element.id)
                throw "element without id - " + element.outerHTML;
            return this.getObjectDataById(element.id);
        };
        /**
         * Removes a data object (a Typescript class) from a tag.
         * @param tagId - The element id (DOM) where the object is attached
         */
        BasicsServices.prototype.removeObjectDataById = function (tagId) {
            this.validateObjectCache();
            this.getElementById(tagId); // used to validate the existence of the element
            for (var i = 0; i < this.DataObjectCache.length; ++i) {
                var doe = this.DataObjectCache[i];
                if (doe.DivId === tagId) {
                    this.DataObjectCache.splice(i, 1);
                    return;
                }
            }
            throw "Element with id " + tagId + " doesn't have attached data"; /*DEBUG*/
        };
        // Selectors
        /**
         * Get an element by id.
         */
        BasicsServices.prototype.getElementById = function (elemId) {
            var div = document.querySelector("#" + elemId);
            if (!div)
                throw "Element with id " + elemId + " not found"; /*DEBUG*/
            return div;
        };
        /**
         * Get an element by id.
         */
        BasicsServices.prototype.getElementByIdCond = function (elemId) {
            var div = document.querySelector("#" + elemId);
            return div;
        };
        /**
         * Get elements from an array of tags by selector. (similar to jquery let x = $(selector, elems); with standard css selectors)
         */
        BasicsServices.prototype.getElementsBySelector = function (selector, elems) {
            var all = [];
            if (!elems) {
                if (!document.body)
                    return all;
                elems = [document.body];
            }
            if (!elems)
                return all;
            for (var _i = 0, elems_1 = elems; _i < elems_1.length; _i++) {
                var elem = elems_1[_i];
                if (elem.matches(selector)) // oddly enough querySelectorAll doesn't return anything even though the element itself matches...
                    all.push(elem);
                else {
                    var list = elem.querySelectorAll(selector);
                    all = all.concat(Array.prototype.slice.call(list));
                }
            }
            return all;
        };
        /**
         * Get the first element from an array of tags by selector. (similar to jquery let x = $(selector, elems); with standard css selectors)
         */
        BasicsServices.prototype.getElement1BySelectorCond = function (selector, elems) {
            if (!elems)
                elems = [document.body];
            for (var _i = 0, elems_2 = elems; _i < elems_2.length; _i++) {
                var elem = elems_2[_i];
                if (elem.matches(selector)) // oddly enough querySelectorAll doesn't return anything even though the element matches...
                    return elem;
                var list = elem.querySelectorAll(selector);
                if (list.length > 0)
                    return list[0];
            }
            return null;
        };
        /**
         * Get the first element from an array of tags by selector. (similar to jquery let x = $(selector, elems); with standard css selectors)
         */
        BasicsServices.prototype.getElement1BySelector = function (selector, elems) {
            var elem = this.getElement1BySelectorCond(selector, elems);
            if (elem == null)
                throw "Element with selector " + selector + " not found";
            return elem;
        };
        /**
         * Removes all input[type='hidden'] fields. (similar to jquery let x = elems.not("input[type='hidden']"); )
         */
        BasicsServices.prototype.limitToNotTypeHidden = function (elems) {
            var all = [];
            for (var _i = 0, elems_3 = elems; _i < elems_3.length; _i++) {
                var elem = elems_3[_i];
                if (elem.tagName !== "INPUT" || elem.getAttribute("type") !== "hidden")
                    all.push(elem);
            }
            return all;
        };
        /**
         * Returns items that are visible. (similar to jquery let x = elems.filter(':visible'); )
         */
        BasicsServices.prototype.limitToVisibleOnly = function (elems) {
            var all = [];
            for (var _i = 0, elems_4 = elems; _i < elems_4.length; _i++) {
                var elem = elems_4[_i];
                if (this.isVisible(elem))
                    all.push(elem);
            }
            return all;
        };
        /**
         * Returns whether the specified element is visible.
         */
        BasicsServices.prototype.isVisible = function (elem) {
            return !!(elem.offsetWidth || elem.offsetHeight || elem.getClientRects().length);
        };
        /**
         * Returns whether the specified element is a parent of the specified child element.
         */
        BasicsServices.prototype.elementHas = function (elem, childElement) {
            var c = childElement;
            for (; c;) {
                if (elem === c)
                    return true;
                c = c.parentElement;
            }
            return false;
        };
        /**
         * Tests whether the specified element matches the selector.
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        BasicsServices.prototype.elementMatches = function (elem, selector) {
            if (elem && elem.matches)
                return elem.matches(selector);
            return false;
        };
        /**
         * Finds the closest element up the DOM hierarchy that matches the selector (including the starting element)
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        BasicsServices.prototype.elementClosestCond = function (elem, selector) {
            var e = elem;
            while (e) {
                if (this.elementMatches(e, selector))
                    return e;
                else
                    e = e.parentElement;
            }
            return null;
        };
        /**
         * Finds the closest element up the DOM hierarchy that matches the selector (including the starting element)
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        BasicsServices.prototype.elementClosest = function (elem, selector) {
            var e = this.elementClosestCond(elem, selector);
            if (!e)
                throw "Closest parent element with selector " + selector + " not found";
            return e;
        };
        // DOM manipulation
        /**
         * Removes the specified element.
         * @param elem - The element to remove.
         */
        BasicsServices.prototype.removeElement = function (elem) {
            if (!elem.parentElement)
                return;
            elem.parentElement.removeChild(elem);
        };
        /**
         * Append content to the specified element. The content is html and optional <script> tags. The scripts are executed after the content is added.
         */
        BasicsServices.prototype.appendMixedHTML = function (elem, content, tableBody) {
            this.calcMixedHTMLRunScripts(content, undefined, function (elems) {
                while (elems.length > 0)
                    elem.insertAdjacentElement("beforeend", elems[0]);
            }, tableBody);
        };
        /**
         * Insert content before the specified element. The content is html and optional <script> tags. The scripts are executed after the content is added.
         */
        BasicsServices.prototype.insertMixedHTML = function (elem, content, tableBody) {
            this.calcMixedHTMLRunScripts(content, undefined, function (elems) {
                while (elems.length > 0)
                    elem.insertAdjacentElement("beforebegin", elems[0]);
            }, tableBody);
        };
        /**
         * Set the specified element's outerHMTL to the content. The content is html and optional <script> tags. The scripts are executed after the content is added.
         */
        BasicsServices.prototype.setMixedOuterHTML = function (elem, content, tableBody) {
            this.calcMixedHTMLRunScripts(content, function (html) {
                elem.outerHTML = content;
            }, undefined, tableBody);
        };
        BasicsServices.prototype.calcMixedHTMLRunScripts = function (content, callbackHTML, callbackChildren, tableBody) {
            // convert the string to DOM representation
            var temp = document.createElement("YetaWFTemp");
            if (tableBody) {
                temp.innerHTML = "<table><tbody>" + content + "</tbody></table>";
                temp = $YetaWF.getElement1BySelector("tbody", [temp]);
            }
            else {
                temp.innerHTML = content;
            }
            // extract all <script> tags
            var scripts = this.getElementsBySelector("script", [temp]);
            for (var _i = 0, scripts_1 = scripts; _i < scripts_1.length; _i++) {
                var script = scripts_1[_i];
                this.removeElement(script); // remove the script element
            }
            // call callback so caller can update whatever needs to be updated
            if (callbackHTML)
                callbackHTML(temp.innerHTML);
            else if (callbackChildren)
                callbackChildren(temp.children);
            // now run/load all scripts we found in the HTML
            for (var _a = 0, scripts_2 = scripts; _a < scripts_2.length; _a++) {
                var script = scripts_2[_a];
                if (script.src) {
                    script.async = false;
                    script.defer = false;
                    var js = document.createElement("script");
                    js.type = "text/javascript";
                    js.async = false; // need to preserve execution order
                    js.defer = false;
                    js.src = script.src;
                    document.body.appendChild(js);
                }
                else if (!script.type || script.type === "application/javascript") {
                    this.runGlobalScript(script.innerHTML);
                }
                else {
                    //throw `Unknown script type ${script.type}`;
                }
            }
        };
        // Element Css
        /**
         * Tests whether the specified element has the given css class.
         * @param elem The element to test.
         * @param css - The css class being tested.
         */
        BasicsServices.prototype.elementHasClass = function (elem, css) {
            css = css.trim();
            if (!elem)
                return false;
            if (css.startsWith("."))
                throw "elementHasClass called with class starting with a . \"" + css + "\" - that probably wasn't intended";
            if (elem.classList)
                return elem.classList.contains(css);
            else
                return new RegExp("(^| )" + css + "( |$)", "gi").test(elem.className);
        };
        /**
         * Tests whether the specified element has a css class that starts with the given prefix.
         * @param elem The element to test.
         * @param cssPrefix - The css class prefix being tested.
         * Returns the entire css class that matches the prefix, or null.
         */
        BasicsServices.prototype.elementHasClassPrefix = function (elem, cssPrefix) {
            var list = [];
            cssPrefix = cssPrefix.trim();
            if (!elem)
                return list;
            if (cssPrefix.startsWith("."))
                throw "elementHasClassPrefix called with cssPrefix starting with a . \"" + cssPrefix + "\" - that probably wasn't intended";
            if (elem.classList) {
                // eslint-disable-next-line @typescript-eslint/prefer-for-of
                for (var i = 0; i < elem.classList.length; ++i) {
                    if (elem.classList[i].startsWith(cssPrefix))
                        list.push(elem.classList[i]);
                }
            }
            else if (elem.className && typeof elem.className === "string") {
                var cs = elem.className.split(" ");
                for (var _i = 0, cs_1 = cs; _i < cs_1.length; _i++) {
                    var c = cs_1[_i];
                    if (c.startsWith(cssPrefix))
                        list.push(c);
                }
            }
            return list;
        };
        /**
         * Add a space separated list of css classes to an element.
         */
        BasicsServices.prototype.elementAddClassList = function (elem, classNames) {
            if (!classNames)
                return;
            for (var _i = 0, _a = classNames.split(" "); _i < _a.length; _i++) {
                var s = _a[_i];
                if (s.length > 0)
                    this.elementAddClass(elem, s);
            }
        };
        /**
         * Add an array of css classes to an element.
         */
        BasicsServices.prototype.elementAddClasses = function (elem, classNames) {
            for (var _i = 0, classNames_1 = classNames; _i < classNames_1.length; _i++) {
                var s = classNames_1[_i];
                if (s.length > 0)
                    this.elementAddClass(elem, s);
            }
        };
        /**
         * Add css class to an element.
         */
        BasicsServices.prototype.elementAddClass = function (elem, className) {
            if (className.startsWith("."))
                throw "elementAddClass called with class starting with a . \"" + className + "\" - that probably wasn't intended";
            if (elem.classList)
                elem.classList.add(className);
            else
                elem.className += " " + className;
        };
        /**
         * Remove a space separated list of css classes from an element.
         */
        BasicsServices.prototype.elementRemoveClassList = function (elem, classNames) {
            if (!classNames)
                return;
            for (var _i = 0, _a = classNames.split(" "); _i < _a.length; _i++) {
                var s = _a[_i];
                if (s.length > 0)
                    this.elementRemoveClass(elem, s);
            }
        };
        /**
         * Remove an array of css classes from an element.
         */
        BasicsServices.prototype.elementRemoveClasses = function (elem, classNames) {
            for (var _i = 0, classNames_2 = classNames; _i < classNames_2.length; _i++) {
                var s = classNames_2[_i];
                if (s.length > 0)
                    this.elementRemoveClass(elem, s);
            }
        };
        /**
         * Remove a css class from an element.
         */
        BasicsServices.prototype.elementRemoveClass = function (elem, className) {
            if (className.startsWith("."))
                throw "elementRemoveClass called with class starting with a . \"" + className + "\" - that probably wasn't intended";
            if (elem.classList)
                elem.classList.remove(className);
            else
                elem.className = elem.className.replace(new RegExp("(^|\\b)" + className.split(" ").join("|") + "(\\b|$)", "gi"), " ");
        };
        /*
         * Add/remove a class to an element.
         */
        BasicsServices.prototype.elementToggleClass = function (elem, className, set) {
            if (set) {
                if (this.elementHasClass(elem, className))
                    return;
                this.elementAddClass(elem, className);
            }
            else {
                this.elementRemoveClass(elem, className);
            }
        };
        // Attributes
        /**
         * Returns an attribute value. Throws an error if the attribute doesn't exist.
         */
        BasicsServices.prototype.getAttribute = function (elem, name) {
            var val = elem.getAttribute(name);
            if (!val)
                throw "missing " + name + " attribute";
            return val;
        };
        /**
         * Returns an attribute value.
         */
        BasicsServices.prototype.getAttributeCond = function (elem, name) {
            return elem.getAttribute(name);
        };
        /**
         * Sets an attribute.
         */
        BasicsServices.prototype.setAttribute = function (elem, name, value) {
            elem.setAttribute(name, value);
        };
        /**
         * Enable element.
         */
        BasicsServices.prototype.elementEnable = function (elem) {
            YetaWF_BasicsImpl.elementEnableToggle(elem, true);
        };
        /**
         * Disable element.
         */
        BasicsServices.prototype.elementDisable = function (elem) {
            YetaWF_BasicsImpl.elementEnableToggle(elem, false);
        };
        /**
         * Enable or disable element.
         */
        BasicsServices.prototype.elementEnableToggle = function (elem, enable) {
            if (enable)
                this.elementEnable(elem);
            else
                this.elementDisable(elem);
        };
        /**
         * Returns whether the element is enabled.
         */
        BasicsServices.prototype.isEnabled = function (elem) {
            return YetaWF_BasicsImpl.isEnabled(elem);
        };
        /**
         * Given an element, returns the owner (typically a module) that owns the element.
         * The DOM hierarchy may not reflect this ownership, for example with popup menus which are appended to the <body> tag, but are owned by specific modules.
         */
        BasicsServices.prototype.getOwnerFromTag = function (tag) {
            return YetaWF_BasicsImpl.getOwnerFromTag(tag);
        };
        // Events
        /**
         * Send a custom event on behalf of an element.
         * @param elem The element sending the event.
         * @param name The name of the event.
         */
        BasicsServices.prototype.sendCustomEvent = function (elem, name, details) {
            var event = new CustomEvent("CustomEvent", { "detail": details !== null && details !== void 0 ? details : {} });
            event.initEvent(name, true, true);
            elem.dispatchEvent(event);
            return !event.cancelBubble && !event.defaultPrevented;
        };
        BasicsServices.prototype.registerDocumentReady = function (callback) {
            if (document.attachEvent ? document.readyState === "complete" : document.readyState !== "loading") {
                callback();
            }
            else {
                document.addEventListener("DOMContentLoaded", callback);
            }
        };
        BasicsServices.prototype.registerEventHandlerBody = function (eventName, selector, callback) {
            var _this = this;
            if (!document.body) {
                $YetaWF.addWhenReadyOnce(function (tag) {
                    _this.registerEventHandler(document.body, eventName, selector, callback);
                });
            }
            else {
                this.registerEventHandler(document.body, eventName, selector, callback);
            }
        };
        BasicsServices.prototype.registerMultipleEventHandlersBody = function (eventNames, selector, callback) {
            var _this = this;
            if (!document.body) {
                $YetaWF.addWhenReadyOnce(function (tag) {
                    for (var _i = 0, eventNames_2 = eventNames; _i < eventNames_2.length; _i++) {
                        var eventName = eventNames_2[_i];
                        document.body.addEventListener(eventName, function (ev) { return _this.handleEvent(document.body, ev, selector, callback); });
                    }
                });
            }
            else {
                for (var _i = 0, eventNames_1 = eventNames; _i < eventNames_1.length; _i++) {
                    var eventName = eventNames_1[_i];
                    document.body.addEventListener(eventName, function (ev) { return _this.handleEvent(document.body, ev, selector, callback); });
                }
            }
        };
        BasicsServices.prototype.registerEventHandlerDocument = function (eventName, selector, callback) {
            var _this = this;
            document.addEventListener(eventName, function (ev) { return _this.handleEvent(null, ev, selector, callback); });
        };
        BasicsServices.prototype.registerMultipleEventHandlersDocument = function (eventNames, selector, callback) {
            var _this = this;
            for (var _i = 0, eventNames_3 = eventNames; _i < eventNames_3.length; _i++) {
                var eventName = eventNames_3[_i];
                document.addEventListener(eventName, function (ev) { return _this.handleEvent(null, ev, selector, callback); });
            }
        };
        BasicsServices.prototype.registerEventHandlerWindow = function (eventName, selector, callback) {
            var _this = this;
            window.addEventListener(eventName, function (ev) { return _this.handleEvent(null, ev, selector, callback); });
        };
        BasicsServices.prototype.registerEventHandler = function (tag, eventName, selector, callback) {
            var _this = this;
            tag.addEventListener(eventName, function (ev) { return _this.handleEvent(tag, ev, selector, callback); });
        };
        BasicsServices.prototype.registerMultipleEventHandlers = function (tags, eventNames, selector, callback) {
            var _this = this;
            var _loop_1 = function (tag) {
                if (tag) {
                    for (var _a = 0, eventNames_4 = eventNames; _a < eventNames_4.length; _a++) {
                        var eventName = eventNames_4[_a];
                        tag.addEventListener(eventName, function (ev) { return _this.handleEvent(tag, ev, selector, callback); });
                    }
                }
            };
            for (var _i = 0, tags_2 = tags; _i < tags_2.length; _i++) {
                var tag = tags_2[_i];
                _loop_1(tag);
            }
        };
        BasicsServices.prototype.registerCustomEventHandlerDocument = function (eventName, selector, callback) {
            var _this = this;
            document.addEventListener(eventName, function (ev) { return _this.handleEvent(document.body, ev, selector, callback); });
        };
        BasicsServices.prototype.registerCustomEventHandler = function (tag, eventName, selector, callback) {
            var _this = this;
            tag.addEventListener(eventName, function (ev) { return _this.handleEvent(tag, ev, selector, callback); });
        };
        BasicsServices.prototype.registerMultipleCustomEventHandlers = function (tags, eventNames, selector, callback) {
            var _this = this;
            var _loop_2 = function (tag) {
                if (tag) {
                    for (var _a = 0, eventNames_5 = eventNames; _a < eventNames_5.length; _a++) {
                        var eventName = eventNames_5[_a];
                        tag.addEventListener(eventName, function (ev) { return _this.handleEvent(tag, ev, selector, callback); });
                    }
                }
            };
            for (var _i = 0, tags_3 = tags; _i < tags_3.length; _i++) {
                var tag = tags_3[_i];
                _loop_2(tag);
            }
        };
        BasicsServices.prototype.handleEvent = function (listening, ev, selector, callback) {
            // about event handling https://www.sitepoint.com/event-bubbling-javascript/
            //console.log(`event ${ev.type} selector ${selector} target ${(ev.target as HTMLElement).outerHTML}`);
            if (ev.cancelBubble || ev.defaultPrevented)
                return;
            var elem = ev.target;
            if (ev.eventPhase === ev.CAPTURING_PHASE) {
                if (selector)
                    return; // if we have a selector we can't possibly have a match because the src element is the main tag where we registered the listener
            }
            else if (ev.eventPhase === ev.AT_TARGET) {
                if (selector)
                    return; // if we have a selector we can't possibly have a match because the src element is the main tag where we registered the listener
            }
            else if (ev.eventPhase === ev.BUBBLING_PHASE) {
                if (selector) {
                    // check elements between the one that caused the event and the listening element (inclusive) for a match to the selector
                    while (elem) {
                        if (this.elementMatches(elem, selector))
                            break;
                        if (listening === elem)
                            return; // checked all elements
                        elem = elem.parentElement || elem.parentNode;
                    }
                }
                else {
                    // check whether the target or one of its parents is the listening element
                    while (elem) {
                        if (listening === elem)
                            break;
                        elem = elem.parentElement || elem.parentNode;
                    }
                }
                if (!elem)
                    return;
            }
            else
                return;
            //console.log(`event ${ev.type} selector ${selector} match`);
            ev.__YetaWFElem = (elem || ev.target); // pass the matching element to the callback
            var result = callback(ev);
            if (!result) {
                //console.log(`event ${ev.type} selector ${selector} stop bubble`);
                ev.stopPropagation();
                ev.preventDefault();
            }
        };
        BasicsServices.prototype.handleInputReturnKeyForButton = function (input, button) {
            $YetaWF.registerEventHandler(input, "keydown", null, function (ev) {
                if (ev.keyCode === 13) {
                    button.click();
                    return false;
                }
                return true;
            });
        };
        // ADDONCHANGE
        // ADDONCHANGE
        // ADDONCHANGE
        BasicsServices.prototype.sendAddonChangedEvent = function (addonGuid, on) {
            var details = { addonGuid: addonGuid.toLowerCase(), on: on };
            this.sendCustomEvent(document.body, BasicsServices.EVENTADDONCHANGED, details);
        };
        // PANELSWITCHED
        // PANELSWITCHED
        // PANELSWITCHED
        BasicsServices.prototype.sendPanelSwitchedEvent = function (panel) {
            var details = { panel: panel };
            this.sendCustomEvent(document.body, BasicsServices.EVENTPANELSWITCHED, details);
        };
        // ACTIVATEDIV
        // ACTIVATEDIV
        // ACTIVATEDIV
        BasicsServices.prototype.sendActivateDivEvent = function (tags) {
            var details = { tags: tags };
            this.sendCustomEvent(document.body, BasicsServices.EVENTACTIVATEDIV, details);
        };
        // CONTAINER SCROLLING
        // CONTAINER SCROLLING
        // CONTAINER SCROLLING
        BasicsServices.prototype.sendContainerScrollEvent = function (container) {
            if (!container)
                container = document.body;
            var details = { container: container };
            this.sendCustomEvent(document.body, BasicsServices.EVENTCONTAINERSCROLL, details);
        };
        // CONTAINER RESIZING
        // CONTAINER RESIZING
        // CONTAINER RESIZING
        BasicsServices.prototype.sendContainerResizeEvent = function (container) {
            if (!container)
                container = document.body;
            var details = { container: container };
            this.sendCustomEvent(document.body, BasicsServices.EVENTCONTAINERRESIZE, details);
        };
        // CONTENT RESIZED
        // CONTENT RESIZED
        // CONTENT RESIZED
        BasicsServices.prototype.sendContentResizedEvent = function (tag) {
            var details = { tag: tag };
            this.sendCustomEvent(document.body, BasicsServices.EVENTCONTENTRESIZED, details);
        };
        // Expand/collapse Support
        /**
         * Expand/collapse support using 2 action links (Name=Expand/Collapse) which make 2 divs hidden/visible  (alternating)
         * @param divId The <div> containing the 2 action links.
         * @param collapsedId - The <div> to hide/show.
         * @param expandedId - The <div> to show/hide.
         */
        BasicsServices.prototype.expandCollapseHandling = function (divId, collapsedId, expandedId) {
            var _this = this;
            var div = this.getElementById(divId);
            var collapsedDiv = this.getElementById(collapsedId);
            var expandedDiv = this.getElementById(expandedId);
            var expLink = this.getElement1BySelector("a[data-name='Expand']", [div]);
            var collLink = this.getElement1BySelector("a[data-name='Collapse']", [div]);
            this.registerEventHandler(expLink, "click", null, function (ev) {
                collapsedDiv.style.display = "none";
                expandedDiv.style.display = "";
                // init any controls that just became visible
                _this.sendActivateDivEvent([expandedDiv]);
                return true;
            });
            this.registerEventHandler(collLink, "click", null, function (ev) {
                collapsedDiv.style.display = "";
                expandedDiv.style.display = "none";
                return true;
            });
        };
        // Rudimentary mobile detection
        BasicsServices.prototype.isMobile = function () {
            return (YVolatile.Skin.MinWidthForPopups > 0 && YVolatile.Skin.MinWidthForPopups > window.outerWidth) || (YVolatile.Skin.MinWidthForPopups === 0 && window.outerWidth <= 970);
        };
        // Positioning
        /**
         * Position an element (sub) below an element (main), or above if there is insufficient space below.
         * The elements are always aligned at their left edges.
         * @param main The main element.
         * @param sub The element to be position below/above the main element.
         */
        BasicsServices.prototype.positionLeftAlignedBelow = function (main, sub) {
            this.positionAlignedBelow(main, sub, true);
        };
        /**
         * Position an element (sub) below an element (main), or above if there is insufficient space below.
         * The elements are always aligned at their right edges.
         * @param main The main element.
         * @param sub The element to be position below/above the main element.
         */
        BasicsServices.prototype.positionRightAlignedBelow = function (main, sub) {
            this.positionAlignedBelow(main, sub, false);
        };
        /**
         * Position an element (sub) below an element (main), or above if there is insufficient space below.
         * The elements are always aligned at their left or right edges.
         * @param main The main element.
         * @param sub The element to be position below/above the main element.
         * @param left Defines whether the sub element is positioned to the left (true) or right (false).
         */
        BasicsServices.prototype.positionAlignedBelow = function (main, sub, left) {
            // position within view to calculate size
            sub.style.top = "0px";
            sub.style.left = "0px";
            sub.style.right = "";
            sub.style.bottom = "";
            // position to fit
            var mainRect = main.getBoundingClientRect();
            var subRect = sub.getBoundingClientRect();
            var bottomAvailable = window.innerHeight - mainRect.bottom;
            var topAvailable = mainRect.top;
            // Top/bottom position and height calculation
            var top = 0, bottom = 0;
            if (bottomAvailable < subRect.height && topAvailable > bottomAvailable) {
                bottom = window.innerHeight - mainRect.top;
                top = mainRect.top - subRect.height;
                if (top <= 0)
                    sub.style.top = "0px";
                else
                    sub.style.top = "";
                sub.style.bottom = bottom - window.pageYOffset + "px";
            }
            else {
                top = mainRect.bottom;
                bottom = top + subRect.height;
                bottom = window.innerHeight - bottom;
                if (bottom < 0)
                    sub.style.bottom = "0px";
                sub.style.top = top + window.pageYOffset + "px";
            }
            if (left) {
                // set left
                sub.style.left = mainRect.left + window.pageXOffset + "px";
                if (mainRect.left + subRect.right > window.innerWidth)
                    sub.style.right = "0px";
            }
            else {
                // set right
                var left_1 = mainRect.right - subRect.width + window.pageXOffset;
                if (left_1 < 0)
                    left_1 = 0;
                sub.style.left = left_1 + "px";
            }
        };
        BasicsServices.prototype.init = function () {
            var _this = this;
            this.AnchorHandling.init();
            this.ContentHandling.init();
            // screen size yCondense/yNoCondense support
            $YetaWF.registerCustomEventHandlerDocument(YetaWF.BasicsServices.EVENTCONTAINERRESIZE, null, function (ev) {
                _this.setCondense(document.body, window.innerWidth);
                return true;
            });
            this.registerDocumentReady(function () {
                _this.setCondense(document.body, window.innerWidth);
            });
            // Navigation
            this.registerEventHandlerWindow("popstate", null, function (ev) {
                if (_this.suppressPopState > 0) {
                    --_this.suppressPopState;
                    return true;
                }
                var uri = _this.parseUrl(window.location.href);
                return _this.ContentHandling.setContent(uri, false) !== YetaWF.SetContentResult.NotContent;
            });
            // <A> links
            // <a> links that only have a hash are intercepted so we don't go through content handling
            this.registerEventHandlerBody("click", "a[href^='#']", function (ev) {
                // find the real anchor, ev.target was clicked, but it may not be the anchor itself
                if (!ev.target)
                    return true;
                var anchor = $YetaWF.elementClosestCond(ev.target, "a");
                if (!anchor)
                    return true;
                ++_this.suppressPopState;
                setTimeout(function () {
                    if (_this.suppressPopState > 0)
                        --_this.suppressPopState;
                }, 200);
                return true;
            });
            // Scrolling
            window.addEventListener("scroll", function (ev) {
                $YetaWF.sendContainerScrollEvent();
            });
            // Debounce resizing
            var resizeTimeout = 0;
            window.addEventListener("resize", function (ev) {
                if (resizeTimeout) {
                    clearTimeout(resizeTimeout);
                }
                resizeTimeout = setTimeout(function () {
                    $YetaWF.sendContainerResizeEvent();
                    resizeTimeout = 0;
                }, 100);
            });
            // WhenReady
            this.registerDocumentReady(function () {
                _this.processAllReadyOnce();
            });
            setTimeout(function () {
                $YetaWF.sendCustomEvent(document.body, YetaWF.Content.EVENTNAVPAGELOADED, { containers: [document.body] });
            }, 1);
        };
        Object.defineProperty(BasicsServices.prototype, "isPrinting", {
            /* Print support */
            get: function () {
                return BasicsServices.printing;
            },
            enumerable: false,
            configurable: true
        });
        BasicsServices.prototype.DoPrint = function () {
            YetaWF.BasicsServices.onBeforePrint(); // window.print doesn't generate onBeforePrint
            window.print();
        };
        BasicsServices.onBeforePrint = function () {
            BasicsServices.printing = true;
            $YetaWF.sendCustomEvent(window.document, BasicsServices.EVENTBEFOREPRINT);
        };
        BasicsServices.onAfterPrint = function () {
            BasicsServices.printing = false;
            $YetaWF.sendCustomEvent(window.document, BasicsServices.EVENTAFTERPRINT);
        };
        Object.defineProperty(BasicsServices.prototype, "pageChanged", {
            // Page modification support (used with onbeforeunload)
            get: function () {
                return this._pageChanged;
            },
            set: function (value) {
                if (this._pageChanged !== value) {
                    this._pageChanged = value;
                    this.sendCustomEvent(document.body, BasicsServices.PAGECHANGEDEVENT);
                }
            },
            enumerable: false,
            configurable: true
        });
        BasicsServices.PAGECHANGEDEVENT = "page_change";
        BasicsServices.EVENTBEFOREPRINT = "print_before";
        BasicsServices.EVENTAFTERPRINT = "print_after";
        BasicsServices.EVENTCONTAINERSCROLL = "container_scroll";
        BasicsServices.EVENTCONTAINERRESIZE = "container_resize";
        BasicsServices.EVENTCONTENTRESIZED = "content_resized";
        BasicsServices.EVENTACTIVATEDIV = "activate_div";
        BasicsServices.EVENTPANELSWITCHED = "panel_switched";
        BasicsServices.EVENTADDONCHANGED = "addon_changed";
        BasicsServices.printing = false;
        return BasicsServices;
    }());
    YetaWF.BasicsServices = BasicsServices;
})(YetaWF || (YetaWF = {}));
/**
 * Basic services available throughout YetaWF.
 */
var $YetaWF = new YetaWF.BasicsServices();
$YetaWF.AnchorHandling = new YetaWF.Anchors();
$YetaWF.ContentHandling = new YetaWF.Content();
/* Print support */
if (window.matchMedia) {
    var mediaQueryList = window.matchMedia("print");
    mediaQueryList.addListener(function (ev) {
        // eslint-disable-next-line no-invalid-this
        if (this.matches) {
            YetaWF.BasicsServices.onBeforePrint();
        }
        else {
            YetaWF.BasicsServices.onAfterPrint();
        }
    });
}
window.onbeforeprint = function (ev) { YetaWF.BasicsServices.onBeforePrint(); };
window.onafterprint = function (ev) { YetaWF.BasicsServices.onAfterPrint(); };
window.onbeforeunload = function (ev) {
    if ($YetaWF.pageChanged) {
        ev.returnValue = "Are you sure you want to leave this page? There are unsaved changes."; // Chrome requires returnValue to be set
        $YetaWF.setLoading(false); // turn off loading indicator in case it's set
        ev.preventDefault(); // If you prevent default behavior in Mozilla Firefox prompt will always be shown
    }
};

//# sourceMappingURL=Basics.js.map
