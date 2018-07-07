/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var _YetaWF_Basics = {};

function Y_ReloadWindowPage(w, keepPosition) {
    'use strict';

    var uri = new URI(w.location.href);
    uri.removeSearch(YGlobals.Link_ScrollLeft);
    uri.removeSearch(YGlobals.Link_ScrollTop);
    if (keepPosition == true) {
        var v = $(w).scrollLeft();
        if (v != 0)
            uri.addSearch(YGlobals.Link_ScrollLeft, v);
        v = $(w).scrollTop();
        if (v != 0)
            uri.addSearch(YGlobals.Link_ScrollTop, v);
    }
    uri.removeSearch("!rand");
    uri.addSearch("!rand", (new Date()).getTime());// cache buster

    if (YVolatile.Basics.UnifiedMode != 0) {
        if (_YetaWF_Basics.setContent(uri, true))
            return;
    }
    if (keepPosition == true) {
        w.location.assign(uri.toString());
        return;
    }
    w.location.reload(true);
}

function Y_ReloadPage(keepPosition) {
    Y_ReloadWindowPage(window, keepPosition);
}

function Y_ReloadModule(tag) {
    'use strict';
    if (tag == undefined) tag = YetaWF_Basics.reloadingModule_TagInModule;
    var $mod = YetaWF_Basics.getModuleFromTag(tag);
    if ($mod.length == 0) throw "No module found";/*DEBUG*/
    var $form = $('form', $mod);
    if ($mod.length == 0) throw "No form found";/*DEBUG*/
    YetaWF_Forms.submit($form, false, YGlobals.Link_SubmitIsApply + "=y");// the form must support a simple Apply
}

// PANES
// PANES
// PANES

YetaWF_Basics.showPaneSet = function (id, editMode, equalHeights) {
    'use strict';
    var $div = $('#{0}'.format(id));// the pane
    var shown = false;
    if (editMode) {
        $div.show();
        shown = true;
    } else {
        // show the pane if it has modules
        if ($('div.yModule', $div).length > 0) {
            $div.show();
            shown = true;
        }
    }
    if (shown && equalHeights) {
        // make all panes the same height
        // this should happen late in case the content is changed dynamically (use with caution)
        // if it does, the pane will still expand because we're only setting the minimum height
        $(document).ready(function () {
            var $panes = $('#{0} > div:visible'.format(id));// get all immediate child divs (i.e., the panes)
            $panes = $panes.not('.y_cleardiv');
            var height = 0;
            // calc height
            $panes.each(function () {
                var h = $(this).height();
                if (h > height)
                    height = h;
            });
            // set each pane's height
            $panes.css('min-height', height);
        });
    }
}

// PAGE/MODULE REFRESH
// PAGE/MODULE REFRESH
// PAGE/MODULE REFRESH

// Usage:
// reloadInfo.push({
//   module: $mod,              // module <div> to be refreshed
//   callback: function() {}    // function to be called
// });

YetaWF_Basics.reloadInfo = [];

YetaWF_Basics.getModuleFromTag_Cond = function (t) {
    var $t = $(t);
    if ($t.length != 1) { debugger; throw "Invalid tag"; }/*DEBUG*/
    var $mod = $t.closest('.yModule');
    if ($mod.length == 0) return null;
    return $mod;
};
YetaWF_Basics.getModuleFromTag = function (t) {
    var $mod = YetaWF_Basics.getModuleFromTag_Cond(t);
    if ($mod == null || $mod.length != 1) { debugger; throw "Can't find containing module"; }/*DEBUG*/
    return $mod;
};

YetaWF_Basics.getModuleGuidFromTag = function (t) {
    var $t = $(t);
    if ($t.length != 1) { debugger; throw "Invalid tag"; }/*DEBUG*/
    var $mod = $t.closest('.yModule');
    if ($mod.length != 1) { debugger; throw "Can't find containing module"; }/*DEBUG*/
    var guid = $mod.attr('data-moduleguid');
    if (guid == undefined || guid == "") throw "Can't find module guid";/*DEBUG*/
    return guid;
};

