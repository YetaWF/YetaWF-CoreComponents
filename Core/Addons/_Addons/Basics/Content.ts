/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
                $.globalEval(found[0].Text);
                YVolatile.Basics.KnownScriptsDynamic.push(name);// save as dynamically loaded script
                this.processScript(scripts, payload, total, ix, run);
            } else {
                var loaded;
                var js = document.createElement('script');
                js.type = 'text/javascript';
                js.async = false; // need to preserve execution order
                js.src = urlEntry.Url;
                var dataName = document.createAttribute("data-name");
                dataName.value = name;
                js.setAttributeNode(dataName);
                js.onload = js.onerror = js['onreadystatechange'] = () => {
                    if ((js['readyState'] && !(/^c|loade/.test(js['readyState']))) || loaded) return;
                    js.onload = js['onreadystatechange'] = null;
                    loaded = true;
                    this.processScript(scripts, payload, total, ix, run);
                };
                if (YVolatile.Basics.JSLocation) {// location doesn't really matter, but done for consistency
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
        public setContent(uri: uri.URI, setState: boolean, popupCB?: (result: ContentResult) => JQuery<HTMLElement>): boolean {

            if (YVolatile.Basics.EditModeActive) return false; // edit mode
            if (YVolatile.Basics.UnifiedMode == 0) return false; // not unified mode
            if (popupCB) {
                if (YVolatile.Basics.UnifiedMode !== 3 /*UnifiedModeEnum.DynamicContent*/ && YVolatile.Basics.UnifiedMode !== 4 /*UnifiedModeEnum.SkinDynamicContent*/)
                    return false; // popups can only be used with some unified modes
                if (!YVolatile.Basics.UnifiedPopups)
                    return false; // popups not wanted for this UPS
            }

            // check if we're clicking a link which is part of this unified page
            var path = uri.path();
            if (YVolatile.Basics.UnifiedMode === 3 /*UnifiedModeEnum.DynamicContent*/ || YVolatile.Basics.UnifiedMode === 4 /*UnifiedModeEnum.SkinDynamicContent*/) {
                // find all panes that support dynamic content and replace with new modules
                var $divs = $('.yUnified[data-pane]');
                // build data context (like scripts, css files we have)
                var data: ContentData = {
                    CacheVersion: YVolatile.Basics.CacheVersion,
                    Path: path,
                    QueryString: uri.query(),
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
                if (YVolatile.Basics.UnifiedMode === 4 /*UnifiedModeEnum.SkinDynamicContent*/) {
                    data.UnifiedSkinCollection = YVolatile.Basics.UnifiedSkinCollection;
                    data.UnifiedSkinFileName = YVolatile.Basics.UnifiedSkinName;
                }
                $divs.each(function () {
                    data.Panes.push($(this).attr('data-pane') as string);
                });
                data.KnownCss = [];
                var $css = $('link[rel="stylesheet"][data-name]');
                $css.each(function () {
                    data.KnownCss.push($(this).attr('data-name') as string);
                });
                $css = $('style[type="text/css"][data-name]');
                $css.each(function () {
                    data.KnownCss.push($(this).attr('data-name') as string);
                });
                data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles);// add known css files that were added via bundles
                var $scripts = $('script[src][data-name]');
                $scripts.each(function () {
                    data.KnownScripts.push($(this).attr('data-name') as string);
                });
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic);// known javascript files that were added by content pages
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles);// add known javascript files that were added via bundles

                YetaWF_Basics.setLoading();
                $.ajax({
                    url: '/YetaWF_Core/PageContent/Show?' + uri.query(),
                    type: 'POST',
                    data: JSON.stringify(data),
                    dataType: 'json',
                    traditional: true,
                    contentType: "application/json",
                    processData: false,
                    headers: {
                        "X-HTTP-Method-Override": "GET" // server has to think this is a GET request so all actions that are invoked actually work
                    },
                    success: (result: ContentResult, textStatus: string, jqXHR: JQuery.jqXHR) => {
                        this.closemenus();
                        if (result.Status != null && result.Status.length > 0) {
                            YetaWF_Basics.setLoading(false);
                            YetaWF_Basics.Y_Alert(result.Status, YLocs.Forms.AjaxErrorTitle);
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
                            this.setContent(new URI(result.RedirectContent), setState, popupCB);
                            return;
                        }
                        // run all global scripts (YConfigs, etc.)
                        $.globalEval(result.Scripts);
                        // add all new css files
                        var cssLength = result.CssFiles.length;
                        for (var i = 0; i < cssLength; i++) {
                            var urlEntry = result.CssFiles[i];
                            var found = result.CssFilesPayload.filter(function (elem) { return elem.Name == urlEntry.Name; });
                            if (found.length > 0) {
                                if (YVolatile.Basics.CssLocation) {
                                    $('head').append($('<style />').attr('type', 'text/css').attr('data-name', found[0].Name).html(found[0].Text));
                                } else {
                                    $('body').append($('<style />').attr('type', 'text/css').attr('data-name', found[0].Name).html(found[0].Text));
                                }
                            } else {
                                if (YVolatile.Basics.CssLocation) {
                                    $('head').append($('<link />').attr('rel', 'stylesheet').attr('type', 'text/css').attr('data-name', urlEntry.Name).attr('href', urlEntry.Url));
                                } else {
                                    $('body').append($('<link />').attr('rel', 'stylesheet').attr('type', 'text/css').attr('data-name', urlEntry.Name).attr('href', urlEntry.Url));
                                }
                            }
                        }
                        if (result.CssBundleFiles != null) {
                            YVolatile.Basics.UnifiedCssBundleFiles = YVolatile.Basics.UnifiedCssBundleFiles || [];
                            YVolatile.Basics.UnifiedCssBundleFiles.concat(result.CssBundleFiles);
                        }
                        // add all new script files
                        this.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, () => {
                            YVolatile.Basics.UnifiedScriptBundleFiles = YVolatile.Basics.UnifiedScriptBundleFiles || [];
                            YVolatile.Basics.UnifiedScriptBundleFiles.concat(result.ScriptBundleFiles);
                            if (!popupCB) {
                                // Update the browser page title
                                document.title = result.PageTitle;
                                // Update the browser address bar with the new path
                                if (setState) {
                                    try {
                                        var stateObj = {};
                                        history.pushState(stateObj, "", uri.toString());
                                    } catch (err) { }
                                }
                                // remove all pane contents
                                $divs.each(function () {
                                    YetaWF_Basics.processClearDiv(this);
                                    var $div = $(this);
                                    $div.empty();
                                    if ($div.attr("data-conditional") !== undefined)
                                        $div.hide();// hide, it's a conditional pane
                                });
                                // Notify that page is changing
                                $(document).trigger('YetaWF_Basics_PageChange', []);
                                // remove prior page css classes
                                var $body = $('body');
                                $body.removeClass($body.attr('data-pagecss'));
                                // add new css classes
                                $body.addClass(result.PageCssClasses);
                                $body.attr('data-pagecss', result.PageCssClasses);// remember so we can remove them for the next page
                            }
                            var $tags = $(); // collect all panes
                            if (!popupCB) {
                                // add pane content
                                var contentLength = result.Content.length;
                                for (var i = 0; i < contentLength; i++) {
                                    // replace the pane
                                    var $pane = $(`.yUnified[data-pane="${result.Content[i].Pane}"]`);
                                    $pane.show();// show in case this is a conditional pane
                                    $pane.append(result.Content[i].HTML);
                                    // run all registered initializations for the pane
                                    $tags = $tags.add($pane);
                                }
                            } else {
                                $tags = popupCB(result);
                            }
                            // add addons
                            $('body').append(result.Addons);
                            YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonModsPrevious || [];
                            YVolatile.Basics.UnifiedAddonMods = YVolatile.Basics.UnifiedAddonMods || [];
                            // end of page scripts
                            $.globalEval(result.EndOfPageScripts);
                            // turn off all previously active modules that are no longer active
                            YVolatile.Basics.UnifiedAddonModsPrevious.forEach(function (guid) {
                                if (YVolatile.Basics.UnifiedAddonMods.indexOf(guid) < 0)
                                    $(document).trigger('YetaWF_Basics_Addon', [guid, false]);
                            });
                            // turn on all newly active modules (if they were previously loaded)
                            // new referenced modules that were just loaded now are already active and don't need to be called
                            YVolatile.Basics.UnifiedAddonMods.forEach(function (guid) {
                                if (YVolatile.Basics.UnifiedAddonModsPrevious.indexOf(guid) < 0 && YetaWF_Basics.UnifiedAddonModsLoaded.indexOf(guid) >= 0)
                                    $(document).trigger('YetaWF_Basics_Addon', [guid, true]);
                                if (YetaWF_Basics.UnifiedAddonModsLoaded.indexOf(guid) < 0)
                                    YetaWF_Basics.UnifiedAddonModsLoaded.push(guid);
                            });
                            YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonMods;
                            YVolatile.Basics.UnifiedAddonMods = [];
                            // call ready handlers
                            YetaWF_Basics.processAllReady($tags);
                            YetaWF_Basics.processAllReadyOnce($tags);
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
                                $.globalEval(result.AnalyticsContent);
                            } catch (e) { }
                            $(document).trigger('YetaWF_Basics_NewPage', [uri.toString()]);// notify listeners that there is a new page
                            // done, set focus
                            YetaWF_Basics.setFocus($tags);
                            YetaWF_Basics.setLoading(false);
                        });
                    },
                    error: (jqXHR: JQuery.jqXHR, textStatus: string, errorThrown: string) => {
                        YetaWF_Basics.setLoading(false);
                        YetaWF_Basics.Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
                        debugger;
                    }
                });
                return true;
            } else {
                // check if we have anything with that path as a unified pane and activate the panes
                var $divs = $(`.yUnified[data-url="${path}"]`);
                if ($divs.length > 0) {
                    this.closemenus();
                    // Update the browser address bar with the new path
                    if (setState) {
                        try {
                            var stateObj = {};
                            history.pushState(stateObj, "", uri.toString());
                        } catch (err) { }
                    }
                    if (YVolatile.Basics.UnifiedMode === 1 /*UnifiedModeEnum.HideDivs*/) {
                        $('.yUnified').hide();
                        $divs.show();
                        // send event that a new section became active/visible
                        $('body').trigger('YetaWF_PropertyList_PanelSwitched', $divs);
                        // scroll
                        var scrolled = YetaWF_Basics.setScrollPosition();
                        if (!scrolled) {
                            $(window).scrollLeft(0);
                            $(window).scrollTop(0);
                        }
                        YetaWF_Basics.setFocus();
                    } else if (YVolatile.Basics.UnifiedMode === 2 /*UnifiedModeEnum.ShowDivs*/) {
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