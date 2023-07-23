/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF_Core {

    export class PanelModuleHandler {
        public static saveExpandCollapseStatus(url: string, tag: HTMLElement, expanded: boolean) : void {
            // send save request, we don't care about the response
            let uri = $YetaWF.parseUrl(url);
            let query = {
                Expanded: expanded,
            };
            const formJson = $YetaWF.Forms.getJSONInfo(tag);
            $YetaWF.postJSONIgnore(uri, formJson, query, null);
        }
    }

    $YetaWF.registerEventHandlerBody("click",".modPanel .yModuleExpColl button", (ev: MouseEvent): boolean => {
        let expElem = ev.__YetaWFElem;
        let mod = $YetaWF.elementClosestCond(expElem, ".modPanel");
        if (!mod) return true;
        let contents = $YetaWF.getElement1BySelector(".yModuleContents", [mod]);
        if (!contents) return true;
        const url = mod.getAttribute("data-url");
        if (!url) return true;

        if ($YetaWF.elementHasClass(mod, "t_expanded")) {
            // collapse
            PanelModuleHandler.saveExpandCollapseStatus(url, expElem, false);
            $YetaWF.animateHeight(contents, false, (): void => {
                $YetaWF.elementRemoveClasses(mod!, ["t_expanded", "t_collapsed"]);
                $YetaWF.elementAddClass(mod!, "t_collapsed");
            });
            return false;
        } else if ($YetaWF.elementHasClass(mod, "t_collapsed")) {
            $YetaWF.elementRemoveClasses(mod, ["t_expanded", "t_collapsed"]);
            $YetaWF.elementAddClass(mod, "t_expanded");
            $YetaWF.sendActivateDivEvent([contents]);// init any controls that just became visible
            PanelModuleHandler.saveExpandCollapseStatus(url, expElem, true);
            $YetaWF.animateHeight(contents, true);
            return false;
        }
        return true;
    });
}