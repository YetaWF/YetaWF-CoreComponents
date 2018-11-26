"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    }
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var YetaWF;
(function (YetaWF) {
    /** A simple control without internal data management. */
    var ComponentBaseImpl = /** @class */ (function () {
        function ComponentBaseImpl(controlId) {
            this.ControlId = controlId;
            this.Control = $YetaWF.getElementById(controlId);
        }
        return ComponentBaseImpl;
    }());
    YetaWF.ComponentBaseImpl = ComponentBaseImpl;
    /** A control with internal data management. clearDiv must be called to clean up, typically used with $YetaWF.registerClearDiv. */
    var ComponentBaseDataImpl = /** @class */ (function (_super) {
        __extends(ComponentBaseDataImpl, _super);
        function ComponentBaseDataImpl(controlId) {
            var _this = _super.call(this, controlId) || this;
            $YetaWF.addObjectDataById(controlId, _this);
            return _this;
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
    }(ComponentBaseImpl));
    YetaWF.ComponentBaseDataImpl = ComponentBaseDataImpl;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=ComponentBase.js.map
