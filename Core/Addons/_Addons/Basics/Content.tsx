/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// Anchor handling, navigation

namespace YetaWF {

    interface ContentData {
        CacheVersion: string;
        CacheFailUrl: string | null;
        Path: string;
        QueryString: string;
        UnifiedAddonMods: string[];
        UniqueIdCounters: UniqueIdInfo;
        IsMobile: boolean;
        Panes: string[];
        KnownCss: string[];
        KnownScripts: string[];
    }
    interface AddonsContentData {
        Addons: AddonDescription[];
        KnownCss: string[];
        KnownScripts: string[];
    }

    export interface InplaceContents {
        TargetTag: string; // The target tag to be replaced by the content
        FromPane: string; // The pane (within ContentUrl) where the content is located
        ContentUrl: string; // the URL where the content is located
        PageUrl: string;// The URL used by the browser state
    }

    export interface ContentResult {
        Status: string;
        Redirect: string;
        RedirectContent: string;
        Content: PaneContent[];
        Addons: string;
        PageTitle: string;
        PageCssClasses: string;
        CanonicalUrl: string;
        LocalUrl: string;
        Scripts: string;
        EndOfPageScripts: string;
        ScriptFiles: UrlEntry[];
        ScriptFilesPayload: Payload[];
        CssFiles: UrlEntry[];
        CssFilesPayload: Payload[];
        ScriptBundleFiles: string[];
        CssBundleFiles: string[];
        AnalyticsContent: string;
    }
    export interface PaneContent {
        Pane: string;
        HTML: string;
    }
    export interface Payload {
        Name: string;
        Text: string;
    }
    export interface UrlEntry {
        Name: string;
        Url: string;
        Attributes: object;
    }
    export interface AddonDescription {
        AreaName: string;
        ShortName: string;
        Argument1: string|null;
    }

    export enum SetContentResult {
        NotContent = 0,
        ContentReplaced = 1,
        Abort = 2,
    }

    export interface DetailsEventNavPageLoaded {
        containers: HTMLElement[];
    }

    export class Content {

        public static readonly EVENTNAVCANCEL: string = "content_navcancel";
        public static readonly EVENTNAVPAGELOADED: string = "content_navpageloaded";

        // loads all scripts - we need to preserve the order of initialization hence the recursion
        private loadScripts(scripts: UrlEntry[], payload: Payload[], run: () => void): void {

            YVolatile.Basics.KnownScriptsDynamic = YVolatile.Basics.KnownScriptsDynamic || [];
            let total = scripts.length;
            if (total === 0) {
                run();
                return;
            }
            this.loadNextScript(scripts, payload, total, 0, run);
        }

        private loadNextScript(scripts: UrlEntry[], payload: Payload[], total: number, ix: number, run: () => void) : void {

            let urlEntry = scripts[ix];
            let name = urlEntry.Name;

            let found = payload.filter((elem: Payload): boolean => { return elem.Name === name; });
            if (found.length > 0) {
                $YetaWF.runGlobalScript(found[0].Text);
                YVolatile.Basics.KnownScriptsDynamic.push(name);// save as dynamically loaded script
                this.processScript(scripts, payload, total, ix, run);
            } else {
                let loaded = false;
                let js = document.createElement("script");
                js.type = "text/javascript";
                js.async = false; // need to preserve execution order
                js.src = urlEntry.Url;
                // eslint-disable-next-line guard-for-in
                for (let attr in urlEntry.Attributes)
                    $YetaWF.setAttribute(js, attr, urlEntry.Attributes[attr]);
                js.setAttribute("data-name", name);
                js.onload = js.onerror = js["onreadystatechange"] = (ev: any) : void => {
                    if ((js["readyState"] && !(/^c|loade/.test(js["readyState"]))) || loaded) return;
                    js.onload = js["onreadystatechange"] = null;
                    loaded = true;
                    this.processScript(scripts, payload, total, ix, run);
                };
                if (YVolatile.Basics.JSLocation === JSLocationEnum.Top) {// location doesn't really matter, but done for consistency
                    let head = document.getElementsByTagName("head")[0];
                    head.insertBefore(js, head.lastChild);
                } else {
                    let body = document.getElementsByTagName("body")[0];
                    body.insertBefore(js, body.lastChild);
                }
            }
        }

