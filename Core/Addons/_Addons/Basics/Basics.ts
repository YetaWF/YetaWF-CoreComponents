/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// %%%%%%% TODO: There are JQuery references

/* TODO : While transitioning to TypeScript and to maintain compatibility with all plain JavaScript, these defs are all global rather than in their own namespace.
   Once the transition is complete, we need to revisit this */

/* Basics API, to be implemented by rendering-specific code - rendering code must define a YetaWF_BasicsImpl object implementing IBasicsImpl */

//$$ https://github.com/nefe/You-Dont-Need-jQuery

/**
    Implemented by custom rendering.
 */
declare var YetaWF_BasicsImpl: YetaWF.IBasicsImpl;
declare var YetaWF_Basics: YetaWF.BasicsServices;

interface String {
    startsWith: (text: string) => boolean;
    endWith: (text: string) => boolean;
    isValidInt(s: number, e: number): boolean;
    format(...args: any[]): string;
}

interface Window { // expose this as a known window property
    YetaWF_Basics: YetaWF.BasicsServices
}

/**
 * Class implementing basic services used throughout YetaWF.
 */
namespace YetaWF {

    export interface MessageOptions {
        encoded: boolean;
    }
    export interface ReloadInfo {
        module: HTMLElement;
        callback(): void;
    }
    export interface CharSize {
        width: number;
        height: number;
    }
    export interface ContentChangeEntry {
        callback(addonGuid: string, on: boolean);
    };
    export interface NewPageEntry {
        callback(url: string);
    };

    export interface IBasicsImpl {

        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        setLoading(on?: boolean): void;

