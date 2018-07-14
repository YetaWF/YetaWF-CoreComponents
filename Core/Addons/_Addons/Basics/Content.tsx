/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// %%%%%%% TODO: There are JQuery references

// Anchor handling, navigation

namespace YetaWF {

    interface ContentData {
        CacheVersion: string;
        Path: string;
        QueryString: string;
        UnifiedSetGuid: string;
        UnifiedMode: number;
        UnifiedAddonMods: string[];
        UniqueIdPrefixCounter: number;
        IsMobile: boolean;
        UnifiedSkinCollection: string | null;
        UnifiedSkinFileName: string | null
        Panes: string[];
        KnownCss: string[];
        KnownScripts: string[];
    };

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

    export class Content {

        // loads all scripts - we need to preserve the order of initialization hence the recursion
        private loadScripts(scripts: UrlEntry[], payload: Payload[], run: () => void): void {

            YVolatile.Basics.KnownScriptsDynamic = YVolatile.Basics.KnownScriptsDynamic || [];
            var total = scripts.length;
            if (total == 0) {
                run();
                return;
            }
            this.loadNextScript(scripts, payload, total, 0, run);
        }

        private loadNextScript(scripts: UrlEntry[], payload: Payload[], total: number, ix: number, run: () => void) {

            var urlEntry = scripts[ix];
            var name = urlEntry.Name;

            var found = payload.filter(function (elem) { return elem.Name == name; });
            if (found.length > 0) {
                YetaWF_Basics.runGlobalScript(found[0].Text);
                YVolatile.Basics.KnownScriptsDynamic.push(name);// save as dynamically loaded script
                this.processScript(scripts, payload, total, ix, run);
            } else {
                var loaded;
                var js = document.createElement('script');
                js.type = 'text/javascript';
                js.async = false; // need to preserve execution order
                js.src = urlEntry.Url;
                js.setAttribute("data-name", name);
                //$$ var dataName = document.createAttribute("data-name");
                // dataName.value = name;
                // js.setAttributeNode(dataName);
                js.onload = js.onerror = js['onreadystatechange'] = () => {
                    if ((js['readyState'] && !(/^c|loade/.test(js['readyState']))) || loaded) return;
                    js.onload = js['onreadystatechange'] = null;
                    loaded = true;
                    this.processScript(scripts, payload, total, ix, run);
                };
                if (YVolatile.Basics.JSLocation == JSLocationEnum.Top) {// location doesn't really matter, but done for consistency
                    var head = document.getElementsByTagName('head')[0];
                    head.insertBefore(js, head.lastChild)
                } else {
                    var body = document.getElementsByTagName('body')[0];
                    body.insertBefore(js, body.lastChild)
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

        // Change the current page to the specified Uri (may not be part of the unified page set)
        // returns false if the uri couldn't be processed (i.e., it's not part of a unified page set)
        // returns true if the page is now shown and is part of the unified page set
        public setContent(uri: YetaWF.Url, setState: boolean, popupCB?: (result: ContentResult) => HTMLElement): boolean {

            if (YVolatile.Basics.EditModeActive) return false; // edit mode
            if (YVolatile.Basics.UnifiedMode == UnifiedModeEnum.None) return false; // not unified mode
            if (popupCB) {
                if (YVolatile.Basics.UnifiedMode !== UnifiedModeEnum.DynamicContent && YVolatile.Basics.UnifiedMode !== UnifiedModeEnum.SkinDynamicContent)
                    return false; // popups can only be used with some unified modes
                if (!YVolatile.Basics.UnifiedPopups)
                    return false; // popups not wanted for this UPS
            }

            // check if we're clicking a link which is part of this unified page
            var path = uri.getPath();
            if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.DynamicContent || YVolatile.Basics.UnifiedMode === UnifiedModeEnum.SkinDynamicContent) {
                // find all panes that support dynamic content and replace with new modules
                var divs = YetaWF_Basics.getElementsBySelector('.yUnified[data-pane]');
                // build data context (like scripts, css files we have)
                var data: ContentData = {
                    CacheVersion: YVolatile.Basics.CacheVersion,
                    Path: path,
                    QueryString: uri.getQuery(),
                    UnifiedSetGuid: YVolatile.Basics.UnifiedSetGuid,
                    UnifiedMode: YVolatile.Basics.UnifiedMode,
                    UnifiedAddonMods: YetaWF_Basics.UnifiedAddonModsLoaded,
                    UniqueIdPrefixCounter: YVolatile.Basics.UniqueIdPrefixCounter,
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
                    data.Panes.push(div.getAttribute('data-pane') as string);
                }
                var css = YetaWF_Basics.getElementsBySelector('link[rel="stylesheet"][data-name]');
                for (var c of css) {
                    data.KnownCss.push(c.getAttribute('data-name') as string);
                }
                css = YetaWF_Basics.getElementsBySelector('style[type="text/css"][data-name]');
                for (var c of css) {
                    data.KnownCss.push(c.getAttribute('data-name') as string);
                }
                data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []);// add known css files that were added via bundles
                var scripts = YetaWF_Basics.getElementsBySelector('script[src][data-name]');
                for (var s of scripts) {
                    data.KnownScripts.push(s.getAttribute('data-name') as string);
                }
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic || []);// known javascript files that were added by content pages
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles || []);// add known javascript files that were added via bundles

                YetaWF_Basics.setLoading();

                var request: XMLHttpRequest = new XMLHttpRequest();
                request.open('POST', '/YetaWF_Core/PageContent/Show' + uri.getQuery(true), true);
                request.setRequestHeader("Content-Type", "application/json");
                request.setRequestHeader("X-HTTP-Method-Override", "GET");// server has to think this is a GET request so all actions that are invoked actually work
                request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                request.onreadystatechange = (ev: Event) => {
                    if (request.readyState === 4 /*DONE*/) {
                        YetaWF_Basics.setLoading(false);
                        if (request.status === 200) {
                            var result: ContentResult = JSON.parse(request.responseText)
                            this.processReceivedContent(result, uri, divs, setState, popupCB);
                        } else {
                            YetaWF_Basics.setLoading(false);
                            YetaWF_Basics.alert(YLocs.Forms.AjaxError.format(request.status, request.statusText), YLocs.Forms.AjaxErrorTitle);
                            debugger;
                        }
                    }
                };
                request.send(JSON.stringify(data));
                return true;
            } else {
                // check if we have anything with that path as a unified pane and activate the panes
                var divs = YetaWF_Basics.getElementsBySelector(`.yUnified[data-url="${path}"]`);
                var $divs = $(`.yUnified[data-url="${path}"]`);//$$
                if (divs.length > 0) {
                    this.closemenus();
                    // Update the browser address bar with the new path
                    if (setState) {
                        try {
                            var stateObj = {};
                            history.pushState(stateObj, "", uri.toUrl());
                        } catch (err) { }
                    }
                    if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.HideDivs) {
                        // hide all unified sections
                        var uni = YetaWF_Basics.getElementsBySelector('.yUnified');
                        for (var u of uni) {
                            u.style.display = "none";
                        }
                        // show all unified sections that are on the current page
                        for (var d of divs) {
                            d.style.display = "block";
                        }
                        // send event that a new section became active/visible
                        $('body').trigger('YetaWF_PropertyList_PanelSwitched', $divs); //$$$
                        // scroll
                        var scrolled = YetaWF_Basics.setScrollPosition();
                        if (!scrolled) {
                            $(window).scrollLeft(0);
                            $(window).scrollTop(0);
                        }
                        YetaWF_Basics.setFocus();
                    } else if (YVolatile.Basics.UnifiedMode === UnifiedModeEnum.ShowDivs) {
                        //element.scrollIntoView() as an alternative (check compatibility/options)
                        // calculate an approximate animation time so the shorter the distance, the shorter the animation
                        var h = $('body').height() as number;
                        var t = ($divs.eq(0).offset() as any).top;
                        var anim = YVolatile.Basics.UnifiedAnimation * t / h;
                        $('body,html').animate({
                            scrollTop: t
                        }, anim);
                    } else
                        throw `Invalid UnifiedMode ${YVolatile.Basics.UnifiedMode}`;
                    YetaWF_Basics.setLoading(false);
                    return true;
                }
                //YetaWF_Basics.setLoading(false); // don't hide, let new page take over
                return false;
            }
        };

        private processReceivedContent(result: ContentResult, uri: YetaWF.Url, divs: HTMLElement[], setState: boolean, popupCB?: (result: ContentResult) => HTMLElement) : void {

            this.closemenus();
            if (result.Status != null && result.Status.length > 0) {
                YetaWF_Basics.setLoading(false);
                YetaWF_Basics.alert(result.Status, YLocs.Forms.AjaxErrorTitle);
                return;
            }
            if (result.Redirect != null && result.Redirect.length > 0) {
                //YetaWF_Basics.setLoading(false);
                if (popupCB) {
                    // we want a popup and get a redirect, redirect to iframe popup
                    YetaWF_Popups.openPopup(result.Redirect, true);
                } else {
                    // simple redirect
                    window.location.assign(result.Redirect);
                }
                return;
            }
            if (result.RedirectContent != null && result.RedirectContent.length > 0) {
                this.setContent(YetaWF_Basics.parseUrl(result.RedirectContent), setState, popupCB);
                return;
            }
            // run all global scripts (YConfigs, etc.)
            YetaWF_Basics.runGlobalScript(result.Scripts);
            // add all new css files
            for (let urlEntry of result.CssFiles) {
                var found = result.CssFilesPayload.filter(function (elem) { return elem.Name == urlEntry.Name; });
                if (found.length > 0) {
                    var elem = <style type='text/css' data-name={found[0].Name}>{found[0].Text}</style>;
                    if (YVolatile.Basics.CssLocation === CssLocationEnum.Top) {
                        document.head.appendChild(elem);
                    } else {
                        document.body.appendChild(elem);
                    }
                } else {
                    var elem = <link rel="stylesheet" type='text/css' data-name={urlEntry.Name} href={urlEntry.Url} />;
                    if (YVolatile.Basics.CssLocation === CssLocationEnum.Top) {
                        document.head.appendChild(elem);
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
                if (!popupCB) {
                    // Update the browser page title
                    document.title = result.PageTitle;
                    // Update the browser address bar with the new path
                    if (setState) {
                        try {
                            var stateObj = {};
                            history.pushState(stateObj, "", uri.toUrl());
                        } catch (err) { }
                    }
                    // remove all pane contents
                    for (var div of divs) {
                        YetaWF_Basics.processClearDiv(div);
                        div.innerHTML = '';
                        if (div.getAttribute("data-conditional")) {
                            div.style.display = "none";// hide, it's a conditional pane
                        }
                    }
                    // Notify that page is changing
                    $(document).trigger('YetaWF_Basics_PageChange', []);//$$$
                    // remove prior page css classes
                    YetaWF_Basics.elementRemoveClasses(document.body, document.body.getAttribute('data-pagecss'));
                    // add new css classes
                    YetaWF_Basics.elementAddClasses(document.body, result.PageCssClasses);
                    document.body.setAttribute('data-pagecss', result.PageCssClasses);// remember so we can remove them for the next page
                }
                var tags: HTMLElement[] = []; // collect all panes
                if (!popupCB) {
                    // add pane content
                    for (let content of result.Content) {
                        // replace the pane
                        var pane = YetaWF_Basics.getElement1BySelector(`.yUnified[data-pane="${content.Pane}"]`);
                        pane.style.display = "block";// show in case this is a conditional pane
                        // add pane (can contain mixed html/scripts)
                        YetaWF_Basics.appendMixedHTML(pane, content.HTML);
                        // run all registered initializations for the pane
                        tags.push(pane);
                    }
                } else {
                    tags.push(popupCB(result));
                }
                // add addons (can contain mixed html/scripts)
                if (result.Addons.length > 0)
                    YetaWF_Basics.appendMixedHTML(document.body, result.Addons);
                YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonModsPrevious || [];
                YVolatile.Basics.UnifiedAddonMods = YVolatile.Basics.UnifiedAddonMods || [];
                // end of page scripts
                YetaWF_Basics.runGlobalScript(result.EndOfPageScripts);
                // turn off all previously active modules that are no longer active
                YVolatile.Basics.UnifiedAddonModsPrevious.forEach(function (guid) {
                    if (YVolatile.Basics.UnifiedAddonMods.indexOf(guid) < 0)
                        YetaWF_Basics.processContentChange(guid, false);
                });
                // turn on all newly active modules (if they were previously loaded)
                // new referenced modules that were just loaded now are already active and don't need to be called
                YVolatile.Basics.UnifiedAddonMods.forEach(function (guid) {
                    if (YVolatile.Basics.UnifiedAddonModsPrevious.indexOf(guid) < 0 && YetaWF_Basics.UnifiedAddonModsLoaded.indexOf(guid) >= 0)
                        YetaWF_Basics.processContentChange(guid, true);
                    if (YetaWF_Basics.UnifiedAddonModsLoaded.indexOf(guid) < 0)
                        YetaWF_Basics.UnifiedAddonModsLoaded.push(guid);
                });
                YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonMods;
                YVolatile.Basics.UnifiedAddonMods = [];
                // call ready handlers
                YetaWF_Basics.processAllReady(tags);
                YetaWF_Basics.processAllReadyOnce(tags);
                if (!popupCB) {
                    // scroll
                    var scrolled = YetaWF_Basics.setScrollPosition();
                    if (!scrolled) {
                        $(window).scrollLeft(0);
                        $(window).scrollTop(0);
                    }
                    // in case there is a popup open, close it now (typically when returning to the page from a popup)
                    if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined)
                        YetaWF_Popups.closeInnerPopup();
                }
                try {
                    YetaWF_Basics.runGlobalScript(result.AnalyticsContent);
                } catch (e) { }
                YetaWF_Basics.processNewPage(uri.toUrl());
                // done, set focus
                YetaWF_Basics.setFocus(tags);
                YetaWF_Basics.setLoading(false);
            });
        }

        //$$$$ MOVE THIS TO COMPONENTSHTML
        private closemenus(): void {
            // Close open bootstrap nav menus (if any) by clicking on the page
            $('body').trigger('click');
            // Close any open kendo menus (if any)
            var $menus = $(".k-menu");
            $menus.each(function () {
                var menu = $(this).data("kendoMenu");
                menu.close("li.k-item");
            });
            // Close any open smartmenus
            try {
                ($('.YetaWF_Menus') as any).collapse('hide'); //$$$$
            } catch (e) { }
        }
    }
}
