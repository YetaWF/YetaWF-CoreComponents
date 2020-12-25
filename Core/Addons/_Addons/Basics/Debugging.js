"use strict";
/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
// This is only loaded in non-deployed builds
var YetaWF_Core_Debugging;
(function (YetaWF_Core_Debugging) {
    // Verify that we don't have duplicate element ids, which would be an error (usually an incorrectly generated component). Id collisions due to SPA should not happen. This will pinpoint any such errors.
    $YetaWF.registerCustomEventHandlerDocument(YetaWF.Content.EVENTNAVPAGELOADED, null, function (ev) {
        var elems = $YetaWF.getElementsBySelector("[id]");
        var arr = [];
        var _loop_1 = function (elem) {
            var id = elem.id;
            var found = arr.find(function (el) { return el.id === id; });
            if (!found) {
                arr.push(elem);
            }
            else {
                $YetaWF.error("Duplicate id " + id + " in element " + elem.outerHTML + " - like " + found.outerHTML);
            }
        };
        for (var _i = 0, elems_1 = elems; _i < elems_1.length; _i++) {
            var elem = elems_1[_i];
            _loop_1(elem);
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
            $YetaWF.error(evMsg + " (" + url + ":" + lineNo + ") " + (error === null || error === void 0 ? void 0 : error.stack));
            inDebug = false;
        }
    };
})(YetaWF_Core_Debugging || (YetaWF_Core_Debugging = {}));

//# sourceMappingURL=Debugging.js.map