YetaWF_Basics.refreshModule = function ($mod) {
    for (var entry in YetaWF_Basics.reloadInfo) {
        if (YetaWF_Basics.reloadInfo[entry].module == $mod) {
            YetaWF_Basics.reloadInfo[entry].callback();
        }
    }
};
YetaWF_Basics.refreshModuleByAnyTag = function (t) {
    var $mod = YetaWF_Basics.getModuleFromTag(t);
    for (var entry in YetaWF_Basics.reloadInfo) {
        if (YetaWF_Basics.reloadInfo[entry].module[0].id == $mod[0].id) {
            YetaWF_Basics.reloadInfo[entry].callback();
        }
    }
};
YetaWF_Basics.refreshPage = function () {
    for (var entry in YetaWF_Basics.reloadInfo) {
        YetaWF_Basics.reloadInfo[entry].callback();
    }
};

// AJAX RETURN
// AJAX RETURN
// AJAX RETURN

YetaWF_Basics.reloadingModule_TagInModule = null;

YetaWF_Basics.processAjaxReturn = function (result, textStatus, jqXHR, tagInModule, onSuccess, onHandleResult) {
    'use strict';
    YetaWF_Basics.reloadingModule_TagInModule = tagInModule;
    if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
        var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
        if (script.length == 0) { // all is well, but no script to execute
            if (onSuccess != undefined) {
                onSuccess();
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
        eval(script);// if this uses YetaWF_Basics.Y_Alert or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explictly reload page in your javascript)
        Y_ReloadPage(true);
        return true;
    } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
        var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
        eval(script);// if this uses YetaWF_Basics.Y_Alert or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explictly reload module in your javascript)
        Y_ReloadModule();
        return true;
    } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
        //if (!YetaWF_Basics.isInPopup()) throw "Not supported - only available within a popup";/*DEBUG*/
        var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
        eval(script);
        YetaWF_Basics.refreshModuleByAnyTag(tagInModule);
        return true;
    } else {
        if (onHandleResult != undefined) {
            onHandleResult(result);
        } else {
            YetaWF_Basics.Y_Error(YLocs.Basics.IncorrectServerResp);
        }
        return false;
    }
};

// CHARSIZE (from module or page/YVolatile)

YetaWF_Basics.getCharSizeFromTag = function ($t) {
    var width, height;
    var $mod = YetaWF_Basics.getModuleFromTag_Cond($t);
    if ($mod != null) {
        width = $mod.attr('data-charwidthavg');
        height = $mod.attr('data-charheight');
    } else {
        width = YVolatile.Basics.CharWidthAvg;
        height = YVolatile.Basics.CharHeight;
    }
    return { width: width, height: height };
}

// CONTENT
// CONTENT
// CONTENT

// loads all scripts - we need to preserve the order of initialization hence the recursion
_YetaWF_Basics.loadScripts = function (scripts, payload, run) {
    YVolatile.Basics.KnownScriptsDynamic = YVolatile.Basics.KnownScriptsDynamic || [];
    var total = scripts.length;
    if (total == 0) {
        run();
        return;
    }
    _YetaWF_Basics.loadNextScript(scripts, payload, total, 0, run);
};

_YetaWF_Basics.loadNextScript = function (scripts, payload, total, ix, run) {
    var urlEntry = scripts[ix];
    var name = urlEntry.Name;

    function process() {
        if (ix >= total - 1) {
            run();// we're all done
        } else {
            _YetaWF_Basics.loadNextScript(scripts, payload, total, ix + 1, run);
        }
    }
    var found = payload.filter(function (elem) { return elem.Name == name; });
    if (found.length > 0) {
        $.globalEval(found[0].Text);
        YVolatile.Basics.KnownScriptsDynamic.push(name);// save as dynamically loaded script
        process();
    } else {
        var loaded;
        var js = document.createElement('script');
        js.type = 'text/javascript';
        js.async = false; // need to preserve execution order
        js.src = urlEntry.Url;
        var dataName = document.createAttribute("data-name");
        dataName.value = name;
        js.setAttributeNode(dataName);
        js.onload = js.onerror = js['onreadystatechange'] = function () {
            if ((js['readyState'] && !(/^c|loade/.test(js['readyState']))) || loaded) return;
            js.onload = js['onreadystatechange'] = null;
            loaded = true;
            process();
        };
        if (YVolatile.Basics.JSLocation) {// location doesn't really matter, but done for consistency
            var head = document.getElementsByTagName('head')[0];
            head.insertBefore(js, head.lastChild)
        } else {
            var body = document.getElementsByTagName('body')[0];
            body.insertBefore(js, body.lastChild)
        }
    }
};

