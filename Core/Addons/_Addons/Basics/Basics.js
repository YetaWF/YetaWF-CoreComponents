"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
/**
 * Class implementing basic services used throughout YetaWF.
 */
var YetaWF;
(function (YetaWF) {
    var BasicsServices = /** @class */ (function () {
        function BasicsServices() {
            // Implemented by renderer
            // Implemented by renderer
            // Implemented by renderer
            // Implemented by YetaWF
            // Implemented by YetaWF
            // Implemented by YetaWF
            // Content handling (Unified Page Sets)
            this.ContentHandling = new YetaWF.Content();
            // Page
            /**
             * currently loaded addons
             */
            this.UnifiedAddonModsLoaded = [];
            // Panes
            this.showPaneSet = function (id, editMode, equalHeights) {
                var $div = $("#" + id); // the pane
                var shown = false;
                if (editMode) {
                    $div.show();
                    shown = true;
                }
                else {
                    // show the pane if it has modules
                    if ($('div.yModule', $div).length > 0) {
                        $div.show();
                        shown = true;
                    }
                }
                if (shown && equalHeights) {
                    // make all panes the same height
                    // this should happen late in case the content is changed dynamically (use with caution)
                    // if it does, the pane will still expand because we're only setting the minimum height
                    $(document).ready(function () {
                        var $panes = $("#" + id + " > div:visible"); // get all immediate child divs (i.e., the panes)
                        $panes = $panes.not('.y_cleardiv');
                        var height = 0;
                        // calc height
                        $panes.each(function () {
                            var h = $(this).height() || 0;
                            if (h > height)
                                height = h;
                        });
                        // set each pane's height
                        $panes.css('min-height', height);
                    });
                }
            };
            // Navigation
            this.suppressPopState = false;
            this.reloadingModule_TagInModule = null;
            // Usage:
            // YetaWF_Basics.reloadInfo.push({  //$$$$ revisit this
            //   module: $mod,              // module <div> to be refreshed
            //   callback: function() {}    // function to be called
            // });
            this.reloadInfo = [];
            // WhenReady
            /* TODO: This is public and push() is used to add callbacks (legacy Javascript ONLY) - Once transitioned, make whenReady private and remove $tag support */
            // Usage:
            // YetaWF_Basics.whenReady.push({
            //   callback: function($tag) {}    // function to be called
            // });
            //   or
            // YetaWF_Basics.whenReady.push({
            //   callbackTS: function(elem) {}    // function to be called
            // });
            this.whenReady = [];
            // WhenReadyOnce
            /* TODO: This is public and push() is used to add callbacks (legacy Javascript ONLY) - Once transitioned, make whenReadyOnce private and remove $tag support */
            // Usage:
            // YetaWF_Basics.whenReadyOnce.push({
            //   callback: function($tag) {}    // function to be called
            // });
            //   or
            // YetaWF_Basics.whenReadyOnce.push({
            //   callbackTS: function(elem) {}    // function to be called
            // });
            this.whenReadyOnce = [];
            // ClearDiv
            this.clearDiv = [];
        }
        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        BasicsServices.prototype.setLoading = function (on) {
            YetaWF_BasicsImpl.setLoading(on);
            if (on == false)
                this.Y_PleaseWaitClose();
        };
        /**
         * Displays an informational message, usually in a popup.
         */
        BasicsServices.prototype.Y_Message = function (message, title, onOK, options) { YetaWF_BasicsImpl.Y_Message(message, title, onOK, options); };
        /**
         * Displays an error message, usually in a popup.
         */
        BasicsServices.prototype.Y_Error = function (message, title, onOK, options) { YetaWF_BasicsImpl.Y_Error(message, title, onOK, options); };
        /**
         * Displays a confirmation message, usually in a popup.
         */
        BasicsServices.prototype.Y_Confirm = function (message, title, onOK, options) { YetaWF_BasicsImpl.Y_Confirm(message, title, onOK, options); };
        /**
         * Displays an alert message, usually in a popup.
         */
        BasicsServices.prototype.Y_Alert = function (message, title, onOK, options) { YetaWF_BasicsImpl.Y_Alert(message, title, onOK, options); };
        /**
         * Displays an alert message with Yes/No buttons, usually in a popup.
         */
        BasicsServices.prototype.Y_AlertYesNo = function (message, title, onYes, onNo, options) { YetaWF_BasicsImpl.Y_AlertYesNo(message, title, onYes, onNo, options); };
        /**
         * Displays a "Please Wait" message
         */
        BasicsServices.prototype.Y_PleaseWait = function (message, title) { YetaWF_BasicsImpl.Y_PleaseWait(message, title); };
        /**
         * Closes the "Please Wait" message (if any).
         */
        BasicsServices.prototype.Y_PleaseWaitClose = function () { YetaWF_BasicsImpl.Y_PleaseWaitClose(); };
        // Focus
        /**
         * Set focus to a suitable field within the specified element.
         */
        BasicsServices.prototype.setFocus = function ($elem) {
            //TODO: this should also consider input fields with validation errors (although that seems to magically work right now)
            if ($elem == undefined)
                $elem = $('body');
            var $items = $('.focusonme:visible', $elem);
            var $f = null;
            $items.each(function (index) {
                var item = this;
                if (item.tagName == "DIV") { // if we found a div, find the edit element instead
                    var $i = $('input:visible,select:visible,.yt_dropdownlist_base:visible', $(item)).not("input[type='hidden']");
                    if ($i.length > 0) {
                        $f = $i.eq(0);
                        return false;
                    }
                }
            });
            // We probably don't want to set the focus to any control - made OPT-IN for now
            //if ($f == null) {
            //    $items = $('input:visible,select:visible', $obj).not("input[type='hidden']");// just find something useable
            //    // filter out anything in a grid (filters, pager, etc)
            //    $items.each(function (index) {
            //        var $i = $(this)
            //        if ($i.parents('.ui-jqgrid').length == 0) {
            //            $f = $i;
            //            return false; // not in a grid, so it's ok
            //        }
            //    });
            //}
            if ($f != null) {
                try {
                    $f[0].focus();
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
        BasicsServices.prototype.setCondense = function ($tag, width) {
            if (width < YVolatile.Skin.MinWidthForPopups) {
                $tag.addClass('yCondense');
                $tag.removeClass('yNoCondense');
            }
            else {
                $tag.addClass('yNoCondense');
                $tag.removeClass('yCondense');
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
            var uri = new URI(window.location.href);
            var data = uri.search(true);
            var v = data[YGlobals.Link_ScrollLeft];
            var scrolled = false;
            if (v != undefined) {
                $(window).scrollLeft(Number(v));
                scrolled = true;
            }
            v = data[YGlobals.Link_ScrollTop];
            if (v != undefined) {
                $(window).scrollTop(Number(v));
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
                if (YVolatile.Basics.UnifiedMode === 2 /*UnifiedModeEnum.ShowDivs*/) {
                    var $divs = $('xyzx'); //$$$ $(`.yUnified[data-url="${uri.path()}"]`);  $$$uri is not defined
                    if ($divs && $divs.length > 0) {
                        $(window).scrollTop(($divs.eq(0)).offset().top);
                        scrolled = true;
                    }
                }
            }
            // FOCUS
            // FOCUS
            // FOCUS
            $(document).ready(function () {
                if (!scrolled && location.hash.length <= 1)
                    _this.setFocus();
            });
            // content navigation
            this.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious; // save loaded addons
        };
        ;
        // Reload, refresh
        /**
         * Reloads the current page - in its entirety (full page load)
         */
        BasicsServices.prototype.reloadPage = function (keepPosition, w) {
            if (!w)
                w = window;
            if (!keepPosition)
                keepPosition = false;
            var uri = new URI(w.location.href);
            uri.removeSearch(YGlobals.Link_ScrollLeft);
            uri.removeSearch(YGlobals.Link_ScrollTop);
            if (keepPosition) {
                var v = $(w).scrollLeft();
                if (v)
                    uri.addSearch(YGlobals.Link_ScrollLeft, v);
                v = $(w).scrollTop();
                if (v)
                    uri.addSearch(YGlobals.Link_ScrollTop, v);
            }
            uri.removeSearch("!rand");
            uri.addSearch("!rand", (new Date()).getTime()); // cache buster
            if (YVolatile.Basics.UnifiedMode != 0) {
                if (this.ContentHandling.setContent(uri, true))
                    return;
            }
            if (keepPosition) {
                w.location.assign(uri.toString());
                return;
            }
            w.location.reload(true);
        };
        /**
         * Reloads a module in place, defined by the specified tag (any tag within the module).
         */
        BasicsServices.prototype.reloadModule = function ($tag) {
            if (!$tag) {
                if (!this.reloadingModule_TagInModule)
                    throw "No module found"; /*DEBUG*/
                $tag = this.reloadingModule_TagInModule;
            }
            var $mod = this.getModuleFromTag($tag);
            if ($mod.length == 0)
                throw "No module found"; /*DEBUG*/
            var $form = $('form', $mod);
            if ($mod.length == 0)
                throw "No form found"; /*DEBUG*/
            YetaWF_Forms /*$$$*/.submit($form, false, YGlobals.Link_SubmitIsApply + "=y"); // the form must support a simple Apply
        };
        BasicsServices.prototype.refreshModule = function ($mod) {
            for (var entry in YetaWF_Basics.reloadInfo) {
                if (YetaWF_Basics.reloadInfo[entry].module == $mod) {
                    YetaWF_Basics.reloadInfo[entry].callback();
                }
            }
        };
        ;
        BasicsServices.prototype.refreshModuleByAnyTag = function ($t) {
            var $mod = YetaWF_Basics.getModuleFromTag($t);
            for (var entry in YetaWF_Basics.reloadInfo) {
                if (YetaWF_Basics.reloadInfo[entry].module[0].id == $mod[0].id) {
                    YetaWF_Basics.reloadInfo[entry].callback();
                }
            }
        };
        ;
        BasicsServices.prototype.refreshPage = function () {
            for (var entry in YetaWF_Basics.reloadInfo) {
                YetaWF_Basics.reloadInfo[entry].callback();
            }
        };
        ;
        // Module locator
        /**
         * Get a module defined by the specified tag (any tag within the module). Returns null if none found.
         */
        BasicsServices.prototype.getModuleFromTagCond = function ($t) {
            $t = $($t);
            if ($t.length != 1) {
                debugger;
                throw "Invalid tag";
            } /*DEBUG*/
            var $mod = $t.closest('.yModule');
            if ($mod.length == 0)
                return null;
            return $mod;
        };
        ;
        /**
         * Get a module defined by the specified tag (any tag within the module). Throws exception if none found.
         */
        BasicsServices.prototype.getModuleFromTag = function ($t) {
            var $mod = YetaWF_Basics.getModuleFromTagCond($t);
            if ($mod == null || $mod.length != 1) {
                debugger;
                throw "Can't find containing module";
            } /*DEBUG*/
            return $mod;
        };
        ;
        BasicsServices.prototype.getModuleGuidFromTag = function ($t) {
            var $t = $($t);
            if ($t.length != 1) {
                debugger;
                throw "Invalid tag";
            } /*DEBUG*/
            var $mod = $t.closest('.yModule');
            if ($mod.length != 1) {
                debugger;
                throw "Can't find containing module";
            } /*DEBUG*/
            var guid = $mod.attr('data-moduleguid');
            if (guid == undefined || guid == "")
                throw "Can't find module guid"; /*DEBUG*/
            return guid;
        };
        ;
        // Get character size
        // CHARSIZE (from module or page/YVolatile)
        /**
         * Get the current character size used by the module defined using the specified tag (any tag within the module) or the default size.
         */
        BasicsServices.prototype.getCharSizeFromTag = function ($t) {
            var width, height;
            var $mod = null;
            if ($t)
                $mod = YetaWF_Basics.getModuleFromTagCond($t);
            if ($mod) {
                width = $mod.attr('data-charwidthavg');
                height = $mod.attr('data-charheight');
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
        BasicsServices.prototype.processAjaxReturn = function (result, textStatus, jqXHR, tagInModule, onSuccess, onHandleResult) {
            YetaWF_Basics.reloadingModule_TagInModule = tagInModule || null;
            if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                if (script.length == 0) { // all is well, but no script to execute
                    if (onSuccess != undefined) {
                        onSuccess();
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
                eval(script); // if this uses YetaWF_Basics.Y_Alert or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explicitly reload page in your javascript)
                YetaWF_Basics.reloadPage(true);
                return true;
            }
            else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
                eval(script); // if this uses YetaWF_Basics.Y_Alert or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explicitly reload module in your javascript)
                this.reloadModule();
                return true;
            }
            else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
                //if (!YetaWF_Basics.isInPopup()) throw "Not supported - only available within a popup";/*DEBUG*/
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
                eval(script);
                if (tagInModule)
                    YetaWF_Basics.refreshModuleByAnyTag(tagInModule);
                return true;
            }
            else {
                if (onHandleResult != undefined) {
                    onHandleResult(result);
                }
                else {
                    YetaWF_Basics.Y_Error(YLocs.Basics.IncorrectServerResp);
                }
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
        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReady requests after reloading the grid with data (which doesn't run any javascript automatically).
         * @param def
         */
        BasicsServices.prototype.addWhenReady = function (callback) {
            this.whenReady.push({ callbackTS: callback });
        };
        //TODO: This should take an elem, not a jquery object
        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        BasicsServices.prototype.processAllReady = function ($tag) {
            if (!$tag)
                $tag = $("body");
            for (var _i = 0, _a = this.whenReady; _i < _a.length; _i++) {
                var entry = _a[_i];
                try { // catch errors to insure all callbacks are called
                    if (entry.callback != null)
                        entry.callback($tag);
                    else if (entry.callbackTS != null) {
                        $tag.each(function (ix, val) {
                            entry.callbackTS(val);
                        });
                    }
                }
                catch (err) {
                    console.log(err.message);
                }
            }
        };
        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReadyOnce requests after reloading the grid with data (which doesn't run any javascript automatically).
         * The callback is called for ONCE. Then the callback is removed.
         * @param def
         */
        BasicsServices.prototype.addWhenReadyOnce = function (callback) {
            this.whenReadyOnce.push({ callbackTS: callback });
        };
        //TODO: This should take an elem, not a jquery object
        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        BasicsServices.prototype.processAllReadyOnce = function ($tag) {
            if (!$tag)
                $tag = $("body");
            for (var _i = 0, _a = this.whenReadyOnce; _i < _a.length; _i++) {
                var entry = _a[_i];
                try { // catch errors to insure all callbacks are called
                    if (entry.callback !== undefined)
                        entry.callback($tag);
                    else {
                        $tag.each(function (ix, elem) {
                            entry.callbackTS(this);
                        });
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
         * Process all callbacks for the specified element being cleared. This is used by YetaWF.Core only.
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
            var $el = $("#" + divId);
            if (!$el.hasClass(templateClass))
                throw "addObjectDataById called with class " + templateClass + " - tag with id " + divId + " does not have that css class"; /*DEBUG*/
            var data = $el.data("__Y_Data");
            if (data)
                throw "addObjectDataById - tag with id " + divId + " already has data"; /*DEBUG*/
            $el.data("__Y_Data", obj);
            this.addClearDivForObjects(templateClass);
        };
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param divId - The div id (DOM) that where the object is attached
         */
        BasicsServices.prototype.getObjectDataById = function (divId) {
            var $el = $("#" + divId);
            if ($el.length === 0)
                throw "getObjectDataById - tag with id " + divId + " has no data"; /*DEBUG*/
            var data = $el.data("__Y_Data");
            if (!data)
                throw "getObjectDataById - tag with id " + divId + " has no data"; /*DEBUG*/
            return data;
        };
        /**
         * Removes a data object (a Typescript class) from a tag.
         * @param divId - The div id (DOM) that where the object is attached
         */
        BasicsServices.prototype.removeObjectDataById = function (divId) {
            var $el = $("#" + divId);
            if ($el.length === 0)
                throw "removeObjectDataById - tag with id " + divId + " has no data"; /*DEBUG*/
            var data = $el.data("__Y_Data");
            if (data)
                data.term();
            $el.data("__Y_Data", null);
        };
        /**
         * Register a cleanup (typically used by templates) to terminate any objects that may be
         * attached to the template tag.
         * @param templateClass - The template css class (without leading .)
         */
        BasicsServices.prototype.addClearDivForObjects = function (templateClass) {
            YetaWF_Basics.addClearDiv(function (tag) {
                var list = tag.querySelectorAll("." + templateClass);
                var len = list.length;
                for (var i = 0; i < len; ++i) {
                    var el = list[i];
                    var obj = $(el).data("__Y_Data");
                    if (obj)
                        obj.term();
                }
            });
        };
        // Selectors - APIs to detach selectors from jQuery so this could be replaced with a smaller library (like sizzle).
        /**
         * Tests whether the specified element matches the selector.
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        BasicsServices.prototype.elementMatches = function (elem, selector) {
            if (elem)
                return $(elem).is(selector);
            return false;
        };
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
        // CONTENTCHANGE
        // CONTENTCHANGE
        // CONTENTCHANGE
        // APIs to detach custom event handling from jQuery so this could be replaced with a native mechanism
        BasicsServices.prototype.RegisterContentChange = function (callback) {
            $(document).on("YetaWF_Basics_Addon", function (event, addonGuid, on) { callback(event, addonGuid, on); });
        };
        // NEWPAGE
        // NEWPAGE
        // NEWPAGE
        // APIs to detach custom event handling from jQuery so this could be replaced with a native mechanism
        BasicsServices.prototype.RegisterNewPage = function (callback) {
            $(document).on("YetaWF_Basics_NewPage", function (event, url) { callback(event, url); });
        };
        // Expand/collapse Support
        /**
         * Expand/collapse support using 2 action links (Name=Expand/Collapse) which make 2 divs hidden/visible  (alternating)
         * @param divId The <div> containing the 2 action links.
         * @param collapsedId - The <div> to hide/show.
         * @param expandedId - The <div> to show/hide.
         */
        BasicsServices.prototype.ExpandCollapse = function (divId, collapsedId, expandedId) {
            var div = document.querySelector("#" + divId);
            if (!div)
                throw "#" + divId + " not found"; /*DEBUG*/
            var collapsedDiv = document.querySelector("#" + collapsedId);
            if (!collapsedDiv)
                throw "#" + collapsedId + " not found"; /*DEBUG*/
            var expandedDiv = document.querySelector("#" + expandedId);
            if (!expandedDiv)
                throw "#" + expandedId + " not found"; /*DEBUG*/
            var expLink = div.querySelector("a[data-name='Expand']");
            if (!expLink)
                throw "a[data-name=\"Expand\"] not found"; /*DEBUG*/
            var collLink = div.querySelector("a[data-name='Collapse']");
            if (!collLink)
                throw "a[data-name=\"Expand\"] not found"; /*DEBUG*/
            function expandHandler(event) {
                collapsedDiv.style.display = "none";
                expandedDiv.style.display = "";
                // init any controls that just became visible
                $(document).trigger("YetaWF_PropertyList_PanelSwitched", $(expandedDiv));
            }
            function collapseHandler(event) {
                collapsedDiv.style.display = "";
                expandedDiv.style.display = "none";
            }
            expLink.addEventListener("click", expandHandler, false);
            collLink.addEventListener("click", collapseHandler, false);
        };
        return BasicsServices;
    }());
    YetaWF.BasicsServices = BasicsServices;
    // screen size yCondense/yNoCondense support
    $(window).on('resize', function () {
        YetaWF_Basics.setCondense($('body'), window.innerWidth);
    });
    $(document).ready(function () {
        YetaWF_Basics.setCondense($('body'), window.innerWidth);
    });
    // Navigation
    $(window).on("popstate", function (ev) {
        var uri = new URI(window.location.href);
        if (YetaWF_Basics.suppressPopState) {
            YetaWF_Basics.suppressPopState = false;
            return;
        }
        return !YetaWF_Basics.ContentHandling.setContent(uri, false);
    });
    // <a> links that only have a hash are intercepted so we don't go through content handling
    $("body").on("click", "a[href^='#']", function (e) {
        YetaWF_Basics.suppressPopState = true;
    });
    // <A> links
    var AnchorHandling = new YetaWF.Anchors();
    AnchorHandling.init();
    // WhenReady
    $(document).ready(function () {
        YetaWF_Basics.processAllReady();
        YetaWF_Basics.processAllReadyOnce();
    });
})(YetaWF || (YetaWF = {}));
/**
 * Basic services available throughout YetaWF.
 */
var YetaWF_Basics = new YetaWF.BasicsServices();
