"use strict";
/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    var Utility = /** @class */ (function () {
        function Utility() {
        }
        Utility.formatDateTimeUTC = function (dateVal) {
            var s = "";
            if (dateVal)
                s = "".concat(dateVal.getUTCFullYear(), "-").concat(Utility.zeroPad(dateVal.getUTCMonth() + 1, 2), "-").concat(Utility.zeroPad(dateVal.getUTCDate(), 2), "T").concat(Utility.zeroPad(dateVal.getUTCHours(), 2), ":").concat(Utility.zeroPad(dateVal.getUTCMinutes(), 2), ":00.000Z");
            return s;
        };
        Utility.formatDateUTC = function (dateVal) {
            var s = "";
            if (dateVal)
                s = "".concat(dateVal.getUTCFullYear(), "-").concat(this.zeroPad(dateVal.getUTCMonth() + 1, 2), "-").concat(this.zeroPad(dateVal.getUTCDate(), 2), "T00:00:00.000Z");
            return s;
        };
        Utility.zeroPad = function (val, pos) {
            if (val < 0)
                return val.toFixed();
            var s = val.toFixed(0);
            while (s.length < pos)
                s = "0" + s;
            return s;
        };
        return Utility;
    }());
    YetaWF.Utility = Utility;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Utility.js.map