_YetaWF_Basics.UnifiedAddonModsLoaded = [];// currently loaded addons

// Change the current page to the specified Uri (may not be part of the unified page set)
// returns false if the uri couldn't be processed (i.e., it's not part of a unified page set)
// returns true if the page is now shown and is part of the unified page set
_YetaWF_Basics.setContent = function (uri, setState, popupCB) {
    'use strict';

    function closemenus() {
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
            $('.YetaWF_Menus').collapse('hide');
        } catch (e) { }
    }

    if (YVolatile.Basics.EditModeActive) return false; // edit mode
    if (YVolatile.Basics.UnifiedMode == 0) return false; // not unified mode
    if (popupCB !== undefined) {
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
        var data = {};
        data.CacheVersion = YVolatile.Basics.CacheVersion;
        data.Path = path;
        data.QueryString = uri.query();
        data.UnifiedSetGuid = YVolatile.Basics.UnifiedSetGuid;
        data.UnifiedMode = YVolatile.Basics.UnifiedMode;
        if (YVolatile.Basics.UnifiedMode === 4 /*UnifiedModeEnum.SkinDynamicContent*/) {
            data.UnifiedSkinCollection = YVolatile.Basics.UnifiedSkinCollection;
            data.UnifiedSkinFileName = YVolatile.Basics.UnifiedSkinName;
        }
        data.UnifiedAddonMods = _YetaWF_Basics.UnifiedAddonModsLoaded;// active addons
        data.UniqueIdPrefixCounter = YVolatile.Basics.UniqueIdPrefixCounter;
        data.IsMobile = YVolatile.Skin.MinWidthForPopups > window.outerWidth;
        data.Panes = [];
        $divs.each(function () {
            data.Panes.push($(this).attr('data-pane'));
        });
        data.KnownCss = [];
        var $css = $('link[rel="stylesheet"][data-name]');
        $css.each(function () {
            data.KnownCss.push($(this).attr('data-name'));
        });
        $css = $('style[type="text/css"][data-name]');
        $css.each(function () {
            data.KnownCss.push($(this).attr('data-name'));
        });
        data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles);// add known css files that were added via bundles
        data.KnownScripts = [];
        //var $scripts = $('script[type="text/javascript"][src][data-name]');
        var $scripts = $('script[src][data-name]');
        $scripts.each(function () {
            data.KnownScripts.push($(this).attr('data-name'));
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
            success: function (result, textStatus, jqXHR) {
                closemenus();
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
                    _YetaWF_Basics.setContent(new URI(result.RedirectContent), setState, popupCB);
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
                _YetaWF_Basics.loadScripts(result.ScriptFiles, result.ScriptFilesPayload, function () {
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
                            var $pane = $('.yUnified[data-pane="{0}"]'.format(result.Content[i].Pane));
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
                        if (YVolatile.Basics.UnifiedAddonModsPrevious.indexOf(guid) < 0 && _YetaWF_Basics.UnifiedAddonModsLoaded.indexOf(guid) >= 0)
                            $(document).trigger('YetaWF_Basics_Addon', [guid, true]);
                        if (_YetaWF_Basics.UnifiedAddonModsLoaded.indexOf(guid) < 0)
                            _YetaWF_Basics.UnifiedAddonModsLoaded.push(guid);
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
            error: function (jqXHR, textStatus, errorThrown) {
                YetaWF_Basics.setLoading(false);
                YetaWF_Basics.Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
                debugger;
            }
        });
        return true;
    } else {
        // check if we have anything with that path as a unified pane and activate the panes
        var $divs = $('.yUnified[data-url="{0}"]'.format(path));
        if ($divs.length > 0) {
            closemenus();
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
                var h = $('body').height();
                var t = $divs.eq(0).offset().top;
                var anim = YVolatile.Basics.UnifiedAnimation * t / h;
                $('body,html').animate({
                    scrollTop: t
                }, anim);
            } else
                throw "Invalid UnifiedMode {0}".format(YVolatile.Basics.UnifiedMode);
            YetaWF_Basics.setLoading(false);
            return true;
        }
        //YetaWF_Basics.setLoading(false); // don't hide, let new page take over
        return false;
    }
    return false;
};

