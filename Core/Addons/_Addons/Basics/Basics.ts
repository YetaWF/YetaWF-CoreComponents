/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* eslint-disable no-underscore-dangle */

/* TODO : While transitioning to TypeScript and to maintain compatibility with all plain JavaScript, some defs are global rather than in their own namespace.
   Once the transition is complete, we need to revisit this */

/* Basics API, to be implemented by rendering-specific code - rendering code must define a global YetaWF_BasicsImpl object implementing IBasicsImpl */

/**
 * Implemented by custom rendering.
 */
declare var YetaWF_BasicsImpl: YetaWF.IBasicsImpl;

/* Polyfills */
// eslint-disable-next-line id-blacklist
interface String {
    isValidInt(s: number, e: number): boolean;
    format(...args: any[]): string;
}

interface Window { // expose this as a known window property
    $YetaWF: YetaWF.BasicsServices;
}
interface Event {
    __YetaWFElem: HTMLElement; // the element that matched the selector during event bubbling
}

/**
 * Class implementing basic services used throughout YetaWF.
 */
namespace YetaWF {

    export interface MessageOptions {
        encoded: boolean;
        /* time in milliseconds to auto-close the message (toast only)*/
        autoClose?: number;
        /* Defines whether the user can dismiss the message (toast only) */
        canClose?: boolean;
        /* toast name - If the named toast already exists, it is not added again (toast only) */
        name?: string;
    }

    interface ReloadInfo {
        module: HTMLElement;
        tagId: string;
        callback(module: HTMLElement): void;
    }
    interface PageChangeEntry {
        callback(): void;
        onceOnly: boolean;
    }
    interface ClearDivEntry {
        autoRemove: boolean;
        callback?(elem: HTMLElement): boolean;
    }
    interface WhenReadyEntry {
        callback(tag: HTMLElement): void;
    }

    interface DataObjectEntry {
        DivId: string;
        Data: any;
    }

    export interface DetailsEventContainerResize {
        container: HTMLElement;
    }
    export interface DetailsEventContainerScroll {
        container: HTMLElement;
    }
    export interface DetailsActivateDiv {
        tags: HTMLElement[];
    }
    export interface DetailsPanelSwitched {
        panel: HTMLElement;
    }
    export interface DetailsAddonChanged {
        addonGuid: string;
        on: boolean;
    }

    /**
     * Implemented by rendered (such as ComponentsHTML)
     */
    export interface IBasicsImpl {

        /** Returns whether the loading indicator is on or off */
        isLoading: boolean;

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
         * Displays an warning message, usually in a popup.
         */
        warning(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
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
        /**
         * Closes any open overlays, menus, dropdownlists, tooltips, etc. (Popup windows are not handled and are explicitly closed using $YetaWF.Popups)
         */
        closeOverlays(): void;
        /**
         * Enable/disable an element.
         * Some controls need some extra settings when disabled=disabled isn't enough.
         * Also used to update visual styles to reflect the status.
         */
        elementEnableToggle(elem: HTMLElement, enable: boolean): void;
        /**
         * Returns whether a message popup dialog is currently active.
         */
        messagePopupActive(): boolean;
        /**
         * Given an element, returns the owner (typically a module) that owns the element.
         * The DOM hierarchy may not reflect this ownership, for example with popup menus which are appended to the <body> tag, but are owned by specific modules.
         */
        getOwnerFromTag(ag: HTMLElement): HTMLElement | null;
    }

    export class BasicsServices /* implements IBasicsImpl */ { // doesn't need to implement IBasicImpl, used for type checking only

        public static readonly PAGECHANGEDEVENT: string = "page_change";
        public static readonly EVENTBEFOREPRINT: string = "print_before";
        public static readonly EVENTAFTERPRINT: string = "print_after";
        public static readonly EVENTCONTAINERSCROLL: string = "container_scroll";
        public static readonly EVENTCONTAINERRESIZE: string = "container_resize";
        public static readonly EVENTACTIVATEDIV: string = "activate_div";
        public static readonly EVENTPANELSWITCHED: string = "panel_switched";
        public static readonly EVENTADDONCHANGED: string = "addon_changed";

        // Implemented by renderer
        // Implemented by renderer
        // Implemented by renderer

        /** Returns whether the loading indicator is on or off */
        public get isLoading(): boolean {
            return YetaWF_BasicsImpl.isLoading;
        }

        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        public setLoading(on?: boolean): void {
            YetaWF_BasicsImpl.setLoading(on);
            if (on === false)
                this.pleaseWaitClose();
        }

        /**
         * Displays an informational message, usually in a popup.
         */
        public message(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.message(message, title, onOK, options); }
        /**
         * Displays an error message, usually in a popup.
         */
        public warning(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.warning(message, title, onOK, options); }
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
        public pleaseWait(message?: string, title?: string): void { YetaWF_BasicsImpl.pleaseWait(message, title); }
        /**
         * Closes the "Please Wait" message (if any).
         */
        pleaseWaitClose(): void { YetaWF_BasicsImpl.pleaseWaitClose(); }
        /**
         * Closes any open overlays, menus, dropdownlists, tooltips, etc. (Popup windows are not handled and are explicitly closed using $YetaWF.Popups)
         */
        closeOverlays(): void { YetaWF_BasicsImpl.closeOverlays(); }

        // Implemented by YetaWF
        // Implemented by YetaWF
        // Implemented by YetaWF

        // Content handling (Unified Page Sets)

        public ContentHandling: YetaWF.Content;

        // Anchor handling

        public AnchorHandling: YetaWF.Anchors;

        // Form handling
        private forms: YetaWF.Forms | null = null;

        get Forms(): YetaWF.Forms {
            if (!this.forms) {
                this.forms = new YetaWF.Forms(); // if this fails, forms.*.js was not included automatically
                this.forms.init();
            }
            return this.forms;
        }
        public FormsAvailable() : boolean {
            return this.forms != null;
        }

        // Popup handling
        private popups: YetaWF.Popups | null = null;

