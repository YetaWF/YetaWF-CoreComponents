/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
                let msg = `Duplicate id ${id} in element ${elem.outerHTML} - like ${found.outerHTML}`;
                $YetaWF.error(msg);
                console.error(msg);
            }
        }

        // Verify that no "ui-" classes are used (remnant from jquery ui)
        elems = $YetaWF.getElementsBySelector("*");
        for (let elem of elems) {
            if ($YetaWF.elementHasClassPrefix(elem, "ui-").length > 0) {
                let msg = `Element with class ui-... found: ${elem.outerHTML}`;
                $YetaWF.error(msg);
                console.error(msg);
            }
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

            let msg = `${evMsg} (${url}:${lineNo}) ${error?.stack}`;
            $YetaWF.error(msg);
            console.error(msg);

            inDebug = false;
        }
    };

    // Experimental "Click All Links" code - not currently used
    // Starts at current page (when testAll() is called and collects links and "clicks" each one by one)
    // Used to find JavaScript errors

    interface LinkEntry {
        url: string;
        anchor: HTMLAnchorElement;
    }

    export class Links {

        private Remaining: LinkEntry[] = [];
        private Done: string[] = [];
        private Running: boolean = false;

        // Anchors within parent elements with any of these CSS classes are not checked
        private IgnoredParentElements: string[] = [
            "Softelvdm_Documentation", // documentation pages
            "yt_grid", // actions in grids
        ];
        // Ignore these Urls
        private IgnoreUrls: string[] = [
            "/BlogEntry"
        ];
        // Use actual click handling for anchors in elements with any of these CSS classes
        // private ActualClicks: string[] = [
        //     "yt_panels_pagebarinfo_list",
        // ];

        public testAll(): void {
            if (this.isRunning) return;

            this.Remaining = [];
            this.Done = [];
            this.Running = true;

            this.nextPage();
        }

        public nextPage(): void {

            if (!LinksTest.isRunning) return;

            let todos = $YetaWF.getElementsBySelector("a") as HTMLAnchorElement[];
            for (let todo of todos) {
                let href = todo.href;
                if (!href || href.startsWith("javascript:"))
                    continue;
                if ($YetaWF.elementHasClass(todo, "DebugLoadTest"))
                    continue;
                if ($YetaWF.getAttributeCond(todo, "data-nohref") != null)
                    continue;
                let target = $YetaWF.getAttributeCond(todo, "target");
                if (target && target !== "" && target !== "_self")
                    continue;
                let rel = $YetaWF.getAttributeCond(todo, "rel");
                if (rel === "nofollow")
                    continue;
                if (!$YetaWF.elementHasClass(todo, "yaction-link"))
                    continue;

                let ignore = this.IgnoredParentElements.find((s: string):boolean => {
                    return $YetaWF.elementClosestCond(todo, `.${s}`) != null;
                });
                if (ignore)
                    continue;

                href = this.stripUrl(href);

                let uri = new YetaWF.Url();
                uri.parse(href);
                let path = uri.getPath();
                let foundPath = this.IgnoreUrls.find((s: string):boolean => {
                    return path === s;
                });
                if (foundPath)
                    continue;

                let found = this.Done.find((s: string):boolean => {
                    return href === s;
                });
                if (found)
                    continue;

                this.Remaining.push({ url: window.location.href, anchor: todo});
            }
            this.processRemainder();
        }

        private stripUrl(href: string): string {
            let uri = new YetaWF.Url();
            uri.parse(href);
            uri.setHash(null);
            href = uri.toUrl();
            return href;
        }

        private processRemainder(): void {
            while (this.Remaining.length > 0) {

                let linkEntry = this.Remaining[0];
                this.Remaining.splice(0, 1);

                let todo = linkEntry.anchor;

                let found = this.Done.find((s: string):boolean =>{
                    return todo.href === s;
                });
                if (found)
                    continue;

                console.log(`Debug(${this.Remaining.length}): Clicking ${todo.href} on ${linkEntry.url} - ${todo.outerHTML}`);
                this.Done.push(this.stripUrl(todo.href));

                // let actual = this.ActualClicks.find((s: string):boolean => {
                //     return $YetaWF.elementClosestCond(todo, `.${s}`) != null;
                // });
                // if (actual) {
                //     todo.click();// click it and wait for page content
                // } else {
                let uri = new YetaWF.Url();
                uri.parse(todo.href);
                if ($YetaWF.ContentHandling.setContent(uri, true) !== YetaWF.SetContentResult.ContentReplaced) {
                    // some how this wasn't possible
                    console.error(`setContent failed for ${todo.href}`);
                    setTimeout(():void => { this.nextPage(); }, 1);
                }
                //}

                return;
            }
            console.log("Debug: All pages processed");
        }
        public get isRunning(): boolean {
            return this.Running;
        }
    }

    // handle new content
    $YetaWF.registerCustomEventHandlerDocument(YetaWF.Content.EVENTNAVPAGELOADED, null, (ev: CustomEvent<YetaWF.DetailsEventNavPageLoaded>): boolean => {
        LinksTest.nextPage();
        return true;
    });

    let LinksTest = new Links();
    let a = $YetaWF.getElement1BySelectorCond(".YetaWF_Menus_MainMenu a.DebugLoadTest");
    if (a) {
        $YetaWF.registerEventHandler(a, "click", null, (ev: MouseEvent): boolean => {
            LinksTest.testAll();
            return false;
        });
    }
}