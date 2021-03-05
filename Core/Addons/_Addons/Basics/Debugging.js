"use strict";
/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
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
                var msg = "Duplicate id " + id + " in element " + elem.outerHTML + " - like " + found.outerHTML;
                $YetaWF.error(msg);
                console.log(msg);
            }
        };
        for (var _i = 0, elems_1 = elems; _i < elems_1.length; _i++) {
            var elem = elems_1[_i];
            _loop_1(elem);
        }
        // Verify that no "ui-" classes are used (remnant from jquery ui)
        // eslint-disable-next-line no-debugger
        elems = $YetaWF.getElementsBySelector("*");
        for (var _a = 0, elems_2 = elems; _a < elems_2.length; _a++) {
            var elem = elems_2[_a];
            if ($YetaWF.elementHasClassPrefix(elem, "ui-").length > 0) {
                var msg = "Element with class ui-... found: " + elem.outerHTML;
                $YetaWF.error(msg);
                console.log(msg);
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
            var msg = evMsg + " (" + url + ":" + lineNo + ") " + (error === null || error === void 0 ? void 0 : error.stack);
            $YetaWF.error(msg);
            console.log(msg);
            inDebug = false;
        }
    };
})(YetaWF_Core_Debugging || (YetaWF_Core_Debugging = {}));

//# sourceMappingURL=Debugging.js.map