        get Popups(): YetaWF.Popups {
            if (!this.popups) {
                this.popups = new YetaWF.Popups(); // if this fails, popups.*.js was not included automatically
                this.popups.init();
            }
            return this.popups;
        }
        public PopupsAvailable(): boolean {
            return this.popups != null;
        }

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
            var f: HTMLElement | null = null;
            var items = this.getElementsBySelector(".yFocusOnMe", tags);
            items = this.limitToVisibleOnly(items); //:visible
            for (let item of items) {
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
        public setCondense(tag: HTMLElement, width: number): void {
            if (width < YVolatile.Skin.MinWidthForCondense) {
                this.elementAddClass(tag, "yCondense");
                this.elementRemoveClass(tag, "yNoCondense");
            } else {
                this.elementAddClass(tag, "yNoCondense");
                this.elementRemoveClass(tag, "yCondense");
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
            if (this.PopupsAvailable())
                this.Popups.closePopup(forceReload);
        }

        // Scrolling

        public setScrollPosition(): boolean {
            // positioning isn't exact. For example, TextArea (i.e. CKEditor) will expand the window size which may happen later.
            var uri = this.parseUrl(window.location.href);
            var left = uri.getSearch(YConfigs.Basics.Link_ScrollLeft);
            var top = uri.getSearch(YConfigs.Basics.Link_ScrollTop);
            if (left || top) {
                window.scroll(left ? parseInt(left, 10) : 0, top ? parseInt(top, 10) : 0);
                return true;
            } else
                return false;
        }

        // Page

        /**
         * currently loaded addons
         */
        public UnifiedAddonModsLoaded: string[] = [];

        /**
         * Initialize the current page (full page load) - runs during page load, before document ready
         */
        public initPage(): void {

            this.init();

            // page position

            var scrolled = this.setScrollPosition();
            if (!scrolled) {
                if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.ShowDivs) {
                    var uri = this.parseUrl(window.location.href);
                    var divs = this.getElementsBySelector(`.yUnified[data-url="${uri.getPath()}"]`);
                    if (divs.length > 0) {
                        window.scroll(0, divs[0].offsetTop);
                        scrolled = true;
                    }
                }
            }

            // FOCUS
            // FOCUS
            // FOCUS

            this.registerDocumentReady((): void => { // only needed during full page load
                if (!scrolled && location.hash.length <= 1)
                    this.setFocus();
                else {
                    let hash = location.hash;
                    if (hash && hash.length > 1) {
                        let target: HTMLElement | null = null;
                        try {// handle invalid id
                            target = $YetaWF.getElement1BySelectorCond(hash);
                        } catch (e) { }
                        if (target) {
                            target.scrollIntoView();
                        }
                    }
                }
            });

            // content navigation

            this.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious;// save loaded addons
        }

        // Panes

        public showPaneSet(id: string, editMode: boolean, equalHeights: boolean): void {

            var div = this.getElementById(id);
            var shown = false;
            if (editMode) {
                div.style.display = "block";
                shown = true;
            } else {
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
                this.registerDocumentReady((): void => { // TODO: This only works for full page loads
                    var panes = this.getElementsBySelector(`#${id} > div`);// get all immediate child divs (i.e., the panes)
                    panes = this.limitToVisibleOnly(panes); //:visible
                    // exclude panes that have .y_cleardiv
                    var newPanes: HTMLElement[] = [];
                    for (let pane of panes) {
                        if (!this.elementHasClass(pane, "y_cleardiv"))
                            newPanes.push(pane);
                    }
                    panes = newPanes;

                    var height = 0;
                    // calc height
                    for (let pane of panes) {
                        var h = pane.clientHeight;
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

        public suppressPopState: number = 0;

        public setUrl(url: string): void {
            try {
                var stateObj = {};
                history.pushState(stateObj, "", url);
            } catch (err) { }
        }

        public loadUrl(url: string): void {
            let uri = $YetaWF.parseUrl(url);
            let result = $YetaWF.ContentHandling.setContent(uri, true);
            if (result !== YetaWF.SetContentResult.ContentReplaced)
                window.location.assign(url);
        }

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
                var left = (document.documentElement && document.documentElement.scrollLeft) || document.body.scrollLeft;
                if (left)
                    uri.addSearch(YConfigs.Basics.Link_ScrollLeft, left.toString());
                var top = (document.documentElement && document.documentElement.scrollTop) || document.body.scrollTop;
                if (top)
                    uri.addSearch(YConfigs.Basics.Link_ScrollTop, top.toString());
            }
            uri.removeSearch("!rand");
            uri.addSearch("!rand", ((new Date()).getTime()).toString());// cache buster

            if (YVolatile.Basics.UnifiedMode !== UnifiedModeEnum.None) {
                if (this.ContentHandling.setContent(uri, true) !== SetContentResult.NotContent)
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
        public reloadModule(tag?: HTMLElement): void {
            if (!tag) {
                if (!this.reloadingModuleTagInModule) throw "No module found";/*DEBUG*/
                tag = this.reloadingModuleTagInModule;
            }
            var mod = ModuleBase.getModuleDivFromTag(tag);
            var form = this.getElement1BySelector("form", [mod]) as HTMLFormElement;
            this.Forms.submit(form, false, YConfigs.Basics.Link_SubmitIsApply + "=y");// the form must support a simple Apply
        }

        private reloadingModuleTagInModule: HTMLElement | null = null;

        public refreshModule(mod: HTMLElement): void {
            if (!this.getElementByIdCond(mod.id)) throw `Module with id ${mod.id} not found`;/*DEBUG*/
            this.processReloadInfo(mod.id);
        }
        public refreshModuleByAnyTag(elem: HTMLElement): void {
            var mod = ModuleBase.getModuleDivFromTag(elem);
            this.processReloadInfo(mod.id);
        }
        private processReloadInfo(moduleId: string): void {
            var len = this.reloadInfo.length;
            for (var i = 0; i < len; ++i) {
                let entry = this.reloadInfo[i];
                if (entry.module.id === moduleId) {
                    if (this.getElementByIdCond(entry.tagId)) {
                        // call the reload callback
                        entry.callback(entry.module);
                    } else {
                        // the tag requesting the callback no longer exists
                        this.reloadInfo.splice(i, 1);
                        --len;
                        --i;
                    }
                }
            }
        }

        public refreshPage(): void {
            var len = this.reloadInfo.length;
            for (var i = 0; i < len; ++i) {
                let entry = this.reloadInfo[i];
                if (this.getElementByIdCond(entry.module.id)) { // the module exists
                    if (this.getElementByIdCond(entry.tagId)) {
                        // the tag requesting the callback still exists
                        if (!this.elementClosestCond(entry.module, ".yPopup, .yPopupDyn")) // don't refresh modules within popups when refreshing the page
                            entry.callback(entry.module);
                    } else {
                        // the tag requesting the callback no longer exists
                        this.reloadInfo.splice(i, 1);
                        --len;
                        --i;
                    }
                } else {
                    // the module no longer exists
                    this.reloadInfo.splice(i, 1);
                    --len;
                    --i;
                }
            }
        }

        private reloadInfo: ReloadInfo[] = [];

        /**
         * Registers a callback that is called when a module is to be refreshed/reloaded.
         * @param tag Defines the tag that is requesting the callback when the containing module is refreshed.
         * @param callback Defines the callback to be called.
         * The element defined by tag may no longer exist when a module is refreshed in which case the callback is not called (and removed).
         */
        public registerModuleRefresh(tag: HTMLElement, callback: (module: HTMLElement) => void): void {
            var module = ModuleBase.getModuleDivFromTag(tag); // get the containing module
            if (!tag.id || tag.id.length === 0)
                throw `No id defined for ${tag.outerHTML}`;
            // reuse existing entry if this id is already registered
            for (let entry of this.reloadInfo) {
                if (entry.tagId === tag.id) {
                    entry.callback = callback;
                    return;
                }
            }
            // new id
            this.reloadInfo.push({ module: module, tagId: tag.id, callback: callback });
        }

        // Module locator

        public getModuleGuidFromTag(tag: HTMLElement): string {
            var mod = YetaWF.ModuleBase.getModuleDivFromTag(tag);
            var guid = mod.getAttribute("data-moduleguid");
            if (!guid) throw "Can't find module guid";/*DEBUG*/
            return guid;
        }

        // Utility functions

        public htmlEscape(s: string | undefined, preserveCR?: boolean): string {
            let pre = preserveCR ? "&#13;" : "\n";
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
        }
        public htmlAttrEscape(s: string): string {
            this.escElement.textContent = s;
            s = this.escElement.innerHTML;
            return s.replace(/'/g, "&apos;").replace(/"/g, "&quot;");
        }
        private escElement : HTMLDivElement = document.createElement("div");

        /**
         * string compare that considers null == ""
         */
        public stringCompare(str1: string | null, str2: string | null): boolean {
            if (!str1 && !str2) return true;
            return str1 === str2;
        }

        // Ajax result handling

        public processAjaxReturn(result: string, textStatus: string, xhr: XMLHttpRequest, tagInModule?: HTMLElement,
            onSuccessNoData?: () => void,
            onRawDataResult?: (result: string) => void,
            onJSONResult?: (result: any) => void): boolean {

            //if (xhr.responseType != "json") throw `processAjaxReturn: unexpected responseType ${xhr.responseType}`;
            try {
                // eslint-disable-next-line no-eval
                result = <string>eval(result);
            } catch (e) { }
            result = result || "(??)";
            if (xhr.status === 200) {
                this.reloadingModuleTagInModule = tagInModule || null;
                if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                    if (script.length === 0) { // all is well, but no script to execute
                        if (onSuccessNoData !== undefined) {
                            onSuccessNoData();
                        }
                    } else {
                        // eslint-disable-next-line no-eval
                        eval(script);
                    }
                    return true;
                } else if (result.startsWith(YConfigs.Basics.AjaxJSONReturn)) {
                    var json = result.substring(YConfigs.Basics.AjaxJSONReturn.length);
                    if (onJSONResult) {
                        onJSONResult(JSON.parse(json));
                        return true;
                    }
                    return false;
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                    // eslint-disable-next-line no-eval
                    eval(script);
                    return false;
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadPage)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadPage.length);
                    // eslint-disable-next-line no-eval
                    eval(script);// if this uses $YetaWF.alert or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explicitly reload page in your javascript)
                    this.reloadPage(true);
                    return true;
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
                    // eslint-disable-next-line no-eval
                    eval(script);// if this uses $YetaWF.alert or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explicitly reload module in your javascript)
                    this.reloadModule();
                    return true;
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
                    //if (!this.isInPopup()) throw "Not supported - only available within a popup";/*DEBUG*/
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
                    // eslint-disable-next-line no-eval
                    eval(script);
                    if (tagInModule)
                        this.refreshModuleByAnyTag(tagInModule);
                    return true;
                } else {
                    if (onRawDataResult !== undefined) {
                        onRawDataResult(result);
                        return true;
                    } else {
                        this.error(YLocs.Basics.IncorrectServerResp);
                    }
                    return false;
                }
            } else if (xhr.status >= 400 && xhr.status <= 499) {
                $YetaWF.error(YLocs.Forms.AjaxError.format(xhr.status, YLocs.Forms.AjaxNotAuth), YLocs.Forms.AjaxErrorTitle);
                return false;
            } else if (xhr.status === 0) {
                $YetaWF.error(YLocs.Forms.AjaxError.format(xhr.status, YLocs.Forms.AjaxConnLost), YLocs.Forms.AjaxErrorTitle);
                return false;
            } else {
                $YetaWF.error(YLocs.Forms.AjaxError.format(xhr.status, result), YLocs.Forms.AjaxErrorTitle);
                return false;
            }
        }

        // JSX

        /**
         * React-like createElement function so we can use JSX in our TypeScript/JavaScript code.
         */
        public createElement(tag: string, attrs: any, children: any): HTMLElement {
            var element: HTMLElement = document.createElement(tag);
            for (const name in attrs) {
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
                const child: any = arguments[i];
                element.appendChild(!child.nodeType ? document.createTextNode(child.toString()) : child);
            }
            return element;
        }

        // Global script eval

        public runGlobalScript(script: string) : void {
            var elem = document.createElement("script");
            elem.text = script;

            var newElem = document.head!.appendChild(elem);// add to execute script
            (newElem.parentNode as HTMLElement).removeChild(newElem);// and remove - we're done with it
        }

        // WhenReady

        // Usage:
        // $YetaWF.addWhenReady((tag) => {});

        private whenReady: WhenReadyEntry[] = [];

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
            if (!tags)
                tags = [document.body ];
            for (const entry of this.whenReady) {
                try { // catch errors to insure all callbacks are called
                    for (const tag of tags)
                        entry.callback(tag);
                } catch (err) {
                    console.error(err.message || err);
                }
            }
        }

        // WhenReadyOnce

        // Usage:
        // $YetaWF.addWhenReadyOnce((tag) => {})    // function to be called
        private whenReadyOnce: WhenReadyEntry[] = [];

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
            let dummyEntry: HTMLDivElement|null = null;
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
            for (const entry of this.whenReadyOnce) {
                try { // catch errors to insure all callbacks are called
                    for (const tag of tags)
                        entry.callback(tag);
                } catch (err) {
                    console.error(err.message);
                }
            }
            this.whenReadyOnce = [];

            if (dummyEntry)
                dummyEntry.remove();
        }

        // ClearDiv

        private ClearDivHandlers: ClearDivEntry[] = [];

        /**
         * Registers a callback that is called when a <div> is cleared. This is used so templates can register a cleanup
         * callback so elements can be destroyed when a div is emptied (used by UPS).
         * @param autoRemove Set to true to remove the entry when the callback is called and returns true.
         * @param callback The callback to be called when a div is cleared. The callback returns true if the callback performed cleanup processing, false otherwise.
         */
        public registerClearDiv(autoRemove: boolean, callback: (section: HTMLElement) => boolean): void {
            this.ClearDivHandlers.push({ callback: callback, autoRemove: autoRemove });
        }

        /**
         * Process all callbacks for the specified element being cleared.
         * @param elem The element being cleared.
         */
        public processClearDiv(tag: HTMLElement): void {

            let newList: ClearDivEntry[] = [];
            for (const entry of this.ClearDivHandlers) {
                try { // catch errors to insure all callbacks are called
                    if (entry.callback != null) {
                        if (entry.callback(tag) && !entry.autoRemove)
                            newList.push(entry);
                    }
                } catch (err) {
                    console.error(err.message || err);
                }
            }
            // save new list without removed entries
            this.ClearDivHandlers = newList;

            // also release any attached objects
            for (var i = 0; i < this.DataObjectCache.length; ) {
                var doe = this.DataObjectCache[i];
                if (this.getElement1BySelectorCond(`#${doe.DivId}`, [tag])) {
                    console.log(`Element #${doe.DivId} is being removed but still has a data object - forced cleanup`);
                    if (YConfigs.Basics.DEBUGBUILD) {
                        // eslint-disable-next-line no-debugger
                        debugger; // if we hit this, there is an object that's not cleaned up by handling processClearDiv in a component specific way
                    }
                    this.DataObjectCache.splice(i, 1);
                    continue;
                }
                ++i;
            }
        }

        public validateObjectCache(): void {
            if (YConfigs.Basics.DEBUGBUILD) {
                //DEBUG ONLY
                for (let doe of this.DataObjectCache) {
                    if (!this.getElement1BySelectorCond(`#${doe.DivId}`)) {
                        console.log(`Element #${doe.DivId} no longer exists but still has a data object`);
                        // eslint-disable-next-line no-debugger
                        debugger; // if we hit this, there is an object that has no associated dom element
                    }
                }
            }
        }

        /**
         * Adds an object (a Typescript class) to a tag. Used for cleanup when a parent div is removed.
         * Typically used by templates.
         * Objects attached to divs are terminated by processClearDiv which calls any handlers that registered a
         * template class using addObjectDataById.
         * @param tagId - The element id (DOM) where the object is attached
         * @param obj - the object to attach
         */
        public addObjectDataById(tagId: string, obj: any): void {
            this.validateObjectCache();
            this.getElementById(tagId); // used to validate the existence of the element
            var doe = this.DataObjectCache.filter((entry:DataObjectEntry): boolean => entry.DivId === tagId);
            if (doe.length > 0) throw `addObjectDataById - tag with id ${tagId} already has data`;/*DEBUG*/
            this.DataObjectCache.push({ DivId: tagId, Data: obj });
        }
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param tagId - The element id (DOM) where the object is attached
         */
        public getObjectDataById(tagId: string): any {
            this.getElementById(tagId); // used to validate the existence of the element
            var doe = this.DataObjectCache.filter((entry: DataObjectEntry): boolean => entry.DivId === tagId);
            if (doe.length === 0)
                throw `getObjectDataById - tag with id ${tagId} doesn't have any data`;/*DEBUG*/
            return doe[0].Data;
        }
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param tagId - The element id (DOM) where the object is attached
         */
        public getObjectData(element: HTMLElement): any {
            if (!element.id)
                throw `element without id - ${element.outerHTML}`;
            return this.getObjectDataById(element.id);
        }
        /**
         * Removes a data object (a Typescript class) from a tag.
         * @param tagId - The element id (DOM) where the object is attached
         */
        public removeObjectDataById(tagId: string): void {
            this.validateObjectCache();
            this.getElementById(tagId); // used to validate the existence of the element
            for (var i = 0; i < this.DataObjectCache.length; ++i) {
                var doe = this.DataObjectCache[i];
                if (doe.DivId === tagId) {
                    this.DataObjectCache.splice(i, 1);
                    return;
                }
            }
            throw `Element with id ${tagId} doesn't have attached data`;/*DEBUG*/
        }

        private DataObjectCache: DataObjectEntry[] = [];

        // Selectors

        /**
         * Get an element by id.
         */
        public getElementById(elemId: string): HTMLElement {
            var div: HTMLElement = document.querySelector(`#${elemId}`) as HTMLElement;
            if (!div)
                throw `Element with id ${elemId} not found`;/*DEBUG*/
            return div;
        }
        /**
         * Get an element by id.
         */
        public getElementByIdCond(elemId: string): HTMLElement | null {
            var div: HTMLElement = document.querySelector(`#${elemId}`) as HTMLElement;
            return div;
        }
        /**
         * Get elements from an array of tags by selector. (similar to jquery var x = $(selector, elems); with standard css selectors)
         */
        public getElementsBySelector(selector: string, elems?: HTMLElement[]): HTMLElement[] {
            var all: HTMLElement[] = [];
            if (!elems) {
                if (!document.body)
                    return all;
                elems = [document.body];
            }
            if (!elems)
                return all;
            for (const elem of elems) {
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
            for (const elem of elems) {
                if (elem.matches(selector)) // oddly enough querySelectorAll doesn't return anything even though the element matches...
                    return elem;
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
            for (const elem of elems) {
                if (elem.tagName !== "INPUT" || elem.getAttribute("type") !== "hidden")
                    all.push(elem);
            }
            return all;
        }
        /**
         * Returns items that are visible. (similar to jquery var x = elems.filter(':visible'); )
         */
        public limitToVisibleOnly(elems: HTMLElement[]): HTMLElement[] {
            var all: HTMLElement[] = [];
            for (const elem of elems) {
                if (this.isVisible(elem))
                    all.push(elem);
            }
            return all;
        }
        /**
         * Returns whether the specified element is visible.
         */
        public isVisible(elem: HTMLElement): boolean {
            return !!(elem.offsetWidth || elem.offsetHeight || elem.getClientRects().length);
        }

        /**
         * Returns whether the specified element is a parent of the specified child element.
         */
        public elementHas(elem: HTMLElement, childElement: HTMLElement): boolean {
            var c : HTMLElement | null = childElement;
            for (; c ;) {
                if (elem === c)
                    return true;
                c = c.parentElement;
            }
            return false;
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
        public elementClosestCond(elem: HTMLElement, selector: string): HTMLElement | null {
            var e: HTMLElement | null = elem;
            while (e) {
                if (this.elementMatches(e, selector))
                    return e;
                else
                    e = e.parentElement;
            }
            return null;
        }
        /**
         * Finds the closest element up the DOM hierarchy that matches the selector (including the starting element)
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        public elementClosest(elem: HTMLElement, selector: string): HTMLElement {
            var e = this.elementClosestCond(elem, selector);
            if (!e)
                throw `Closest parent element with selector ${selector} not found`;
            return e;
        }

        // DOM manipulation

        /**
         * Removes the specified element.
         * @param elem - The element to remove.
         */
        public removeElement(elem: HTMLElement): void {
            if (!elem.parentElement) return;
            elem.parentElement.removeChild(elem);
        }

        /**
         * Append content to the specified element. The content is html and optional <script> tags. The scripts are executed after the content is added.
         */
        public appendMixedHTML(elem: HTMLElement, content: string, tableBody?: boolean): void {
            this.calcMixedHTMLRunScripts(content, undefined, (elems: HTMLCollection): void => {
                while (elems.length > 0)
                    elem.insertAdjacentElement("beforeend", elems[0]);
            }, tableBody);
        }
        /**
         * Insert content before the specified element. The content is html and optional <script> tags. The scripts are executed after the content is added.
         */
        public insertMixedHTML(elem: HTMLElement, content: string, tableBody?: boolean): void {
            this.calcMixedHTMLRunScripts(content, undefined, (elems: HTMLCollection): void => {
                while (elems.length > 0)
                    elem.insertAdjacentElement("beforebegin", elems[0]);
            }, tableBody);
        }

        /**
         * Set the specified element's outerHMTL to the content. The content is html and optional <script> tags. The scripts are executed after the content is added.
         */
        public setMixedOuterHTML(elem: HTMLElement, content: string, tableBody?: boolean): void {
            this.calcMixedHTMLRunScripts(content, (html: string): void => {
                elem.outerHTML = content;
            }, undefined, tableBody);
        }

        private calcMixedHTMLRunScripts(content: string, callbackHTML?: (html: string) => void, callbackChildren?: (elems: HTMLCollection) => void, tableBody?: boolean): void {

            // convert the string to DOM representation
            var temp = document.createElement("YetaWFTemp");
            if (tableBody) {
                temp.innerHTML = `<table><tbody>${content}</tbody></table>`;
                temp = $YetaWF.getElement1BySelector("tbody", [temp]);
            } else {
                temp.innerHTML = content;
            }
            // extract all <script> tags
            var scripts: HTMLScriptElement[] = this.getElementsBySelector("script", [temp]) as HTMLScriptElement[];
            for (var script of scripts) {
                this.removeElement(script); // remove the script element
            }

            // call callback so caller can update whatever needs to be updated
            if (callbackHTML)
                callbackHTML(temp.innerHTML);
            else if (callbackChildren)
                callbackChildren(temp.children);

            // now run/load all scripts we found in the HTML
            for (var script of scripts) {
                if (script.src) {
                    script.async = false;
                    script.defer = false;
                    var js = document.createElement("script");
                    js.type = "text/javascript";
                    js.async = false; // need to preserve execution order
                    js.defer = false;
                    js.src = script.src;
                    document.body.appendChild(js);
                } else if (!script.type || script.type === "application/javascript") {
                    this.runGlobalScript(script.innerHTML);
                } else {
                    //throw `Unknown script type ${script.type}`;
                }
            }
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
        /**
         * Tests whether the specified element has a css class that starts with the given prefix.
         * @param elem The element to test.
         * @param cssPrefix - The css class prefix being tested.
         * Returns the entire css class that matches the prefix, or null.
         */
        public elementHasClassPrefix(elem: Element | null, cssPrefix: string): string[] {
            let list: string[] = [];
            cssPrefix = cssPrefix.trim();
            if (!elem) return list;
            if (elem.classList) {
                // eslint-disable-next-line @typescript-eslint/prefer-for-of
                for (let i = 0; i < elem.classList.length; ++i) {
                    if (elem.classList[i].startsWith(cssPrefix))
                        list.push(elem.classList[i]);
                }
            } else {
                let cs = elem.className.split(" ");
                for (let c of cs) {
                    if (c.startsWith(cssPrefix))
                        list.push(c);
                }
            }
            return list;
        }
        /**
         * Add a space separated list of css classes to an element.
         */
        public elementAddClassList(elem: Element, classNames: string | null): void {
            if (!classNames) return;
            for (var s of classNames.split(" ")) {
                if (s.length > 0)
                    this.elementAddClass(elem, s);
            }
        }
        /**
         * Add an array of css classes to an element.
         */
        public elementAddClasses(elem: Element, classNames: string[]): void {
            for (let s of classNames) {
                if (s.length > 0)
                    this.elementAddClass(elem, s);
            }
        }
        /**
         * Add css class to an element.
         */
        public elementAddClass(elem: Element, className: string): void {
            if (elem.classList)
                elem.classList.add(className);
            else
                elem.className += " " + className;
        }
        /**
         * Remove a space separated list of css classes from an element.
         */
        public elementRemoveClassList(elem: Element, classNames: string | null): void {
            if (!classNames) return;
            for (var s of classNames.split(" ")) {
                if (s.length > 0)
                    this.elementRemoveClass(elem, s);
            }
        }
        /**
         * Remove an array of css classes from an element.
         */
        public elementRemoveClasses(elem: Element, classNames: string[]): void {
            for (let s of classNames) {
                if (s.length > 0)
                    this.elementRemoveClass(elem, s);
            }
        }
        /**
         * Remove a css class from an element.
         */
        public elementRemoveClass(elem: Element, className: string): void {
            if (elem.classList)
                elem.classList.remove(className);
            else
                elem.className = elem.className.replace(new RegExp("(^|\\b)" + className.split(" ").join("|") + "(\\b|$)", "gi"), " ");
        }
        /*
         * Add/remove a class to an element.
         */
        public elementToggleClass(elem: Element, className: string, set: boolean): void {
            if (set) {
                if (this.elementHasClass(elem, className))
                    return;
                this.elementAddClass(elem, className);
            } else {
                this.elementRemoveClass(elem, className);
            }
        }

        // Attributes

        /**
         * Returns an attribute value. Throws an error if the attribute doesn't exist.
         */
        public getAttribute(elem: HTMLElement, name: string): string {
            var val = elem.getAttribute(name);
            if (!val)
                throw `missing ${name} attribute`;
            return val;
        }
        /**
         * Returns an attribute value.
         */
        public getAttributeCond(elem: HTMLElement, name: string): string | null {
            return elem.getAttribute(name);
        }
        /**
         * Sets an attribute.
         */
        public setAttribute(elem: HTMLElement, name: string, value: string): void {
            elem.setAttribute(name, value);
        }
        /**
         * Enable element.
         */
        public elementEnable(elem: HTMLElement): void {
            YetaWF_BasicsImpl.elementEnableToggle(elem, true);
        }
        /**
         * Disable element.
         */
        public elementDisable(elem: HTMLElement): void {
            YetaWF_BasicsImpl.elementEnableToggle(elem, false);
        }
        /**
         * Enable or disable element.
         */
        public elementEnableToggle(elem: HTMLElement, enable: boolean): void {
            if (enable)
                this.elementEnable(elem);
            else
                this.elementDisable(elem);
        }
        /**
         * Given an element, returns the owner (typically a module) that owns the element.
         * The DOM hierarchy may not reflect this ownership, for example with popup menus which are appended to the <body> tag, but are owned by specific modules.
         */
        public getOwnerFromTag(tag: HTMLElement): HTMLElement | null {
            return YetaWF_BasicsImpl.getOwnerFromTag(tag);
        }

        // Events
        /**
         * Send a custom event on behalf of an element.
         * @param elem The element sending the event.
         * @param name The name of the event.
         */

        public sendCustomEvent(elem: HTMLElement | Document, name: string, details?: any): boolean {
            let event = new CustomEvent("CustomEvent", { "detail": details ?? {} });
            event.initEvent(name, true, true);
            elem.dispatchEvent(event);
            return !event.cancelBubble && !event.defaultPrevented;
        }

        public registerDocumentReady(callback: () => void): void {
            if ((document as any).attachEvent ? document.readyState === "complete" : document.readyState !== "loading") {
                callback();
            } else {
                document.addEventListener("DOMContentLoaded", callback);
            }
        }

        public registerEventHandlerBody<K extends keyof HTMLElementEventMap>(eventName: K, selector: string | null, callback: (ev: HTMLElementEventMap[K]) => boolean): void {
            if (!document.body) {
                $YetaWF.addWhenReadyOnce((tag: HTMLElement): void => {
                    this.registerEventHandler(document.body, eventName, selector, callback);
                });
            } else {
                this.registerEventHandler(document.body, eventName, selector, callback);
            }
        }
        public registerMultipleEventHandlersBody(eventNames: string[], selector: string | null, callback: (ev: Event) => boolean): void {
            if (!document.body) {
                $YetaWF.addWhenReadyOnce((tag: HTMLElement): void => {
                    for (let eventName of eventNames) {
                        document.body.addEventListener(eventName, (ev: Event): void => this.handleEvent(document.body, ev, selector, callback));
                    }
                });
            } else {
                for (let eventName of eventNames) {
                    document.body.addEventListener(eventName, (ev: Event): void => this.handleEvent(document.body, ev, selector, callback));
                }
            }
        }
        public registerEventHandlerDocument<K extends keyof DocumentEventMap>(eventName: K, selector: string | null, callback: (ev: DocumentEventMap[K]) => boolean): void {
            document.addEventListener(eventName, (ev: DocumentEventMap[K]):void => this.handleEvent(null, ev, selector, callback as (ev:Event)=>boolean));
        }
        public registerMultipleEventHandlersDocument(eventNames: string[], selector: string | null, callback: (ev: Event) => boolean): void {
            for (let eventName of eventNames) {
                document.addEventListener(eventName, (ev: Event): void => this.handleEvent(null, ev, selector, callback as (ev:Event)=>boolean));
            }
        }
        public registerEventHandlerWindow<K extends keyof WindowEventMap>(eventName: K, selector: string | null, callback: (ev: WindowEventMap[K]) => boolean): void {
            window.addEventListener(eventName, (ev: WindowEventMap[K]): void => this.handleEvent(null, ev, selector, callback as (ev:Event)=>boolean));
        }
        public registerEventHandler<K extends keyof HTMLElementEventMap>(tag: HTMLElement, eventName: K, selector: string | null, callback: (ev: HTMLElementEventMap[K]) => boolean): void {
            tag.addEventListener(eventName, (ev: HTMLElementEventMap[K]): void => this.handleEvent(tag, ev, selector, callback as (ev:Event)=>boolean));
        }
        public registerMultipleEventHandlers(tags: (HTMLElement|null)[], eventNames: string[], selector: string | null, callback: (ev: Event) => boolean): void {
            for (let tag of tags) {
                if (tag) {
                    for (let eventName of eventNames) {
                        tag.addEventListener(eventName, (ev: Event): void => this.handleEvent(tag, ev, selector, callback));
                    }
                }
            }
        }
        public registerCustomEventHandlerDocument(eventName: string, selector: string | null, callback: (ev: CustomEvent) => boolean): void {
            document.addEventListener(eventName, (ev: Event): void => this.handleEvent(document.body, ev as CustomEvent, selector, callback));
        }
        public registerCustomEventHandler(control: ComponentBaseNoDataImpl, eventName: string, selector: string | null, callback: (ev: CustomEvent) => boolean): void {
            control.Control.addEventListener(eventName, (ev: Event): void => this.handleEvent(control.Control, ev as CustomEvent, selector, callback));
        }
        public registerMultipleCustomEventHandlers(controls: (ComponentBaseNoDataImpl | null)[], eventNames: string[], selector: string | null, callback: (ev: CustomEvent) => boolean): void {
            for (let control of controls) {
                if (control) {
                    for (let eventName of eventNames) {
                        control.Control.addEventListener(eventName, (ev: Event): void => this.handleEvent(control!.Control, ev as CustomEvent, selector, callback));
                    }
                }
            }
        }
        private handleEvent<EVENTTYPE extends Event|CustomEvent>(listening: HTMLElement | null, ev: EVENTTYPE, selector: string | null, callback: (ev: EVENTTYPE) => boolean): void {
            // about event handling https://www.sitepoint.com/event-bubbling-javascript/
            //console.log(`event ${ev.type} selector ${selector} target ${(ev.target as HTMLElement).outerHTML}`);
            if (ev.cancelBubble || ev.defaultPrevented)
                return;
            var elem: HTMLElement | null = ev.target as HTMLElement | null;
            if (ev.eventPhase === ev.CAPTURING_PHASE) {
                if (selector) return;// if we have a selector we can't possibly have a match because the src element is the main tag where we registered the listener
            } else if (ev.eventPhase === ev.AT_TARGET) {
                if (selector) return;// if we have a selector we can't possibly have a match because the src element is the main tag where we registered the listener
            } else if (ev.eventPhase === ev.BUBBLING_PHASE) {
                if (selector) {
                    // check elements between the one that caused the event and the listening element (inclusive) for a match to the selector
                    while (elem) {
                        if (this.elementMatches(elem, selector))
                            break;
                        if (listening === elem)
                            return;// checked all elements
                        elem = elem.parentElement;
                    }
                } else {
                    // check whether the target or one of its parents is the listening element
                    while (elem) {
                        if (listening === elem)
                            break;
                        elem = elem.parentElement;
                    }
                }
                if (!elem)
                    return;
            } else
                return;
            //console.log(`event ${ev.type} selector ${selector} match`);
            ev.__YetaWFElem = (elem || ev.target) as HTMLElement;// pass the matching element to the callback
            var result: boolean = callback(ev);
            if (!result) {
                //console.log(`event ${ev.type} selector ${selector} stop bubble`);
                ev.stopPropagation();
                ev.preventDefault();
            }
        }
        public handleInputReturnKeyForButton(input: HTMLInputElement, button: HTMLInputElement): void {
            $YetaWF.registerEventHandler(input, "keydown", null, (ev: KeyboardEvent): boolean => {
                if (ev.keyCode === 13) {
                    button.click();
                    return false;
                }
                return true;
            });
        }

        // ADDONCHANGE
        // ADDONCHANGE
        // ADDONCHANGE

        public sendAddonChangedEvent(addonGuid: string, on: boolean): void {
            let details: DetailsAddonChanged = { addonGuid: addonGuid.toLowerCase(), on: on };
            this.sendCustomEvent(document.body, BasicsServices.EVENTADDONCHANGED, details);
        }

        // PANELSWITCHED
        // PANELSWITCHED
        // PANELSWITCHED

        public sendPanelSwitchedEvent(panel: HTMLElement): void {
            let details: DetailsPanelSwitched = { panel: panel };
            this.sendCustomEvent(document.body, BasicsServices.EVENTPANELSWITCHED, details);
        }

        // ACTIVATEDIV
        // ACTIVATEDIV
        // ACTIVATEDIV

        public sendActivateDivEvent(tags: HTMLElement[]): void {
            let details: DetailsActivateDiv = { tags: tags };
            this.sendCustomEvent(document.body, BasicsServices.EVENTACTIVATEDIV, details);
        }

        // CONTAINER SCROLLING
        // CONTAINER SCROLLING
        // CONTAINER SCROLLING

        public sendContainerScrollEvent(container?: HTMLElement): void {
            if (!container) container = document.body;
            let details: DetailsEventContainerScroll = { container: container };
            this.sendCustomEvent(document.body, BasicsServices.EVENTCONTAINERSCROLL, details);
        }

        // CONTAINER RESIZING
        // CONTAINER RESIZING
        // CONTAINER RESIZING

        public sendContainerResizeEvent(container?: HTMLElement): void {
            if (!container) container = document.body;
            let details: DetailsEventContainerScroll = { container: container };
            this.sendCustomEvent(document.body, BasicsServices.EVENTCONTAINERRESIZE, details);
        }

        // PAGECHANGE
        // PAGECHANGE
        // PAGECHANGE

        private PageChangeHandlers: PageChangeEntry[] = [];

        /**
         * Register a callback to be called when the current page is going away (about to be replaced by a new page).
         */
        public registerPageChange(onceOnly: boolean, callback: () => void): void {
            this.PageChangeHandlers.push({ callback: callback, onceOnly: onceOnly });
        }
        /**
         * Called to call all registered callbacks when the current page is going away (about to be replaced by a new page).
         */
        public processPageChange(): void {
            for (var entry of this.PageChangeHandlers) {
                entry.callback();
            }
            // remove once only entries
            this.PageChangeHandlers = this.PageChangeHandlers.filter((el: PageChangeEntry) : boolean => !el.onceOnly);
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

            this.registerEventHandler(expLink, "click", null, (ev: Event): boolean => {
                collapsedDiv.style.display = "none";
                expandedDiv.style.display = "";
                // init any controls that just became visible
                this.sendActivateDivEvent([expandedDiv]);
                return true;
            });
            this.registerEventHandler(collLink, "click", null, (ev: Event): boolean => {
                collapsedDiv.style.display = "";
                expandedDiv.style.display = "none";
                return true;
            });
        }

        // Rudimentary mobile detection

        public isMobile(): boolean {
            return (YVolatile.Skin.MinWidthForPopups > 0 && YVolatile.Skin.MinWidthForPopups > window.outerWidth) || (YVolatile.Skin.MinWidthForPopups === 0 && window.outerWidth <= 970);
        }

        // Positioning

        /**
         * Position an element (sub) below an element (main), or above if there is insufficient space below.
         * The elements are always aligned at their left edges.
         * @param main The main element.
         * @param sub The element to be position below/above the main element.
         */
        public positionLeftAlignedBelow(main: HTMLElement, sub: HTMLElement): void {

            // position within view to calculate size
            sub.style.top = "0px";
            sub.style.left = "0px";
            sub.style.right = "";
            sub.style.bottom = "";

            // position to fit
            let mainRect = main.getBoundingClientRect();
            let subRect = sub.getBoundingClientRect();
            let bottomAvailable = window.innerHeight - mainRect.bottom;
            let topAvailable = mainRect.top;

            // Top/bottom position and height calculation
            let top = 0, bottom = 0;
            if (bottomAvailable < subRect.height && topAvailable > bottomAvailable) {
                bottom = window.innerHeight - mainRect.top;
                top = mainRect.top - subRect.height;
                if (top <= 0)
                    sub.style.top = "0px";
                else
                    sub.style.top = "";
                sub.style.bottom = `${bottom - window.pageYOffset}px`;
            } else {
                top = mainRect.bottom;
                bottom = top + subRect.height;
                bottom = window.innerHeight - bottom;
                if (bottom < 0)
                    sub.style.bottom = "0px";
                sub.style.top = `${top + window.pageYOffset}px`;
            }
            // set left
            sub.style.left = `${mainRect.left + window.pageXOffset}px`;
            if (mainRect.left + subRect.right > window.innerWidth)
                sub.style.right = "0px";
        }

        constructor() {

            $YetaWF = this;// set global so we can initialize anchor/content
            this.AnchorHandling = new YetaWF.Anchors();
            this.ContentHandling = new YetaWF.Content();

        }

        public init(): void {

            this.AnchorHandling.init();
            this.ContentHandling.init();

            // screen size yCondense/yNoCondense support

            $YetaWF.registerCustomEventHandlerDocument(YetaWF.BasicsServices.EVENTCONTAINERRESIZE, null, (ev: CustomEvent<YetaWF.DetailsEventContainerResize>): boolean => {
                this.setCondense(document.body, window.innerWidth);
                return true;
            });

            this.registerDocumentReady((): void => {
                this.setCondense(document.body, window.innerWidth);
            });

            // Navigation

            this.registerEventHandlerWindow("popstate", null, (ev: PopStateEvent): boolean => {
                if (this.suppressPopState > 0) {
                    --this.suppressPopState;
                    return true;
                }
                var uri = this.parseUrl(window.location.href);
                return this.ContentHandling.setContent(uri, false) !== SetContentResult.NotContent;
            });

            // <A> links

            // <a> links that only have a hash are intercepted so we don't go through content handling
            this.registerEventHandlerBody("click", "a[href^='#']", (ev: MouseEvent): boolean => {

                // find the real anchor, ev.target was clicked, but it may not be the anchor itself
                if (!ev.target) return true;
                var anchor = $YetaWF.elementClosestCond(ev.target as HTMLElement, "a") as HTMLAnchorElement;
                if (!anchor) return true;

                ++this.suppressPopState;
                setTimeout((): void => {
                    if (this.suppressPopState > 0)
                        --this.suppressPopState;
                }, 200);
                return true;
            });

            // Scrolling

            window.addEventListener("scroll", (ev: Event): void => {
                $YetaWF.sendContainerScrollEvent();
            });

            // Debounce resizing

            let resizeTimeout;
            window.addEventListener("resize", (ev: UIEvent): void => {
                if (resizeTimeout) {
                    clearTimeout(resizeTimeout);
                }
                resizeTimeout = setTimeout((): void => {
                    $YetaWF.sendContainerResizeEvent();
                    resizeTimeout = null;
                }, 100);
            });

            // WhenReady

            this.registerDocumentReady((): void => {
                this.processAllReady();
                this.processAllReadyOnce();
            });

            setTimeout((): void => {
                $YetaWF.sendCustomEvent(document.body, Content.EVENTNAVPAGELOADED);
            }, 1);
        }

        /* Print support */
        public get isPrinting(): boolean {
            return BasicsServices.printing;
        }

        private static printing: boolean = false;

        public DoPrint(): void {
            YetaWF.BasicsServices.onBeforePrint(); // window.print doesn't generate onBeforePrint
            window.print();
        }

        public static onBeforePrint(): void {
            BasicsServices.printing = true;
            $YetaWF.sendCustomEvent(window.document, BasicsServices.EVENTBEFOREPRINT);
        }
        public static onAfterPrint(): void {
            BasicsServices.printing = false;
            $YetaWF.sendCustomEvent(window.document, BasicsServices.EVENTAFTERPRINT);
        }

        // Page modification support (used with onbeforeunload)
        public get pageChanged(): boolean {
            return this._pageChanged;
        }
        public set pageChanged(value: boolean) {
            if (this._pageChanged !== value) {
                this._pageChanged = value;
                this.sendCustomEvent(document.body, BasicsServices.PAGECHANGEDEVENT);
            }
        }
        private _pageChanged: boolean = false;
    }
}

/**
 * Basic services available throughout YetaWF.
 */
var $YetaWF = new YetaWF.BasicsServices();

/* Print support */

if (window.matchMedia) {
    let mediaQueryList = window.matchMedia("print");
    mediaQueryList.addListener(function (this: MediaQueryList, ev: MediaQueryListEvent): void {
        // eslint-disable-next-line no-invalid-this
        if (this.matches) {
            YetaWF.BasicsServices.onBeforePrint();
        } else {
            YetaWF.BasicsServices.onAfterPrint();
        }
    });
}

window.onbeforeprint = (ev: Event): void => { YetaWF.BasicsServices.onBeforePrint(); };
window.onafterprint = (ev: Event): void => { YetaWF.BasicsServices.onAfterPrint(); };

if (YConfigs.Basics.DEBUGBUILD) {
    let inDebug = false;
    // not really a debugging tool - Any failures result in a popup so at least it's visible without explicitly looking at the console log
    window.onerror = (ev: Event | string, url?: string, lineNo?: number, columnNo?: number, error?: Error): void => {
        if (!inDebug) {
            inDebug = true;

            let evMsg = ev.toString();
            // avoid recursive error with video controls. a bit hacky but this is just a debugging tool.
            if (evMsg.startsWith("ResizeObserver")) return;

            $YetaWF.error(`${evMsg} (${url}:${lineNo}) ${error?.stack}`);
            inDebug = false;
        }
    };
}

window.onbeforeunload = (ev: BeforeUnloadEvent): any => {
    if ($YetaWF.pageChanged) {
        ev.returnValue = "Are you sure you want to leave this page? There are unsaved changes."; // Chrome requires returnValue to be set
        $YetaWF.setLoading(false);// turn off loading indicator in case it's set
        ev.preventDefault(); // If you prevent default behavior in Mozilla Firefox prompt will always be shown
    }
};

