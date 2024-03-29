/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
    export interface DetailsEventContentResized {
        tag: HTMLElement;
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

        /** Called when a new full page has been loaded and needs to be initialized */
        initFullPage(): void;

        /** Returns whether the loading indicator is on or off */
        isLoading: boolean;

        /**
         * Turns a loading indicator on/off.
         * @param on
         */
        setLoading(on?: boolean): void;

        /**
         * Displays an informational message.
         */
        message(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an warning message.
         */
        warning(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an error message.
         */
        error(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays a confirmation message.
         */
        confirm(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void;
        /**
         * Displays an alert message.
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
         * Returns whether the element is enabled.
         */
        isEnabled(elem: HTMLElement): boolean;
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
        public static readonly EVENTCONTENTRESIZED: string = "content_resized";
        public static readonly EVENTACTIVATEDIV: string = "activate_div";
        public static readonly EVENTPANELSWITCHED: string = "panel_switched";
        public static readonly EVENTADDONCHANGED: string = "addon_changed";

        // Implemented by renderer
        // Implemented by renderer
        // Implemented by renderer

        /** Called when a new full page has been loaded and needs to be initialized */
        public initFullPage(): void {
            YetaWF_BasicsImpl.initFullPage();
        }

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
         * Displays an informational message.
         */
        public message(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.message(message, title, onOK, options); }
        /**
         * Displays an error message.
         */
        public warning(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.warning(message, title, onOK, options); }
        /**
         * Displays an error message.
         */
        public error(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.error(message, title, onOK, options); }
        /**
         * Displays a confirmation message.
         */
        public confirm(message: string, title?: string, onOK?: () => void, options?: MessageOptions): void { YetaWF_BasicsImpl.confirm(message, title, onOK, options); }
        /**
         * Displays an alert message with Yes/No buttons.
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

        public ContentHandling!: YetaWF.Content;

        // Anchor handling

        public AnchorHandling!: YetaWF.Anchors;

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
            let uri = new YetaWF.Url();
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
            let f: HTMLElement | null = null;
            let items = this.getElementsBySelector(".yFocusOnMe", tags);
            items = this.limitToVisibleOnly(items); //:visible
            for (let item of items) {
                if (item.tagName === "DIV") { // if we found a div, find the edit element instead
                    let i = this.getElementsBySelector("input,select,textarea,.yt_dropdownlist_base", [item]);
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
            let uri = this.parseUrl(window.location.href);
            let left = uri.getSearch(YConfigs.Basics.Link_ScrollLeft);
            let top = uri.getSearch(YConfigs.Basics.Link_ScrollTop);
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

            this.initFullPage();
            this.init();

            // page position

            let scrolled = this.setScrollPosition();

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

            let div = this.getElementById(id);
            let shown = false;
            if (editMode) {
                div.style.display = "block";
                shown = true;
            } else {
                // show the pane if it has modules
                let mod = this.getElement1BySelectorCond("div.yModule", [div]);
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
                    let panes = this.getElementsBySelector(`#${id} > div`);// get all immediate child divs (i.e., the panes)
                    panes = this.limitToVisibleOnly(panes); //:visible
                    // exclude panes that have .y_cleardiv
                    let newPanes: HTMLElement[] = [];
                    for (let pane of panes) {
                        if (!this.elementHasClass(pane, "y_cleardiv"))
                            newPanes.push(pane);
                    }
                    panes = newPanes;

                    let height = 0;
                    // calc height
                    for (let pane of panes) {
                        let h = pane.clientHeight;
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
                let stateObj = {};
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

            let uri = this.parseUrl(w.location.href);
            uri.removeSearch(YConfigs.Basics.Link_ScrollLeft);
            uri.removeSearch(YConfigs.Basics.Link_ScrollTop);
            if (keepPosition) {
                let left = (document.documentElement && document.documentElement.scrollLeft) || document.body.scrollLeft;
                if (left)
                    uri.addSearch(YConfigs.Basics.Link_ScrollLeft, left.toString());
                let top = (document.documentElement && document.documentElement.scrollTop) || document.body.scrollTop;
                if (top)
                    uri.addSearch(YConfigs.Basics.Link_ScrollTop, top.toString());
            }
            uri.removeSearch("!rand");
            uri.addSearch("!rand", ((new Date()).getTime()).toString());// cache buster

            if (this.ContentHandling.setContent(uri, true) !== SetContentResult.NotContent)
                return;

            if (keepPosition) {
                w.location.assign(uri.toUrl());
                return;
            }
            w.location.reload();
        }

        /**
         * Reloads a module in place, defined by the specified tag (any tag within the module).
         */
        public reloadModule(tag?: HTMLElement): void {
            if (!tag) {
                if (!this.reloadingModuleTagInModule) throw "No module found";/*DEBUG*/
                tag = this.reloadingModuleTagInModule;
            }
            let mod = ModuleBase.getModuleDivFromTag(tag);
            let form = this.getElement1BySelector("form", [mod]) as HTMLFormElement;
            this.Forms.submit(form, false, YConfigs.Basics.Link_SubmitIsApply + "=y");// the form must support a simple Apply
        }

        private reloadingModuleTagInModule: HTMLElement | null = null;

        public refreshModule(mod: HTMLElement): void {
            if (!this.getElementByIdCond(mod.id)) throw `Module with id ${mod.id} not found`;/*DEBUG*/
            this.processReloadInfo(mod.id);
        }
        public refreshModuleByAnyTag(elem: HTMLElement): void {
            let mod = ModuleBase.getModuleDivFromTag(elem);
            this.processReloadInfo(mod.id);
        }
        private processReloadInfo(moduleId: string): void {
            let len = this.reloadInfo.length;
            for (let i = 0; i < len; ++i) {
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
            let len = this.reloadInfo.length;
            for (let i = 0; i < len; ++i) {
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
            let module = ModuleBase.getModuleDivFromTag(tag); // get the containing module
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
            let mod = YetaWF.ModuleBase.getModuleDivFromTag(tag);
            let guid = mod.getAttribute("data-moduleguid");
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

        /** Send a GET/POST/... request to the specified URL, expecting a JSON response. Errors are automatically handled. The callback is called once the POST response is available.
         * @param url The URL used for the POST request.
         * @param data The data to send as form data with the POST request.
         * @param callback The callback to call when the POST response is available. Errors are automatically handled.
         * @param tagInModule The optional tag in a module to refresh when AjaxJavascriptReloadModuleParts is returned.
         */
        public send(method: string, url: string, data: any, callback: (success: boolean, data: any) => void, tagInModule?: HTMLElement): void {
            this.setLoading(true);
            let request: XMLHttpRequest = new XMLHttpRequest();
            request.open(method, url, true);
            if (method.toLowerCase() === "post")
                request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            $YetaWF.handleReadyStateChange(request, callback, tagInModule);
            request.send(data);
        }

        /** POST form data to the specified URL, expecting a JSON response. Errors are automatically handled. The callback is called once the POST response is available.
         * @param url The URL used for the POST request.
         * @param data The data to send as form data with the POST request.
         * @param callback The callback to call when the POST response is available. Errors are automatically handled.
         * @param tagInModule The optional tag in a module to refresh when AjaxJavascriptReloadModuleParts is returned.
         */
        public post(url: string, data: any, callback: (success: boolean, data: any) => void, tagInModule?: HTMLElement): void {
            this.setLoading(true);
            let request: XMLHttpRequest = new XMLHttpRequest();
            request.open("POST", url, true);
            request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            $YetaWF.handleReadyStateChange(request, callback, tagInModule);
            request.send(data);
        }

        /** POST JSON data to the specified URL, expecting a JSON response. Errors are automatically handled. The callback is called once the POST response is available.
         * @param url The URL used for the POST request.
         * @param data The data to send as form data with the POST request.
         * @param callback The callback to call when the POST response is available. Errors are automatically handled.
         */
        public postJSON(url: string, data: any, callback: (success: boolean, data: any) => void): void {
            this.setLoading(true);
            let request: XMLHttpRequest = new XMLHttpRequest();
            request.open("POST", url, true);
            request.setRequestHeader("Content-Type", "application/json");
            $YetaWF.handleReadyStateChange(request, callback);
            request.send(JSON.stringify(data));
        }

        public handleReadyStateChange(request: XMLHttpRequest, callback: (success: boolean, data: any) => void, tagInModule?: HTMLElement): void {
            request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            request.onreadystatechange = (ev: Event): any => {
                if (request.readyState === XMLHttpRequest.DONE) {
                    this.setLoading(false);
                    if (request.status === 200) {
                        let result: any = null;
                        if (request.responseText && !request.responseText.startsWith("<"))
                            result = JSON.parse(request.responseText);
                        else
                            result = request.responseText;
                        if (typeof result === "string") {
                            if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                                let script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                                if (script.length > 0) {
                                    // eslint-disable-next-line no-eval
                                    eval(script);
                                }
                                callback(true, null);
                                return;
                            } else if (result.startsWith(YConfigs.Basics.AjaxJSONReturn)) {
                                let json = result.substring(YConfigs.Basics.AjaxJSONReturn.length);
                                callback(true, JSON.parse(json));
                                return;
                            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                                let script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                                // eslint-disable-next-line no-eval
                                eval(script);
                                callback(false, null);
                                return;
                            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadPage)) {
                                let script = result.substring(YConfigs.Basics.AjaxJavascriptReloadPage.length);
                                // eslint-disable-next-line no-eval
                                eval(script);// if this uses $YetaWF.message or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explicitly reload page in your javascript)
                                this.reloadPage(true);
                                callback(true, null);
                                return;
                            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
                                let script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
                                // eslint-disable-next-line no-eval
                                eval(script);// if this uses $YetaWF.message or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explicitly reload module in your javascript)
                                this.reloadModule();
                                callback(true, null);
                                return;
                            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
                                let script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
                                // eslint-disable-next-line no-eval
                                eval(script);
                                if (tagInModule)
                                    this.refreshModuleByAnyTag(tagInModule);
                                return true;
                            } else {
                                callback(true, result);
                                return;
                            }
                        }
                        callback(true, result);
                    } else if (request.status >= 400 && request.status <= 499) {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, YLocs.Forms.AjaxNotAuth), YLocs.Forms.AjaxErrorTitle);
                        callback(false, null);
                    } else if (request.status === 0) {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, YLocs.Forms.AjaxConnLost), YLocs.Forms.AjaxErrorTitle);
                        callback(false, null);
                    } else {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, request.responseText), YLocs.Forms.AjaxErrorTitle);
                        callback(false, null);
                    }
                }
            };
        }

        // JSX

        /**
         * React-like createElement function so we can use JSX in our TypeScript/JavaScript code.
         */
        public createElement(tag: string, attrs: any, children: any): HTMLElement {
            let element: HTMLElement = document.createElement(tag);
            for (const name in attrs) {
                if (name && attrs.hasOwnProperty(name)) {
                    let value: string | null | boolean = attrs[name];
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
            let elem = document.createElement("script");
            elem.text = script;

            let newElem = document.head!.appendChild(elem);// add to execute script
            (newElem.parentNode as HTMLElement).removeChild(newElem);// and remove - we're done with it
        }

        // WhenReadyOnce

        // Usage:
        // $YetaWF.addWhenReadyOnce((tag) => {})    // function to be called
        private whenReadyOnce: WhenReadyEntry[] = [];

        /**
         * Registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
         * or after a partial form is rendered. The callee must honor tag/elem and only manipulate child objects.
         * THIS IS FOR INTERNAL USE ONLY and is not intended for application use.
         * The callback is called ONCE. Then the callback is removed.
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
                } catch (err: any) {
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
                if (entry.callback != null) {
                    try { // catch errors to insure all callbacks are called
                        entry.callback(tag);
                    } catch (err: any) {
                        const msg = err.message || err;
                        console.error(msg);
                        if (YConfigs.Basics.DEBUGBUILD) {
                            $YetaWF.error(msg);
                        }
                    }
                }
                if (!entry.autoRemove)
                    newList.push(entry);
            }
            // save new list without removed entries
            this.ClearDivHandlers = newList;

            // also release any attached objects
            for (let i = 0; i < this.DataObjectCache.length; ) {
                let doe = this.DataObjectCache[i];
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
            let doe = this.DataObjectCache.filter((entry:DataObjectEntry): boolean => entry.DivId === tagId);
            if (doe.length > 0) throw `addObjectDataById - tag with id ${tagId} already has data`;/*DEBUG*/
            this.DataObjectCache.push({ DivId: tagId, Data: obj });
        }
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param tagId - The element id (DOM) where the object is attached
         */
        public getObjectDataByIdCond(tagId: string): any {
            let doe = this.DataObjectCache.filter((entry: DataObjectEntry): boolean => entry.DivId === tagId);
            if (doe.length === 0)
                return null;
            return doe[0].Data;
        }
        /**
         * Retrieves a data object (a Typescript class) from a tag
         * @param tagId - The element id (DOM) where the object is attached
         */
        public getObjectDataById(tagId: string): any {
            let data = this.getObjectDataByIdCond(tagId);
            if (!data)
                throw `getObjectDataById - tag with id ${tagId} doesn't have any data`;/*DEBUG*/
            return data;
        }
        /**
         * Retrieves a data object (a Typescript class) from a tag. The data object may not be available.
         * @param tagId - The element id (DOM) where the object is attached
         */
        public getObjectDataCond(element: HTMLElement): any {
            if (!element.id)
                throw `element without id - ${element.outerHTML}`;
            return this.getObjectDataByIdCond(element.id);
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
            for (let i = 0; i < this.DataObjectCache.length; ++i) {
                let doe = this.DataObjectCache[i];
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
            let div: HTMLElement = document.querySelector(`#${elemId}`) as HTMLElement;
            if (!div)
                throw `Element with id ${elemId} not found`;/*DEBUG*/
            return div;
        }
        /**
         * Get an element by id.
         */
        public getElementByIdCond(elemId: string): HTMLElement | null {
            let div: HTMLElement = document.querySelector(`#${elemId}`) as HTMLElement;
            return div;
        }
        /**
         * Get elements from an array of tags by selector. (similar to jquery let x = $(selector, elems); with standard css selectors)
         */
        public getElementsBySelector(selector: string, elems?: HTMLElement[]): HTMLElement[] {
            let all: HTMLElement[] = [];
            if (!elems) {
                if (!document.body)
                    return all;
                elems = [document.body];
            }
            if (!elems)
                return all;
            for (const elem of elems) {
                let list: NodeListOf<Element> = elem.querySelectorAll(selector);
                all = all.concat(Array.prototype.slice.call(list));
                if (elem.matches(selector)) // oddly enough querySelectorAll doesn't return anything even though the element itself matches...
                    all.push(elem);
            }
            return all;
        }
        /**
         * Get the first element from an array of tags by selector. (similar to jquery let x = $(selector, elems); with standard css selectors)
         */
        public getElement1BySelectorCond(selector: string, elems?: HTMLElement[]): HTMLElement | null {
            if (!elems)
                elems = [document.body];
            for (const elem of elems) {
                if (elem.matches(selector)) // oddly enough querySelectorAll doesn't return anything even though the element matches...
                    return elem;
                let list: NodeListOf<Element> = elem.querySelectorAll(selector);
                if (list.length > 0)
                    return list[0] as HTMLElement;
            }
            return null;
        }
        /**
         * Get the first element from an array of tags by selector. (similar to jquery let x = $(selector, elems); with standard css selectors)
         */
        public getElement1BySelector(selector: string, elems?: HTMLElement[]): HTMLElement {
            let elem = this.getElement1BySelectorCond(selector, elems);
            if (elem == null)
                throw `Element with selector ${selector} not found`;
            return elem;
        }
        /**
         * Removes all input[type='hidden'] fields. (similar to jquery let x = elems.not("input[type='hidden']"); )
         */
        public limitToNotTypeHidden(elems: HTMLElement[]): HTMLElement[] {
            let all: HTMLElement[] = [];
            for (const elem of elems) {
                if (elem.tagName !== "INPUT" || elem.getAttribute("type") !== "hidden")
                    all.push(elem);
            }
            return all;
        }
        /**
         * Returns items that are visible. (similar to jquery let x = elems.filter(':visible'); )
         */
        public limitToVisibleOnly(elems: HTMLElement[]): HTMLElement[] {
            let all: HTMLElement[] = [];
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
            let c : HTMLElement | null = childElement;
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
            if (elem && elem.matches)
                return elem.matches(selector);
            return false;
        }
        /**
         * Finds the closest element up the DOM hierarchy that matches the selector (including the starting element)
         * @param elem - The element to test.
         * @param selector - The selector to match.
         */
        public elementClosestCond(elem: HTMLElement, selector: string): HTMLElement | null {
            let e: HTMLElement | null = elem;
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
            let e = this.elementClosestCond(elem, selector);
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
            let temp = document.createElement("YetaWFTemp");
            if (tableBody) {
                temp.innerHTML = `<table><tbody>${content}</tbody></table>`;
                temp = $YetaWF.getElement1BySelector("tbody", [temp]);
            } else {
                temp.innerHTML = content;
            }
            // extract all <script> tags
            let scripts: HTMLScriptElement[] = this.getElementsBySelector("script", [temp]) as HTMLScriptElement[];
            for (let script of scripts) {
                this.removeElement(script); // remove the script element
            }

            // call callback so caller can update whatever needs to be updated
            if (callbackHTML)
                callbackHTML(temp.innerHTML);
            else if (callbackChildren)
                callbackChildren(temp.children);

            // now run/load all scripts we found in the HTML
            for (let script of scripts) {
                if (script.src) {
                    script.async = false;
                    script.defer = false;
                    let js = document.createElement("script");
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
            if (css.startsWith(".")) throw `elementHasClass called with class starting with a . "${css}" - that probably wasn't intended`;
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
            if (cssPrefix.startsWith(".")) throw `elementHasClassPrefix called with cssPrefix starting with a . "${cssPrefix}" - that probably wasn't intended`;
            if (elem.classList) {
                // eslint-disable-next-line @typescript-eslint/prefer-for-of
                for (let i = 0; i < elem.classList.length; ++i) {
                    if (elem.classList[i].startsWith(cssPrefix))
                        list.push(elem.classList[i]);
                }
            } else if (elem.className && typeof elem.className === "string") {
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
            for (let s of classNames.split(" ")) {
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
            if (className.startsWith(".")) throw `elementAddClass called with class starting with a . "${className}" - that probably wasn't intended`;
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
            for (let s of classNames.split(" ")) {
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
            if (className.startsWith(".")) throw `elementRemoveClass called with class starting with a . "${className}" - that probably wasn't intended`;
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
            let val = elem.getAttribute(name);
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
         * Returns whether the element is enabled.
         */
        public isEnabled(elem: HTMLElement): boolean {
            return YetaWF_BasicsImpl.isEnabled(elem);
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
        public registerEventHandlerDocument<K extends keyof DocumentEventMap>(eventName: K, selector: string /* null not supported by handleEvent() */, callback: (ev: DocumentEventMap[K]) => boolean): void {
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
        public registerCustomEventHandler(tag: HTMLElement, eventName: string, selector: string | null, callback: (ev: CustomEvent) => boolean): void {
            tag.addEventListener(eventName, (ev: Event): void => this.handleEvent(tag, ev as CustomEvent, selector, callback));
        }
        public registerMultipleCustomEventHandlers(tags: (HTMLElement | null)[], eventNames: string[], selector: string | null, callback: (ev: CustomEvent) => boolean): void {
            for (let tag of tags) {
                if (tag) {
                    for (let eventName of eventNames) {
                        tag.addEventListener(eventName, (ev: Event): void => this.handleEvent(tag, ev as CustomEvent, selector, callback));
                    }
                }
            }
        }
        private handleEvent<EVENTTYPE extends Event|CustomEvent>(listening: HTMLElement | null, ev: EVENTTYPE, selector: string | null, callback: (ev: EVENTTYPE) => boolean): void {
            // about event handling https://www.sitepoint.com/event-bubbling-javascript/
            //console.log(`event ${ev.type} selector ${selector} target ${(ev.target as HTMLElement).outerHTML}`);
            if (ev.cancelBubble || ev.defaultPrevented)
                return;
            let elem: HTMLElement | null = ev.target as HTMLElement | null;
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
                        elem = elem.parentElement|| (elem.parentNode as HTMLElement);
                    }
                } else {
                    // check whether the target or one of its parents is the listening element
                    while (elem) {
                        if (listening === elem)
                            break;
                        elem = elem.parentElement || (elem.parentNode as HTMLElement);
                    }
                }
                if (!elem)
                    return;
            } else
                return;
            //console.log(`event ${ev.type} selector ${selector} match`);
            ev.__YetaWFElem = (elem || ev.target) as HTMLElement;// pass the matching element to the callback
            let result: boolean = callback(ev);
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
            let details: DetailsEventContainerResize = { container: container };
            this.sendCustomEvent(document.body, BasicsServices.EVENTCONTAINERRESIZE, details);
        }

        // CONTENT RESIZED
        // CONTENT RESIZED
        // CONTENT RESIZED

        public sendContentResizedEvent(tag: HTMLElement): void {
            let details: DetailsEventContentResized = { tag: tag };
            this.sendCustomEvent(document.body, BasicsServices.EVENTCONTENTRESIZED, details);
        }

        // Expand/collapse Support

        /**
         * Expand/collapse support using 2 action links (Name=Expand/Collapse) which make 2 divs hidden/visible  (alternating)
         * @param divId The <div> containing the 2 action links.
         * @param collapsedId - The <div> to hide/show.
         * @param expandedId - The <div> to show/hide.
         */
        public expandCollapseHandling(divId: string, collapsedId: string, expandedId: string): void {
            let div = this.getElementById(divId);
            let collapsedDiv = this.getElementById(collapsedId);
            let expandedDiv = this.getElementById(expandedId);

            let expLink = this.getElement1BySelector("a[data-name='Expand']", [div]);
            let collLink = this.getElement1BySelector("a[data-name='Collapse']", [div]);

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
            this.positionAlignedBelow(main, sub, true);
        }

        /**
         * Position an element (sub) below an element (main), or above if there is insufficient space below.
         * The elements are always aligned at their right edges.
         * @param main The main element.
         * @param sub The element to be position below/above the main element.
         */
        public positionRightAlignedBelow(main: HTMLElement, sub: HTMLElement): void {
            this.positionAlignedBelow(main, sub, false);
        }

        /**
         * Position an element (sub) below an element (main), or above if there is insufficient space below.
         * The elements are always aligned at their left or right edges.
         * @param main The main element.
         * @param sub The element to be position below/above the main element.
         * @param left Defines whether the sub element is positioned to the left (true) or right (false).
         */
        private positionAlignedBelow(main: HTMLElement, sub: HTMLElement, left: boolean): void {

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
            if (left) {
                // set left
                sub.style.left = `${mainRect.left + window.pageXOffset}px`;
                if (mainRect.left + subRect.right > window.innerWidth)
                    sub.style.right = "0px";
            } else {
                // set right
                let left = mainRect.right - subRect.width + window.pageXOffset;
                if (left < 0) left = 0;
                sub.style.left = `${left}px`;
            }
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
                let uri = this.parseUrl(window.location.href);
                return this.ContentHandling.setContent(uri, false) !== SetContentResult.NotContent;
            });

            // <A> links

            // <a> links that only have a hash are intercepted so we don't go through content handling
            this.registerEventHandlerBody("click", "a[href^='#']", (ev: MouseEvent): boolean => {

                // find the real anchor, ev.target was clicked, but it may not be the anchor itself
                if (!ev.target) return true;
                let anchor = $YetaWF.elementClosestCond(ev.target as HTMLElement, "a") as HTMLAnchorElement;
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

            let resizeTimeout: number = 0;
            window.addEventListener("resize", (ev: UIEvent): void => {
                if (resizeTimeout) {
                    clearTimeout(resizeTimeout);
                }
                resizeTimeout = setTimeout((): void => {
                    $YetaWF.sendContainerResizeEvent();
                    resizeTimeout = 0;
                }, 100);
            });

            // WhenReady

            this.registerDocumentReady((): void => {
                this.processAllReadyOnce();
            });

            setTimeout((): void => {
                $YetaWF.sendCustomEvent(document.body, Content.EVENTNAVPAGELOADED, { containers: [document.body] });
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
$YetaWF.AnchorHandling = new YetaWF.Anchors();
$YetaWF.ContentHandling = new YetaWF.Content();

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

window.onbeforeunload = (ev: BeforeUnloadEvent): any => {
    if ($YetaWF.pageChanged) {
        ev.returnValue = "Are you sure you want to leave this page? There are unsaved changes."; // Chrome requires returnValue to be set
        $YetaWF.setLoading(false);// turn off loading indicator in case it's set
        ev.preventDefault(); // If you prevent default behavior in Mozilla Firefox prompt will always be shown
    }
};

