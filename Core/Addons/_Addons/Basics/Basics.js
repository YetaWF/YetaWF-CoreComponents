/* Copyright � 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Basics = {};
var _YetaWF_Basics = {};

// Extend string type
if (typeof String.prototype.startsWith != 'function') {
    String.prototype.startsWith = function (str) {
        return this.indexOf(str) == 0;
    };
}
if (typeof String.prototype.endsWith != 'function') {
    String.prototype.endsWith = function (str) {
        return this.indexOf(str) == this.length - str.length;
    };
}

// string compare that considers null == ""
function StringYCompare(str1, str2) {
    if (!str1 && !str2) return true;
    return str1 == str2;
};

// String.format
if (!String.prototype.format) {
    String.prototype.format = function () {
        var args = arguments;
        return this.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != 'undefined'
              ? args[number]
              : match
            ;
        });
    };
}

// check for valid int
String.prototype.isValidInt = function (start, end) { // http://stackoverflow.com/questions/10834796/validate-that-a-string-is-a-positive-integer
    var n = ~~Number(this);
    return String(n) == this && n >= start && (end == undefined || n <= end);
}

function YZeroPad(num, places) {
    var zero = places - num.toString().length + 1;
    return Array(+(zero > 0 && zero)).join("0") + num;
}

function Y_AttrEscape(s) {
    return $('<div/>').text(s).html();
}

// escape data for html attributes
function Y_HtmlEscape(s, preserveCR) {
    'use strict';
    preserveCR = preserveCR ? '&#13;' : '\n';
    return ('' + s) /* Forces the conversion to string. */
        .replace(/&/g, '&amp;') /* This MUST be the 1st replacement. */
        .replace(/'/g, '&apos;') /* The 4 other predefined entities, required. */
        .replace(/"/g, '&quot;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        /*
        You may add other replacements here for HTML only
        (but it's not necessary).
        Or for XML, only if the named entities are defined in its DTD.
        */
        .replace(/\r\n/g, preserveCR) /* Must be before the next replacement. */
        .replace(/[\r\n]/g, preserveCR);
}

// escape data for javascript/json strings
function Y_Escape(s) {
    return ('' + s) /* Forces the conversion to string. */
        .replace(/\\/g, '\\\\') /* This MUST be the 1st replacement. */
        .replace(/\t/g, '\\t') /* These 2 replacements protect whitespaces. */
        .replace(/\n/g, '\\n')
        .replace(/\u00A0/g, '\\u00A0') /* Useful but not absolutely necessary. */
        .replace(/&/g, '\\x26') /* These 5 replacements protect from HTML/XML. */
        .replace(/'/g, '\\x27')
        .replace(/"/g, '\\x22')
        .replace(/</g, '\\x3C')
        .replace(/>/g, '\\x3E')
    ;
}

function Y_UrlEncodePath(s) { //like manager.cs UrlEncodePath()
    'use strict';
    s = s.replace("&", "-"); // can't use &
    s = s.replace("*", "-"); // can't use *
    s = s.replace("/", "-"); // can't use /
    s = s.replace(".", "-"); // http can't find a page if the url has a period
    return encodeURIComponent(s);
}

/* Show alerts, please wait windows */
function Y_Message(text, onOk)
{
    Y_Alert(text, YLocs.Basics.DefaultSuccessTitle, onOk);
}

function Y_Error(text, onOk) {
    Y_Alert(text, YLocs.Basics.DefaultErrorTitle, onOk);
}

function Y_Alert(text, title, onOk)
{
    'use strict';
    var $body = $("body");
    //if (Y_InPopup()) // This doesn't really work - dragging/positioning/overlay issues, jquery uses 'body'
    //    $body = $(window.parent.document.body);
    $body.prepend("<div id='yalert'></div>");
    var $dialog = $("#yalert", $body);

    // change \n to <br/>
    $dialog.text(text);
    var s = $dialog.html();
    s = s.replace(/\(\+nl\)/g, '<br/>');
    $dialog.html(s);

    if (title == undefined)
        title = YLocs.Basics.DefaultAlertTitle;

    $dialog.dialog({
        autoOpen: true,
        modal: true,
        width: YConfigs.Basics.DefaultAlertWaitWidth,
        height: YConfigs.Basics.DefaultAlertWaitHeight == 0 ? "auto" : YConfigs.Basics.DefaultAlertWaitHeight,
        closeOnEscape: true,
        closeText: YLocs.Basics.CloseButtonText,
        close: function (event, ui) {
            if (onOk != undefined)
                onOk();
        },
        draggable: true,
        resizable: false,
        'title': title,
        buttons: [ {
            text: YLocs.Basics.OKButtonText,
            click: function () {
                var endFunc = onOk;
                onOk = undefined; // clear this so close function doesn't call onOK handler also
                $dialog.dialog("close").dialog("destroy");
                $dialog.remove();
                if (endFunc != undefined)
                    endFunc();
            }
        } ]
    });
}
function Y_Confirm(text, title, onOk)
{
    if (title == undefined)
        title = YLocs.Basics.DefaultSuccessTitle;
    Y_Alert(text, title, onOk)
}

function Y_AlertYesNo(text, title, onYes, onNo) {
    'use strict';
    var $body = $("body");
    $body.prepend("<div id='yalert'></div>");
    var $dialog = $("#yalert", $body);

    // change \n to <br/>
    $dialog.text(text);
    var s = $dialog.html();
    s = s.replace(/\(\+nl\)/g, '<br/>');
    $dialog.html(s);

    if (title == undefined)
        title = YLocs.Basics.DefaultAlertYesNoTitle;

    $dialog.dialog({
        autoOpen: true,
        modal: true,
        width: YConfigs.Basics.DefaultAlertYesNoWidth,
        height: YConfigs.Basics.DefaultAlertYesNoHeight == 0 ? "auto" : YConfigs.Basics.DefaultAlertYesNoHeight,
        closeOnEscape: true,
        closeText: YLocs.Basics.CloseButtonText,
        close: function () {
            $dialog.dialog("destroy");
            $dialog.remove();
            if (onNo != undefined)
                onNo();
        },
        draggable: true,
        resizable: false,
        'title': title,
        buttons: [{
            text: YLocs.Basics.YesButtonText,
                click: function () {
                    var endFunc = onYes;
                    onYes = undefined;// clear this so close function doesn't try do call these
                    onNo = undefined;
                    $dialog.dialog("destroy");
                    $dialog.remove();
                    if (endFunc != undefined)
                        endFunc();
                }
            },
            {
                text: YLocs.Basics.NoButtonText,
                click: function () {
                    var endFunc = onNo;
                    onYes = undefined;// clear this so close function doesn't try do call these
                    onNo = undefined;
                    $dialog.dialog("destroy");
                    $dialog.remove();
                    if (endFunc != undefined)
                        endFunc();
                }
            }
        ],
    });
}

function Y_PleaseWait(text, title) {
    'use strict';

    // insert <div id="yplwait"></div> at top of page for the window
    // this is automatically removed when destroy() is called
    $("body").prepend("<div id='yplwait'></div>");
    var $popupwin = $("#yplwait");
    var popup = null;

    if (text == undefined)
        text = YLocs.Basics.PleaseWaitText;
    if (title == undefined)
        title = YLocs.Basics.PleaseWaitTitle;
    $popupwin.text(text);

    // Create the window
    $popupwin.kendoWindow({
        actions: [],
        width: YConfigs.Basics.DefaultPleaseWaitWidth,
        height: YConfigs.Basics.DefaultPleaseWaitHeight,
        draggable: true,
        iframe: true,
        modal: true,
        resizable: false,
        'title': Y_HtmlEscape(title),
        visible: false,
        close: function () {
            var popup = $popupwin.data("kendoWindow");
            popup.destroy();
            popup = null;
        },
    });

    // show and center the window
    popup = $popupwin.data("kendoWindow");
    popup.open().center();

    return false;//e.preventDefault();
}

function Y_PleaseWaitClose() {
    'use string';
    var $popupwin = $("#yplwait");
    if ($popupwin.length == 0) return
    var popup = $popupwin.data("kendoWindow");
    popup.destroy();
}

// check if we're in a popup window
function Y_InPopup() {
    return YVolatile.Basics.IsInPopup;
}

// close any popup window
function Y_ClosePopup(forceReload) {
    if (typeof YetaWF_Popup !== 'undefined' && YetaWF_Popup.closePopup != undefined)
        return YetaWF_Popup.closePopup(forceReload);
    return false;
}

function Y_Loading(starting)
{
    if (starting != false) {
        $.prettyLoader.show();
    } else {
        $.prettyLoader.hide();
        Y_PleaseWaitClose();
    }
}

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

// RESIZE
// RESIZE
// RESIZE

// usage:
//$(window).smartresize(function () {
//    // code that makes it easy...
//});

(function ($, sr) {
    // debouncing function from John Hann
    // http://unscriptable.com/index.php/2009/03/20/debouncing-javascript-methods/
    var debounce = function (func, threshold, execAsap) {
        var timeout;

        return function debounced() {
            var obj = this, args = arguments;
            function delayed() {
                if (!execAsap)
                    func.apply(obj, args);
                timeout = null;
            };

            if (timeout)
                clearTimeout(timeout);
            else if (execAsap)
                func.apply(obj, args);

            timeout = setTimeout(delayed, threshold || 100);
        };
    }
    // smartresize
    jQuery.fn[sr] = function (fn) { return fn ? this.bind('resize', debounce(fn)) : this.trigger(sr); };

})(jQuery, 'smartresize');

// FOCUS
// FOCUS
// FOCUS

function Y_SetFocus($obj) {
    'use strict';
    //TODO: this should also consider input fields with validation errors (although that seems to magically work right now)
    if ($obj == undefined)
        $obj = $('body')
    var $items = $('.focusonme:visible', $obj)
    var $f = null
    $items.each(function (index) {
        var item = this;
        if (item.tagName == "DIV") { // if we found a div, find the edit element instead
            var $i = $('input:visible,select:visible,.yt_dropdownlist_base:visible', $(item)).not("input[type='hidden']")
            if ($i.length > 0) {
                $f = $i.eq(0);
                return false;
            }
        }
    });
    // We probably don't want to set the focus to any control - made OPT-IN for now
    //if ($f == null) {
    //    $items = $('input:visible,select:visible', $obj).not("input[type='hidden']");// just find something useable
    //    // filter out anything in a grid (filters, pager, etc)
    //    $items.each(function (index) {
    //        var $i = $(this)
    //        if ($i.parents('.ui-jqgrid').length == 0) {
    //            $f = $i;
    //            return false; // not in a grid, so it's ok
    //        }
    //    });
    //}
    if ($f != null) {
        try {
            $f[0].focus();
        } catch (e) {  }
    }
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
            $panes.each(function() {
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

YetaWF_Basics.getModuleFromId = function (id) {
    var $id = ('#' + id);
    if ($id.length != 1) { debugger; throw "Invalid id"; }/*DEBUG*/
    var $mod = $id.closest('.yModule');
    if ($mod.length != 1) { debugger; throw "Can't find containing module"; }/*DEBUG*/
    return $mod;
};
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

YetaWF_Basics.refreshModule = function($mod) {
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
YetaWF_Basics.refreshModuleById = function (id) {
    YetaWF_Basics.refreshModule(YetaWF_Basics.getModuleFromId(id));
};
YetaWF_Basics.refreshPage = function () {
    for (var entry in YetaWF_Basics.reloadInfo) {
        YetaWF_Basics.reloadInfo[entry].callback();
    }
};

// FORMATTING
// FORMATTING
// FORMATTING

// Format a date value
// fmt is of type Formatting.DateFormatEnum
YetaWF_Basics.formatDate = function (date, fmt)
{
    'use strict';
    var dt = new Date(date);
    var day = dt.getDate();
    var month = dt.getMonth() + 1;
    var year = dt.getFullYear();

    switch(fmt) {

        default:
        case 0: //"Month/Day/Year"
            return "{0}/{1}/{2}".format(YZeroPad(month, 2),YZeroPad(day, 2),YZeroPad(year, 4));
        case 1: //"Month-Day-Year"
            return "{0}-{1}-{2}".format(YZeroPad(month, 2), YZeroPad(day, 2), YZeroPad(year, 4));
        case 2: //"Day/Month/Year"
            return "{0}/{1}/{2}".format(YZeroPad(day, 2), YZeroPad(month, 2), YZeroPad(year, 4));
        case 10: //"Day.Month.Year"
            return "{0}.{1}.{2}".format(YZeroPad(day, 2), YZeroPad(month, 2), YZeroPad(year, 4));
        case 11: //"Day-Month-Year"
            return "{0}-{1}-{2}".format(YZeroPad(day, 2), YZeroPad(month, 2), YZeroPad(year, 4));
        case 12: //"Year/Month/Day"
            return "{0}/{1}/{2}".format(YZeroPad(year, 4), YZeroPad(month, 2), YZeroPad(day, 2));
        case 20: //"Year.Month.Day"
            return "{0}.{1}.{2}".format(YZeroPad(year, 4), YZeroPad(month, 2), YZeroPad(day, 2));
        case 21: //"Year-Month-Day"
            return "{0}-{1}-{2}".format(YZeroPad(year, 4), YZeroPad(month, 2), YZeroPad(day, 2));
        case 22: //"Year.Month.Day"
            return "{0}.{1}.{2}".format(YZeroPad(year, 4), YZeroPad(month, 2), YZeroPad(day, 2));
    }
}

// WHENREADY
// WHENREADY
// WHENREADY

YetaWF_Basics.whenReady = [];

// Usage:
// YetaWF_Basics.whenReady.push({
//   callback: function($tag) {}    // function to be called
// });
// This registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
// or after a partial form is rendered. The callee must honor $tag and only manipulate child objects.
// Callback functions are registered by whomever needs this type of processing. For example, a grid can
// process all whenready requests after reloading the grid with data (which doesn't run any javascript).
// $tag describes the section that is new and needs to be initialized.
YetaWF_Basics.processAllReady = function ($tag) {
    if ($tag === undefined) $tag = $('body');
    for (var index in YetaWF_Basics.whenReady) {
        var entry = YetaWF_Basics.whenReady[index];
        try { // catch errors to insure all callbacks are called
            entry.callback($tag);
        } catch (err) {
            console.log(err.message);
        }
    }
}

YetaWF_Basics.whenReadyOnce = [];

// Usage:
// YetaWF_Basics.whenReadyOnce.push({
//   callback: function($tag) {}    // function to be called
// });
// This registers a callback that is called when the document is ready (similar to $(document).ready()), after page content is rendered (for dynamic content),
// or after a partial form is rendered. The callee must honor $tag and only manipulate child objects.
// Callback functions are registered by whomever needs this type of processing. For example, a grid can
// process all whenready requests after reloading the grid with data (which doesn't run any javascript).
// $tag describes the section that is new and needs to be initialized.
// The callback is called for ONCE. Then the callback is removed.
// $tag describes the section that is new and needs to be initialized.
YetaWF_Basics.processAllReadyOnce = function ($tag) {
    if ($tag === undefined) $tag = $('body');
    for (var index in YetaWF_Basics.whenReadyOnce) {
        var entry = YetaWF_Basics.whenReadyOnce[index];
        try { // catch errors to insure all callbacks are called
            entry.callback($tag);
        } catch (err) {
            console.log(err.message);
        }
    }
    YetaWF_Basics.whenReadyOnce = [];
}

// BEAUTIFY BUTTONS
// BEAUTIFY BUTTONS
// BEAUTIFY BUTTONS

_YetaWF_Basics.initButtons = function ($tag) {
    'use strict';
    if (YVolatile.Skin.Bootstrap && YVolatile.Skin.BootstrapButtons) {
        // bootstrap
        $("input[type=submit],input[type=button],input[type=reset],input[type=file]", $tag).not('.y_jqueryui').addClass('btn btn-default')
        $("button", $tag).not('.y_jqueryui').addClass('btn')
        $("a[" + YConfigs.Basics.CssAttrActionButton + "]", $tag).not('.y_jqueryui').addClass('btn btn-default') // action link as a button
        // explicitly marked for jquery
        $("input[type=submit].y_jqueryui,input[type=button].y_jqueryui,input[type=reset].y_jqueryui,input[type=file].y_jqueryui,button.y_jqueryui", $tag).button()
        $("a[" + YConfigs.Basics.CssAttrActionButton + "].y_jqueryui", $tag).button() // action link as a button
    } else {
        // jquery-ui
        $("input[type=submit],input[type=button],input[type=reset],input[type=file],button", $tag).not('.y_bootstrap').button() // beautify all buttons
        $("a[" + YConfigs.Basics.CssAttrActionButton + "]", $tag).not('.y_bootstrap').button() // action link as a button
        // explicitly marked for bootstrap
        $("input[type=submit].y_bootstrap,input[type=button].y_bootstrap,input[type=reset].y_bootstrap,input[type=file].y_bootstrap", $tag).addClass('btn btn-default')
        $("button.y_bootstrap", $tag).addClass('btn')
        $("a[" + YConfigs.Basics.CssAttrActionButton + "].y_bootstrap", $tag).addClass('btn btn-default') // action link as a button
    }
}

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
        eval(script);// if this uses Y_Alert or other "modal" calls, the page will reload immediately (use AjaxJavascriptReturn instead and explictly reload page in your javascript)
        Y_ReloadPage(true);
        return true;
    } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModule)) {
        var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModule.length);
        eval(script);// if this uses Y_Alert or other "modal" calls, the module will reload immediately (use AjaxJavascriptReturn instead and explictly reload module in your javascript)
        Y_ReloadModule();
        return true;
    } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptReloadModuleParts)) {
        //if (!Y_InPopup()) throw "Not supported - only available within a popup";/*DEBUG*/
        var script = result.substring(YConfigs.Basics.AjaxJavascriptReloadModuleParts.length);
        eval(script);
        YetaWF_Basics.refreshModuleByAnyTag(tagInModule);
        return true;
    } else {
        if (onHandleResult != undefined) {
            onHandleResult(result);
        } else {
            Y_Error(YLocs.Basics.IncorrectServerResp);
        }
        return false;
    }
};

// TOOLTIPS
// TOOLTIPS
// TOOLTIPS

function Y_KillTooltips() {
    $('.ui-tooltip').remove();
}

// CONTENT
// CONTENT
// CONTENT

// loads all scripts - we need to preserve the order of initialization hence the recursion
_YetaWF_Basics.loadScripts = function (scripts, run) {
    if (!YVolatile.Basics.hasOwnProperty('KnownScriptsDynamic')) YVolatile.Basics.KnownScriptsDynamic = [];
    var total = scripts.length;
    if (total == 0) {
        run();
        return;
    }
    _YetaWF_Basics.loadNextScript(scripts, total, 0, run);
};

_YetaWF_Basics.loadNextScript = function (scripts, total, ix, run) {
    var name = scripts[ix];

    function process() {
        if (ix >= total - 1) {
            // adding <script> to <head> and using $.getScript may be sync but the scripts don't necessarily RUN...
            // We just loaded all scripts. By setting a timeout we try to make sure they actually run... smh
            // this is all voodoo bullshit... Just give me a method to load scripts in the correct order and let me know when
            // they're done (emphasis) running
            setTimeout(function () {
                run();// we're all done
            }, 50);
        } else {
            _YetaWF_Basics.loadNextScript(scripts, total, ix + 1, run);
        }
    }
    if (name.startsWith('//') || name.startsWith('http://') || name.startsWith('https://')) {
        // cross-site
        YVolatile.Basics.KnownScriptsDynamic.push(name);// save as dynamically loaded script
        $.getScript(name).done(function (script, textStatus) {
            process();
        }).fail(function (jqxhr, settings, exception) {
            console.log("Couldn't load script " + name);
            process();
        });
    } else {
        // same site
        $('head').append('<script type="text/javascript" src="{0}"></script>'.format(name));
        process();
    }
};

_YetaWF_Basics.UnifiedAddonModsLoaded = [];// currently loaded addons

// Change the current page to the specified Uri (may not be part of the unified page set)
// returns false if the uri couldn't be processed (i.e., it's not part of a unified page set)
// returns true if the page is now shown and is part of the unified page set
_YetaWF_Basics.setContent = function (uri, setState, popupCB) {
    'use strict';

    // Close open bootstrap nav menus (if any) by clicking on the page
    function closemenus() {
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
        } catch (e) {}
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
        var $css = $('link[rel="stylesheet"]');
        $css.each(function () {
            data.KnownCss.push($(this).attr('href').split('?')[0]); // remove ?+querystring
        });
        data.KnownCss = data.KnownCss.concat(YVolatile.Basics.UnifiedCssBundleFiles);// add known css files that were added via bundles
        data.KnownScripts = [];
        var $scripts = $('script[type="text/javascript"][src]');
        $scripts.each(function () {
            data.KnownScripts.push($(this).attr('src').split('?')[0]); // remove ?+querystring
        });
        data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.KnownScriptsDynamic);// known javascript files that were added by content pages
        data.KnownScripts = data.KnownScripts.concat(YVolatile.Basics.UnifiedScriptBundleFiles);// add known javascript files that were added via bundles

        Y_Loading();
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
                    Y_Loading(false);
                    Y_Alert(result.Status, YLocs.Forms.AjaxErrorTitle);
                    return;
                }
                if (result.Redirect != null && result.Redirect.length > 0) {
                    //Y_Loading(false);
                    if (popupCB) {
                        // we want a popup and get a redirect, redirect to iframe popup
                        YetaWF_Popup.openPopup(result.Redirect, true);
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
                // run all global scripts (YConfigs, etc.)
                $.globalEval(result.Scripts);
                // add all new css files
                var cssLength = result.CssFiles.length;
                for (var i = 0; i < cssLength; i++) {
                    var sUrl = result.CssFiles[i];
                    var sUrl = '{0}{1}{2}={3},{4}'.format(sUrl, sUrl.indexOf("?") < 0 ? "?" : "&", YGlobals.Link_CharInfo, YVolatile.Basics.CharWidthAvg, YVolatile.Basics.CharHeight);
                    $('head').append($('<link />').attr('rel', 'stylesheet').attr('type', 'text/css').attr('href', sUrl));
                }
                if (result.CssBundleFiles != null) {
                    if (YVolatile.Basics.UnifiedCssBundleFiles != null)
                        YVolatile.Basics.UnifiedCssBundleFiles.concat(result.CssBundleFiles);
                    else
                        YVolatile.Basics.UnifiedCssBundleFiles = result.CssBundleFiles
                }
                // add all new script files
                _YetaWF_Basics.loadScripts(result.ScriptFiles, function () {
                    if (result.ScriptBundleFiles != null) {
                        if (YVolatile.Basics.UnifiedScriptBundleFiles != null)
                            YVolatile.Basics.UnifiedScriptBundleFiles.concat(result.ScriptBundleFiles);
                        else
                            YVolatile.Basics.UnifiedScriptBundleFiles = result.ScriptBundleFiles;
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
                    if (!YVolatile.Basics.hasOwnProperty('UnifiedAddonModsPrevious')) YVolatile.Basics.UnifiedAddonModsPrevious = [];
                    if (!YVolatile.Basics.hasOwnProperty('UnifiedAddonMods')) YVolatile.Basics.UnifiedAddonMods = [];
                    // end of page scripts
                    $.globalEval(result.EndOfPageScripts);
                    // turn off all previously active modules that are no longer active
                    YVolatile.Basics.UnifiedAddonModsPrevious.forEach(function (guid) {
                        if (YVolatile.Basics.UnifiedAddonMods.indexOf(guid) < 0)
                            $(document).trigger('YetaWF_Basics_Addon', [guid, false]);
                    });
                    // turn on all newly active modules (if they were previously loaded)
                    // new referenced modules that were just loaded now are already active and don't need to be called
                    YVolatile.Basics.UnifiedAddonMods.forEach(function(guid) {
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
                        if (typeof YetaWF_Popup !== 'undefined' && YetaWF_Popup.closePopup != undefined)
                            YetaWF_Popup.closeInnerPopup();
                    }
                    // done, set focus
                    Y_SetFocus($tags);
                    Y_Loading(false);
                    try {
                        $.globalEval(result.AnalyticsContent);
                    } catch (e) {}
                });
            },
            error: function (jqXHR, textStatus, errorThrown) {
                Y_Loading(false);
                Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
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
                Y_SetFocus();
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
            Y_Loading(false);
            return true;
        }
        //Y_Loading(false); // don't hide, let new page take over
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

    // TOOLTIPS
    // TOOLTIPS
    // TOOLTIPS

    var selectors = 'img,label,input:not(".ui-button-disabled"),a:not("{0},.ui-button-disabled"),i,.ui-jqgrid span[{1}],span[{2}],li[{1}],div[{1}]';
    var ddsel = '.k-list-container.k-popup li[data-offset-index]';
    $('body').tooltip({
        items: (selectors+','+ddsel).format(YVolatile.Basics.CssNoTooltips, YConfigs.Basics.CssTooltip, YConfigs.Basics.CssTooltipSpan),
        content: function (a, b, c) {
            var $this = $(this);
            if ($this.is(ddsel)) {
                // dropdown list - find who owns this and get the matching tooltip
                // this is a bit hairy - we save all the tooltips for a dropdown list in a variable
                // named ..id.._tooltips. The popup/dropdown is named ..id..-list so we deduce the
                // variable name from the popup/dropdown. This is going to break at some point...
                var ttindex = $this.attr("data-offset-index");
                if (ttindex === undefined) return null;
                var $container = $this.closest('.k-list-container.k-popup');
                if ($container.length != 1) return null;
                var id = $container.attr("id");
                id = id.replace("-list", "");
                var tip = YetaWF_TemplateDropDownList.getTitleFromId(id, ttindex);
                if (tip == null) return null;
                return Y_HtmlEscape(tip);
            }
            for ( ; ; ) {
                if (!$this.is(':hover') && $this.is(':focus'))
                    return null;
                if ($this.attr("disabled") !== undefined)
                    return null;
                var s = $this.attr(YConfigs.Basics.CssTooltip);
                if (s != undefined)
                    return Y_HtmlEscape(s);
                s = $this.attr(YConfigs.Basics.CssTooltipSpan);
                if (s != undefined)
                    return Y_HtmlEscape(s);
                s = $this.attr('title');
                if (s != undefined)
                    return Y_HtmlEscape(s);
                if ($this[0].tagName != "IMG" && $this[0].tagName != "I")
                    break;
                // we're in an IMG or I tag, find enclosing A (if any) and try again
                $this = $this.closest('a:not("{0}")'.format(YVolatile.Basics.CssNoTooltips));
                if ($this.length == 0) return null;
                // if the a link is a menu, don't show a tooltip for the image because the tooltip would be in a bad location
                if ($this.closest('.k-menu').length > 0) return null;
            }
            if ($this[0].tagName == "A") {
                var href = $this[0].href;
                if (href == undefined || href.startsWith('javascript') || href.startsWith('#') || href.startsWith('mailto:'))
                    return null;
                var target = $this[0].target;
                if (target === '_blank') {
                    var uri = new URI(href);
                    return Y_HtmlEscape(YLocs.Basics.OpenNewWindowTT.format(uri.hostname()));
                }
            }
            return null;
        },
        position: { my: "left top", at: "right bottom", collision: "flipfit" }
    });

    // <A> LINKS
    // <A> LINKS
    // <A> LINKS

    $("body").on("mousedown", 'a', function () {
        // when we click on an <a> link, we don't want the next tooltip
        // this may be a bug because after clicking an a link, the tooltip will be created (again?) so we want to suppress this
        // Repro steps (without hack): right click on an a link (that COULD have a tooltip) and open a new tab/window. On return to this page we'll get a tooltip
        Y_KillTooltips();
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

        var width, height;
        var $mod = YetaWF_Basics.getModuleFromTag_Cond($t);
        if ($mod != null) {
            width = $mod.attr('data-charwidthavg');
            height = $mod.attr('data-charheight');
        } else {
            width = YVolatile.Basics.CharWidthAvg;
            height = YVolatile.Basics.CharHeight;
        }
        uri.removeSearch(YGlobals.Link_CharInfo);
        uri.addSearch(YGlobals.Link_CharInfo, width + ',' + height);

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
                var newOrigin = { Url: currUri.toString(), EditMode: YVolatile.Basics.EditModeActive != 0, InPopup: Y_InPopup() };
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
        if (typeof YetaWF_Popup !== 'undefined' && YetaWF_Popup.handleOuterWindow != undefined) {
            if (YetaWF_Popup.handleOuterWindow($t))
                return false;
        }
        // first try to handle this as a popup link
        if (typeof YetaWF_Popup !== 'undefined' && YetaWF_Popup.handlePopupLink != undefined) {
            if (YetaWF_Popup.handlePopupLink($t))
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
                Y_Loading(false);// turn off loading indicator
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
            Y_Loading();
            waitForCookie();

            $.ajax({
                'url': url,
                type: 'post',
                data: {},
                success: function (result, textStatus, jqXHR) {
                    Y_Loading(false);
                    YetaWF_Basics.processAjaxReturn(result, textStatus, jqXHR, $t);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    Y_Loading(false);
                    Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
                    debugger;
                }
            });
        }

        if (cookieToReturn) {
            // this is a file download
            var confirm = $t.attr(YConfigs.Basics.CssConfirm);
            if (confirm != undefined) {
                Y_AlertYesNo(confirm, null, function () {
                    window.location.assign(url);
                    Y_Loading();
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
                Y_AlertYesNo(confirm, null, function () {
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

        return ! _YetaWF_Basics.setContent(uri, true);
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
        Y_Loading(true);
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
        Y_Loading(true);
    });

    // PRETTYLOADER
    // PRETTYLOADER
    // PRETTYLOADER

    $.prettyLoader({
        animation_speed: 'fast', /* fast/normal/slow/integer */
        bind_to_ajax: true, /* true/false */
        delay: false, /* false OR time in milliseconds (ms) */
        loader: YConfigs.Basics.LoaderGif, /* Path to your loader gif */
        offset_top: 13, /* integer */
        offset_left: 10 /* integer */
    });

    // WHENREADY
    // WHENREADY
    // WHENREADY

    YetaWF_Basics.processAllReady();
    YetaWF_Basics.processAllReadyOnce();
});

// BUTTONS
// BUTTONS
// BUTTONS

YetaWF_Basics.whenReady.push({
    callback: _YetaWF_Basics.initButtons
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
            Y_SetFocus();
    });

    // CONTENT NAVIGATION
    // CONTENT NAVIGATION
    // CONTENT NAVIGATION

    _YetaWF_Basics.UnifiedAddonModsLoaded = YVolatile.Basics.UnifiedAddonModsPrevious;// save loaded addons
};

$(window).on("popstate", function () {
    var uri = new URI(window.location.href);
    return ! _YetaWF_Basics.setContent(uri, false);
});

// Set rendering mode based on window size
// we can't really use @media (max-width:...) in css because popups (in Unified Page Sets) don't use iframes so their size may be small but
// doesn't match @media screen (ie. the window). So, instead we add the css class yCondense to the <body> or popup <div> to indicate we want
// a more condensed appearance.

YetaWF_Basics.setCondense = function($tag, width) {
    if (width < YVolatile.Skin.MinWidthForPopups)
        $tag.addClass('yCondense');
    else
        $tag.removeClass('yCondense');
}

$(window).on('resize', function () {
    YetaWF_Basics.setCondense($('body'), window.innerWidth);
});
$(document).ready(function () {
    YetaWF_Basics.setCondense($('body'), window.innerWidth);
});
