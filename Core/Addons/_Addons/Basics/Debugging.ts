/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// This is only loaded in non-deployed builds

namespace YetaWF_Core_Debugging {

    $YetaWF.registerCustomEventHandlerDocument(YetaWF.Content.EVENTNAVPAGELOADED, null, (ev: CustomEvent<YetaWF.DetailsEventNavPageLoaded>): boolean => {

        // Verify that we don't have duplicate element ids, which would be an error (usually an incorrectly generated component). Id collisions due to SPA should not happen. This will pinpoint any such errors.

        let elems = $YetaWF.getElementsBySelector("[id]");
        let arr: HTMLElement[] = [];

        for (let elem of elems) {
            let id = elem.id;
            let found = arr.find((el: HTMLElement): boolean => { return el.id === id; });
            if (!found) {
                arr.push(elem);
            } else {
                $YetaWF.error(`Duplicate id ${id} in element ${elem.outerHTML} - like ${found.outerHTML}`);
            }
        }

        // Verify that no "ui-" classes are used (remnant from jquery ui)
        // eslint-disable-next-line no-debugger
        elems = $YetaWF.getElementsBySelector("*");
        for (let elem of elems) {
            if ($YetaWF.elementHasClassPrefix(elem, "ui-").length > 0)
                $YetaWF.error(`Element with class ui-... found: ${elem.outerHTML}`);
        }

        return true;
    });

    // Any JavaScript failures result in a popup so at least it's visible without explicitly looking at the console log.

    let inDebug = false;
    window.onerror = (ev: Event | string, url?: string, lineNo?: number, columnNo?: number, error?: Error): void => {
        if (!inDebug) {
            inDebug = true;

            let evMsg = ev.toString();
            // avoid recursive error with video controls. a bit hacky but this is just a debugging tool.
            if (evMsg.startsWith("ResizeObserver")) return;

            $YetaWF.error(`${evMsg} (${url}:${lineNo}) ${error?.stack}`);
            inDebug = false;
        }
    };

}