// DOCUMENT READY
// DOCUMENT READY
// DOCUMENT READY

$(document).ready(function () {
    'use strict';

    // SKIN
    // SKIN
    // SKIN

    if (YVolatile.Skin == undefined) throw "SetSkinOptions call missing in page skin";/*DEBUG*/

    // <A> LINKS
    // <A> LINKS
    // <A> LINKS

    // <a> links that only have a hash are intercepted so we don't go through content handling
    $("body").on("click", "a[href^='#']", function (e) {
        YetaWF_Basics.suppressPopState = true;
    });

    // For an <a> link clicked, add the page we're coming from (not for popup links though)
    $("body").on("click", "a.yaction-link,area.yaction-link", function (e) {
        var $t = $(this);

        var uri = $t.uri();
        var url = $t[0].href;

        // send tracking info
        if ($t.hasClass('yTrack')) {
            // find the unique skinvisitor module so we have antiforgery tokens and other context info
            var $f = $('.YetaWF_Visitors_SkinVisitor.YetaWF_Visitors.yModule form');
            if ($f.length == 1) {
                var data = { 'url': url };
                var info = YetaWF_Forms.getFormInfo($f);
                data[YConfigs.Basics.ModuleGuid] = info.ModuleGuid;
                data[YConfigs.Forms.RequestVerificationToken] = info.RequestVerificationToken;
                data[YConfigs.Forms.UniqueIdPrefix] = info.UniqueIdPrefix;
                var urlTrack = $f.attr('data-track');
                if (urlTrack == undefined) throw "data-track not defined";/*DEBUG*/
                $.ajax({
                    'url': urlTrack,
                    'type': 'post',
                    'data': data,
                });
            }
        }

        if (uri.path().length == 0 || url.startsWith('javascript:') || url.startsWith('mailto:') || url.startsWith('tel:')) return true;

        // if we're on an edit page, propagate edit to new link unless the new uri explicitly has !Noedit
        if (!uri.hasSearch(YGlobals.Link_EditMode) && !uri.hasSearch(YGlobals.Link_NoEditMode)) {
            var currUri = new URI(window.location.href);
            if (currUri.hasSearch(YGlobals.Link_EditMode))
                uri.addSearch(YGlobals.Link_EditMode, 'y');
        }
        // add status/visibility of page control module
        uri.removeSearch(YGlobals.Link_PageControl);
        if (YVolatile.Basics.PageControlVisible)
            uri.addSearch(YGlobals.Link_PageControl, 'y');

        // add our module context info (if requested)
        if ($t.attr(YConfigs.Basics.CssAddModuleContext) != undefined) {
            if (!uri.hasSearch(YConfigs.Basics.ModuleGuid)) {
                var guid = YetaWF_Basics.getModuleGuidFromTag($t);
                uri.addSearch(YConfigs.Basics.ModuleGuid, guid);
            }
        }

        // pass along the charsize
        {
            var charSize = YetaWF_Basics.getCharSizeFromTag($t);
            uri.removeSearch(YGlobals.Link_CharInfo);
            uri.addSearch(YGlobals.Link_CharInfo, charSize.width + ',' + charSize.height);
        }

        // fix the url to include where we came from
        var target = $t.attr("target");
        if ((target == undefined || target == "" || target == "_self") && $t.attr(YConfigs.Basics.CssSaveReturnUrl) != undefined) {
            // add where we currently are so we can save it in case we need to return to this page
            var currUri = new URI(window.location.href);
            currUri.removeSearch(YGlobals.Link_OriginList);// remove originlist from current URL
            currUri.removeSearch(YGlobals.Link_InPopup);// remove popup info from current URL
            // now update url (where we're going with originlist)
            uri.removeSearch(YGlobals.Link_OriginList);
            var originList = YVolatile.Basics.OriginList.slice(0);// copy saved originlist

            if ($t.attr(YConfigs.Basics.CssDontAddToOriginList) == undefined) {
                var newOrigin = { Url: currUri.toString(), EditMode: YVolatile.Basics.EditModeActive != 0, InPopup: YetaWF_Basics.isInPopup() };
                originList.push(newOrigin);
                if (originList.length > 5)// only keep the last 5 urls
                    originList = originList.slice(originList.length - 5);
            }
            uri.addSearch(YGlobals.Link_OriginList, JSON.stringify(originList));
            target = "_self";
        }
        if (target == undefined || target == "" || target == "_self")
            target = "_self";

        // first try to handle this as a link to the outer window (only used in a popup)
        if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined) {
            if (YetaWF_Popups.handleOuterWindow($t))
                return false;
        }
        // try to handle this as a popup link
        if (typeof YetaWF_Popups !== 'undefined' && YetaWF_Popups != undefined) {
            if (YetaWF_Popups.handlePopupLink($t))
                return false;
        }

        var cookieToReturn = undefined;
        var cookiePattern = undefined;
        var cookieTimer = undefined;
        var post = undefined;

        if ($t.attr(YConfigs.Basics.CookieDoneCssAttr) != undefined) {
            cookieToReturn = (new Date()).getTime();
            uri.removeSearch(YConfigs.Basics.CookieToReturn);
            uri.addSearch(YConfigs.Basics.CookieToReturn, JSON.stringify(cookieToReturn));
        }
        if ($t.attr(YConfigs.Basics.PostAttr) != undefined) {
            post = true;
        }

        function checkCookies() {
            if (cookiePattern == undefined) throw "cookie pattern not defined";/*DEBUG*/
            if (cookieTimer == undefined) throw "cookie timer not defined";/*DEBUG*/
            if (document.cookie.search(cookiePattern) >= 0) {
                clearInterval(cookieTimer);
                YetaWF_Basics.setLoading(false);// turn off loading indicator
                console.log("Download complete!!");
                return false;
            }
            console.log("File still downloading...", new Date().getTime());
        }
        function waitForCookie() {
            if (cookieToReturn) {
                // check for cookie to see whether download started
                cookiePattern = new RegExp((YConfigs.Basics.CookieDone + "=" + cookieToReturn), "i");
                cookieTimer = setInterval(checkCookies, 500);
            }
        }
        function postLink() {
            YetaWF_Basics.setLoading();
            waitForCookie();

            $.ajax({
                'url': url,
                type: 'post',
                data: {},
                success: function (result, textStatus, jqXHR) {
                    YetaWF_Basics.setLoading(false);
                    YetaWF_Basics.processAjaxReturn(result, textStatus, jqXHR, $t);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    YetaWF_Basics.setLoading(false);
                    YetaWF_Basics.Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
                    debugger;
                }
            });
        }

        if (cookieToReturn) {
            // this is a file download
            var confirm = $t.attr(YConfigs.Basics.CssConfirm);
            if (confirm != undefined) {
                YetaWF_Basics.Y_AlertYesNo(confirm, null, function () {
                    window.location.assign(url);
                    YetaWF_Basics.setLoading();
                    waitForCookie();
                });
                return false;
            }
            window.location.assign(url);
        } else {
            // if a confirmation is wanted, show it
            // this means that it's posted by definition
            var confirm = $t.attr(YConfigs.Basics.CssConfirm);
            if (confirm != undefined) {
                YetaWF_Basics.Y_AlertYesNo(confirm, null, function () {
                    postLink();
                    if ($t.attr(YConfigs.Basics.CssPleaseWait) != undefined)
                        Y_PleaseWait($t.attr(YConfigs.Basics.CssPleaseWait))
                    return false;
                });
                return false;
            } else if (post) {
                if ($t.attr(YConfigs.Basics.CssPleaseWait) != undefined)
                    Y_PleaseWait($t.attr(YConfigs.Basics.CssPleaseWait))
                postLink();
                return false;
            }
        }

        if (target == "_self") {
            // add overlay if desired
            if ($t.attr(YConfigs.Basics.CssPleaseWait) != undefined) {
                Y_PleaseWait($t.attr(YConfigs.Basics.CssPleaseWait))
            }
        }
        waitForCookie(); // if any

        // Handle unified page clicks by activating the desired pane(s) or swapping out pane contents
        if (cookieToReturn) return true; // expecting cookie return
        if (uri.domain() !== "" && uri.hostname() !== window.document.domain) return true; // wrong domain
        // if we're switching from https->http or from http->https don't use a unified page set
        if (!url.startsWith("http") || !window.document.location.href.startsWith("http")) return true; // neither http nor https
        if ((url.startsWith("http://") != window.document.location.href.startsWith("http://")) ||
              (url.startsWith("https://") != window.document.location.href.startsWith("https://"))) return true; // switching http<>https

        if (target == "_self")
            return !_YetaWF_Basics.setContent(uri, true);
        return true;
    });

    // SUBMITFORMONCHANGE
    // SUBMITFORMONCHANGE
    // SUBMITFORMONCHANGE

    var submitFormTimer = undefined;
    var submitForm = null;
    /* SUBMIT */
    var submitFormOnChange = function () {
        clearInterval(submitFormTimer);
        YetaWF_Forms.submit(submitForm, false);
    }
    $('body').on('keyup', '.ysubmitonchange select', function (e) {
        if (e.keyCode == 13) {
            submitForm = YetaWF_Forms.getForm(this);
            submitFormOnChange();
        }
    });
    $('body').on('change', '.ysubmitonchange select,.ysubmitonchange input[type="checkbox"]', function (e) {
        clearInterval(submitFormTimer);
        submitForm = YetaWF_Forms.getForm(this);
        submitFormTimer = setInterval(submitFormOnChange, 1000);// wait 1 second and automatically submit the form
        YetaWF_Basics.setLoading(true);
    });
    /* APPLY */
    var applyFormOnChange = function () {
        clearInterval(submitFormTimer);
        YetaWF_Forms.submit(submitForm, false, YGlobals.Link_SubmitIsApply + "=y");
    }
    $('body').on('keyup', '.yapplyonchange select,.yapplyonchange input[type="checkbox"]', function (e) {
        if (e.keyCode == 13) {
            submitForm = YetaWF_Forms.getForm(this);
            applyFormOnChange();
        }
    });
    $('body').on('change', '.yapplyonchange select', function (e) {
        clearInterval(submitFormTimer);
        submitForm = YetaWF_Forms.getForm(this);
        submitFormTimer = setInterval(applyFormOnChange, 1000);// wait 1 second and automatically submit the form
        YetaWF_Basics.setLoading(true);
    });

    // WHENREADY
    // WHENREADY
    // WHENREADY

    YetaWF_Basics.processAllReady();
    YetaWF_Basics.processAllReadyOnce();
});

