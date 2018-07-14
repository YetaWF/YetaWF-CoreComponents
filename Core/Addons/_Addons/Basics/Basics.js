"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
/**
 * Class implementing basic services used throughout YetaWF.
 */
var YetaWF;
(function (YetaWF) {
    ;
    ;
    var BasicsServices /* implements IBasicsImpl */ = /** @class */ (function () {
        function BasicsServices() {
            var _this = this;
            // Page
            /**
             * currently loaded addons
             */
            this.UnifiedAddonModsLoaded = [];
            // Navigation
            this.suppressPopState = false;
            this.reloadingModule_TagInModule = null;
            // Usage:
            // YetaWF_Basics.reloadInfo.push({  // TODO: revisit this (not a nice interface, need add(), but only used in grid for now)
            //   module: mod,               // module <div> to be refreshed
            //   callback: function() {}    // function to be called
            // });
            this.reloadInfo = [];
            // WhenReady
            // Usage:
            // YetaWF_Basics.addWhenReady((tag) => {});
            this.whenReady = [];
            // WhenReadyOnce
            /* TODO: This is public and push() is used to add callbacks (legacy Javascript ONLY) */
            // Usage:
            // YetaWF_Basics.whenReadyOnce.push({
            //   callback: function(tag) {}    // function to be called
            // });
            //   or
            // YetaWF_Basics.whenReadyOnce.push({
            //   callbackTS: function(elem) {}    // function to be called
            // });
            this.whenReadyOnce = [];
            // ClearDiv
            this.clearDiv = [];
            // CONTENTCHANGE
            // CONTENTCHANGE
            // CONTENTCHANGE
            this.ContentChangeHandlers = [];
            // NEWPAGE
            // NEWPAGE
            // NEWPAGE
            this.NewPageHandlers = [];
            YetaWF_Basics = this; // set global so we can initialize anchor/content
            this.AnchorHandling = new YetaWF.Anchors();
            this.ContentHandling = new YetaWF.Content();
            // screen size yCondense/yNoCondense support
            this.registerEventHandlerWindow("resize", null, function (ev) {
                _this.setCondense(document.body, window.innerWidth);
                return true;
            });
            this.registerDocumentReady(function () {
                _this.setCondense(document.body, window.innerWidth);
            });
            // Navigation
            this.registerEventHandlerWindow("popstate", null, function (ev) {
                if (_this.suppressPopState) {
                    _this.suppressPopState = false;
                    return true;
                }
                var uri = _this.parseUrl(window.location.href);
                return !_this.ContentHandling.setContent(uri, false);
            });
            // <a> links that only have a hash are intercepted so we don't go through content handling
            this.registerEventHandlerBody("click", "a[href^='#']", function (ev) {
                // find the real anchor, ev.srcElement was clicked, but it may not be the anchor itself
                if (!ev.srcElement)
                    return true;
                var anchor = YetaWF_Basics.elementClosest(ev.srcElement, "a");
                if (!anchor)
                    return true;
                _this.suppressPopState = true;
                return true;
            });
            // <A> links
            // WhenReady
            this.registerDocumentReady(function () {
                _this.processAllReady();
                _this.processAllReadyOnce();
            });
        }
        // Implemented by renderer
        // Implemented by renderer
        // Implemented by renderer
        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        BasicsServices.prototype.setLoading = function (on) {
            YetaWF_BasicsImpl.setLoading(on);
            if (on == false)
                this.pleaseWaitClose();
        };
        /**
         * Displays an informational message, usually in a popup.
         */
        BasicsServices.prototype.message = function (message, title, onOK, options) { YetaWF_BasicsImpl.message(message, title, onOK, options); };
        /**
         * Displays an error message, usually in a popup.
         */
        BasicsServices.prototype.error = function (message, title, onOK, options) { YetaWF_BasicsImpl.error(message, title, onOK, options); };
        /**
         * Displays a confirmation message, usually in a popup.
         */
        BasicsServices.prototype.confirm = function (message, title, onOK, options) { YetaWF_BasicsImpl.confirm(message, title, onOK, options); };
        /**
         * Displays an alert message, usually in a popup.
         */
        BasicsServices.prototype.alert = function (message, title, onOK, options) { YetaWF_BasicsImpl.alert(message, title, onOK, options); };
        /**
         * Displays an alert message with Yes/No buttons, usually in a popup.
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
            //TODO: this should also consider input fields with validation errors (although that seems to magically work right now)
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            var f = null;
            var items = this.getElementsBySelector('.focusonme', tags);
            items = this.limitToVisibleOnly(items); //:visible
            for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
                var item = items_1[_i];
                if (item.tagName == "DIV") { // if we found a div, find the edit element instead
                    var i = this.getElementsBySelector('input,select,.yt_dropdownlist_base', [item]);
                    i = this.limitToNotTypeHidden(i); // .not("input[type='hidden']")
                    i = this.limitToVisibleOnly(i); // :visible
                    if (i.length > 0) {
                        f = i[0];
                        break;
                    }
                }
            }
            // We probably don't want to set the focus to any control - made OPT-IN for now
            //if ($f == null) {
            //    $items = $('input:visible,select:visible', $obj).not("input[type='hidden']");// just find something usable
            //    // filter out anything in a grid (filters, pager, etc)
            //    $items.each(function (index) {
            //        var $i = $(this)
            //        if ($i.parents('.ui-jqgrid').length == 0) {
            //            $f = $i;
            //            return false; // not in a grid, so it's ok
            //        }
            //    });
            //}
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
            if (width < YVolatile.Skin.MinWidthForPopups) {
                this.elementAddClass(tag, 'yCondense');
                this.elementRemoveClass(tag, 'yNoCondense');
            }
            else {
                this.elementAddClass(tag, 'yNoCondense');
                this.elementRemoveClass(tag, 'yCondense');
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
            if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined)
                YetaWF_Popups.closePopup(forceReload);
        };
        // Scrolling
        BasicsServices.prototype.setScrollPosition = function () {
            // positioning isn't exact. For example, TextArea (i.e. CKEditor) will expand the window size which may happen later.
            var uri = this.parseUrl(window.location.href);
            var v = uri.getSearch(YConfigs.Basics.Link_ScrollLeft);
            var scrolled = false;
            if (v != undefined) {
                $(window).scrollLeft(Number(v)); // JQuery use
                scrolled = true;
            }
            v = uri.getSearch(YConfigs.Basics.Link_ScrollTop);
            if (v != undefined) {
                $(window).scrollTop(Number(v)); // JQuery use
                scrolled = true;
            }
            return scrolled;
        };
        ;
        /**
         * Initialize the current page (full page load) - runs during page load, before document ready
         */
        BasicsServices.prototype.initPage = function () {
            // page position
            var _this = this;
            var scrolled = this.setScrollPosition();
            if (!scrolled) {
                if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.ShowDivs) {
                    var uri = this.parseUrl(window.location.href);
                    var divs = this.getElementsBySelector(".yUnified[data-url=\"" + uri.getPath() + "\"]");
                    if (divs.length > 0) {
                        $(window).scrollTop($(divs).offset().top);
                        scrolled = true;
                    }
                }
            }
            // FOCUS
            // FOCUS
            // FOCUS
            this.registerDocumentReady(function () {
                if (!scrolled && location.hash.length <= 1)
                    _this.setFocus();
            });
            // content navigation
            this.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious; // save loaded addons
        };
        ;
        // Panes
        BasicsServices.prototype.showPaneSet = function (id, editMode, equalHeights) {
            var _this = this;
            var div = this.getElementById(id);
            var shown = false;
            if (editMode) {
                div.style.display = 'block';
                shown = true;
            }
            else {
                // show the pane if it has modules
                var mod = this.getElement1BySelectorCond('div.yModule', [div]);
                if (mod) {
                    div.style.display = 'block';
                    shown = true;
                }
            }
            if (shown && equalHeights) {
                // make all panes the same height
                // this should happen late in case the content is changed dynamically (use with caution)
                // if it does, the pane will still expand because we're only setting the minimum height
                this.registerDocumentReady(function () {
                    var panes = _this.getElementsBySelector("#" + id + " > div:visible"); // get all immediate child divs (i.e., the panes)
                    // exclude panes that have .y_cleardiv
                    var newPanes = [];
                    for (var _i = 0, panes_1 = panes; _i < panes_1.length; _i++) {
                        var pane = panes_1[_i];
                        if (!_this.elementHasClass(pane, 'y_cleardiv'))
                            newPanes.push(pane);
                    }
                    panes = newPanes;
                    var height = 0;
                    // calc height
                    for (var _a = 0, panes_2 = panes; _a < panes_2.length; _a++) {
                        var pane = panes_2[_a];
                        var h = $(pane).height() || 0;
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
                var v = $(w).scrollLeft();
                if (v)
                    uri.addSearch(YConfigs.Basics.Link_ScrollLeft, v.toString());
                v = $(w).scrollTop();
                if (v)
                    uri.addSearch(YConfigs.Basics.Link_ScrollTop, v.toString());
            }
            uri.removeSearch("!rand");
            uri.addSearch("!rand", ((new Date()).getTime()).toString()); // cache buster
            if (YVolatile.Basics.UnifiedMode != YetaWF.UnifiedModeEnum.None) {
                if (this.ContentHandling.setContent(uri, true))
                    return;
            }
            if (keepPosition) {
                w.location.assign(uri.toUrl());
                return;
            }
            w.location.reload(true);
        };
        /**
         * Reloads a module in place, defined by the specified tag (any tag within the module).
         */
        BasicsServices.prototype.reloadModule = function (tag) {
            if (!tag) {
                if (!this.reloadingModule_TagInModule)
                    throw "No module found"; /*DEBUG*/
                tag = this.reloadingModule_TagInModule;
            }
            var mod = this.getModuleFromTag(tag);
            var form = this.getElement1BySelector('form', [mod]);
            YetaWF_Forms.submit(form, false, YConfigs.Basics.Link_SubmitIsApply + "=y"); // the form must support a simple Apply
        };
        BasicsServices.prototype.refreshModule = function (mod) {
            for (var _i = 0, _a = this.reloadInfo; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (entry.module.id == mod.id) {
                    entry.callback();
                }
            }
        };
        ;
        BasicsServices.prototype.refreshModuleByAnyTag = function (elem) {
            var mod = this.getModuleFromTag(elem);
            for (var _i = 0, _a = this.reloadInfo; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (entry.module.id == mod.id) {
                    entry.callback();
                }
            }
        };
        ;
        BasicsServices.prototype.refreshPage = function () {
            for (var _i = 0, _a = this.reloadInfo; _i < _a.length; _i++) {
                var entry = _a[_i];
                entry.callback();
            }
        };
        ;
        // Module locator
        /**
         * Get a module defined by the specified tag (any tag within the module). Returns null if none found.
         */
        BasicsServices.prototype.getModuleFromTagCond = function (tag) {
            var mod = this.elementClosest(tag, '.yModule');
            if (mod)
                return null;
            return mod;
        };
        ;
        /**
         * Get a module defined by the specified tag (any tag within the module). Throws exception if none found.
         */
        BasicsServices.prototype.getModuleFromTag = function (tag) {
            var mod = this.getModuleFromTagCond(tag);
            if (mod == null) {
                debugger;
                throw "Can't find containing module";
            } /*DEBUG*/
            return mod;
        };
        ;
        BasicsServices.prototype.getModuleGuidFromTag = function (tag) {
            var mod = this.getModuleFromTag(tag);
            var guid = mod.getAttribute('data-moduleguid');
            if (!guid)
                throw "Can't find module guid"; /*DEBUG*/
            return guid;
        };
        ;
        // Get character size
        // CHARSIZE (from module or page/YVolatile)
        /**
         * Get the current character size used by the module defined using the specified tag (any tag within the module) or the default size.
         */
        BasicsServices.prototype.getCharSizeFromTag = function (tag) {
            var width, height;
            var mod = null;
            if (tag)
                mod = this.getModuleFromTagCond(tag);
            if (mod) {
                var w = mod.getAttribute('data-charwidthavg');
                if (!w)
                    throw "missing data-charwidthavg attribute"; /*DEBUG*/
                width = Number(w);
                var h = mod.getAttribute('data-charheight');
                if (!h)
                    throw "missing data-charheight attribute"; /*DEBUG*/
                height = Number(h);
            }
            else {
                width = YVolatile.Basics.CharWidthAvg;
                height = YVolatile.Basics.CharHeight;
            }
            return { width: width, height: height };
        };
        // Utility functions
        BasicsServices.prototype.htmlEscape = function (s, preserveCR) {
            preserveCR = preserveCR ? '&#13;' : '\n';
            return ('' + s) /* Forces the conversion to string. */
                .replace(/&/g, '&amp;') /* This MUST be the 1st replacement. */
                .replace(/'/g, '&apos;') /* The 4 other predefined entities, required. */
                .replace(/"/g, '&quot;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                /*
                You may add other replacements here for HTML only
                (but it's not necessary).
                Or for XML, only if the named entities are defined in its DTD.
                */
                .replace(/\r\n/g, preserveCR) /* Must be before the next replacement. */
                .replace(/[\r\n]/g, preserveCR);
        };
        BasicsServices.prototype.htmlAttrEscape = function (s) {
            return $('<div/>').text(s).html();
        };
        // Ajax result handling
        BasicsServices.prototype.processAjaxReturn = function (result, textStatus, xhr, tagInModule, onSuccessNoData, onHandleErrorResult) {
            //if (xhr.responseType != "json") throw `processAjaxReturn: unexpected responseType ${xhr.responseType}`;
            var result;
            try {
                result = eval(result);
            }
            catch (e) { }
            result = result || '(??)';
            if (xhr.status === 200) {
                this.reloadingModule_TagInModule = tagInModule || null;
                if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                    if (script.length == 0) { // all is well, but no script to execute
                        if (onSuccessNoData != undefined) {
                            onSuccessNoData();
                        }
                    }
                    else {
                        eval(script);
                    }
                    return true;
                }
                else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                    eval(script);
                    return false;
                }
                else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadPage)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadPage.length);
                    eval(script); // if this uses YetaWF_Basics.alert or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explicitly reload page in your javascript)
                    this.reloadPage(true);
                    return true;
                }
                else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
                    eval(script); // if this uses YetaWF_Basics.alert or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explicitly reload module in your javascript)
                    this.reloadModule();
                    return true;
                }
                else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
                    //if (!this.isInPopup()) throw "Not supported - only available within a popup";/*DEBUG*/
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
                    eval(script);
                    if (tagInModule)
                        this.refreshModuleByAnyTag(tagInModule);
                    return true;
                }
                else {
                    if (onHandleErrorResult != undefined) {
                        onHandleErrorResult(result);
                    }
                    else {
                        this.error(YLocs.Basics.IncorrectServerResp);
                    }
                    return false;
                }
            }
            else {
                YetaWF_Basics.alert(YLocs.Forms.AjaxError.format(xhr.status, result, YLocs.Forms.AjaxErrorTitle));
                return false;
            }
        };
        ;
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
                element.appendChild(child.nodeType == null ?
                    document.createTextNode(child.toString()) : child);
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
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReady requests after reloading the grid with data (which doesn't run any javascript automatically).
         * @param def
         */
        BasicsServices.prototype.addWhenReady = function (callback) {
            this.whenReady.push({ callback: callback });
        };
        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        BasicsServices.prototype.processAllReady = function (tags) {
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            for (var _i = 0, _a = this.whenReady; _i < _a.length; _i++) {
                var entry = _a[_i];
                try { // catch errors to insure all callbacks are called
                    for (var _b = 0, tags_1 = tags; _b < tags_1.length; _b++) {
                        var tag = tags_1[_b];
                        entry.callback(tag);
                    }
                }
                catch (err) {
                    console.log(err.message);
                }
            }
        };
        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor tag/elem and only manipulate child objects.
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReadyOnce requests after reloading the grid with data (which doesn't run any javascript automatically).
         * The callback is called for ONCE. Then the callback is removed.
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
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            for (var _i = 0, _a = this.whenReadyOnce; _i < _a.length; _i++) {
                var entry = _a[_i];
                try { // catch errors to insure all callbacks are called
                    for (var _b = 0, tags_2 = tags; _b < tags_2.length; _b++) {
                        var tag = tags_2[_b];
                        entry.callback(tag);
                    }
                }
                catch (err) {
                    console.log(err.message);
                }
            }
            this.whenReadyOnce = [];
        };
        /**
         * Registers a callback that is called when a <div> is cleared. This is used so templates can register a cleanup
         * callback so elements can be destroyed when a div is emptied (used by UPS).
         */
        BasicsServices.prototype.addClearDiv = function (callback) {
            this.clearDiv.push({ callback: callback });
        };
        /**
         * Process all callbacks for the specified element being cleared.
         * @param elem The element being cleared.
         */
        BasicsServices.prototype.processClearDiv = function (tag) {
            for (var _i = 0, _a = this.clearDiv; _i < _a.length; _i++) {
                var entry = _a[_i];
                try { // catch errors to insure all callbacks are called
                    if (entry.callback != null)
                        entry.callback(tag);
                }
                catch (err) {
                    console.log(err.message);
                }
            }
        };
        /**
         * Adds an object (a Typescript class) to a tag. Used for cleanup when a parent div is removed.
         * Typically used by templates.
         * Objects attached to divs are terminated by processClearDiv which calls any handlers that registered a
         * template class using addClearDivForObjects.
         * @param templateClass - The template css class (without leading .)
         * @param divId - The div id (DOM) that where the object is attached
         * @param obj - the object to attach
         */
        BasicsServices.prototype.addObjectDataById = function (templateClass, divId, obj) {
            var el = this.getElementById(divId);
            var data = $(el).data("__Y_Data");
            if (data)
                throw "addObjectDataById - tag with id " + divId + " already has data"; /*DEBUG*/
            $(el).data("__Y_Data", obj);
            this.addClearDivForObjects(templateClass);
        };
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param divId - The div id (DOM) that where the object is attached
         */
        BasicsServices.prototype.getObjectDataById = function (divId) {
            var el = this.getElementById(divId);
            var data = $(el).data("__Y_Data");
            if (!data)
                throw "getObjectDataById - tag with id " + divId + " has no data"; /*DEBUG*/
            return data;
        };
        /**
         * Removes a data object (a Typescript class) from a tag.
         * @param divId - The div id (DOM) that where the object is attached
         */
        BasicsServices.prototype.removeObjectDataById = function (divId) {
            var el = this.getElementById(divId);
            var data = $(el).data("__Y_Data");
            if (data)
                data.term();
            $(el).data("__Y_Data", null);
        };
        /**
         * Register a cleanup (typically used by templates) to terminate any objects that may be
         * attached to the template tag.
         * @param templateClass - The template css class (without leading .)
         */
        BasicsServices.prototype.addClearDivForObjects = function (templateClass) {
            var _this = this;
            this.addClearDiv(function (tag) {
                var list = _this.getElementsBySelector("." + templateClass, [tag]);
                for (var _i = 0, list_1 = list; _i < list_1.length; _i++) {
                    var el = list_1[_i];
                    var obj = $(el).data("__Y_Data");
                    if (obj)
                        obj.term();
                }
            });
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
         * Get elements from an array of tags by selector. (similar to jquery var x = $(selector, elems); with standard css selectors)
         */
        BasicsServices.prototype.getElementsBySelector = function (selector, elems) {
            var all = [];
            if (!elems)
                elems = [document.body];
            for (var _i = 0, elems_1 = elems; _i < elems_1.length; _i++) {
                var elem = elems_1[_i];
                var list = elem.querySelectorAll(selector);
                var len = list.length;
                for (var i = 0; i < len; ++i) {
                    all.push(list[i]);
                }
            }
            return all;
        };
        /**
         * Get the first element from an array of tags by selector. (similar to jquery var x = $(selector, elems); with standard css selectors)
         */
        BasicsServices.prototype.getElement1BySelectorCond = function (selector, elems) {
            if (!elems)
                elems = [document.body];
            for (var _i = 0, elems_2 = elems; _i < elems_2.length; _i++) {
                var elem = elems_2[_i];
                var list = elem.querySelectorAll(selector);
                if (list.length > 0)
                    return list[0];
            }
            return null;
        };
        /**
         * Get the first element from an array of tags by selector. (similar to jquery var x = $(selector, elems); with standard css selectors)
         */
        BasicsServices.prototype.getElement1BySelector = function (selector, elems) {
            var elem = this.getElement1BySelectorCond(selector, elems);
            if (elem == null)
                throw "Element with selector " + selector + " not found";
            return elem;
        };
        /**
         * Removes all input[type='hidden'] fields. (similar to jquery var x = elems.not("input[type='hidden']"); )
         */
        BasicsServices.prototype.limitToNotTypeHidden = function (elems) {
            var all = [];
            for (var _i = 0, elems_3 = elems; _i < elems_3.length; _i++) {
                var elem = elems_3[_i];
                if (elem.tagName !== "INPUT" || elem.getAttribute("type") !== "hidden") //$$$check casing
                    all.push(elem);
            }
            return all;
        };
        /**
         * Returns items that are visible. (similar to jquery var x = elems.filter(':visible'); )
         */
        BasicsServices.prototype.limitToVisibleOnly = function (elems) {
            var all = [];
            for (var _i = 0, elems_4 = elems; _i < elems_4.length; _i++) {
                var elem = elems_4[_i];
                if (elem.clientWidth > 0 && elem.clientHeight > 0)
                    all.push(elem);
            }
            return all;
        };
        /**
         * Tests whether the specified element matches the selector.
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        BasicsServices.prototype.elementMatches = function (elem, selector) {
            if (elem)
                return elem.matches(selector);
            return false;
        };
        /**
         * Finds the closest element up the DOM hierarchy that matches the selector (including the starting element)
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        BasicsServices.prototype.elementClosest = function (elem, selector) {
            var e = elem;
            while (e) {
                if (this.elementMatches(e, selector))
                    return e;
                else
                    e = e.parentElement;
            }
            return null;
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
        BasicsServices.prototype.appendMixedHTML = function (elem, content) {
            // convert the string to DOM representation
            var temp = document.createElement('YetaWFTemp');
            temp.innerHTML = content;
            // extract all <script> tags
            var scripts = YetaWF_Basics.getElementsBySelector('script', [temp]);
            for (var _i = 0, scripts_1 = scripts; _i < scripts_1.length; _i++) {
                var script = scripts_1[_i];
                YetaWF_Basics.removeElement(script); // remove the script element
            }
            // insert all the html bits
            while (temp.childElementCount > 0)
                elem.insertAdjacentElement('beforeend', temp.children[0]);
            // run/load all scripts we found
            for (var _a = 0, scripts_2 = scripts; _a < scripts_2.length; _a++) {
                var script = scripts_2[_a];
                if (script.src) {
                    script.async = false;
                    script.defer = false;
                    var js = document.createElement('script');
                    js.type = 'text/javascript';
                    js.async = false; // need to preserve execution order
                    js.defer = false;
                    js.src = script.src;
                    document.body.appendChild(js);
                }
                else
                    this.runGlobalScript(script.innerHTML);
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
            if (elem.classList)
                return elem.classList.contains(css);
            else
                return new RegExp("(^| )" + css + "( |$)", "gi").test(elem.className);
        };
        /**
         * Add a space separated list of css classes to an element.
         */
        BasicsServices.prototype.elementAddClasses = function (elem, classNames) {
            if (!classNames)
                return;
            for (var _i = 0, _a = classNames.split(" "); _i < _a.length; _i++) {
                var s = _a[_i];
                if (s.length > 0)
                    this.elementAddClass(elem, s);
            }
        };
        /**
         * Add css class to an element.
         */
        BasicsServices.prototype.elementAddClass = function (elem, className) {
            if (elem.classList)
                elem.classList.add(className);
            else
                elem.className += ' ' + className;
        };
        /**
         * Remove a space separated list of css classes from an element.
         */
        BasicsServices.prototype.elementRemoveClasses = function (elem, classNames) {
            if (!classNames)
                return;
            for (var _i = 0, _a = classNames.split(" "); _i < _a.length; _i++) {
                var s = _a[_i];
                if (s.length > 0)
                    this.elementRemoveClass(elem, s);
            }
        };
        /**
         * Remove a css class from an element.
         */
        BasicsServices.prototype.elementRemoveClass = function (elem, className) {
            if (elem.classList)
                elem.classList.remove(className);
            else
                elem.className = elem.className.replace(new RegExp('(^|\\b)' + className.split(' ').join('|') + '(\\b|$)', 'gi'), ' ');
        };
        // Events
        BasicsServices.prototype.registerDocumentReady = function (callback) {
            if (document.attachEvent ? document.readyState === "complete" : document.readyState !== "loading") {
                callback();
            }
            else {
                document.addEventListener('DOMContentLoaded', callback);
            }
        };
        BasicsServices.prototype.registerEventHandlerDocument = function (eventName, selector, callback) {
            var _this = this;
            window.addEventListener(eventName, function (ev) { return _this.handleEvent(null, ev, selector, callback); });
        };
        BasicsServices.prototype.registerEventHandlerWindow = function (eventName, selector, callback) {
            var _this = this;
            window.addEventListener(eventName, function (ev) { return _this.handleEvent(null, ev, selector, callback); });
        };
        BasicsServices.prototype.registerEventHandlerBody = function (eventName, selector, callback) {
            this.registerEventHandler(document.body, eventName, selector, callback);
        };
        BasicsServices.prototype.registerEventHandler = function (tag, eventName, selector, callback) {
            var _this = this;
            tag.addEventListener(eventName, function (ev) { return _this.handleEvent(tag, ev, selector, callback); });
        };
        BasicsServices.prototype.handleEvent = function (listening, ev, selector, callback) {
            // about event handling https://www.sitepoint.com/event-bubbling-javascript/
            // srcElement should be target//$$$$ srcElement is non-standard
            //console.log(`event ${ev.type} selector ${selector} srcElement ${(ev.srcElement as HTMLElement).outerHTML}`);
            if (ev.eventPhase == ev.CAPTURING_PHASE) {
                if (selector)
                    return; // if we have a selector we can't possibly have a match because the src element is the main tag where we registered the listener
            }
            else if (ev.eventPhase == ev.BUBBLING_PHASE) {
                if (!selector)
                    return;
                // check elements between the one that caused the event and the listening element (inclusive) for a match to the selector
                var elem = ev.srcElement;
                while (elem) {
                    if (YetaWF_Basics.elementMatches(elem, selector))
                        break;
                    if (listening == elem)
                        return; // checked all elements
                    elem = elem.parentElement;
                    if (elem == null)
                        return;
                }
            }
            else
                return;
            //console.log(`event ${ev.type} selector ${selector} match`);
            var result = callback(ev);
            if (!result) {
                //console.log(`event ${ev.type} selector ${selector} stop bubble`);
                ev.stopPropagation();
                ev.preventDefault();
            }
        };
        BasicsServices.prototype.registerContentChange = function (callback) {
            this.ContentChangeHandlers.push({ callback: callback });
        };
        BasicsServices.prototype.processContentChange = function (addonGuid, on) {
            for (var _i = 0, _a = this.ContentChangeHandlers; _i < _a.length; _i++) {
                var entry = _a[_i];
                entry.callback(addonGuid, on);
            }
        };
        BasicsServices.prototype.registerNewPage = function (callback) {
            this.NewPageHandlers.push({ callback: callback });
        };
        BasicsServices.prototype.processNewPage = function (url) {
            for (var _i = 0, _a = this.NewPageHandlers; _i < _a.length; _i++) {
                var entry = _a[_i];
                entry.callback(url);
            }
        };
        // Expand/collapse Support
        /**
         * Expand/collapse support using 2 action links (Name=Expand/Collapse) which make 2 divs hidden/visible  (alternating)
         * @param divId The <div> containing the 2 action links.
         * @param collapsedId - The <div> to hide/show.
         * @param expandedId - The <div> to show/hide.
         */
        BasicsServices.prototype.expandCollapseHandling = function (divId, collapsedId, expandedId) {
            var div = this.getElementById(divId);
            var collapsedDiv = this.getElementById(collapsedId);
            var expandedDiv = this.getElementById(expandedId);
            var expLink = this.getElement1BySelector("a[data-name='Expand']", [div]);
            var collLink = this.getElement1BySelector("a[data-name='Collapse']", [div]);
            this.registerEventHandler(expLink, "click", null, function (ev) {
                collapsedDiv.style.display = "none";
                expandedDiv.style.display = "";
                // init any controls that just became visible
                $(document).trigger("YetaWF_PropertyList_PanelSwitched", $(expandedDiv));
                return true;
            });
            this.registerEventHandler(collLink, "click", null, function (ev) {
                collapsedDiv.style.display = "";
                expandedDiv.style.display = "none";
                return true;
            });
        };
        return BasicsServices;
    }());
    YetaWF.BasicsServices = BasicsServices;
})(YetaWF || (YetaWF = {}));
/**
 * Basic services available throughout YetaWF.
 */
var YetaWF_Basics = new YetaWF.BasicsServices();

//# sourceMappingURL=Basics.js.map
