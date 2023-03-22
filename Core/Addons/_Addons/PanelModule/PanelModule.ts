/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF_Core {

    export class PanelModuleHandler {
        public static saveExpandCollapseStatus(guid: string, expanded: boolean) : void {
            // send save request, we don't care about the response
            let uri = $YetaWF.parseUrl(`${YConfigs.Basics.ApiPrefix}/YetaWF_Core/PanelModuleSaveSettings/SaveExpandCollapse`);//$$$$Url
            uri.addSearch("ModuleGuid", guid);
            uri.addSearch("Expanded", expanded.toString());

            let request: XMLHttpRequest = new XMLHttpRequest();
            request.open("POST", uri.toUrl(), true);
            request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            request.send(/*uri.toFormData()*/);
        }
    }

    $YetaWF.registerEventHandlerBody("click",".modPanel .yModuleExpColl button", (ev: MouseEvent): boolean => {
        let exp = ev.__YetaWFElem;
        let mod = $YetaWF.elementClosestCond(exp, ".modPanel");
        if (!mod) return true;
        let modGuid = $YetaWF.getAttribute(mod, "data-moduleguid");
        let contents = $YetaWF.getElement1BySelector(".yModuleContents", [mod]);
        if (!contents) return true;

        if ($YetaWF.elementHasClass(mod, "t_expanded")) {
            $YetaWF.elementRemoveClasses(mod, ["t_expanded", "t_collapsed"]);
            $YetaWF.elementAddClass(mod, "t_collapsed");
            PanelModuleHandler.saveExpandCollapseStatus(modGuid, false);
            return false;
        } else if ($YetaWF.elementHasClass(mod, "t_collapsed")) {
            $YetaWF.elementRemoveClasses(mod, ["t_expanded", "t_collapsed"]);
            $YetaWF.elementAddClass(mod, "t_expanded");
            $YetaWF.sendActivateDivEvent([contents]);// init any controls that just became visible
            PanelModuleHandler.saveExpandCollapseStatus(modGuid, true);
            return false;
        }
        return true;
    });
}