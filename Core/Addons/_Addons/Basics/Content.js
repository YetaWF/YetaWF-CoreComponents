"use strict";
/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// Anchor handling, navigation
var YetaWF;
(function (YetaWF) {
    var SetContentResult;
    (function (SetContentResult) {
        SetContentResult[SetContentResult["NotContent"] = 0] = "NotContent";
        SetContentResult[SetContentResult["ContentReplaced"] = 1] = "ContentReplaced";
        SetContentResult[SetContentResult["Abort"] = 2] = "Abort";
    })(SetContentResult = YetaWF.SetContentResult || (YetaWF.SetContentResult = {}));
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
                var loaded_1 = false;
                var js_1 = document.createElement("script");
                js_1.type = "text/javascript";
                js_1.async = false; // need to preserve execution order
                js_1.src = urlEntry.Url;
                // eslint-disable-next-line guard-for-in
                for (var attr in urlEntry.Attributes)
                    $YetaWF.setAttribute(js_1, attr, urlEntry.Attributes[attr]);
                js_1.setAttribute("data-name", name);
                js_1.onload = js_1.onerror = js_1["onreadystatechange"] = function (ev) {
                    if ((js_1["readyState"] && !(/^c|loade/.test(js_1["readyState"]))) || loaded_1)
                        return;
                    js_1.onload = js_1["onreadystatechange"] = null;
                    loaded_1 = true;
                    _this.processScript(scripts, payload, total, ix, run);
                };
                if (YVolatile.Basics.JSLocation === YetaWF.JSLocationEnum.Top) { // location doesn't really matter, but done for consistency
                    var head = document.getElementsByTagName("head")[0];
                    head.insertBefore(js_1, head.lastChild);
                }
                else {
                    var body = document.getElementsByTagName("body")[0];
                    body.insertBefore(js_1, body.lastChild);
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
            if ($YetaWF.ContentHandling.setContent(uri, true) === SetContentResult.NotContent)
                window.location.assign(uri.toUrl());
        };
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
        Content.prototype.setContent = function (uriRequested, setState, popupCB, inplace, contentCB) {
            if (!this.allowNavigateAway()) {
                $YetaWF.sendCustomEvent(document.body, Content.EVENTNAVCANCEL);
                return SetContentResult.Abort;
            }
            return this.setContentForce(uriRequested, setState, popupCB, inplace, contentCB);
        };
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
        Content.prototype.setContentForce = function (uriRequested, setState, popupCB, inplace, contentCB) {
            var _this = this;
            if (YVolatile.Basics.EditModeActive)
                return SetContentResult.NotContent; // edit mode
            // check if we're clicking a link which is part of this unified page
            var uri;
            if (inplace)
                uri = $YetaWF.parseUrl(inplace.ContentUrl);
            else
                uri = uriRequested;
            var path = uri.getPath();
            var divs;
            if (inplace)
                divs = $YetaWF.getElementsBySelector(".".concat(inplace.FromPane, ".yUnified[data-pane]")); // only requested pane
            else
                divs = $YetaWF.getElementsBySelector(".yUnified[data-pane]"); // all panes
            if (divs.length === 0) // can occur in popups while in edit mode
                return SetContentResult.NotContent; // edit mode
            // build data context (like scripts, css files we have)
            var data = {
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
            request.open("POST", "".concat(YConfigs.Basics.ApiPrefix, "/YetaWF_Core/PageContent/Show") + uri.getQuery(true), true);
            request.setRequestHeader("Content-Type", "application/json");
            request.setRequestHeader("X-HTTP-Method-Override", "GET"); // server has to think this is a GET request so all actions that are invoked actually work
            request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            request.onreadystatechange = function (ev) {
                if (request.readyState === XMLHttpRequest.DONE) {
                    $YetaWF.setLoading(false);
                    if (request.status === 200) {
                        var result = JSON.parse(request.responseText);
                        _this.processReceivedContent(result, uri, divs, setState, popupCB, inplace, contentCB);
                    }
                    else if (request.status === 0) {
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, YLocs.Forms.AjaxConnLost), YLocs.Forms.AjaxErrorTitle);
                        return SetContentResult.NotContent;
                    }
                    else {
                        $YetaWF.setLoading(false);
                        $YetaWF.error(YLocs.Forms.AjaxError.format(request.status, request.statusText), YLocs.Forms.AjaxErrorTitle);
                        // eslint-disable-next-line no-debugger
                        debugger;
                    }
                }
            };
            request.send(JSON.stringify(data));
            return SetContentResult.ContentReplaced;
        };
        Content.prototype.allowNavigateAway = function () {
            return !$YetaWF.pageChanged || confirm("Changes to this page have not yet been saved. Are you sure you want to navigate away from this page without saving?");
        };
        Content.prototype.processReceivedContent = function (result, uri, divs, setState, popupCB, inplace, contentCB) {
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
                }
                else {
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
            var _loop_1 = function (urlEntry) {
                var found = result.CssFilesPayload.filter(function (elem) { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    var elem = $YetaWF.createElement("style", { type: "text/css", "data-name": found[0].Name }, found[0].Text);
                    if (YVolatile.Basics.CssLocation === YetaWF.CssLocationEnum.Top) {
                        document.head.appendChild(elem);
                    }
                    else {
                        document.body.appendChild(elem);
                    }
                }
                else {
                    var elem = $YetaWF.createElement("link", { rel: "stylesheet", type: "text/css", "data-name": urlEntry.Name, href: urlEntry.Url });
                    if (YVolatile.Basics.CssLocation === YetaWF.CssLocationEnum.Top) {
                        document.head.appendChild(elem);
                    }
                    else {
                        document.body.appendChild(elem);
                    }
                }
            };
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
                            $YetaWF.pushUrl(inplace.PageUrl);
                        else
                            $YetaWF.pushUrl(uri.toUrl());
                    }
                    // remove all pane contents
                    if (inplace) {
                        var target = $YetaWF.getElementById(inplace.TargetTag);
                        $YetaWF.processClearDiv(target);
                        target.innerHTML = "";
                    }
                    else {
                        for (var _i = 0, divs_2 = divs; _i < divs_2.length; _i++) {
                            var div = divs_2[_i];
                            $YetaWF.processClearDiv(div);
                            div.innerHTML = "";
                        }
                    }
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
                            var pane = $YetaWF.getElement1BySelector(".yUnified[data-pane=\"".concat(content.Pane, "\"]"));
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
                        $YetaWF.sendAddonChangedEvent(guid, false);
                });
                // turn on all newly active modules (if they were previously loaded)
                // new referenced modules that were just loaded now are already active and don't need to be called
                YVolatile.Basics.UnifiedAddonMods.forEach(function (guid) {
                    if (YVolatile.Basics.UnifiedAddonModsPrevious.indexOf(guid) < 0 && $YetaWF.UnifiedAddonModsLoaded.indexOf(guid) >= 0)
                        $YetaWF.sendAddonChangedEvent(guid, true);
                    if ($YetaWF.UnifiedAddonModsLoaded.indexOf(guid) < 0)
                        $YetaWF.UnifiedAddonModsLoaded.push(guid);
                });
                YVolatile.Basics.UnifiedAddonModsPrevious = YVolatile.Basics.UnifiedAddonMods;
                YVolatile.Basics.UnifiedAddonMods = [];
                // call ready handlers
                $YetaWF.processAllReadyOnce(tags);
                $YetaWF.sendCustomEvent(document.body, Content.EVENTNAVPAGELOADED, { containers: tags });
                if (!popupCB) {
                    // scroll
                    var scrolled = $YetaWF.setScrollPosition();
                    if (!scrolled) {
                        window.scroll(0, 0);
                        if (inplace) {
                            var pane = $YetaWF.getElementById(inplace.TargetTag);
                            try { // ignore errors on crappy browsers
                                pane.scroll(0, 0);
                            }
                            catch (e) { }
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
                // locate the hash if there is one
                var setFocus = true;
                var hash = uri.getHash();
                if (hash) {
                    var target = null;
                    try { // handle invalid id
                        target = $YetaWF.getElement1BySelectorCond("#".concat(hash));
                    }
                    catch (e) { }
                    if (target) {
                        target.scrollIntoView();
                        setFocus = false;
                    }
                }
                // done, set focus
                if (setFocus) {
                    setTimeout(function () {
                        $YetaWF.setFocus(tags);
                    }, 1);
                }
                $YetaWF.setLoading(false);
            });
            if (contentCB)
                contentCB(result);
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
            this.getAddonsData("".concat(YConfigs.Basics.ApiPrefix, "/YetaWF_Core/AddonContent/ShowAddons"), data)
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
                    if (request.readyState === XMLHttpRequest.DONE) {
                        $YetaWF.setLoading(false);
                        if (request.status === 200) {
                            resolve(JSON.parse(request.responseText));
                        }
                        else {
                            reject(new Error(YLocs.Forms.AjaxError.format(request.status, request.statusText)));
                            // eslint-disable-next-line no-debugger
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
                var found = result.CssFilesPayload.filter(function (elem) { return elem.Name === urlEntry.Name; });
                if (found.length > 0) {
                    var elem = $YetaWF.createElement("style", { type: "text/css", "data-name": found[0].Name }, found[0].Text);
                    document.body.appendChild(elem);
                }
                else {
                    var elem = $YetaWF.createElement("link", { rel: "stylesheet", type: "text/css", "data-name": urlEntry.Name, href: urlEntry.Url });
                    document.body.appendChild(elem);
                }
            };
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
        Content.EVENTNAVCANCEL = "content_navcancel";
        Content.EVENTNAVPAGELOADED = "content_navpageloaded";
        return Content;
    }());
    YetaWF.Content = Content;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Content.js.map
