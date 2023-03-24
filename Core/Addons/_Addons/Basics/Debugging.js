"use strict";
/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// This is only loaded in non-deployed builds
var YetaWF_Core_Debugging;
(function (YetaWF_Core_Debugging) {
    $YetaWF.registerCustomEventHandlerDocument(YetaWF.Content.EVENTNAVPAGELOADED, null, function (ev) {
        // Verify that we don't have duplicate element ids, which would be an error (usually an incorrectly generated component). Id collisions due to SPA should not happen. This will pinpoint any such errors.
        var elems = $YetaWF.getElementsBySelector("[id]");
        var arr = [];
        var _loop_1 = function (elem) {
            var id = elem.id;
            var found = arr.find(function (el) { return el.id === id; });
            if (!found) {
                arr.push(elem);
            }
            else {
                var msg = "Duplicate id ".concat(id, " in element ").concat(elem.outerHTML, " - like ").concat(found.outerHTML);
                $YetaWF.error(msg);
                console.error(msg);
            }
        };
        for (var _i = 0, elems_1 = elems; _i < elems_1.length; _i++) {
            var elem = elems_1[_i];
            _loop_1(elem);
        }
        // Verify that no "ui-" classes are used (remnant from jquery ui)
        elems = $YetaWF.getElementsBySelector("*");
        for (var _a = 0, elems_2 = elems; _a < elems_2.length; _a++) {
            var elem = elems_2[_a];
            if ($YetaWF.elementHasClassPrefix(elem, "ui-").length > 0) {
                var msg = "Element with class ui-... found: ".concat(elem.outerHTML);
                $YetaWF.error(msg);
                console.error(msg);
            }
        }
        return true;
    });
    // Any JavaScript failures result in a popup so at least it's visible without explicitly looking at the console log.
    var inDebug = false;
    window.onerror = function (ev, url, lineNo, columnNo, error) {
        if (!inDebug) {
            inDebug = true;
            var evMsg = ev.toString();
            // avoid recursive error with video controls. a bit hacky but this is just a debugging tool.
            if (evMsg.startsWith("ResizeObserver"))
                return;
            var msg = "".concat(evMsg, " (").concat(url, ":").concat(lineNo, ") ").concat(error === null || error === void 0 ? void 0 : error.stack);
            $YetaWF.error(msg);
            console.error(msg);
            inDebug = false;
        }
    };
    var Links = /** @class */ (function () {
        function Links() {
            this.Remaining = [];
            this.Done = [];
            this.Running = false;
            // Anchors within parent elements with any of these CSS classes are not checked
            this.IgnoredParentElements = [
                "Softelvdm_Documentation",
                "yt_grid", // actions in grids
            ];
            // Ignore these Urls
            this.IgnoreUrls = [
                "/BlogEntry"
            ];
        }
        // Use actual click handling for anchors in elements with any of these CSS classes
        // private ActualClicks: string[] = [
        //     "yt_panels_pagebarinfo_list",
        // ];
        Links.prototype.testAll = function () {
            if (this.isRunning)
                return;
            this.Remaining = [];
            this.Done = [];
            this.Running = true;
            this.nextPage();
        };
        Links.prototype.nextPage = function () {
            if (!LinksTest.isRunning)
                return;
            var todos = $YetaWF.getElementsBySelector("a");
            var _loop_2 = function (todo) {
                var href = todo.href;
                if (!href || href.startsWith("javascript:"))
                    return "continue";
                if ($YetaWF.elementHasClass(todo, "DebugLoadTest"))
                    return "continue";
                if ($YetaWF.getAttributeCond(todo, "data-nohref") != null)
                    return "continue";
                var target = $YetaWF.getAttributeCond(todo, "target");
                if (target && target !== "" && target !== "_self")
                    return "continue";
                var rel = $YetaWF.getAttributeCond(todo, "rel");
                if (rel === "nofollow")
                    return "continue";
                if (!$YetaWF.elementHasClass(todo, "yaction-link"))
                    return "continue";
                var ignore = this_1.IgnoredParentElements.find(function (s) {
                    return $YetaWF.elementClosestCond(todo, ".".concat(s)) != null;
                });
                if (ignore)
                    return "continue";
                href = this_1.stripUrl(href);
                var uri = new YetaWF.Url();
                uri.parse(href);
                var path = uri.getPath();
                var foundPath = this_1.IgnoreUrls.find(function (s) {
                    return path === s;
                });
                if (foundPath)
                    return "continue";
                var found = this_1.Done.find(function (s) {
                    return href === s;
                });
                if (found)
                    return "continue";
                this_1.Remaining.push({ url: window.location.href, anchor: todo });
            };
            var this_1 = this;
            for (var _i = 0, todos_1 = todos; _i < todos_1.length; _i++) {
                var todo = todos_1[_i];
                _loop_2(todo);
            }
            this.processRemainder();
        };
        Links.prototype.stripUrl = function (href) {
            var uri = new YetaWF.Url();
            uri.parse(href);
            uri.removeSearch(YConfigs.Basics.Link_OriginList);
            uri.setHash(null);
            href = uri.toUrl();
            return href;
        };
        Links.prototype.processRemainder = function () {
            var _this = this;
            var _loop_3 = function () {
                var linkEntry = this_2.Remaining[0];
                this_2.Remaining.splice(0, 1);
                var todo = linkEntry.anchor;
                var found = this_2.Done.find(function (s) {
                    return todo.href === s;
                });
                if (found)
                    return "continue";
                console.log("Debug(".concat(this_2.Remaining.length, "): Clicking ").concat(todo.href, " on ").concat(linkEntry.url, " - ").concat(todo.outerHTML));
                this_2.Done.push(this_2.stripUrl(todo.href));
                // let actual = this.ActualClicks.find((s: string):boolean => {
                //     return $YetaWF.elementClosestCond(todo, `.${s}`) != null;
                // });
                // if (actual) {
                //     todo.click();// click it and wait for page content
                // } else {
                var uri = new YetaWF.Url();
                uri.parse(todo.href);
                if ($YetaWF.ContentHandling.setContent(uri, true) !== YetaWF.SetContentResult.ContentReplaced) {
                    // some how this wasn't possible
                    console.error("setContent failed for ".concat(todo.href));
                    setTimeout(function () { _this.nextPage(); }, 1);
                }
                return { value: void 0 };
            };
            var this_2 = this;
            while (this.Remaining.length > 0) {
                var state_1 = _loop_3();
                if (typeof state_1 === "object")
                    return state_1.value;
            }
            console.log("Debug: All pages processed");
        };
        Object.defineProperty(Links.prototype, "isRunning", {
            get: function () {
                return this.Running;
            },
            enumerable: false,
            configurable: true
        });
        return Links;
    }());
    YetaWF_Core_Debugging.Links = Links;
    // handle new content
    $YetaWF.registerCustomEventHandlerDocument(YetaWF.Content.EVENTNAVPAGELOADED, null, function (ev) {
        LinksTest.nextPage();
        return true;
    });
    var LinksTest = new Links();
    var a = $YetaWF.getElement1BySelectorCond(".YetaWF_Menus_MainMenu a.DebugLoadTest");
    if (a) {
        $YetaWF.registerEventHandler(a, "click", null, function (ev) {
            LinksTest.testAll();
            return false;
        });
    }
})(YetaWF_Core_Debugging || (YetaWF_Core_Debugging = {}));

//# sourceMappingURL=Debugging.js.map
