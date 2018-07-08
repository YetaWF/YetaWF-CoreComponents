/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* TODO : While transitioning to TypeScript and to maintain compatibility with all plain JavaScript, these defs are all global rather than in their own namespace.
   Once the transition is complete, we need to revisit this */

/* Basics API, to be implemented by rendering-specific code - rendering code must define a YetaWF_BasicsImpl object implementing IBasicsImpl */

/**
    Implemented by custom rendering.
 */
declare var YetaWF_BasicsImpl: YetaWF.IBasicsImpl;

interface String {
    startsWith: (text: string) => boolean;
    endWith: (text: string) => boolean;
}

/**
 * Class implementing basic services used throughout YetaWF.
 */
namespace YetaWF {

    export interface MessageOptions {
        encoded: boolean;
    }
    export interface ReloadInfo {
        module: JQuery<HTMLElement>;
        callback(): void;
    }
    export interface CharSize {
        width: number;
        height: number;
    }

    export interface IBasicsImpl {

        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        setLoading(on?: boolean): void;

        /**
         * Displays an informational message, usually in a popup.
         */
        Y_Message(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an error message, usually in a popup.
         */
        Y_Error(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays a confirmation message, usually in a popup.
         */
        Y_Confirm(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an alert message, usually in a popup.
         */
        Y_Alert(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an alert message, usually in a popup.
         */
        Y_AlertYesNo(message: string, title?: string, onYes?: () => void, onNo?: () => void, options?: MessageOptions): void;
        /**
         * Displays a "Please Wait" message.
         */
        Y_PleaseWait(message?: string, title?: string): void;
        /**
         * Closes the "Please Wait" message (if any).
         */
        Y_PleaseWaitClose(): void;

    }

    export interface IWhenReady {
        /**
         * @deprecated
         */
        callback?($tag: JQuery<HTMLElement>): void;

        callbackTS?(elem: HTMLElement): void;
    }
    export interface IClearDiv {
        callback?(elem: HTMLElement): void;
    }

    export class BasicsServices implements IBasicsImpl { //$$$ doesn't need to implement IBasicImpl, done for type checking only

        // Implemented by renderer
        // Implemented by renderer
        // Implemented by renderer

        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        public setLoading(on?: boolean): void {
            YetaWF_BasicsImpl.setLoading(on);
            if (on == false)
                this.Y_PleaseWaitClose();
        }

        /**
         * Displays an informational message, usually in a popup.
         */
        public Y_Message(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.Y_Message(message, title, onOK, options); }
        /**
         * Displays an error message, usually in a popup.
         */
        public Y_Error(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.Y_Error(message, title, onOK, options); }
        /**
         * Displays a confirmation message, usually in a popup.
         */
        public Y_Confirm(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.Y_Confirm(message, title, onOK, options); }
        /**
         * Displays an alert message, usually in a popup.
         */
        public Y_Alert(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.Y_Alert(message, title, onOK, options); }
        /**
         * Displays an alert message with Yes/No buttons, usually in a popup.
         */
        public Y_AlertYesNo(message: string, title?: string, onYes?: () => void, onNo?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.Y_AlertYesNo(message, title, onYes, onNo, options); }
        /**
         * Displays a "Please Wait" message
         */
        public Y_PleaseWait(message?: string, title?: string) { YetaWF_BasicsImpl.Y_PleaseWait(message, title); }
        /**
         * Closes the "Please Wait" message (if any).
         */
        Y_PleaseWaitClose(): void { YetaWF_BasicsImpl.Y_PleaseWaitClose(); }

        // Implemented by YetaWF
        // Implemented by YetaWF
        // Implemented by YetaWF

        // Content handling (Unified Page Sets)

        public ContentHandling: YetaWF.Content = new YetaWF.Content();

        // Focus

        /**
         * Set focus to a suitable field within the specified element.
         */
        public setFocus($elem?: JQuery<HTMLElement>): void {
            //TODO: this should also consider input fields with validation errors (although that seems to magically work right now)
            if ($elem == undefined)
                $elem = $('body')
            var $items = $('.focusonme:visible', $elem)
            var $f: JQuery<HTMLElement> | null = null;
            $items.each(function (index): false | void {
                var item = this;
                if (item.tagName == "DIV") { // if we found a div, find the edit element instead
                    var $i = $('input:visible,select:visible,.yt_dropdownlist_base:visible', $(item)).not("input[type='hidden']")
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
                } catch (e) { }
            }
        }

        // Screen size

        /**
         * Sets yCondense/yNoCondense css class on popup or body to indicate screen size.
         * Sets rendering mode based on window size
         * we can't really use @media (max-width:...) in css because popups (in Unified Page Sets) don't use iframes so their size may be small but
         * doesn't match @media screen (ie. the window). So, instead we add the css class yCondense to the <body> or popup <div> to indicate we want
         * a more condensed appearance.
         */
        public setCondense($tag: JQuery<HTMLElement>, width: number) {
            if (width < YVolatile.Skin.MinWidthForPopups) {
                $tag.addClass('yCondense');
                $tag.removeClass('yNoCondense');
            } else {
                $tag.addClass('yNoCondense');
                $tag.removeClass('yCondense');
            }
        }

        // Popup

        /**
         * Returns whether a popup is active
         */
        public isInPopup(): boolean {
            return YVolatile.Basics.IsInPopup;
        }
        //
        /**
         * Close any popup window.
         */
        public closePopup(forceReload?: boolean): void {
            if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined)
                YetaWF_Popups.closePopup(forceReload);
        }

        // Scrolling

        public setScrollPosition(): boolean {
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

        // Page

        /**
         * currently loaded addons
         */
        public UnifiedAddonModsLoaded: string[] = [];

        /**
         * Initialize the current page (full page load) - runs during page load, before document ready
         */
        public initPage(): void {

            // page position

            var scrolled = this.setScrollPosition();
            if (!scrolled) {
                if (YVolatile.Basics.UnifiedMode === 2 /*UnifiedModeEnum.ShowDivs*/) {
                    var $divs: JQuery<HTMLElement> = $('xyzx');//$$$ $(`.yUnified[data-url="${uri.path()}"]`);  $$$uri is not defined
                    if ($divs && $divs.length > 0) {
                        $(window).scrollTop((($divs.eq(0)).offset() as JQuery.Coordinates).top);
                        scrolled = true;
                    }
                }
            }

            // FOCUS
            // FOCUS
            // FOCUS

            $(document).ready(() => { // only needed during full page load
                if (!scrolled && location.hash.length <= 1)
                    this.setFocus();
            });

            // content navigation

            this.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious;// save loaded addons
        };

        // Panes

        public showPaneSet = function (id: string, editMode: boolean, equalHeights: boolean) {

            var $div = $(`#${id}`);// the pane
            var shown = false;
            if (editMode) {
                $div.show();
                shown = true;
            } else {
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
                $(document).ready(function () { // TODO: This only works for full page loads
                    var $panes = $(`#${id} > div:visible`);// get all immediate child divs (i.e., the panes)
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
        }

        // Navigation

        public suppressPopState: boolean = false;

        // Reload, refresh

        /**
         * Reloads the current page - in its entirety (full page load)
         */
        public reloadPage(keepPosition?: boolean, w?: Window): void {

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
            uri.addSearch("!rand", (new Date()).getTime());// cache buster

            if (YVolatile.Basics.UnifiedMode != 0) {
                if (this.ContentHandling.setContent(uri, true))
                    return;
            }
            if (keepPosition) {
                w.location.assign(uri.toString());
                return;
            }
            w.location.reload(true);
        }

        /**
         * Reloads a module in place, defined by the specified tag (any tag within the module).
         */
        public reloadModule($tag?: JQuery<HTMLElement>) {
            if (!$tag) {
                if (!this.reloadingModule_TagInModule) throw "No module found";/*DEBUG*/
                $tag = this.reloadingModule_TagInModule;
            }
            var $mod = this.getModuleFromTag($tag);
            if ($mod.length == 0) throw "No module found";/*DEBUG*/
            var $form = $('form', $mod);
            if ($mod.length == 0) throw "No form found";/*DEBUG*/
            (YetaWF_Forms as any /*$$$*/).submit($form, false, YGlobals.Link_SubmitIsApply + "=y");// the form must support a simple Apply
        }

        private reloadingModule_TagInModule: JQuery<HTMLElement> | null = null;

        // Usage:
        // YetaWF_Basics.reloadInfo.push({  //$$$$ revisit this
        //   module: $mod,              // module <div> to be refreshed
        //   callback: function() {}    // function to be called
        // });

        public reloadInfo: ReloadInfo[] = [];

        public refreshModule($mod: JQuery<HTMLElement>) {
            for (var entry in YetaWF_Basics.reloadInfo) {
                if (YetaWF_Basics.reloadInfo[entry].module == $mod) {
                    YetaWF_Basics.reloadInfo[entry].callback();
                }
            }
        };
        public refreshModuleByAnyTag($t: JQuery<HTMLElement>) {
            var $mod = YetaWF_Basics.getModuleFromTag($t);
            for (var entry in YetaWF_Basics.reloadInfo) {
                if (YetaWF_Basics.reloadInfo[entry].module[0].id == $mod[0].id) {
                    YetaWF_Basics.reloadInfo[entry].callback();
                }
            }
        };
        public refreshPage(): void {
            for (var entry in YetaWF_Basics.reloadInfo) {
                YetaWF_Basics.reloadInfo[entry].callback();
            }
        };

        // Module locator

        /**
         * Get a module defined by the specified tag (any tag within the module). Returns null if none found.
         */
        private getModuleFromTagCond($t: JQuery<HTMLElement>): JQuery<HTMLElement> | null {
            $t = $($t);
            if ($t.length != 1) { debugger; throw "Invalid tag"; }/*DEBUG*/
            var $mod = $t.closest('.yModule');
            if ($mod.length == 0) return null;
            return $mod;
        };
        /**
         * Get a module defined by the specified tag (any tag within the module). Throws exception if none found.
         */
        private getModuleFromTag($t: JQuery<HTMLElement>): JQuery<HTMLElement> {
            var $mod = YetaWF_Basics.getModuleFromTagCond($t);
            if ($mod == null || $mod.length != 1) { debugger; throw "Can't find containing module"; }/*DEBUG*/
            return $mod;
        };

        public getModuleGuidFromTag($t: JQuery<HTMLElement>): string {
            var $t = $($t);
            if ($t.length != 1) { debugger; throw "Invalid tag"; }/*DEBUG*/
            var $mod = $t.closest('.yModule');
            if ($mod.length != 1) { debugger; throw "Can't find containing module"; }/*DEBUG*/
            var guid = $mod.attr('data-moduleguid');
            if (guid == undefined || guid == "") throw "Can't find module guid";/*DEBUG*/
            return guid;
        };

        // Get character size

        // CHARSIZE (from module or page/YVolatile)
        /**
         * Get the current character size used by the module defined using the specified tag (any tag within the module) or the default size.
         */
        public getCharSizeFromTag($t: JQuery<HTMLElement> | null): CharSize{
            var width, height;
            var $mod: JQuery<HTMLElement> | null = null;
            if ($t)
                $mod = YetaWF_Basics.getModuleFromTagCond($t);
            if ($mod) {
                width = $mod.attr('data-charwidthavg');
                height = $mod.attr('data-charheight');
            } else {
                width = YVolatile.Basics.CharWidthAvg;
                height = YVolatile.Basics.CharHeight;
            }
            return { width: width, height: height };
        }

        // Utility functions

        public htmlEscape(s: string|undefined, preserveCR?: string): string {
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
        }
        public htmlAttrEscape(s: string): string {
            return $('<div/>').text(s).html();
        }

        // Ajax result handling

        public processAjaxReturn(result: string, textStatus: string, jqXHR, tagInModule?: JQuery<HTMLElement>, onSuccess?: () => void, onHandleResult?: (string) => void) : boolean {
            YetaWF_Basics.reloadingModule_TagInModule = tagInModule || null;
            if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                if (script.length == 0) { // all is well, but no script to execute
                    if (onSuccess != undefined) {
                        onSuccess();
                    }
                } else {
                    eval(script);
                }
                return true;
            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                eval(script);
                return false;
            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadPage)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadPage.length);
                eval(script);// if this uses YetaWF_Basics.Y_Alert or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explicitly reload page in your javascript)
                YetaWF_Basics.reloadPage(true);
                return true;
            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
                eval(script);// if this uses YetaWF_Basics.Y_Alert or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explicitly reload module in your javascript)
                this.reloadModule();
                return true;
            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
                //if (!YetaWF_Basics.isInPopup()) throw "Not supported - only available within a popup";/*DEBUG*/
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
                eval(script);
                if (tagInModule)
                    YetaWF_Basics.refreshModuleByAnyTag(tagInModule);
                return true;
            } else {
                if (onHandleResult != undefined) {
                    onHandleResult(result);
                } else {
                    YetaWF_Basics.Y_Error(YLocs.Basics.IncorrectServerResp);
                }
                return false;
            }
        };

        // JSX

        /**
         * React-like createElement function so we can use JSX in our TypeScript/JavaScript code.
         */
        public createElement(tag: string, attrs: any, children: any): HTMLElement {
            var element: HTMLElement = document.createElement(tag);
            for (let name in attrs) {
                if (name && attrs.hasOwnProperty(name)) {
                    var value: string | null | boolean = attrs[name];
                    if (value === true) {
                        element.setAttribute(name, name);
                    } else if (value !== false && value != null) {
                        element.setAttribute(name, value.toString());
                    }
                }
            }
            for (let i: number = 2; i < arguments.length; i++) {
                let child: any = arguments[i];
                element.appendChild(
                    child.nodeType == null ?
                        document.createTextNode(child.toString()) : child);
            }
            return element;
        }

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
        public whenReady: IWhenReady[] = [];

        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReady requests after reloading the grid with data (which doesn't run any javascript automatically).
         * @param def
         */
        public addWhenReady(callback: (section: HTMLElement) => void): void {
            this.whenReady.push({ callbackTS: callback });
        }

        //TODO: This should take an elem, not a jquery object
        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        public processAllReady($tag?: JQuery<HTMLElement>): void {
            if (!$tag) $tag = $("body");
            for (var entry of this.whenReady) {
                try { // catch errors to insure all callbacks are called
                    if (entry.callback != null)
                        entry.callback($tag);
                    else if (entry.callbackTS != null) {
                        $tag.each(function (ix: number, val: HTMLElement): void {
                            (entry.callbackTS as any)(val);
                        });
                    }
                } catch (err) {
                    console.log(err.message);
                }
            }
        }

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
        public whenReadyOnce: IWhenReady[] = [];

        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor $tag/elem and only manipulate child objects.
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReadyOnce requests after reloading the grid with data (which doesn't run any javascript automatically).
         * The callback is called for ONCE. Then the callback is removed.
         * @param def
         */
        public addWhenReadyOnce(callback: (section: HTMLElement) => void): void {
            this.whenReadyOnce.push({ callbackTS: callback });
        }

        //TODO: This should take an elem, not a jquery object
        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        public processAllReadyOnce($tag?: JQuery<HTMLElement>): void {
            if (!$tag) $tag = $("body");
            for (var entry of this.whenReadyOnce) {
                try { // catch errors to insure all callbacks are called
                    if (entry.callback !== undefined)
                        entry.callback($tag);
                    else {
                        $tag.each(function (ix: number, elem: HTMLElement): void {
                            (entry.callbackTS as any)(this);
                        });
                    }
                } catch (err) {
                    console.log(err.message);
                }
            }
            this.whenReadyOnce = [];
        }

        // ClearDiv

        private clearDiv: IClearDiv[] = [];

        /**
         * Registers a callback that is called when a <div> is cleared. This is used so templates can register a cleanup
         * callback so elements can be destroyed when a div is emptied (used by UPS).
         */
        public addClearDiv(callback: (section: HTMLElement) => void): void {
            this.clearDiv.push({ callback: callback });
        }

        /**
         * Process all callbacks for the specified element being cleared. This is used by YetaWF.Core only.
         * @param elem The element being cleared.
         */
        public processClearDiv(tag: HTMLElement): void {
            for (var entry of this.clearDiv) {
                try { // catch errors to insure all callbacks are called
                    if (entry.callback != null)
                        entry.callback(tag);
                } catch (err) {
                    console.log(err.message);
                }
            }
        }

        /**
         * Adds an object (a Typescript class) to a tag. Used for cleanup when a parent div is removed.
         * Typically used by templates.
         * Objects attached to divs are terminated by processClearDiv which calls any handlers that registered a
         * template class using addClearDivForObjects.
         * @param templateClass - The template css class (without leading .)
         * @param divId - The div id (DOM) that where the object is attached
         * @param obj - the object to attach
         */
        public addObjectDataById(templateClass: string, divId: string, obj: any): void {
            var $el: JQuery<HTMLElement> = $(`#${divId}`);
            if (!$el.hasClass(templateClass)) throw `addObjectDataById called with class ${templateClass} - tag with id ${divId} does not have that css class`;/*DEBUG*/
            var data: any = $el.data("__Y_Data");
            if (data) throw `addObjectDataById - tag with id ${divId} already has data`;/*DEBUG*/
            $el.data("__Y_Data", obj);
            this.addClearDivForObjects(templateClass);
        }
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param divId - The div id (DOM) that where the object is attached
         */
        public getObjectDataById(divId: string): any {
            var $el: JQuery<HTMLElement> = $(`#${divId}`);
            if ($el.length === 0) throw `getObjectDataById - tag with id ${divId} has no data`;/*DEBUG*/
            var data: any = $el.data("__Y_Data");
            if (!data) throw `getObjectDataById - tag with id ${divId} has no data`;/*DEBUG*/
            return data;
        }
        /**
         * Removes a data object (a Typescript class) from a tag.
         * @param divId - The div id (DOM) that where the object is attached
         */
        public removeObjectDataById(divId: string): void {
            var $el: JQuery<HTMLElement> = $(`#${divId}`);
            if ($el.length === 0) throw `removeObjectDataById - tag with id ${divId} has no data`;/*DEBUG*/
            var data: any = $el.data("__Y_Data");
            if (data) data.term();
            $el.data("__Y_Data", null);
        }
        /**
         * Register a cleanup (typically used by templates) to terminate any objects that may be
         * attached to the template tag.
         * @param templateClass - The template css class (without leading .)
         */
        public addClearDivForObjects(templateClass: string): void {
            YetaWF_Basics.addClearDiv(function (tag: HTMLElement): void {
                var list: NodeListOf<Element> = tag.querySelectorAll(`.${templateClass}`);
                var len: number = list.length;
                for (var i: number = 0; i < len; ++i) {
                    var el: HTMLElement = list[i] as HTMLElement;
                    var obj: any = $(el).data("__Y_Data");
                    if (obj) obj.term();
                }
            });
        }

        // Selectors - APIs to detach selectors from jQuery so this could be replaced with a smaller library (like sizzle).

        /**
         * Tests whether the specified element matches the selector.
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        public elementMatches(elem: Element | null, selector: string): boolean {
            if (elem) return $(elem).is(selector);
            return false;
        }
        /**
         * Tests whether the specified element has the given css class.
         * @param elem The element to test.
         * @param css - The css class being tested.
         */
        public elementHasClass(elem: Element | null, css: string): boolean {
            css = css.trim();
            if (!elem) return false;
            if (elem.classList)
                return elem.classList.contains(css);
            else
                return new RegExp("(^| )" + css + "( |$)", "gi").test(elem.className);
        }

        // CONTENTCHANGE
        // CONTENTCHANGE
        // CONTENTCHANGE
        // APIs to detach custom event handling from jQuery so this could be replaced with a native mechanism

        public RegisterContentChange(callback: (event: Event, addonGuid: string, on: boolean) => void): void {
            $(document).on("YetaWF_Basics_Addon", function (event: any, addonGuid: string, on: boolean): void { callback(event, addonGuid, on); });
        }

        // NEWPAGE
        // NEWPAGE
        // NEWPAGE
        // APIs to detach custom event handling from jQuery so this could be replaced with a native mechanism

        public RegisterNewPage(callback: (event: Event, url: string) => void): void {
            $(document).on("YetaWF_Basics_NewPage", function (event: any, url: string): void { callback(event, url); });
        }

        // Expand/collapse Support

        /**
         * Expand/collapse support using 2 action links (Name=Expand/Collapse) which make 2 divs hidden/visible  (alternating)
         * @param divId The <div> containing the 2 action links.
         * @param collapsedId - The <div> to hide/show.
         * @param expandedId - The <div> to show/hide.
         */
        public ExpandCollapse(divId: string, collapsedId: string, expandedId: string): void {
            var div: HTMLElement = document.querySelector(`#${divId}`) as HTMLElement;
            if (!div) throw `#${divId} not found`;/*DEBUG*/
            var collapsedDiv: HTMLElement = document.querySelector(`#${collapsedId}`) as HTMLElement;
            if (!collapsedDiv) throw `#${collapsedId} not found`;/*DEBUG*/
            var expandedDiv: HTMLElement = document.querySelector(`#${expandedId}`) as HTMLElement;
            if (!expandedDiv) throw `#${expandedId} not found`;/*DEBUG*/

            var expLink: HTMLElement = div.querySelector("a[data-name='Expand']") as HTMLElement;
            if (!expLink) throw "a[data-name=\"Expand\"] not found";/*DEBUG*/
            var collLink: HTMLElement = div.querySelector("a[data-name='Collapse']") as HTMLElement;
            if (!collLink) throw "a[data-name=\"Expand\"] not found";/*DEBUG*/

            function expandHandler(event: Event): void {
                collapsedDiv.style.display = "none";
                expandedDiv.style.display = "";
                // init any controls that just became visible
                $(document).trigger("YetaWF_PropertyList_PanelSwitched", $(expandedDiv));
            }
            function collapseHandler(event: Event): void {
                collapsedDiv.style.display = "";
                expandedDiv.style.display = "none";
            }
            expLink.addEventListener("click", expandHandler, false);
            collLink.addEventListener("click", collapseHandler, false);
        }
    }

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

    var AnchorHandling: YetaWF.Anchors = new YetaWF.Anchors();
    AnchorHandling.init();

    // WhenReady

    $(document).ready(() => {
        YetaWF_Basics.processAllReady();
        YetaWF_Basics.processAllReadyOnce();
    });
}

/**
 * Basic services available throughout YetaWF.
 */
var YetaWF_Basics: YetaWF.BasicsServices = new YetaWF.BasicsServices();

