"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    var Forms = /** @class */ (function () {
        function Forms() {
        }
        Forms.prototype.initPartialFormTS = function (elem) {
            YetaWF_FormsImpl.initPartialFormTS(elem);
        };
        /**
         * Deprecated
         */
        Forms.prototype.initPartialForm = function ($elem) {
            YetaWF_FormsImpl.initPartialForm($elem);
        };
        return Forms;
    }());
    YetaWF.Forms = Forms;
})(YetaWF || (YetaWF = {}));
var YetaWF_Forms = new YetaWF.Forms();

//# sourceMappingURL=Forms.js.map
