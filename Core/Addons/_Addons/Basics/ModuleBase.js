"use strict";
/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
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
    var ModuleBase = /** @class */ (function () {
        function ModuleBase() {
        }
        ModuleBase.register = function (selector, hasData, userData, destroyModule) {
            var found = ModuleBase.RegisteredModules.find(function (e) {
                return (e.Selector === selector);
            });
            if (found)
                return;
            ModuleBase.RegisteredModules.push({
                Selector: selector, HasData: hasData, UserData: userData,
                DestroyModule: destroyModule
            });
        };
        // locating modules
        ModuleBase.getModuleDivFromTagCond = function (elem) {
            var module = ModuleBase.elementClosestModuleDivCond(elem);
            return module;
        };
        ModuleBase.getModuleDivFromTag = function (elem) {
            var module = ModuleBase.getModuleDivFromTagCond(elem);
            if (!module)
                throw "No module found in getModuleDivFromTag for " + elem.outerHTML;
            return module;
        };
        ModuleBase.elementClosestModuleDivCond = function (elem) {
            var module = $YetaWF.elementClosestCond(elem, ".yModule");
            return module;
        };
        ModuleBase.getModuleDivs = function (selector, tags) {
            if (!tags)
                tags = [document.body];
            return $YetaWF.getElementsBySelector(selector, tags);
        };
        ModuleBase.getModuleDefinitionCond = function (selector) {
            var found = ModuleBase.RegisteredModules.find(function (e) {
                return (e.Selector === selector);
            });
            return found || null;
        };
        ModuleBase.getModuleDefinition = function (selector) {
            var found = ModuleBase.getModuleDefinitionCond(selector);
            if (!found)
                throw "Module definition for " + selector + " not found";
            return found;
        };
        // Module registration
        ModuleBase.RegisteredModules = [];
        return ModuleBase;
    }());
    YetaWF.ModuleBase = ModuleBase;
    /** A module without internal data management. Based on a native div. */
    var ModuleBaseNoDataImpl = /** @class */ (function (_super) {
        __extends(ModuleBaseNoDataImpl, _super);
        function ModuleBaseNoDataImpl(moduleId, selector, userData, destroyModule, hasData) {
            var _this = _super.call(this) || this;
            _this.ModuleId = moduleId;
            _this.Module = $YetaWF.getElementById(moduleId);
            _this.registerModule(selector, userData, destroyModule, hasData);
            return _this;
        }
        ModuleBaseNoDataImpl.prototype.registerModule = function (selector, userData, destroyModule, hasData) {
            ModuleBase.register(selector, hasData || false, userData, destroyModule);
        };
        return ModuleBaseNoDataImpl;
    }(ModuleBase));
    YetaWF.ModuleBaseNoDataImpl = ModuleBaseNoDataImpl;
    /** A module with internal data management. */
    var ModuleBaseDataImpl = /** @class */ (function (_super) {
        __extends(ModuleBaseDataImpl, _super);
        function ModuleBaseDataImpl(moduleId, selector, userData, destroyModule) {
            var _this = _super.call(this, moduleId, selector, userData, destroyModule, true) || this;
            $YetaWF.addObjectDataById(moduleId, _this);
            return _this;
        }
        // Various ways to find the module object (using tag, selector or id)
        /**
         * Given an element within a module, find the containing module object.
         * @param elem The element within the module.
         * Returns null if not found.
         */
        ModuleBaseDataImpl.getModuleFromTagCond = function (elem) {
            var mod = ModuleBase.getModuleDivFromTagCond(elem);
            if (!mod)
                return null;
            var obj = $YetaWF.getObjectData(mod);
            if (obj.Module !== mod)
                throw "object data doesn't match module type - " + mod.outerHTML;
            return obj;
        };
        /**
         * Given an element within a module, find the containing module object.
         * @param elem The element within the module.
         */
        ModuleBaseDataImpl.getModuleFromTag = function (elem) {
            var obj = ModuleBaseDataImpl.getModuleFromTagCond(elem);
            if (obj == null)
                throw "Object not found - " + elem.outerHTML;
            return obj;
        };
        /**
         * Finds an module within tags using the provided module selector and returns the module object.
         * @param moduleSelector The module-specific selector.
         * @param tags The elements to search for the specified selector.
         * Returns null if not found.
         */
        ModuleBaseDataImpl.getModuleFromSelectorCond = function (moduleSelector, tags) {
            var mod = $YetaWF.getElement1BySelectorCond(moduleSelector, tags);
            if (mod == null)
                return null;
            var obj = $YetaWF.getObjectData(mod);
            return obj;
        };
        /**
         * Finds an module within tags using the provided module selector and returns the module object.
         * @param moduleSelector The module-specific selector.
         * @param tags The elements to search for the specified selector.
         */
        ModuleBaseDataImpl.getModuleFromSelector = function (moduleSelector, tags) {
            var mod = $YetaWF.getElement1BySelector(moduleSelector, tags);
            return $YetaWF.getObjectData(mod);
        };
        /**
         * Given an id of a module, returns the module object.
         * @param id The id of the module.
         * Returns null if not found.
         */
        ModuleBaseDataImpl.getModuleByIdCond = function (id) {
            var mod = $YetaWF.getElementByIdCond(id);
            if (mod == null)
                return null;
            var obj = $YetaWF.getObjectData(mod);
            return obj;
        };
        /**
         * Given an id of an element, finds the module object.
         * @param id The id of the module.
         */
        ModuleBaseDataImpl.getModuleById = function (id) {
            var mod = $YetaWF.getElementById(id);
            var obj = $YetaWF.getObjectData(mod);
            return obj;
        };
        ModuleBaseDataImpl.getModules = function (selector, tags) {
            var objs = [];
            var mods = ModuleBase.getModuleDivs(selector, tags);
            for (var _i = 0, mods_1 = mods; _i < mods_1.length; _i++) {
                var mod = mods_1[_i];
                var obj = $YetaWF.getObjectData(mod);
                objs.push(obj);
            }
            return objs;
        };
        ModuleBaseDataImpl.prototype.destroy = function () {
            $YetaWF.removeObjectDataById(this.Module.id);
        };
        return ModuleBaseDataImpl;
    }(ModuleBaseNoDataImpl));
    YetaWF.ModuleBaseDataImpl = ModuleBaseDataImpl;
    // A <div> is being emptied. Destroy all modules the <div> may contain.
    $YetaWF.registerClearDiv(false, function (tag) {
        for (var _i = 0, _a = ModuleBaseDataImpl.RegisteredModules; _i < _a.length; _i++) {
            var moduleDef = _a[_i];
            if (moduleDef.HasData) {
                var list = $YetaWF.getElementsBySelector(moduleDef.Selector, [tag]);
                for (var _b = 0, list_1 = list; _b < list_1.length; _b++) {
                    var module = list_1[_b];
                    var obj = $YetaWF.getObjectData(module);
                    if (obj.Module !== module)
                        throw "object data doesn't match module type - " + moduleDef.Selector + " - " + module.outerHTML;
                    if (moduleDef.DestroyModule)
                        moduleDef.DestroyModule(tag, obj);
                    obj.destroy();
                }
            }
        }
        return true;
    });
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=ModuleBase.js.map
