"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    /** A control with internal data management. clearDiv must be called to clean up, typically used with $YetaWF.registerClearDiv. */
    var ComponentBaseDataImpl = /** @class */ (function () {
        function ComponentBaseDataImpl(controlId) {
            this.ControlId = controlId;
            this.Control = $YetaWF.getElementById(controlId);
            $YetaWF.addObjectDataById(controlId, this);
        }
        // Various ways to find the control object (using tag, selector or id)
        ComponentBaseDataImpl.getControlFromTagCond = function (elem, controlSelector) {
            var control = $YetaWF.elementClosestCond(elem, controlSelector);
            if (control == null)
                return null;
            var obj = $YetaWF.getObjectData(control);
            if (obj.Control !== control)
                throw "object data doesn't match control type - " + control.outerHTML;
            return obj;
        };
        ComponentBaseDataImpl.getControlFromTag = function (elem, controlSelector) {
            var obj = ComponentBaseDataImpl.getControlFromTagCond(elem, controlSelector);
            if (obj == null)
                throw "Object matching " + controlSelector + " not found";
            return obj;
        };
        ComponentBaseDataImpl.getControlFromSelectorCond = function (selector, controlSelector, tags) {
            var tag = $YetaWF.getElement1BySelectorCond(selector, tags);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        };
        ComponentBaseDataImpl.getControlFromSelector = function (selector, controlSelector, tags) {
            var tag = $YetaWF.getElement1BySelector(selector, tags);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        };
        ComponentBaseDataImpl.getControlByIdCond = function (id, controlSelector) {
            var tag = $YetaWF.getElementByIdCond(id);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        };
        ComponentBaseDataImpl.getControlById = function (id, controlSelector) {
            var tag = $YetaWF.getElementById(id);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        };
        ComponentBaseDataImpl.prototype.destroy = function () {
            $YetaWF.removeObjectDataById(this.Control.id);
        };
        /**
         * A <div> is being emptied. Destroy all controls described by controlSelector (the control type) and call the optional callback.
         */
        ComponentBaseDataImpl.clearDiv = function (tag, controlSelector, callback) {
            var list = $YetaWF.getElementsBySelector(controlSelector, [tag]);
            for (var _i = 0, list_1 = list; _i < list_1.length; _i++) {
                var el = list_1[_i];
                var control = ComponentBaseDataImpl.getControlFromTag(el, controlSelector);
                if (callback)
                    callback(control);
                control.destroy();
            }
        };
        return ComponentBaseDataImpl;
    }());
    YetaWF.ComponentBaseDataImpl = ComponentBaseDataImpl;
    // OBSOLETE!
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
            var control = $YetaWF.elementClosestCond(elem, controlSelector);
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
            for (var _i = 0, list_2 = list; _i < list_2.length; _i++) {
                var el = list_2[_i];
                callback(ComponentBase.getControlBaseFromTag(el, controlSelector));
            }
        };
        return ComponentBase;
    }());
    YetaWF.ComponentBase = ComponentBase;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=ComponentBase.js.map
