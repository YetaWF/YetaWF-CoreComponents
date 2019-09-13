/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// Anchor handling, navigation

namespace YetaWF {

    interface ContentData {
        CacheVersion: string;
        Path: string;
        QueryString: string;
        UnifiedSetGuid: string;
        UnifiedMode: number;
        UnifiedAddonMods: string[];
        UniqueIdCounters: UniqueIdInfo;
        IsMobile: boolean;
        UnifiedSkinCollection: string | null;
        UnifiedSkinFileName: string | null;
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
    }
    export interface AddonDescription {
        AreaName: string;
        ShortName: string;
        Argument1: string|null;
    }

    export class Content {

        // loads all scripts - we need to preserve the order of initialization hence the recursion
        private loadScripts(scripts: UrlEntry[], payload: Payload[], run: () => void): void {

            YVolatile.Basics.KnownScriptsDynamic = YVolatile.Basics.KnownScriptsDynamic || [];
            var total = scripts.length;
            if (total === 0) {
                run();
                return;
            }
            this.loadNextScript(scripts, payload, total, 0, run);
        }

        private loadNextScript(scripts: UrlEntry[], payload: Payload[], total: number, ix: number, run: () => void) : void {

            var urlEntry = scripts[ix];
            var name = urlEntry.Name;

            var found = payload.filter((elem: Payload) => { return elem.Name === name; });
            if (found.length > 0) {
                $YetaWF.runGlobalScript(found[0].Text);
                YVolatile.Basics.KnownScriptsDynamic.push(name);// save as dynamically loaded script
                this.processScript(scripts, payload, total, ix, run);
            } else {
                var loaded;
                var js = document.createElement("script");
                js.type = "text/javascript";
                js.async = false; // need to preserve execution order
                js.src = urlEntry.Url;
                js.setAttribute("data-name", name);
                js.onload = js.onerror = js["onreadystatechange"] = (ev: any) : void => {
                    if ((js["readyState"] && !(/^c|loade/.test(js["readyState"]))) || loaded) return;
                    js.onload = js["onreadystatechange"] = null;
                    loaded = true;
                    this.processScript(scripts, payload, total, ix, run);
                };
                if (YVolatile.Basics.JSLocation === JSLocationEnum.Top) {// location doesn't really matter, but done for consistency
                    var head = document.getElementsByTagName("head")[0];
                    head.insertBefore(js, head.lastChild);
                } else {
                    var body = document.getElementsByTagName("body")[0];
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
            if (!$YetaWF.ContentHandling.setContent(uri, true))
                window.location.assign(uri.toUrl());
        }

        /**
         * Changes the current page to the specified Uri (may not be part of a unified page set).
         * Returns false if the uri couldn't be processed (i.e., it's not part of a unified page set).
         * Returns true if the page is now shown and is part of the unified page set.
         * @param uriRequested The new page.
         * @param setState Defines whether the browser's history should be updated.
         * @param popupCB A callback to process popup content. May be null.
         */
        public setContent(uriRequested: YetaWF.Url, setState: boolean, popupCB?: (result: ContentResult, done: (dialog: HTMLElement) => void) => void, inplace?: InplaceContents): boolean {

            if (YVolatile.Basics.EditModeActive) return false; // edit mode
            if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.None) return false; // not unified mode
            if (popupCB) {
                if (YVolatile.Basics.UnifiedMode !== UnifiedModeEnum.DynamicContent && YVolatile.Basics.UnifiedMode !== UnifiedModeEnum.SkinDynamicContent)
                    return false; // popups can only be used with some unified modes
                if (!YVolatile.Basics.UnifiedPopups)
                    return false; // popups not wanted for this UPS
            }

            // check if we're clicking a link which is part of this unified page
            let uri: YetaWF.Url;
            if (inplace)
                uri = $YetaWF.parseUrl(inplace.ContentUrl);
            else
                uri = uriRequested;
            var path = uri.getPath();
            if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.DynamicContent || YVolatile.Basics.UnifiedMode === UnifiedModeEnum.SkinDynamicContent) {
                var divs: HTMLElement[];
                if (inplace)
                    divs = $YetaWF.getElementsBySelector(`.${inplace.FromPane}.yUnified[data-pane]`); // only requested pane
                else
                    divs = $YetaWF.getElementsBySelector(".yUnified[data-pane]"); // all panes
                if (divs.length === 0)
                    throw "No panes support dynamic content";

                // build data context (like scripts, css files we have)
                var data: ContentData = {
                    CacheVersion: YVolatile.Basics.CacheVersion,
                    Path: path,
                    QueryString: uri.getQuery(),
                    UnifiedSetGuid: YVolatile.Basics.UnifiedSetGuid,
                    UnifiedMode: YVolatile.Basics.UnifiedMode,
                    UnifiedAddonMods: $YetaWF.UnifiedAddonModsLoaded,
                    UniqueIdCounters: YVolatile.Basics.UniqueIdCounters,
                    IsMobile: YVolatile.Skin.MinWidthForPopups > window.outerWidth,
                    UnifiedSkinCollection: null,
                    UnifiedSkinFileName: null,
                    Panes: [],
                    KnownCss: [],
                    KnownScripts: []
                };
                if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.SkinDynamicContent) {
                    data.UnifiedSkinCollection = YVolatile.Basics.UnifiedSkinCollection;
                    data.UnifiedSkinFileName = YVolatile.Basics.UnifiedSkinName;
                }
                for (var div of divs) {
                    data.Panes.push(div.getAttribute("data-pane") as string);
                }
                var css = $YetaWF.getElementsBySelector("link[rel=\"stylesheet\"][data-name]");
                for (var c of css) {
                    data.KnownCss.push(c.getAttribute("data-name") as string);
                }
                css = $YetaWF.getElementsBySelector("style[type=\"text/css\"][data-name]");
                for (var c of css) {
                    data.KnownCss.push(c.getAttribute("data-name") as string);
                }
                data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []);// add known css files that were added via bundles
                var scripts = $YetaWF.getElementsBySelector("script[src][data-name]");
                for (var s of scripts) {
                    data.KnownScripts.push(s.getAttribute("data-name") as string);
                }
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic || []);// known javascript files that were added by content pages
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles || []);// add known javascript files that were added via bundles

                $YetaWF.setLoading();

                var request: XMLHttpRequest = new XMLHttpRequest();
                request.open("POST", "/YetaWF_Core/PageContent/Show" + uri.getQuery(true), true);
                request.setRequestHeader("Content-Type", "application/json");
                request.setRequestHeader("X-HTTP-Method-Override", "GET");// server has to think this is a GET request so all actions that are invoked actually work
                request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                request.onreadystatechange = (ev: Event) : any => {
                    if (request.readyState === 4 /*DONE*/) {
                        $YetaWF.setLoading(false);
                        if (request.status === 200) {
                            var result: ContentResult = JSON.parse(request.responseText);
                            this.processReceivedContent(result, uri, divs, setState, popupCB, inplace);
                        } else {
                            $YetaWF.setLoading(false);
                            $YetaWF.alert(YLocs.Forms.AjaxError.format(request.status, request.statusText), YLocs.Forms.AjaxErrorTitle);
                            // tslint:disable-next-line:no-debugger
                            debugger;
                        }
                    }
                };
                request.send(JSON.stringify(data));
                return true;
            } else {
                // check if we have anything with that path as a unified pane and activate the panes
                var divs = $YetaWF.getElementsBySelector(`.yUnified[data-url="${path}"]`);
                if (divs.length > 0) {

                    $YetaWF.closeOverlays();

                    // Update the browser address bar with the new path
                    if (setState) //$$$inplace
                        $YetaWF.setUrl(uri.toUrl());
                    if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.HideDivs) {
                        // hide all unified sections
                        var uni = $YetaWF.getElementsBySelector(".yUnified");
                        for (var u of uni) {
                            u.style.display = "none";
                        }
                        // show all unified sections that are on the current page
                        for (var d of divs) {
                            d.style.display = "block";
                        }
                        // send event that a new sections became active/visible
                        $YetaWF.processActivateDivs(divs);
                        // scroll
                        var scrolled = $YetaWF.setScrollPosition();
                        if (!scrolled)
                            window.scroll(0, 0);
                        $YetaWF.setFocus();
                    } else if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.ShowDivs) {

                        divs[0].scrollIntoView({ behavior: "smooth", block: "start" });

                        //// calculate an approximate animation time so the shorter the distance, the shorter the animation
                        //var h = document.body.scrollHeight;
                        //var newTop = divs[0].offsetTop;
                        //var scrollDuration = YVolatile.Basics.UnifiedAnimation * (Math.abs(newTop - window.scrollY) / h);

                        //var scrollStep = (newTop - window.scrollY) / (scrollDuration / 15);
                        //if (!isNaN(scrollStep)) {

                        //    console.log(`scrolling ${scrollDuration} ${scrollStep}`);
                        //    var scrollInterval = setInterval(function () {
                        //        if (scrollStep > 0 ? window.scrollY + scrollStep > newTop : window.scrollY + scrollStep < newTop) {
                        //            console.log(`scrolling done`);
                        //            window.scrollTo(0, newTop);
                        //            clearInterval(scrollInterval);
                        //        } else {
                        //            console.log(`scrolling step`);
                        //            window.scrollBy(0, scrollStep);
                        //        }
                        //    }, scrollDuration / 15);
                        //}
                    } else
                        throw `Invalid UnifiedMode ${YVolatile.Basics.UnifiedMode}`;
                    $YetaWF.setLoading(false);
                    return true;
                }
                //$YetaWF.setLoading(false); // don't hide, let new page take over
                return false;
            }
        }

        private processReceivedContent(result: ContentResult, uri: YetaWF.Url, divs: HTMLElement[], setState: boolean, popupCB?: (result: ContentResult, done: (dialog: HTMLElement) => void) => void, inplace?: InplaceContents ) : void {

            $YetaWF.closeOverlays();

            if (result.Status != null && result.Status.length > 0) {
                $YetaWF.setLoading(false);
                $YetaWF.alert(result.Status, YLocs.Forms.AjaxErrorTitle);
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
                this.setContent($YetaWF.parseUrl(result.RedirectContent), setState, popupCB);
                return;
            }
            // run all global scripts (YConfigs, etc.)
            $YetaWF.runGlobalScript(result.Scripts);
            // add all new css files
            for (let urlEntry of result.CssFiles) {
                var found = result.CssFilesPayload.filter((elem: Payload) => { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    var elem = <style type="text/css" data-name={found[0].Name}>{found[0].Text}</style>;
                    if (YVolatile.Basics.CssLocation === CssLocationEnum.Top) {
                        document.head!.appendChild(elem);
                    } else {
                        document.body.appendChild(elem);
                    }
                } else {
                    var elem = <link rel="stylesheet" type="text/css" data-name={urlEntry.Name} href={urlEntry.Url} />;
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
            this.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, () => {
                YVolatile.Basics.UnifiedScriptBundleFiles = YVolatile.Basics.UnifiedScriptBundleFiles || [];
                YVolatile.Basics.UnifiedScriptBundleFiles.concat(result.ScriptBundleFiles || []);
                var tags: HTMLElement[] = []; // collect all panes
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
                        for (var div of divs) {
                            $YetaWF.processClearDiv(div);
                            div.innerHTML = "";
                            if (div.getAttribute("data-conditional")) {
                                div.style.display = "none";// hide, it's a conditional pane
                            }
                        }
                    }
                    // Notify that the page is changing
                    $YetaWF.processPageChange();
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
                YVolatile.Basics.UnifiedAddonModsPrevious.forEach((guid: string) => {
                    if (YVolatile.Basics.UnifiedAddonMods.indexOf(guid) < 0)
                        $YetaWF.processContentChange(guid, false);
                });
                // turn on all newly active modules (if they were previously loaded)
                // new referenced modules that were just loaded now are already active and don't need to be called
                YVolatile.Basics.UnifiedAddonMods.forEach((guid: string) => {
                    if (YVolatile.Basics.UnifiedAddonModsPrevious.indexOf(guid) < 0 && $YetaWF.UnifiedAddonModsLoaded.indexOf(guid) >= 0)
                        $YetaWF.processContentChange(guid, true);
                    if ($YetaWF.UnifiedAddonModsLoaded.indexOf(guid) < 0)
                        $YetaWF.UnifiedAddonModsLoaded.push(guid);
                });
                YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonMods;
                YVolatile.Basics.UnifiedAddonMods = [];
                // call ready handlers
                $YetaWF.processAllReady(tags);
                $YetaWF.processAllReadyOnce(tags);
                if (!popupCB) {
                    // scroll
                    var scrolled = $YetaWF.setScrollPosition();
                    if (!scrolled)
                        window.scroll(0, 0);
                    // in case there is a popup open, close it now (typically when returning to the page from a popup)
                    if ($YetaWF.PopupsAvailable())
                        $YetaWF.Popups.closeInnerPopup();
                }
                try {
                    $YetaWF.runGlobalScript(result.AnalyticsContent);
                } catch (e) { }
                $YetaWF.processNewPage(uri.toUrl());
                // done, set focus
                setTimeout(():void => { // defer setting focus (popups, controls may not yet be visible)
                    $YetaWF.setFocus(tags);
                }, 1);
                $YetaWF.setLoading(false);
            });
        }

        public loadAddons(addons: AddonDescription[], run: () => void): void {

            // build data context (like scripts, css files we have)
            var data: AddonsContentData = {
                Addons: addons,
                KnownCss: [],
                KnownScripts: []
            };

            var css = $YetaWF.getElementsBySelector("link[rel=\"stylesheet\"][data-name]");
            for (var c of css) {
                data.KnownCss.push(c.getAttribute("data-name") as string);
            }
            css = $YetaWF.getElementsBySelector("style[type=\"text/css\"][data-name]");
            for (var c of css) {
                data.KnownCss.push(c.getAttribute("data-name") as string);
            }
            data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []);// add known css files that were added via bundles
            var scripts = $YetaWF.getElementsBySelector("script[src][data-name]");
            for (var s of scripts) {
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
            var p = new Promise<ContentResult>((resolve:(result:ContentResult)=>void, reject:(reason:Error)=>void): void => {

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
                            // tslint:disable-next-line:no-debugger
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
                var found = result.CssFilesPayload.filter((elem: Payload) => { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    var elem = <style type="text/css" data-name={found[0].Name}>{found[0].Text}</style>;
                    document.body.appendChild(elem);
                } else {
                    var elem = <link rel="stylesheet" type="text/css" data-name={urlEntry.Name} href={urlEntry.Url} />;
                    document.body.appendChild(elem);
                }
            }
            YVolatile.Basics.UnifiedCssBundleFiles = YVolatile.Basics.UnifiedCssBundleFiles || [];
            YVolatile.Basics.UnifiedCssBundleFiles.concat(result.CssBundleFiles || []);

            // add all new script files
            this.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, () => {
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
