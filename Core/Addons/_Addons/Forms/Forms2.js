"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF_Core;
(function (YetaWF_Core) {
    var Forms = /** @class */ (function () {
        function Forms() {
        }
        Forms.ValidateElement = function (elem) {
            if (typeof YetaWF_Forms !== "undefined" && YetaWF_Forms !== undefined)
                YetaWF_Forms.validateElement($(elem));
        };
        return Forms;
    }());
    YetaWF_Core.Forms = Forms;
})(YetaWF_Core || (YetaWF_Core = {}));

//# sourceMappingURL=Forms2.js.map
