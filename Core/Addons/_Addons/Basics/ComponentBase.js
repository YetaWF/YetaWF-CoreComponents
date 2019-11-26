"use strict";
/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var YetaWF;
(function (YetaWF) {
    var ComponentBase = /** @class */ (function () {
        function ComponentBase() {
        }
        ComponentBase.register = function (template, selector, hasData, userData, display, destroyControl) {
            display = display ? true : false;
            var found = ComponentBase.RegisteredTemplates.find(function (e) {
                return (e.Template === template && e.Display === display);
            });
            if (found)
                return;
            ComponentBase.RegisteredTemplates.push({
                Template: template, HasData: hasData, Selector: selector, UserData: userData, Display: display,
                DestroyControl: destroyControl
            });
        };
        ComponentBase.getTemplateDefinitionCond = function (templateName, display) {
            display = display ? true : false;
            var found = ComponentBase.RegisteredTemplates.find(function (e) {
                return (e.Template === templateName && e.Display === display);
            });
            return found || null;
        };
        ComponentBase.getTemplateDefinition = function (templateName, display) {
            var found = ComponentBase.getTemplateDefinitionCond(templateName, display);
            if (!found)
                throw "Template " + templateName + " not found";
            return found;
        };
        ComponentBase.getTemplateDefinitionFromTemplate = function (elem) {
            var cls = $YetaWF.elementHasClassPrefix(elem, "yt_");
            if (cls.length === 0)
                throw "Template definition requested for element " + elem.outerHTML + " that is not a template";
            for (var _i = 0, cls_1 = cls; _i < cls_1.length; _i++) {
                var cl = cls_1[_i];
                var templateDef = null;
                if ($YetaWF.elementHasClass(elem, "t_display"))
                    templateDef = ComponentBase.getTemplateDefinitionCond(cl, true);
                if (!templateDef)
                    templateDef = ComponentBase.getTemplateDefinitionCond(cl, false);
                if (templateDef)
                    return templateDef;
            }
            throw "No template definition for element " + elem.outerHTML;
        };
        ComponentBase.getTemplateFromControlNameCond = function (name, containers) {
            var elem = $YetaWF.getElement1BySelectorCond("[name='" + name + "']", containers);
            if (!elem) {
                elem = $YetaWF.getElement1BySelectorCond("[name^='" + name + ".']", containers); // composite fields
                if (!elem)
                    return null;
                // we found an element (a composite field). This may be a template within another template (usually a propertylist)
                // so we use its parent element instead
                elem = elem.parentElement;
                if (!elem)
                    return null;
            }
            var template = ComponentBase.elementClosestTemplateCond(elem);
            if (!template)
                throw "No template found in getTemplateFromControlNameCond";
            return template;
        };
        ComponentBase.getTemplateFromControlName = function (name, containers) {
            var template = ComponentBase.getTemplateFromControlNameCond(name, containers);
            if (!template)
                throw "No template found in getTemplateFromControlName";
            return template;
        };
        ComponentBase.getTemplateFromTagCond = function (elem) {
            var template = ComponentBase.elementClosestTemplateCond(elem);
            return template;
        };
        ComponentBase.getTemplateFromTag = function (elem) {
            var template = ComponentBase.getTemplateFromTagCond(elem);
            if (!template)
                throw "No template found in getTemplateFromControlName";
            return template;
        };
        ComponentBase.elementClosestTemplateCond = function (elem) {
            var template = elem;
            while (template) {
                var cls = $YetaWF.elementHasClassPrefix(template, "yt_");
                if (cls.length > 0)
                    break;
                else
                    template = template.parentElement;
            }
            return template;
        };
        // Template registration
        ComponentBase.RegisteredTemplates = [];
        return ComponentBase;
    }());
    YetaWF.ComponentBase = ComponentBase;
    /** A control without internal data management. Based on a native control. */
    var ComponentBaseNoDataImpl = /** @class */ (function (_super) {
        __extends(ComponentBaseNoDataImpl, _super);
        function ComponentBaseNoDataImpl(controlId, template, selector, userData, display, destroyControl, hasData) {
            var _this = _super.call(this) || this;
            _this.ControlId = controlId;
            _this.Control = $YetaWF.getElementById(controlId);
            _this.registerTemplate(template, selector, userData, display, destroyControl, hasData);
            return _this;
        }
        ComponentBaseNoDataImpl.prototype.registerTemplate = function (template, selector, userData, display, destroyControl, hasData) {
            ComponentBase.register(template, selector, hasData || false, userData, display, destroyControl);
        };
        return ComponentBaseNoDataImpl;
    }(ComponentBase));
    YetaWF.ComponentBaseNoDataImpl = ComponentBaseNoDataImpl;
    /** A control with internal data management. */
    var ComponentBaseDataImpl = /** @class */ (function (_super) {
        __extends(ComponentBaseDataImpl, _super);
        function ComponentBaseDataImpl(controlId, template, selector, userData, display, destroyControl) {
            var _this = _super.call(this, controlId, template, selector, userData, display, destroyControl, true) || this;
            $YetaWF.addObjectDataById(controlId, _this);
            return _this;
        }
        // Various ways to find the control object (using tag, selector or id)
        /**
         * Given an element within a component, find the containing component object.
         * @param elem The element within the component.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * Returns null if not found.
         */
        ComponentBaseDataImpl.getControlFromTagCond = function (elem, controlSelector) {
            var template = $YetaWF.elementClosestCond(elem, controlSelector);
            if (!template)
                return null;
            var control = $YetaWF.getElement1BySelectorCond(controlSelector, [template]);
            if (control == null)
                return null;
            var obj = $YetaWF.getObjectData(control);
            if (obj.Control !== control)
                throw "object data doesn't match control type - " + controlSelector + " - " + control.outerHTML;
            return obj;
        };
        /**
         * Given an element within a component, find the containing component object.
         * @param elem The element within the component.
         * @param controlSelector The component-specific selector used to find the containing component object.
         */
        ComponentBaseDataImpl.getControlFromTag = function (elem, controlSelector) {
            var obj = ComponentBaseDataImpl.getControlFromTagCond(elem, controlSelector);
            if (obj == null)
                throw "Object matching " + controlSelector + " not found";
            return obj;
        };
        /**
         * Finds an element within tags using the provided selector and then finds the containing component object.
         * @param selector The selector used to find an element.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * @param tags The elements to search for the specified selector.
         * Returns null if not found.
         */
        ComponentBaseDataImpl.getControlFromSelectorCond = function (selector, controlSelector, tags) {
            var tag = $YetaWF.getElement1BySelectorCond(selector, tags);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        };
        /**
         * Finds an element within tags using the provided selector and then finds the containing component object.
         * @param selector The selector used to find an element.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * @param tags The elements to search for the specified selector.
         */
        ComponentBaseDataImpl.getControlFromSelector = function (selector, controlSelector, tags) {
            var tag = $YetaWF.getElement1BySelector(selector, tags);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        };
        /**
         * Given an id of an element, finds the containing component object.
         * @param id The id to find.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * Returns null if not found.
         */
        ComponentBaseDataImpl.getControlByIdCond = function (id, controlSelector) {
            var tag = $YetaWF.getElementByIdCond(id);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        };
        /**
         * Given an id of an element, finds the containing component object.
         * @param id The id to find.
         * @param controlSelector The component-specific selector used to find the containing component object.
         */
        ComponentBaseDataImpl.getControlById = function (id, controlSelector) {
            var tag = $YetaWF.getElementById(id);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        };
        ComponentBaseDataImpl.prototype.destroy = function () {
            $YetaWF.removeObjectDataById(this.Control.id);
        };
        ComponentBaseDataImpl.prototype.registerTemplate = function (template, selector, userData, display, destroyControl) {
            ComponentBase.register(template, selector, true, userData, display, destroyControl);
        };
        return ComponentBaseDataImpl;
    }(ComponentBaseNoDataImpl));
    YetaWF.ComponentBaseDataImpl = ComponentBaseDataImpl;
    // A <div> is being emptied. Destroy all controls the <div> may contain.
    $YetaWF.registerClearDiv(function (tag) {
        for (var _i = 0, _a = ComponentBaseDataImpl.RegisteredTemplates; _i < _a.length; _i++) {
            var templateDef = _a[_i];
            if (templateDef.HasData) {
                var list = $YetaWF.getElementsBySelector(templateDef.Selector, [tag]);
                for (var _b = 0, list_1 = list; _b < list_1.length; _b++) {
                    var control = list_1[_b];
                    //if ($YetaWF.elementHasClass(control, "yt_propertylist"))
                    //    debugger;
                    var obj = $YetaWF.getObjectData(control);
                    if (obj.Control !== control)
                        throw "object data doesn't match control type - " + templateDef.Selector + " - " + control.outerHTML;
                    if (templateDef.DestroyControl)
                        templateDef.DestroyControl(tag, obj);
                    obj.destroy();
                }
            }
        }
    });
})(YetaWF || (YetaWF = {}));
