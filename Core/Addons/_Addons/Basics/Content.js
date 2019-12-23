"use strict";
/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// Anchor handling, navigation
var YetaWF;
(function (YetaWF) {
    var Content = /** @class */ (function () {
        function Content() {
        }
        // loads all scripts - we need to preserve the order of initialization hence the recursion
        Content.prototype.loadScripts = function (scripts, payload, run) {
            YVolatile.Basics.KnownScriptsDynamic = YVolatile.Basics.KnownScriptsDynamic || [];
            var total = scripts.length;
            if (total === 0) {
                run();
                return;
            }
            this.loadNextScript(scripts, payload, total, 0, run);
        };
        Content.prototype.loadNextScript = function (scripts, payload, total, ix, run) {
            var _this = this;
            var urlEntry = scripts[ix];
            var name = urlEntry.Name;
            var found = payload.filter(function (elem) { return elem.Name === name; });
            if (found.length > 0) {
                $YetaWF.runGlobalScript(found[0].Text);
                YVolatile.Basics.KnownScriptsDynamic.push(name); // save as dynamically loaded script
                this.processScript(scripts, payload, total, ix, run);
            }
            else {
                var loaded;
                var js = document.createElement("script");
                js.type = "text/javascript";
                js.async = false; // need to preserve execution order
                js.src = urlEntry.Url;
                js.setAttribute("data-name", name);
                js.onload = js.onerror = js["onreadystatechange"] = function (ev) {
                    if ((js["readyState"] && !(/^c|loade/.test(js["readyState"]))) || loaded)
                        return;
                    js.onload = js["onreadystatechange"] = null;
                    loaded = true;
                    _this.processScript(scripts, payload, total, ix, run);
                };
                if (YVolatile.Basics.JSLocation === YetaWF.JSLocationEnum.Top) { // location doesn't really matter, but done for consistency
                    var head = document.getElementsByTagName("head")[0];
                    head.insertBefore(js, head.lastChild);
                }
                else {
                    var body = document.getElementsByTagName("body")[0];
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
        /**
         * Changes the current page to the specified Uri (may not be part of a unified page set). If a Unified Page Set is active,
         * the page is incrementally updated, otherwise the entire page is loaded.
         * @param uri The new page.
         */
        Content.prototype.setNewUri = function (uri) {
            if (!$YetaWF.ContentHandling.setContent(uri, true))
                window.location.assign(uri.toUrl());
        };
        /**
         * Changes the current page to the specified Uri (may not be part of a unified page set).
         * Returns false if the uri couldn't be processed (i.e., it's not part of a unified page set).
         * Returns true if the page is now shown and is part of the unified page set.
         * @param uriRequested The new page.
         * @param setState Defines whether the browser's history should be updated.
         * @param popupCB A callback to process popup content. May be null.
         */
        Content.prototype.setContent = function (uriRequested, setState, popupCB, inplace) {
            var _this = this;
            if (YVolatile.Basics.EditModeActive)
                return false; // edit mode
            if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.None)
                return false; // not unified mode
            if (popupCB) {
                if (YVolatile.Basics.UnifiedMode !== YetaWF.UnifiedModeEnum.DynamicContent && YVolatile.Basics.UnifiedMode !== YetaWF.UnifiedModeEnum.SkinDynamicContent)
                    return false; // popups can only be used with some unified modes
                if (!YVolatile.Basics.UnifiedPopups)
                    return false; // popups not wanted for this UPS
            }
            // check if we're clicking a link which is part of this unified page
            var uri;
            if (inplace)
                uri = $YetaWF.parseUrl(inplace.ContentUrl);
            else
                uri = uriRequested;
            var path = uri.getPath();
            if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.DynamicContent || YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.SkinDynamicContent) {
                var divs;
                if (inplace)
                    divs = $YetaWF.getElementsBySelector("." + inplace.FromPane + ".yUnified[data-pane]"); // only requested pane
                else
                    divs = $YetaWF.getElementsBySelector(".yUnified[data-pane]"); // all panes
                if (divs.length === 0)
                    throw "No panes support dynamic content";
                // build data context (like scripts, css files we have)
                var data = {
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
                if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.SkinDynamicContent) {
                    data.UnifiedSkinCollection = YVolatile.Basics.UnifiedSkinCollection;
                    data.UnifiedSkinFileName = YVolatile.Basics.UnifiedSkinName;
                }
                for (var _i = 0, divs_1 = divs; _i < divs_1.length; _i++) {
                    var div = divs_1[_i];
                    data.Panes.push(div.getAttribute("data-pane"));
                }
                var css = $YetaWF.getElementsBySelector("link[rel=\"stylesheet\"][data-name]");
                for (var _a = 0, css_1 = css; _a < css_1.length; _a++) {
                    var c = css_1[_a];
                    data.KnownCss.push(c.getAttribute("data-name"));
                }
                css = $YetaWF.getElementsBySelector("style[type=\"text/css\"][data-name]");
                for (var _b = 0, css_2 = css; _b < css_2.length; _b++) {
                    var c = css_2[_b];
                    data.KnownCss.push(c.getAttribute("data-name"));
                }
                data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []); // add known css files that were added via bundles
                var scripts = $YetaWF.getElementsBySelector("script[src][data-name]");
                for (var _c = 0, scripts_1 = scripts; _c < scripts_1.length; _c++) {
                    var s = scripts_1[_c];
                    data.KnownScripts.push(s.getAttribute("data-name"));
                }
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic || []); // known javascript files that were added by content pages
                data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles || []); // add known javascript files that were added via bundles
                $YetaWF.setLoading();
                var request = new XMLHttpRequest();
                request.open("POST", "/YetaWF_Core/PageContent/Show" + uri.getQuery(true), true);
                request.setRequestHeader("Content-Type", "application/json");
                request.setRequestHeader("X-HTTP-Method-Override", "GET"); // server has to think this is a GET request so all actions that are invoked actually work
                request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                request.onreadystatechange = function (ev) {
                    if (request.readyState === 4 /*DONE*/) {
                        $YetaWF.setLoading(false);
                        if (request.status === 200) {
                            var result = JSON.parse(request.responseText);
                            _this.processReceivedContent(result, uri, divs, setState, popupCB, inplace);
                        }
                        else {
                            $YetaWF.setLoading(false);
                            $YetaWF.alert(YLocs.Forms.AjaxError.format(request.status, request.statusText), YLocs.Forms.AjaxErrorTitle);
                            // tslint:disable-next-line:no-debugger
                            debugger;
                        }
                    }
                };
                request.send(JSON.stringify(data));
                return true;
            }
            else {
                // check if we have anything with that path as a unified pane and activate the panes
                var divs = $YetaWF.getElementsBySelector(".yUnified[data-url=\"" + path + "\"]");
                if (divs.length > 0) {
                    $YetaWF.closeOverlays();
                    // Update the browser address bar with the new path
                    if (setState) //$$$inplace
                        $YetaWF.setUrl(uri.toUrl());
                    if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.HideDivs) {
                        // hide all unified sections
                        var uni = $YetaWF.getElementsBySelector(".yUnified");
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
                        $YetaWF.processActivateDivs(divs);
                        // scroll
                        var scrolled = $YetaWF.setScrollPosition();
                        if (!scrolled)
                            window.scroll(0, 0);
                        $YetaWF.setFocus();
                    }
                    else if (YVolatile.Basics.UnifiedMode === YetaWF.UnifiedModeEnum.ShowDivs) {
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
                    }
                    else
                        throw "Invalid UnifiedMode " + YVolatile.Basics.UnifiedMode;
                    $YetaWF.setLoading(false);
                    return true;
                }
                //$YetaWF.setLoading(false); // don't hide, let new page take over
                return false;
            }
        };
        Content.prototype.processReceivedContent = function (result, uri, divs, setState, popupCB, inplace) {
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
                }
                else {
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
            var _loop_1 = function (urlEntry) {
                found = result.CssFilesPayload.filter(function (elem) { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    elem = $YetaWF.createElement("style", { type: "text/css", "data-name": found[0].Name }, found[0].Text);
                    if (YVolatile.Basics.CssLocation === YetaWF.CssLocationEnum.Top) {
                        document.head.appendChild(elem);
                    }
                    else {
                        document.body.appendChild(elem);
                    }
                }
                else {
                    elem = $YetaWF.createElement("link", { rel: "stylesheet", type: "text/css", "data-name": urlEntry.Name, href: urlEntry.Url });
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
                var tags = []; // collect all panes
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
                        var target = $YetaWF.getElementById(inplace.TargetTag);
                        $YetaWF.processClearDiv(target);
                        target.innerHTML = "";
                    }
                    else {
                        for (var _i = 0, divs_3 = divs; _i < divs_3.length; _i++) {
                            var div = divs_3[_i];
                            $YetaWF.processClearDiv(div);
                            div.innerHTML = "";
                            if (div.getAttribute("data-conditional")) {
                                div.style.display = "none"; // hide, it's a conditional pane
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
                        document.body.setAttribute("data-pagecss", result.PageCssClasses); // remember so we can remove them for the next page
                    }
                    // add pane content
                    if (inplace) {
                        if (result.Content.length > 0) {
                            var pane = $YetaWF.getElementById(inplace.TargetTag);
                            $YetaWF.appendMixedHTML(pane, result.Content[0].HTML);
                            tags.push(pane); // run all registered initializations for the pane
                        }
                    }
                    else {
                        for (var _a = 0, _b = result.Content; _a < _b.length; _a++) {
                            var content = _b[_a];
                            // replace the pane
                            var pane = $YetaWF.getElement1BySelector(".yUnified[data-pane=\"" + content.Pane + "\"]");
                            pane.style.display = "block"; // show in case this is a conditional pane
                            // add pane (can contain mixed html/scripts)
                            $YetaWF.appendMixedHTML(pane, content.HTML);
                            // run all registered initializations for the pane
                            tags.push(pane);
                        }
                    }
                }
                else {
                    popupCB(result, function (dialog) {
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
                YVolatile.Basics.UnifiedAddonModsPrevious.forEach(function (guid) {
                    if (YVolatile.Basics.UnifiedAddonMods.indexOf(guid) < 0)
                        $YetaWF.processContentChange(guid, false);
                });
                // turn on all newly active modules (if they were previously loaded)
                // new referenced modules that were just loaded now are already active and don't need to be called
                YVolatile.Basics.UnifiedAddonMods.forEach(function (guid) {
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
                    if (!scrolled) {
                        window.scroll(0, 0);
                        if (inplace) {
                            var pane = $YetaWF.getElementById(inplace.TargetTag);
                            pane.scroll(0, 0);
                        }
                    }
                    // in case there is a popup open, close it now (typically when returning to the page from a popup)
                    if ($YetaWF.PopupsAvailable())
                        $YetaWF.Popups.closeInnerPopup();
                }
                try {
                    $YetaWF.runGlobalScript(result.AnalyticsContent);
                }
                catch (e) { }
                $YetaWF.processNewPage(uri.toUrl());
                // done, set focus
                setTimeout(function () {
                    $YetaWF.setFocus(tags);
                }, 1);
                $YetaWF.setLoading(false);
            });
        };
        Content.prototype.loadAddons = function (addons, run) {
            var _this = this;
            // build data context (like scripts, css files we have)
            var data = {
                Addons: addons,
                KnownCss: [],
                KnownScripts: []
            };
            var css = $YetaWF.getElementsBySelector("link[rel=\"stylesheet\"][data-name]");
            for (var _i = 0, css_3 = css; _i < css_3.length; _i++) {
                var c = css_3[_i];
                data.KnownCss.push(c.getAttribute("data-name"));
            }
            css = $YetaWF.getElementsBySelector("style[type=\"text/css\"][data-name]");
            for (var _a = 0, css_4 = css; _a < css_4.length; _a++) {
                var c = css_4[_a];
                data.KnownCss.push(c.getAttribute("data-name"));
            }
            data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles || []); // add known css files that were added via bundles
            var scripts = $YetaWF.getElementsBySelector("script[src][data-name]");
            for (var _b = 0, scripts_2 = scripts; _b < scripts_2.length; _b++) {
                var s = scripts_2[_b];
                data.KnownScripts.push(s.getAttribute("data-name"));
            }
            data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic || []); // known javascript files that were added by content pages
            data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles || []); // add known javascript files that were added via bundles
            $YetaWF.setLoading();
            this.getAddonsData("/YetaWF_Core/AddonContent/ShowAddons", data)
                .then(function (data) {
                _this.processReceivedAddons(data, run);
            })
                .catch(function (error) { return alert(error.message); });
        };
        Content.prototype.getAddonsData = function (url, data) {
            var p = new Promise(function (resolve, reject) {
                var request = new XMLHttpRequest();
                request.open("POST", url, true);
                request.setRequestHeader("Content-Type", "application/json");
                request.setRequestHeader("X-HTTP-Method-Override", "GET"); // server has to think this is a GET request so all actions that are invoked actually work
                request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
                request.onreadystatechange = function (ev) {
                    if (request.readyState === 4 /*DONE*/) {
                        $YetaWF.setLoading(false);
                        if (request.status === 200) {
                            resolve(JSON.parse(request.responseText));
                        }
                        else {
                            reject(new Error(YLocs.Forms.AjaxError.format(request.status, request.statusText)));
                            // tslint:disable-next-line:no-debugger
                            debugger;
                        }
                    }
                };
                request.send(JSON.stringify(data));
            });
            return p;
        };
        Content.prototype.processReceivedAddons = function (result, run) {
            if (result.Status != null && result.Status.length > 0) {
                alert(result.Status);
                return;
            }
            // run all global scripts (YConfigs, etc.)
            $YetaWF.runGlobalScript(result.Scripts);
            var _loop_2 = function (urlEntry) {
                found = result.CssFilesPayload.filter(function (elem) { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    elem = $YetaWF.createElement("style", { type: "text/css", "data-name": found[0].Name }, found[0].Text);
                    document.body.appendChild(elem);
                }
                else {
                    elem = $YetaWF.createElement("link", { rel: "stylesheet", type: "text/css", "data-name": urlEntry.Name, href: urlEntry.Url });
                    document.body.appendChild(elem);
                }
            };
            var found, elem, elem;
            // add all new css files
            for (var _i = 0, _a = result.CssFiles; _i < _a.length; _i++) {
                var urlEntry = _a[_i];
                _loop_2(urlEntry);
            }
            YVolatile.Basics.UnifiedCssBundleFiles = YVolatile.Basics.UnifiedCssBundleFiles || [];
            YVolatile.Basics.UnifiedCssBundleFiles.concat(result.CssBundleFiles || []);
            // add all new script files
            this.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, function () {
                YVolatile.Basics.UnifiedScriptBundleFiles = YVolatile.Basics.UnifiedScriptBundleFiles || [];
                YVolatile.Basics.UnifiedScriptBundleFiles.concat(result.ScriptBundleFiles || []);
                // end of page scripts
                $YetaWF.runGlobalScript(result.EndOfPageScripts);
                run();
            });
        };
        Content.prototype.init = function () {
        };
        return Content;
    }());
    YetaWF.Content = Content;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Content.js.map