        /**
         * Displays an informational message, usually in a popup.
         */
        message(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an error message, usually in a popup.
         */
        error(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays a confirmation message, usually in a popup.
         */
        confirm(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an alert message, usually in a popup.
         */
        alert(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an alert message, usually in a popup.
         */
        alertYesNo(message: string, title?: string, onYes?: () => void, onNo?: () => void, options?: MessageOptions): void;
        /**
         * Displays a "Please Wait" message.
         */
        pleaseWait(message?: string, title?: string): void;
        /**
         * Closes the "Please Wait" message (if any).
         */
        pleaseWaitClose(): void;

    }

    export interface IWhenReady {
        callback(tag: HTMLElement): void;
    }
    export interface IClearDiv {
        callback?(elem: HTMLElement): void;
    }

    export class BasicsServices /* implements IBasicsImpl */ { // doesn't need to implement IBasicImpl, used for type checking only

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
                this.pleaseWaitClose();
        }

        /**
         * Displays an informational message, usually in a popup.
         */
        public message(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.message(message, title, onOK, options); }
        /**
         * Displays an error message, usually in a popup.
         */
        public error(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.error(message, title, onOK, options); }
        /**
         * Displays a confirmation message, usually in a popup.
         */
        public confirm(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.confirm(message, title, onOK, options); }
        /**
         * Displays an alert message, usually in a popup.
         */
        public alert(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.alert(message, title, onOK, options); }
        /**
         * Displays an alert message with Yes/No buttons, usually in a popup.
         */
        public alertYesNo(message: string, title?: string, onYes?: () => void, onNo?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.alertYesNo(message, title, onYes, onNo, options); }
        /**
         * Displays a "Please Wait" message
         */
        public pleaseWait(message?: string, title?: string) { YetaWF_BasicsImpl.pleaseWait(message, title); }
        /**
         * Closes the "Please Wait" message (if any).
         */
        pleaseWaitClose(): void { YetaWF_BasicsImpl.pleaseWaitClose(); }

        // Implemented by YetaWF
        // Implemented by YetaWF
        // Implemented by YetaWF

        // Content handling (Unified Page Sets)

        public ContentHandling: YetaWF.Content;

        // Anchor handling

        public AnchorHandling: YetaWF.Anchors;

        // Url parsing

        public parseUrl(url: string): YetaWF.Url {
            var uri = new YetaWF.Url();
            uri.parse(url);
            return uri;
        }

        // Focus

        /**
         * Set focus to a suitable field within the specified elements.
         */
        public setFocus(tags?: HTMLElement[]): void {
            //TODO: this should also consider input fields with validation errors (although that seems to magically work right now)
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            var f: HTMLElement | null = null;
            var items = this.getElementsBySelector('.focusonme', tags);
            items = this.limitToVisibleOnly(items); //:visible
            for (let item of items) {
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
        public setCondense(tag: HTMLElement, width: number) {
            if (width < YVolatile.Skin.MinWidthForPopups) {
                this.elementAddClass(tag, 'yCondense');
                this.elementRemoveClass(tag, 'yNoCondense');
            } else {
                this.elementAddClass(tag, 'yNoCondense');
                this.elementRemoveClass(tag, 'yCondense');
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
                if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.ShowDivs) {
                    var uri = this.parseUrl(window.location.href);
                    var divs = this.getElementsBySelector(`.yUnified[data-url="${uri.getPath()}"]`);
                    if (divs.length > 0) {
                        $(window).scrollTop(($(divs).offset() as JQuery.Coordinates).top);
                        scrolled = true;
                    }
                }
            }

            // FOCUS
            // FOCUS
            // FOCUS

            this.registerDocumentReady(() => { // only needed during full page load
                if (!scrolled && location.hash.length <= 1)
                    this.setFocus();
            });

            // content navigation

            this.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious;// save loaded addons
        };

        // Panes

        public showPaneSet(id: string, editMode: boolean, equalHeights: boolean) {

            var div = this.getElementById(id);
            var shown = false;
            if (editMode) {
                div.style.display = 'block';
                shown = true;
            } else {
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
                this.registerDocumentReady(() => { // TODO: This only works for full page loads
                    var panes = this.getElementsBySelector(`#${id} > div:visible`);// get all immediate child divs (i.e., the panes)
                    // exclude panes that have .y_cleardiv
                    var newPanes: HTMLElement[] = [];
                    for (let pane of panes) {
                        if (!this.elementHasClass(pane, 'y_cleardiv'))
                            newPanes.push(pane);
                    }
                    panes = newPanes;

                    var height = 0;
                    // calc height
                    for (let pane of panes) {
                        var h = $(pane).height() || 0;
                        if (h > height)
                            height = h;
                    }
                    // set each pane's height
                    for (let pane of panes) {
                        pane.style.minHeight = `${height}px`;
                    }
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
            uri.addSearch("!rand", ((new Date()).getTime()).toString());// cache buster

            if (YVolatile.Basics.UnifiedMode != UnifiedModeEnum.None) {
                if (this.ContentHandling.setContent(uri, true))
                    return;
            }
            if (keepPosition) {
                w.location.assign(uri.toUrl());
                return;
            }
            w.location.reload(true);
        }

        /**
         * Reloads a module in place, defined by the specified tag (any tag within the module).
         */
        public reloadModule(tag?: HTMLElement) {
            if (!tag) {
                if (!this.reloadingModule_TagInModule) throw "No module found";/*DEBUG*/
                tag = this.reloadingModule_TagInModule;
            }
            var mod = this.getModuleFromTag(tag);
            var form = this.getElement1BySelector('form', [mod]) as HTMLFormElement;
            YetaWF_Forms.submit(form, false, YConfigs.Basics.Link_SubmitIsApply + "=y");// the form must support a simple Apply
        }

        private reloadingModule_TagInModule: HTMLElement | null = null;

        // Usage:
        // YetaWF_Basics.reloadInfo.push({  // TODO: revisit this (not a nice interface, need add(), but only used in grid for now)
        //   module: mod,               // module <div> to be refreshed
        //   callback: function() {}    // function to be called
        // });

        public reloadInfo: ReloadInfo[] = [];

        public refreshModule(mod: HTMLElement) {
            for (let entry of this.reloadInfo) {
                if (entry.module.id == mod.id) {
                    entry.callback();
                }
            }
        };
        public refreshModuleByAnyTag(elem: HTMLElement) {
            var mod = this.getModuleFromTag(elem);
            for (let entry of this.reloadInfo) {
                if (entry.module.id == mod.id) {
                    entry.callback();
                }
            }
        };
        public refreshPage(): void {
            for (let entry of this.reloadInfo) {
                entry.callback();
            }
        };

        // Module locator

        /**
         * Get a module defined by the specified tag (any tag within the module). Returns null if none found.
         */
        private getModuleFromTagCond(tag: HTMLElement): HTMLElement | null {
            var mod = this.elementClosest(tag, '.yModule');
            if (mod) return null;
            return mod;
        };
        /**
         * Get a module defined by the specified tag (any tag within the module). Throws exception if none found.
         */
        private getModuleFromTag(tag: HTMLElement): HTMLElement {
            var mod = this.getModuleFromTagCond(tag);
            if (mod == null) { debugger; throw "Can't find containing module"; }/*DEBUG*/
            return mod;
        };

        public getModuleGuidFromTag(tag: HTMLElement): string {
            var mod = this.getModuleFromTag(tag);
            var guid = mod.getAttribute('data-moduleguid');
            if (!guid) throw "Can't find module guid";/*DEBUG*/
            return guid;
        };

        // Get character size

        // CHARSIZE (from module or page/YVolatile)
        /**
         * Get the current character size used by the module defined using the specified tag (any tag within the module) or the default size.
         */
        public getCharSizeFromTag(tag: HTMLElement | null): CharSize {
            var width: number, height: number;
            var mod: HTMLElement | null = null;
            if (tag)
                mod = this.getModuleFromTagCond(tag);
            if (mod) {
                var w = mod.getAttribute('data-charwidthavg');
                if (!w) throw "missing data-charwidthavg attribute";/*DEBUG*/
                width = Number(w);
                var h = mod.getAttribute('data-charheight');
                if (!h) throw "missing data-charheight attribute";/*DEBUG*/
                height = Number(h);
            } else {
                width = YVolatile.Basics.CharWidthAvg;
                height = YVolatile.Basics.CharHeight;
            }
            return { width: width, height: height };
        }

        // Utility functions

        public htmlEscape(s: string | undefined, preserveCR?: string): string {
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

        public processAjaxReturn(result: string, textStatus: string, xhr: XMLHttpRequest, tagInModule?: HTMLElement, onSuccessNoData?: () => void, onHandleErrorResult?: (string) => void): boolean {
            //if (xhr.responseType != "json") throw `processAjaxReturn: unexpected responseType ${xhr.responseType}`;
            var result: string;
            try {
                result = <string>eval(result);
            } catch (e) { }
            result = result || '(??)';
            if (xhr.status === 200) {
                this.reloadingModule_TagInModule = tagInModule || null;
                if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                    if (script.length == 0) { // all is well, but no script to execute
                        if (onSuccessNoData != undefined) {
                            onSuccessNoData();
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
                    eval(script);// if this uses YetaWF_Basics.alert or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explicitly reload page in your javascript)
                    this.reloadPage(true);
                    return true;
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
                    eval(script);// if this uses YetaWF_Basics.alert or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explicitly reload module in your javascript)
                    this.reloadModule();
                    return true;
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
                    //if (!this.isInPopup()) throw "Not supported - only available within a popup";/*DEBUG*/
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
                    eval(script);
                    if (tagInModule)
                        this.refreshModuleByAnyTag(tagInModule);
                    return true;
                } else {
                    if (onHandleErrorResult != undefined) {
                        onHandleErrorResult(result);
                    } else {
                        this.error(YLocs.Basics.IncorrectServerResp);
                    }
                    return false;
                }
            } else {
                YetaWF_Basics.alert(YLocs.Forms.AjaxError.format(xhr.status, result, YLocs.Forms.AjaxErrorTitle));
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

        // Usage:
        // YetaWF_Basics.addWhenReady((tag) => {});

        private whenReady: IWhenReady[] = [];

        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor tag/elem and only manipulate child objects.
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReady requests after reloading the grid with data (which doesn't run any javascript automatically).
         * @param def
         */
        public addWhenReady(callback: (section: HTMLElement) => void): void {
            this.whenReady.push({ callback: callback });
        }

        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        public processAllReady(tags?: HTMLElement[]): void {
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            for (let entry of this.whenReady) {
                try { // catch errors to insure all callbacks are called
                    for (let tag of tags)
                        entry.callback(tag);
                } catch (err) {
                    console.log(err.message);
                }
            }
        }

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
        public whenReadyOnce: IWhenReady[] = [];

        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor tag/elem and only manipulate child objects.
         * Callback functions are registered by whomever needs this type of processing. For example, a grid can
         * process all whenReadyOnce requests after reloading the grid with data (which doesn't run any javascript automatically).
         * The callback is called for ONCE. Then the callback is removed.
         * @param def
         */
        public addWhenReadyOnce(callback: (section: HTMLElement) => void): void {
            this.whenReadyOnce.push({ callback: callback });
        }

        /**
         * Process all callbacks for the specified element to initialize children. This is used by YetaWF.Core only.
         * @param elem The element for which all callbacks should be called to initialize children.
         */
        public processAllReadyOnce(tags?: HTMLElement[]): void {
            if (!tags) {
                tags = [];
                tags.push(document.body);
            }
            for (let entry of this.whenReadyOnce) {
                try { // catch errors to insure all callbacks are called
                    for (let tag of tags)
                        entry.callback(tag);
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
         * Process all callbacks for the specified element being cleared.
         * @param elem The element being cleared.
         */
        public processClearDiv(tag: HTMLElement): void {
            for (let entry of this.clearDiv) {
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
            var el = this.getElementById(divId);
            var data: any = $(el).data("__Y_Data");
            if (data) throw `addObjectDataById - tag with id ${divId} already has data`;/*DEBUG*/
            $(el).data("__Y_Data", obj);
            this.addClearDivForObjects(templateClass);
        }
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param divId - The div id (DOM) that where the object is attached
         */
        public getObjectDataById(divId: string): any {
            var el = this.getElementById(divId);
            var data: any = $(el).data("__Y_Data");
            if (!data) throw `getObjectDataById - tag with id ${divId} has no data`;/*DEBUG*/
            return data;
        }
        /**
         * Removes a data object (a Typescript class) from a tag.
         * @param divId - The div id (DOM) that where the object is attached
         */
        public removeObjectDataById(divId: string): void {
            var el = this.getElementById(divId);
            var data: any = $(el).data("__Y_Data");
            if (data) data.term();
            $(el).data("__Y_Data", null);
        }
        /**
         * Register a cleanup (typically used by templates) to terminate any objects that may be
         * attached to the template tag.
         * @param templateClass - The template css class (without leading .)
         */
        public addClearDivForObjects(templateClass: string): void {
            this.addClearDiv((tag: HTMLElement) => {
                var list = this.getElementsBySelector(`.${templateClass}`, [tag]);
                for (let el of list) {
                    var obj: any = $(el).data("__Y_Data");
                    if (obj) obj.term();
                }
            });
        }

        // Selectors

        /**
         * Get an element by id.
         */
        public getElementById(elemId: string): HTMLElement {
            var div: HTMLElement = document.querySelector(`#${elemId}`) as HTMLElement;
            if (!div) throw `Element with id ${elemId} not found`;/*DEBUG*/
            return div;
        }
        /**
         * Get elements from an array of tags by selector. (similar to jquery var x = $(selector, elems); with standard css selectors)
         */
        public getElementsBySelector(selector: string, elems?: HTMLElement[]): HTMLElement[] {
            var all: HTMLElement[] = [];
            if (!elems)
                elems = [document.body];
            for (let elem of elems) {
                var list: NodeListOf<Element> = elem.querySelectorAll(selector);
                var len: number = list.length;
                for (var i: number = 0; i < len; ++i) {
                    all.push(list[i] as HTMLElement);
                }
            }
            return all;
        }
        /**
         * Get the first element from an array of tags by selector. (similar to jquery var x = $(selector, elems); with standard css selectors)
         */
        public getElement1BySelectorCond(selector: string, elems?: HTMLElement[]): HTMLElement | null {
            if (!elems)
                elems = [document.body];
            for (let elem of elems) {
                var list: NodeListOf<Element> = elem.querySelectorAll(selector);
                if (list.length > 0)
                    return list[0] as HTMLElement;
            }
            return null;
        }
        /**
         * Get the first element from an array of tags by selector. (similar to jquery var x = $(selector, elems); with standard css selectors)
         */
        public getElement1BySelector(selector: string, elems?: HTMLElement[]): HTMLElement {
            var elem = this.getElement1BySelectorCond(selector, elems);
            if (elem == null)
                throw `Element with selector ${selector} not found`;
            return elem;
        }
        /**
         * Removes all input[type='hidden'] fields. (similar to jquery var x = elems.not("input[type='hidden']"); )
         */
        public limitToNotTypeHidden(elems: HTMLElement[]): HTMLElement[] {
            var all: HTMLElement[] = [];
            for (let elem of elems) {
                if (elem.tagName !== "INPUT" || elem.getAttribute("type") !== "hidden") //$$$check casing
                    all.push(elem);
            }
            return all;
        }
        /**
         * Returns items that are visible. (similar to jquery var x = elems.filter(':visible'); )
         */
        public limitToVisibleOnly(elems: HTMLElement[]): HTMLElement[] {
            var all: HTMLElement[] = [];
            for (let elem of elems) {
                if (elem.clientWidth > 0 && elem.clientHeight > 0)
                    all.push(elem);
            }
            return all;
        }

        /**
         * Tests whether the specified element matches the selector.
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        public elementMatches(elem: Element | null, selector: string): boolean {
            if (elem)
                return elem.matches(selector);
            return false;
        }
        /**
         * Finds the closest element up the DOM hierarchy that matches the selector (including the starting element)
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        public elementClosest(elem: HTMLElement, selector: string): HTMLElement | null {
            var e: HTMLElement | null = elem;
            while (e) {
                if (this.elementMatches(e, selector))
                    return e;
                else
                    e = e.parentElement;
            }
            return null;
        }

        // DOM manipulation

        /**
         * Removes the specified element.
         * @param elem - The element to remove.
         */
        public removeElement(elem: HTMLElement) {
            if (!elem.parentElement) return;
            elem.parentElement.removeChild(elem);
        }

        // Element Css

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

        public elementAddClass(elem: Element, className: string): void {
            if (elem.classList)
                elem.classList.add(className);
            else
                elem.className += ' ' + className;
        }
        public elementRemoveClass(elem: Element, className: string): void {
            if (elem.classList)
                elem.classList.remove(className);
            else
                elem.className = elem.className.replace(new RegExp('(^|\\b)' + className.split(' ').join('|') + '(\\b|$)', 'gi'), ' ');
        }

        // Events

        public registerDocumentReady(callback: () => void): void {
            if ((document as any).attachEvent ? document.readyState === "complete" : document.readyState !== "loading") {
                callback();
            } else {
                document.addEventListener('DOMContentLoaded', callback);
            }
        }
        public registerEventHandlerDocument<K extends keyof DocumentEventMap>(eventName: K, selector: string | null, callback: (ev: DocumentEventMap[K]) => boolean): void {
            window.addEventListener(eventName, (ev) => this.handleEvent(null, ev, selector, callback));
        }
        public registerEventHandlerWindow<K extends keyof HTMLFrameSetElementEventMap>(eventName: K, selector: string | null, callback: (ev: HTMLFrameSetElementEventMap[K]) => boolean): void {
            window.addEventListener(eventName, (ev) => this.handleEvent(null, ev, selector, callback));
        }
        public registerEventHandlerBody<K extends keyof HTMLElementEventMap>(eventName: K, selector: string | null, callback: (ev: HTMLElementEventMap[K]) => boolean): void {
            this.registerEventHandler(document.body, eventName, selector, callback);
        }
        public registerEventHandler<K extends keyof HTMLElementEventMap>(tag: HTMLElement, eventName: K, selector: string | null, callback: (ev: HTMLElementEventMap[K]) => boolean): void {
            tag.addEventListener(eventName, (ev) => this.handleEvent(tag, ev, selector, callback));
        }
        private handleEvent(listening: HTMLElement | null, ev: Event, selector: string | null, callback: (ev: Event) => boolean): void {
            // about event handling https://www.sitepoint.com/event-bubbling-javascript/
            // srcElement should be target//$$$$ srcElement is non-standard
            //console.log(`event ${ev.type} selector ${selector} srcElement ${(ev.srcElement as HTMLElement).outerHTML}`);
            if (ev.eventPhase == ev.CAPTURING_PHASE) {
                if (selector) return;// if we have a selector we can't possibly have a match because the src element is the main tag where we registered the listener
            } else if (ev.eventPhase == ev.BUBBLING_PHASE) {
                if (!selector) return;
                // check elements between the one that caused the event and the listening element (inclusive) for a match to the selector
                var elem: HTMLElement | null = ev.srcElement as HTMLElement | null;
                while (elem) {
                    if (YetaWF_Basics.elementMatches(elem, selector))
                        break;
                    if (listening == elem)
                        return;// checked all elements
                    elem = elem.parentElement;
                    if (elem == null)
                        return;
                }
            } else
                return;
            //console.log(`event ${ev.type} selector ${selector} match`);
            var result: boolean = callback(ev);
            if (!result) {
                //console.log(`event ${ev.type} selector ${selector} stop bubble`);
                ev.stopPropagation();
                ev.preventDefault();
            }
        }


        // CONTENTCHANGE
        // CONTENTCHANGE
        // CONTENTCHANGE

        private ContentChangeHandlers: ContentChangeEntry[] = [];

        public registerContentChange(callback: (addonGuid: string, on: boolean) => void): void {
            this.ContentChangeHandlers.push({ callback: callback });
        }
        public processContentChange(addonGuid: string, on: boolean) {
            for (var entry of this.ContentChangeHandlers) {
                entry.callback(addonGuid, on);
            }
        }

        // NEWPAGE
        // NEWPAGE
        // NEWPAGE

        private NewPageHandlers: NewPageEntry[] = [];

        public registerNewPage(callback: (url: string) => void): void {
            this.NewPageHandlers.push({ callback: callback });
        }
        public processNewPage(url: string): void {
            for (var entry of this.NewPageHandlers) {
                entry.callback(url);
            }
        }

        // Expand/collapse Support

        /**
         * Expand/collapse support using 2 action links (Name=Expand/Collapse) which make 2 divs hidden/visible  (alternating)
         * @param divId The <div> containing the 2 action links.
         * @param collapsedId - The <div> to hide/show.
         * @param expandedId - The <div> to show/hide.
         */
        public expandCollapseHandling(divId: string, collapsedId: string, expandedId: string): void {
            var div = this.getElementById(divId);
            var collapsedDiv = this.getElementById(collapsedId);
            var expandedDiv = this.getElementById(expandedId);

            var expLink = this.getElement1BySelector("a[data-name='Expand']", [div]);
            var collLink = this.getElement1BySelector("a[data-name='Collapse']", [div]);

            this.registerEventHandler(expLink, "click", null, (ev: Event) => {
                collapsedDiv.style.display = "none";
                expandedDiv.style.display = "";
                // init any controls that just became visible
                $(document).trigger("YetaWF_PropertyList_PanelSwitched", $(expandedDiv));
                return true;
            });
            this.registerEventHandler(collLink, "click", null, (ev: Event) => {
                collapsedDiv.style.display = "";
                expandedDiv.style.display = "none";
                return true;
            });
        }

        constructor() {

            YetaWF_Basics = this;// set global so we can initialize anchor/content
            this.AnchorHandling = new YetaWF.Anchors();
            this.ContentHandling = new YetaWF.Content();

            // screen size yCondense/yNoCondense support

            this.registerEventHandlerWindow("resize", null, (ev) => {
                this.setCondense(document.body, window.innerWidth);
                return true;
            });

            this.registerDocumentReady(() => {
                this.setCondense(document.body, window.innerWidth);
            });

            // Navigation

            this.registerEventHandlerWindow("popstate", null, (ev) => {
                if (this.suppressPopState) {
                    this.suppressPopState = false;
                    return true;
                }
                var uri = this.parseUrl(window.location.href);
                return !this.ContentHandling.setContent(uri, false);
            });

            // <a> links that only have a hash are intercepted so we don't go through content handling
            this.registerEventHandlerBody("click", "a[href^='#']", (ev) => {

                // find the real anchor, ev.srcElement was clicked, but it may not be the anchor itself
                if (!ev.srcElement) return true;
                var anchor = YetaWF_Basics.elementClosest(ev.srcElement as HTMLElement, "a") as HTMLAnchorElement;
                if (!anchor) return true;

                this.suppressPopState = true;
                return true;
            });

            // <A> links

            // WhenReady

            this.registerDocumentReady(() => {
                this.processAllReady();
                this.processAllReadyOnce();
            });
        }
    }
}

/**
 * Basic services available throughout YetaWF.
 */
var YetaWF_Basics: YetaWF.BasicsServices = new YetaWF.BasicsServices();