// PAGE
// PAGE
// PAGE

YetaWF_Basics.setScrollPosition = function () {
    'use strict';
    // positioning isn't exact. For example, TextArea (i.e. CKEditor) will expand the window size which may happen later.
    var uri = new URI(window.location.href);
    var data = uri.search(true);
    var v = data[YGlobals.Link_ScrollLeft];
    var scrolled = false;
    if (v != undefined) {
        $(window).scrollLeft(Number(v));
        scrolled = true;
    }
    v = data[YGlobals.Link_ScrollTop];
    if (v != undefined) {
        $(window).scrollTop(Number(v));
        scrolled = true;
    }
    return scrolled;
};

YetaWF_Basics.initPage = function () {
    'use strict';

    // PAGE POSITION
    // PAGE POSITION
    // PAGE POSITION

    // check if we have anything with that path as a unified pane
    var scrolled = YetaWF_Basics.setScrollPosition();
    if (!scrolled) {
        if (YVolatile.Basics.UnifiedMode === 2 /*UnifiedModeEnum.ShowDivs*/) {
            var $divs = $('.yUnified[data-url="{0}"]'.format(uri.path()));
            if ($divs.length > 0) {
                $(window).scrollTop($divs.eq(0).offset().top);
                scrolled = true;
            }
        }
    }

    // FOCUS
    // FOCUS
    // FOCUS

    $(document).ready(function () {
        if (!scrolled && location.hash.length <= 1)
            YetaWF_Basics.setFocus();
    });

    // CONTENT NAVIGATION
    // CONTENT NAVIGATION
    // CONTENT NAVIGATION

    _YetaWF_Basics.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious;// save loaded addons
};

YetaWF_Basics.suppressPopState = 0;

$(window).on("popstate", function (ev) {
    var uri = new URI(window.location.href);
    if (YetaWF_Basics.suppressPopState) {
        YetaWF_Basics.suppressPopState = 0;
        return;
    }
    return !_YetaWF_Basics.setContent(uri, false);
});

