"use strict";
/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF_Core;
(function (YetaWF_Core) {
    var PanelModuleHandler = /** @class */ (function () {
        function PanelModuleHandler() {
        }
        PanelModuleHandler.saveExpandCollapseStatus = function (url, tag, expanded) {
            // send save request, we don't care about the response
            var uri = $YetaWF.parseUrl(url);
            var query = {
                Expanded: expanded,
            };
            var formJson = $YetaWF.Forms.getJSONInfo(tag);
            $YetaWF.postJSONIgnore(uri, formJson, query, null);
        };
        return PanelModuleHandler;
    }());
    YetaWF_Core.PanelModuleHandler = PanelModuleHandler;
    $YetaWF.registerEventHandlerBody("click", ".modPanel .yModuleExpColl button", function (ev) {
        var expElem = ev.__YetaWFElem;
        var mod = $YetaWF.elementClosestCond(expElem, ".modPanel");
        if (!mod)
            return true;
        var contents = $YetaWF.getElement1BySelector(".yModuleContents", [mod]);
        if (!contents)
            return true;
        var url = mod.getAttribute("data-url");
        if (!url)
            return true;
        if ($YetaWF.elementHasClass(mod, "t_expanded")) {
            // collapse
            PanelModuleHandler.saveExpandCollapseStatus(url, expElem, false);
            $YetaWF.animateHeight(contents, false, function () {
                $YetaWF.elementRemoveClasses(mod, ["t_expanded", "t_collapsed"]);
                $YetaWF.elementAddClass(mod, "t_collapsed");
            });
            return false;
        }
        else if ($YetaWF.elementHasClass(mod, "t_collapsed")) {
            $YetaWF.elementRemoveClasses(mod, ["t_expanded", "t_collapsed"]);
            $YetaWF.elementAddClass(mod, "t_expanded");
            $YetaWF.sendActivateDivEvent([contents]); // init any controls that just became visible
            PanelModuleHandler.saveExpandCollapseStatus(url, expElem, true);
            $YetaWF.animateHeight(contents, true, function () {
                contents.style.height = "auto";
            });
            return false;
        }
        return true;
    });
})(YetaWF_Core || (YetaWF_Core = {}));

//# sourceMappingURL=PanelModule.js.map
