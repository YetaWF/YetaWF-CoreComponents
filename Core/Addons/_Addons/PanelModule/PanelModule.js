"use strict";
/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/ComponentsHTML#License */
var YetaWF_Core;
(function (YetaWF_Core) {
    var PanelModuleHandler = /** @class */ (function () {
        function PanelModuleHandler() {
        }
        PanelModuleHandler.saveExpandCollapseStatus = function (guid, expanded) {
            // send save request, we don't care about the response
            var uri = $YetaWF.parseUrl("/YetaWF_Core/PanelModuleSaveSettings/SaveExpandCollapse");
            uri.addSearch("ModuleGuid", guid);
            uri.addSearch("Expanded", expanded.toString());
            var request = new XMLHttpRequest();
            request.open("POST", uri.toUrl(), true);
            request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            request.setRequestHeader("X-Requested-With", "XMLHttpRequest");
            request.send( /*uri.toFormData()*/);
        };
        return PanelModuleHandler;
    }());
    YetaWF_Core.PanelModuleHandler = PanelModuleHandler;
    $YetaWF.registerEventHandlerBody("click", ".modPanel .yModuleExpColl button", function (ev) {
        var exp = ev.__YetaWFElem;
        var mod = $YetaWF.elementClosestCond(exp, ".modPanel");
        if (!mod)
            return true;
        var modGuid = $YetaWF.getAttribute(mod, "data-moduleguid");
        var contents = $YetaWF.getElement1BySelector(".yModuleContents", [mod]);
        if (!contents)
            return true;
        if ($YetaWF.elementHasClass(mod, "t_expanded")) {
            $YetaWF.elementRemoveClasses(mod, ["t_expanded", "t_collapsed"]);
            $YetaWF.elementAddClass(mod, "t_collapsed");
            PanelModuleHandler.saveExpandCollapseStatus(modGuid, false);
            return false;
        }
        else if ($YetaWF.elementHasClass(mod, "t_collapsed")) {
            $YetaWF.elementRemoveClasses(mod, ["t_expanded", "t_collapsed"]);
            $YetaWF.elementAddClass(mod, "t_expanded");
            $YetaWF.sendActivateDivEvent([contents]); // init any controls that just became visible
            PanelModuleHandler.saveExpandCollapseStatus(modGuid, true);
            return false;
        }
        return true;
    });
})(YetaWF_Core || (YetaWF_Core = {}));

//# sourceMappingURL=PanelModule.js.map