        private processScript(scripts: UrlEntry[], payload: Payload[], total: number, ix: number, run: () => void): void {
            if (ix >= total - 1) {
                run();// we're all done
            } else {
                this.loadNextScript(scripts, payload, total, ix + 1, run);
            }
        }

        /**
         * Changes the current page to the specified Uri (may not be part of a unified page set). If a Unified Page Set is active,
         * the page is incrementally updated, otherwise the entire page is loaded.
         * @param uri The new page.
         */
        public setNewUri(uri: YetaWF.Url): void {
            if ($YetaWF.ContentHandling.setContent(uri, true) === SetContentResult.NotContent)
                window.location.assign(uri.toUrl());
        }

        /**
         * Changes the current page to the specified Uri (may not be part of a unified page set).
         * The result depends on the current page changed status.
         * Returns SetContentResult.NotContent if the uri couldn't be processed (i.e., it's not part of a unified page set).
         * Returns SetContentResult.ContentReplaced if the page is now shown and is part of the unified page set.
         * @param uriRequested The new page.
         * @param setState Defines whether the browser's history should be updated.
         * @param popupCB A callback to process popup content. May be null.
         * @param inplace Inplace content replacement options. May be null.
         */
        public setContent(uriRequested: YetaWF.Url, setState: boolean, popupCB?: (result: ContentResult, done: (dialog: HTMLElement) => void) => void, inplace?: InplaceContents, contentCB?: (result: ContentResult|null) => void): SetContentResult {
            if (!this.allowNavigateAway()) {
                $YetaWF.sendCustomEvent(document.body, Content.EVENTNAVCANCEL);
                return SetContentResult.Abort;
            }
            return this.setContentForce(uriRequested, setState, popupCB, inplace, contentCB);
        }

