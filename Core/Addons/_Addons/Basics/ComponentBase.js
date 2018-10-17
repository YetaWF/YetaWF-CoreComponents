"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    var ComponentBase = /** @class */ (function () {
        function ComponentBase(controlId) {
            this.ControlId = controlId;
            this.Control = $YetaWF.getElementById(controlId);
        }
        ComponentBase.getControlBaseFromTag = function (elem, controlSelector) {
            var obj = ComponentBase.getControlBaseFromTagCond(elem, controlSelector);
            if (obj == null)
                throw "Object matching " + controlSelector + " not found";
            return obj;
        };
        ComponentBase.getControlBaseFromTagCond = function (elem, controlSelector) {
            var control = $YetaWF.elementClosest(elem, controlSelector);
            if (control == null)
                return null;
            var obj = $YetaWF.getObjectData(control);
            if (obj.Control !== control)
                throw "object data doesn't match control type - " + control.outerHTML;
            return obj;
        };
        ComponentBase.getControlBaseFromSelector = function (selector, controlSelector, tags) {
            var tag = $YetaWF.getElement1BySelector(selector, tags);
            return ComponentBase.getControlBaseFromTag(tag, controlSelector);
        };
        ComponentBase.getControlBaseById = function (id, controlSelector) {
            var tag = $YetaWF.getElementById(id);
            return ComponentBase.getControlBaseFromTag(tag, controlSelector);
        };
        /**
         * A <div> is being emptied. Call the callback for the control type described by controlSelector.
         */
        ComponentBase.clearDiv = function (tag, controlSelector, callback) {
            var list = $YetaWF.getElementsBySelector(controlSelector, [tag]);
            for (var _i = 0, list_1 = list; _i < list_1.length; _i++) {
                var el = list_1[_i];
                callback(ComponentBase.getControlBaseFromTag(el, controlSelector));
            }
        };
        return ComponentBase;
    }());
    YetaWF.ComponentBase = ComponentBase;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=ComponentBase.js.map
