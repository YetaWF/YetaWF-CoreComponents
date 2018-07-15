"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// %%%%%%% TODO: There are JQuery references
// Anchor handling, navigation
var YetaWF;
(function (YetaWF) {
    ;
    var Content = /** @class */ (function () {
        function Content() {
        }
        // loads all scripts - we need to preserve the order of initialization hence the recursion
        Content.prototype.loadScripts = function (scripts, payload, run) {
            YVolatile.Basics.KnownScriptsDynamic = YVolatile.Basics.KnownScriptsDynamic || [];
            var total = scripts.length;
            if (total == 0) {
                run();
                return;
            }
            this.loadNextScript(scripts, payload, total, 0, run);
        };
        Content.prototype.loadNextScript = function (scripts, payload, total, ix, run) {
            var _this = this;
            var urlEntry = scripts[ix];
            var name = urlEntry.Name;
            var found = payload.filter(function (elem) { return elem.Name == name; });
            if (found.length > 0) {
                YetaWF_Basics.runGlobalScript(found[0].Text);
                YVolatile.Basics.KnownScriptsDynamic.push(name); // save as dynamically loaded script
                this.processScript(scripts, payload, total, ix, run);
            }
            else {
                var loaded;
                var js = document.createElement('script');
                js.type = 'text/javascript';
                js.async = false; // need to preserve execution order
                js.src = urlEntry.Url;
                js.setAttribute("data-name", name);
                js.onload = js.onerror = js['onreadystatechange'] = function () {
                    if ((js['readyState'] && !(/^c|loade/.test(js['readyState']))) || loaded)
                        return;
                    js.onload = js['onreadystatechange'] = null;
                    loaded = true;
                    _this.processScript(scripts, payload, total, ix, run);
                };
                if (YVolatile.Basics.JSLocation == YetaWF.JSLocationEnum.Top) { // location doesn't really matter, but done for consistency
                    var head = document.getElementsByTagName('head')[0];
                    head.insertBefore(js, head.lastChild);
                }
                else {
                    var body = document.getElementsByTagName('body')[0];
                    body.insertBefore(js, body.lastChild);
                }
            }
        };
        Content.prototype.processScript = function (scripts, payload, total, ix, run) {
            if (ix >= total - 1) {
                run(); // we're all done
            }
            else {
                this.loadNextScript(scripts, payload, total, ix + 1, run);
            }
        };
        // Change the current page to the specified Uri (may not be part of the unified page set)
        // returns false if the uri couldn't be processed (i.e., it's not part of a unified page set)
        // returns true if the page is now shown and is part of the unified page set
        Content.prototype.setContent = function (uri, setState, popupCB) {
            var _this = this;
            if (YVolatile.Basics.EditModeActive)
                return false; // edit mode
            if (YVolatile.Basics.UnifiedMode == YetaWF.UnifiedModeEnum.None)
                return false; // not unified mode
            if (popupCB) {
                if (YVolatile.Basics.UnifiedMode !== YetaWF.UnifiedModeEnum.DynamicContent && YVolatile.Basics.UnifiedMode !== YetaWF.UnifiedModeEnum.SkinDynamicContent)
                    return false; // popups can only be used with some unified modes
                if (!YVolatile.Basics.UnifiedPopups)
                    return false; // popups not wanted for this UPS
            }
            // check if we're clicking a link which is part of this unified page
            var path = uri.getPath();
            if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.DynamicContent || YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.SkinDynamicContent) {
                // find all panes that support dynamic content and replace with new modules
                var divs = YetaWF_Basics.getElementsBySelector('.yUnified[data-pane]');
                // build data context (like scripts, css files we have)
                var data = {
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
                if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.SkinDynamicContent) {
                    data.UnifiedSkinCollection = YVolatile.Basics.UnifiedSkinCollection;
                    data.UnifiedSkinFileName = YVolatile.Basics.UnifiedSkinName;
                }
                for (var _i = 0, divs_1 = divs; _i < divs_1.length; _i++) {
                    var div = divs_1[_i];
                    data.Panes.push(div.getAttribute('data-pane'));
                }
                var css = YetaWF_Basics.getElementsBySelector('link[rel="stylesheet"][data-name]');
                for (var _a = 0, css_1 = css; _a < css_1.length; _a++) {
                    var c = css_1[_a];
                    data.KnownCss.push(c.getAttribute('data-name'));
                }
                css = YetaWF_Basics.getElementsBySelector('style[type="text/css"][data-name]');
                for (var _b = 0, css_2 = css; _b < css_2.length; _b++) {
                    var c = css_2[_b];
                    data.KnownCss.push(c.getAttribute('data-name'));
                }
                data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []); // add known css files that were added via bundles
                var scripts = YetaWF_Basics.getElementsBySelector('script[src][data-name]');
                for (var _c = 0, scripts_1 = scripts; _c < scripts_1.length; _c++) {
                    var s = scripts_1[_c];
                    data.KnownScripts.push(s.getAttribute('data-name'));
                }
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic || []); // known javascript files that were added by content pages
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles || []); // add known javascript files that were added via bundles
                YetaWF_Basics.setLoading();
                var request = new XMLHttpRequest();
                request.open('POST', '/YetaWF_Core/PageContent/Show' + uri.getQuery(true), true);
                request.setRequestHeader("Content-Type", "application/json");
                request.setRequestHeader("X-HTTP-Method-Override", "GET"); // server has to think this is a GET request so all actions that are invoked actually work
                request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                request.onreadystatechange = function (ev) {
                    if (request.readyState === 4 /*DONE*/) {
                        YetaWF_Basics.setLoading(false);
                        if (request.status === 200) {
                            var result = JSON.parse(request.responseText);
                            _this.processReceivedContent(result, uri, divs, setState, popupCB);
                        }
                        else {
                            YetaWF_Basics.setLoading(false);
                            YetaWF_Basics.alert(YLocs.Forms.AjaxError.format(request.status, request.statusText), YLocs.Forms.AjaxErrorTitle);
                            debugger;
                        }
                    }
                };
                request.send(JSON.stringify(data));
                return true;
            }
            else {
                // check if we have anything with that path as a unified pane and activate the panes
                var divs = YetaWF_Basics.getElementsBySelector(".yUnified[data-url=\"" + path + "\"]");
                if (divs.length > 0) {
                    this.closemenus();
                    // Update the browser address bar with the new path
                    if (setState) {
                        try {
                            var stateObj = {};
                            history.pushState(stateObj, "", uri.toUrl());
                        }
                        catch (err) { }
                    }
                    if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.HideDivs) {
                        // hide all unified sections
                        var uni = YetaWF_Basics.getElementsBySelector('.yUnified');
                        for (var _d = 0, uni_1 = uni; _d < uni_1.length; _d++) {
                            var u = uni_1[_d];
                            u.style.display = "none";
                        }
                        // show all unified sections that are on the current page
                        for (var _e = 0, divs_2 = divs; _e < divs_2.length; _e++) {
                            var d = divs_2[_e];
                            d.style.display = "block";
                        }
                        // send event that a new sections became active/visible
                        YetaWF_Basics.processActivateDivs(divs);
                        // scroll
                        var scrolled = YetaWF_Basics.setScrollPosition();
                        if (!scrolled) {
                            $(window).scrollLeft(0);
                            $(window).scrollTop(0);
                        }
                        YetaWF_Basics.setFocus();
                    }
                    else if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.ShowDivs) {
                        //element.scrollIntoView() as an alternative (check compatibility/options)
                        // calculate an approximate animation time so the shorter the distance, the shorter the animation
                        var h = $('body').height();
                        var t = $(divs[0]).offset().top; //$$$
                        var anim = YVolatile.Basics.UnifiedAnimation * t / h;
                        $('body,html').animate({
                            scrollTop: t
                        }, anim);
                    }
                    else
                        throw "Invalid UnifiedMode " + YVolatile.Basics.UnifiedMode;
                    YetaWF_Basics.setLoading(false);
                    return true;
                }
                //YetaWF_Basics.setLoading(false); // don't hide, let new page take over
                return false;
            }
        };
        ;
        Content.prototype.processReceivedContent = function (result, uri, divs, setState, popupCB) {
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
                }
                else {
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
            var _loop_1 = function (urlEntry) {
                found = result.CssFilesPayload.filter(function (elem) { return elem.Name == urlEntry.Name; });
                if (found.length > 0) {
                    elem = YetaWF_Basics.createElement("style", { type: 'text/css', "data-name": found[0].Name }, found[0].Text);
                    if (YVolatile.Basics.CssLocation === YetaWF.CssLocationEnum.Top) {
                        document.head.appendChild(elem);
                    }
                    else {
                        document.body.appendChild(elem);
                    }
                }
                else {
                    elem = YetaWF_Basics.createElement("link", { rel: "stylesheet", type: 'text/css', "data-name": urlEntry.Name, href: urlEntry.Url });
                    if (YVolatile.Basics.CssLocation === YetaWF.CssLocationEnum.Top) {
                        document.head.appendChild(elem);
                    }
                    else {
                        document.body.appendChild(elem);
                    }
                }
            };
            var found, elem, elem;
            // add all new css files
            for (var _i = 0, _a = result.CssFiles; _i < _a.length; _i++) {
                var urlEntry = _a[_i];
                _loop_1(urlEntry);
            }
            YVolatile.Basics.UnifiedCssBundleFiles = YVolatile.Basics.UnifiedCssBundleFiles || [];
            YVolatile.Basics.UnifiedCssBundleFiles.concat(result.CssBundleFiles || []);
            // add all new script files
            this.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, function () {
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
                        }
                        catch (err) { }
                    }
                    // remove all pane contents
                    for (var _i = 0, divs_3 = divs; _i < divs_3.length; _i++) {
                        var div = divs_3[_i];
                        YetaWF_Basics.processClearDiv(div);
                        div.innerHTML = '';
                        if (div.getAttribute("data-conditional")) {
                            div.style.display = "none"; // hide, it's a conditional pane
                        }
                    }
                    // Notify that the page is changing
                    YetaWF_Basics.processPageChange();
                    // remove prior page css classes
                    YetaWF_Basics.elementRemoveClasses(document.body, document.body.getAttribute('data-pagecss'));
                    // add new css classes
                    YetaWF_Basics.elementAddClasses(document.body, result.PageCssClasses);
                    document.body.setAttribute('data-pagecss', result.PageCssClasses); // remember so we can remove them for the next page
                }
                var tags = []; // collect all panes
                if (!popupCB) {
                    // add pane content
                    for (var _a = 0, _b = result.Content; _a < _b.length; _a++) {
                        var content = _b[_a];
                        // replace the pane
                        var pane = YetaWF_Basics.getElement1BySelector(".yUnified[data-pane=\"" + content.Pane + "\"]");
                        pane.style.display = "block"; // show in case this is a conditional pane
                        // add pane (can contain mixed html/scripts)
                        YetaWF_Basics.appendMixedHTML(pane, content.HTML);
                        // run all registered initializations for the pane
                        tags.push(pane);
                    }
                }
                else {
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
                }
                catch (e) { }
                YetaWF_Basics.processNewPage(uri.toUrl());
                // done, set focus
                YetaWF_Basics.setFocus(tags);
                YetaWF_Basics.setLoading(false);
            });
        };
        //$$$$ MOVE THIS TO COMPONENTSHTML
        Content.prototype.closemenus = function () {
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
                $('.YetaWF_Menus').collapse('hide'); //$$$$
            }
            catch (e) { }
        };
        return Content;
    }());
    YetaWF.Content = Content;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Content.js.map