        /**
         * Changes the current page to the specified Uri (may not be part of a unified page set).
         * Does not prevent changing a page, regardless of the page changed status
         * Returns SetContentResult.NotContent if the uri couldn't be processed (i.e., it's not part of a unified page set).
         * Returns SetContentResult.ContentReplaced if the page is now shown and is part of the unified page set.
         * Returns SetContentResult.Abort if the page cannot be shown because the user doesn't want to navigate away from the page.
         * @param uriRequested The new page.
         * @param setState Defines whether the browser's history should be updated.
         * @param popupCB A callback to process popup content. May be null.
         */
        public setContentForce(uriRequested: YetaWF.Url, setState: boolean, popupCB?: (result: ContentResult, done: (dialog: HTMLElement) => void) => void, inplace?: InplaceContents, contentCB?: (result: ContentResult|null) => void): SetContentResult {

            if (YVolatile.Basics.EditModeActive) return SetContentResult.NotContent; // edit mode

            // check if we're clicking a link which is part of this unified page
            let uri: YetaWF.Url;
            if (inplace)
                uri = $YetaWF.parseUrl(inplace.ContentUrl);
            else
                uri = uriRequested;
            let path = uri.getPath();

            let divs: HTMLElement[];
            if (inplace)
                divs = $YetaWF.getElementsBySelector(`.${inplace.FromPane}.yUnified[data-pane]`); // only requested pane
            else
                divs = $YetaWF.getElementsBySelector(".yUnified[data-pane]"); // all panes
            if (divs.length === 0) // can occur in popups while in edit mode
                return SetContentResult.NotContent; // edit mode

            // build data context (like scripts, css files we have)
            let data: ContentData = {
                CacheVersion: YVolatile.Basics.CacheVersion,
                CacheFailUrl: inplace ? inplace.PageUrl : null,
                Path: path,
                QueryString: uri.getQuery(),
                UnifiedAddonMods: $YetaWF.UnifiedAddonModsLoaded,
                UniqueIdCounters: YVolatile.Basics.UniqueIdCounters,
                IsMobile: $YetaWF.isMobile(),
                Panes: [],
                KnownCss: [],
                KnownScripts: []
            };
            for (let div of divs) {
                data.Panes.push(div.getAttribute("data-pane") as string);
            }
            let css = $YetaWF.getElementsBySelector("link[rel=\"stylesheet\"][data-name]");
            for (let c of css) {
                data.KnownCss.push(c.getAttribute("data-name") as string);
            }
            css = $YetaWF.getElementsBySelector("style[type=\"text/css\"][data-name]");
            for (let c of css) {
                data.KnownCss.push(c.getAttribute("data-name") as string);
            }
            data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []);// add known css files that were added via bundles
            let scripts = $YetaWF.getElementsBySelector("script[src][data-name]");
            for (let s of scripts) {
                data.KnownScripts.push(s.getAttribute("data-name") as string);
            }
            data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic || []);// known javascript files that were added by content pages
            data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles || []);// add known javascript files that were added via bundles

            $YetaWF.setLoading();

            let request: XMLHttpRequest = new XMLHttpRequest();
            request.open("POST", "/YetaWF_Core/PageContent/Show" + uri.getQuery(true), true);
            request.setRequestHeader("Content-Type", "application/json");
            request.setRequestHeader("X-HTTP-Method-Override", "GET");// server has to think this is a GET request so all actions that are invoked actually work
            request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            request.onreadystatechange = (ev: Event) : any => {
                if (request.readyState === 4 /*DONE*/) {
                    $YetaWF.setLoading(false);
                    if (request.status === 200) {
                        let result: ContentResult = JSON.parse(request.responseText);
                        this.processReceivedContent(result, uri, divs, setState, popupCB, inplace, contentCB);
                    } else if (request.status === 0) {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, YLocs.Forms.AjaxConnLost), YLocs.Forms.AjaxErrorTitle);
                        return SetContentResult.NotContent;
                    } else {
                        $YetaWF.setLoading(false);
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, request.statusText), YLocs.Forms.AjaxErrorTitle);
                        // eslint-disable-next-line no-debugger
                        debugger;
                    }
                }
            };
            request.send(JSON.stringify(data));
            return SetContentResult.ContentReplaced;
        }

        private allowNavigateAway(): boolean {
            return !$YetaWF.pageChanged || confirm("Changes to this page have not yet been saved. Are you sure you want to navigate away from this page without saving?");
        }

        private processReceivedContent(result: ContentResult, uri: YetaWF.Url, divs: HTMLElement[], setState: boolean, popupCB?: (result: ContentResult, done: (dialog: HTMLElement) => void) => void, inplace?: InplaceContents, contentCB?: (result: ContentResult|null) => void) : void {

            $YetaWF.closeOverlays();
            $YetaWF.pageChanged = false;

            if (result.Status != null && result.Status.length > 0) {
                $YetaWF.setLoading(false);
                $YetaWF.error(result.Status, YLocs.Forms.AjaxErrorTitle);
                return;
            }
            if (result.Redirect != null && result.Redirect.length > 0) {
                //$YetaWF.setLoading(false);
                if (popupCB) {
                    // we want a popup and get a redirect, redirect to iframe popup
                    $YetaWF.Popups.openPopup(result.Redirect, true);
                } else {
                    // simple redirect
                    window.location.assign(result.Redirect);
                }
                return;
            }
            if (result.RedirectContent != null && result.RedirectContent.length > 0) {
                this.setContentForce($YetaWF.parseUrl(result.RedirectContent), setState, popupCB);
                return;
            }
            // run all global scripts (YConfigs, etc.)
            $YetaWF.runGlobalScript(result.Scripts);
            // add all new css files
            for (let urlEntry of result.CssFiles) {
                let found = result.CssFilesPayload.filter((elem: Payload): boolean => { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    let elem = <style type="text/css" data-name={found[0].Name}>{found[0].Text}</style>;
                    if (YVolatile.Basics.CssLocation === CssLocationEnum.Top) {
                        document.head!.appendChild(elem);
                    } else {
                        document.body.appendChild(elem);
                    }
                } else {
                    let elem = <link rel="stylesheet" type="text/css" data-name={urlEntry.Name} href={urlEntry.Url} />;
                    if (YVolatile.Basics.CssLocation === CssLocationEnum.Top) {
                        document.head!.appendChild(elem);
                    } else {
                        document.body.appendChild(elem);
                    }
                }
            }
            YVolatile.Basics.UnifiedCssBundleFiles = YVolatile.Basics.UnifiedCssBundleFiles || [];
            YVolatile.Basics.UnifiedCssBundleFiles.concat(result.CssBundleFiles || []);

            // add all new script files
            this.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, (): void => {
                YVolatile.Basics.UnifiedScriptBundleFiles = YVolatile.Basics.UnifiedScriptBundleFiles || [];
                YVolatile.Basics.UnifiedScriptBundleFiles.concat(result.ScriptBundleFiles || []);
                let tags: HTMLElement[] = []; // collect all panes
                if (!popupCB) {
                    // Update the browser page title
                    document.title = result.PageTitle;
                    // Update the browser address bar with the new path
                    if (setState) {
                        if (inplace)
                            $YetaWF.setUrl(inplace.PageUrl);
                        else
                            $YetaWF.setUrl(uri.toUrl());
                    }
                    // remove all pane contents
                    if (inplace) {
                        let target = $YetaWF.getElementById(inplace.TargetTag);
                        $YetaWF.processClearDiv(target);
                        target.innerHTML = "";
                    } else {
                        for (let div of divs) {
                            $YetaWF.processClearDiv(div);
                            div.innerHTML = "";
                        }
                    }
                    // remove prior page css classes
                    if (!inplace) {
                        $YetaWF.elementRemoveClassList(document.body, document.body.getAttribute("data-pagecss"));
                        // add new css classes
                        $YetaWF.elementAddClassList(document.body, result.PageCssClasses);
                        document.body.setAttribute("data-pagecss", result.PageCssClasses);// remember so we can remove them for the next page
                    }
                    // add pane content
                    if (inplace) {
                        if (result.Content.length > 0) {
                            let pane = $YetaWF.getElementById(inplace.TargetTag);
                            $YetaWF.appendMixedHTML(pane, result.Content[0].HTML);
                            tags.push(pane);// run all registered initializations for the pane
                        }
                    } else {
                        for (let content of result.Content) {
                            // replace the pane
                            let pane = $YetaWF.getElement1BySelector(`.yUnified[data-pane="${content.Pane}"]`);
                            pane.style.display = "block";// show in case this is a conditional pane
                            // add pane (can contain mixed html/scripts)
                            $YetaWF.appendMixedHTML(pane, content.HTML);
                            // run all registered initializations for the pane
                            tags.push(pane);
                        }
                    }
                } else {
                    popupCB(result, (dialog: HTMLElement): void => {
                        tags.push(dialog);
                    });
                }
                // add addons (can contain mixed html/scripts)
                if (result.Addons.length > 0)
                    $YetaWF.appendMixedHTML(document.body, result.Addons);
                YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonModsPrevious || [];
                YVolatile.Basics.UnifiedAddonMods = YVolatile.Basics.UnifiedAddonMods || [];
                // end of page scripts
                $YetaWF.runGlobalScript(result.EndOfPageScripts);
                // turn off all previously active modules that are no longer active
                YVolatile.Basics.UnifiedAddonModsPrevious.forEach((guid: string): void => {
                    if (YVolatile.Basics.UnifiedAddonMods.indexOf(guid) < 0)
                        $YetaWF.sendAddonChangedEvent(guid, false);
                });
                // turn on all newly active modules (if they were previously loaded)
                // new referenced modules that were just loaded now are already active and don't need to be called
                YVolatile.Basics.UnifiedAddonMods.forEach((guid: string): void => {
                    if (YVolatile.Basics.UnifiedAddonModsPrevious.indexOf(guid) < 0 && $YetaWF.UnifiedAddonModsLoaded.indexOf(guid) >= 0)
                        $YetaWF.sendAddonChangedEvent(guid, true);
                    if ($YetaWF.UnifiedAddonModsLoaded.indexOf(guid) < 0)
                        $YetaWF.UnifiedAddonModsLoaded.push(guid);
                });
                YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonMods;
                YVolatile.Basics.UnifiedAddonMods = [];
                // call ready handlers
                $YetaWF.processAllReadyOnce(tags);
                $YetaWF.sendCustomEvent(document.body, Content.EVENTNAVPAGELOADED, { containers: tags});
                if (!popupCB) {
                    // scroll
                    let scrolled = $YetaWF.setScrollPosition();
                    if (!scrolled) {
                        window.scroll(0, 0);
                        if (inplace) {
                            let pane = $YetaWF.getElementById(inplace.TargetTag);
                            try {// ignore errors on crappy browsers
                                pane.scroll(0, 0);
                            } catch (e) { }
                        }
                    }
                    // in case there is a popup open, close it now (typically when returning to the page from a popup)
                    if ($YetaWF.PopupsAvailable())
                        $YetaWF.Popups.closeInnerPopup();
                }
                try {
                    $YetaWF.runGlobalScript(result.AnalyticsContent);
                } catch (e) { }

                // locate the hash if there is one
                let setFocus = true;
                let hash = uri.getHash();
                if (hash) {
                    let target: HTMLElement|null = null;
                    try {// handle invalid id
                        target = $YetaWF.getElement1BySelectorCond(`#${hash}`);
                    } catch (e) { }
                    if (target) {
                        target.scrollIntoView();
                        setFocus = false;
                    }
                }
                // done, set focus
                if (setFocus) {
                    setTimeout((): void => { // defer setting focus (popups, controls may not yet be visible)
                        $YetaWF.setFocus(tags);
                    }, 1);
                }
                $YetaWF.setLoading(false);
            });
            if (contentCB)
                contentCB(result);
        }

        public loadAddons(addons: AddonDescription[], run: () => void): void {

            // build data context (like scripts, css files we have)
            let data: AddonsContentData = {
                Addons: addons,
                KnownCss: [],
                KnownScripts: []
            };

            let css = $YetaWF.getElementsBySelector("link[rel=\"stylesheet\"][data-name]");
            for (let c of css) {
                data.KnownCss.push(c.getAttribute("data-name") as string);
            }
            css = $YetaWF.getElementsBySelector("style[type=\"text/css\"][data-name]");
            for (let c of css) {
                data.KnownCss.push(c.getAttribute("data-name") as string);
            }
            data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []);// add known css files that were added via bundles
            let scripts = $YetaWF.getElementsBySelector("script[src][data-name]");
            for (let s of scripts) {
                data.KnownScripts.push(s.getAttribute("data-name") as string);
            }
            data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic || []);// known javascript files that were added by content pages
            data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles || []);// add known javascript files that were added via bundles

            $YetaWF.setLoading();

            this.getAddonsData("/YetaWF_Core/AddonContent/ShowAddons", data)
                .then((data: ContentResult): void => {
                    this.processReceivedAddons(data, run);
                })
                .catch((error: Error): void => alert(error.message));
        }

        private getAddonsData(url: string, data: AddonsContentData): Promise<ContentResult> {
            let p = new Promise<ContentResult>((resolve:(result:ContentResult)=>void, reject:(reason:Error)=>void): void => {

                const request: XMLHttpRequest = new XMLHttpRequest();

                request.open("POST", url, true);
                request.setRequestHeader("Content-Type", "application/json");
                request.setRequestHeader("X-HTTP-Method-Override", "GET");// server has to think this is a GET request so all actions that are invoked actually work
                request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                request.onreadystatechange = (ev: Event): any => {
                    if (request.readyState === 4 /*DONE*/) {
                        $YetaWF.setLoading(false);
                        if (request.status === 200) {
                            resolve(JSON.parse(request.responseText) as ContentResult);
                        } else {
                            reject(new Error(YLocs.Forms.AjaxError.format(request.status, request.statusText)));
                            // eslint-disable-next-line no-debugger
                            debugger;
                        }
                    }
                };
                request.send(JSON.stringify(data));
            });
            return p;
        }

        private processReceivedAddons(result: ContentResult, run: () => void): void {

            if (result.Status != null && result.Status.length > 0) {
                alert(result.Status);
                return;
            }
            // run all global scripts (YConfigs, etc.)
            $YetaWF.runGlobalScript(result.Scripts);
            // add all new css files
            for (let urlEntry of result.CssFiles) {
                let found = result.CssFilesPayload.filter((elem: Payload): boolean => { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    let elem = <style type="text/css" data-name={found[0].Name}>{found[0].Text}</style>;
                    document.body.appendChild(elem);
                } else {
                    let elem = <link rel="stylesheet" type="text/css" data-name={urlEntry.Name} href={urlEntry.Url} />;
                    document.body.appendChild(elem);
                }
            }
            YVolatile.Basics.UnifiedCssBundleFiles = YVolatile.Basics.UnifiedCssBundleFiles || [];
            YVolatile.Basics.UnifiedCssBundleFiles.concat(result.CssBundleFiles || []);

            // add all new script files
            this.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, (): void => {
                YVolatile.Basics.UnifiedScriptBundleFiles = YVolatile.Basics.UnifiedScriptBundleFiles || [];
                YVolatile.Basics.UnifiedScriptBundleFiles.concat(result.ScriptBundleFiles || []);
                // end of page scripts
                $YetaWF.runGlobalScript(result.EndOfPageScripts);

                run();
            });
        }

        public init(): void {
        }
    }
}
