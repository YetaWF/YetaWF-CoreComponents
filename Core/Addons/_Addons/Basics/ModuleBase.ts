/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF {

    export interface ModuleDefinition {
        Selector: string;
        HasData: boolean;
        UserData: any;
        DestroyModule?: (tag: HTMLElement, module: any) => void;
    }

    export abstract class ModuleBase {

        // Module registration

        public static RegisteredModules: ModuleDefinition[] = [];

        public static register(selector: string, hasData: boolean, userData: any, destroyModule?: (tag: HTMLElement, module: any) => void): void {
            let found = ModuleBase.RegisteredModules.find((e: ModuleDefinition): boolean => {
                return (e.Selector === selector);
            });
            if (found)
                return;
            ModuleBase.RegisteredModules.push({
                Selector: selector, HasData: hasData, UserData: userData,
                DestroyModule: destroyModule
            });
        }

        // locating modules

        public static getModuleDivFromTagCond(elem: HTMLElement): HTMLDivElement | null {
            let module = ModuleBase.elementClosestModuleDivCond(elem);
            return module;
        }
        public static getModuleDivFromTag(elem: HTMLElement): HTMLDivElement {
            let module = ModuleBase.getModuleDivFromTagCond(elem);
            if (!module)
                throw `No module found in getModuleDivFromTag for ${elem.outerHTML}`;
            return module;
        }
        protected static elementClosestModuleDivCond(elem: HTMLElement): HTMLDivElement | null {
            let module = $YetaWF.elementClosestCond(elem, ".yModule");
            return module as HTMLDivElement | null;
        }

        public static getModuleDivs(selector: string, tags?: HTMLElement[]): HTMLDivElement[] {
            if (!tags)
                tags = [document.body];
            return $YetaWF.getElementsBySelector(selector, tags) as HTMLDivElement[];
        }

        public static getModuleDefinitionCond(selector: string): ModuleDefinition | null {
            let found = ModuleBase.RegisteredModules.find((e: ModuleDefinition): boolean => {
                return (e.Selector === selector);
            });
            return found || null;
        }
        public static getModuleDefinition(selector: string): ModuleDefinition {
            let found = ModuleBase.getModuleDefinitionCond(selector);
            if (!found)
                throw `Module definition for ${selector} not found`;
            return found;
        }
    }


    /** A module without internal data management. Based on a native div. */
    export abstract class ModuleBaseNoDataImpl extends ModuleBase {

        public readonly Module: HTMLDivElement;
        public readonly ModuleId: string;

        constructor(moduleId: string, selector: string, userData: any, destroyModule?: (tag: HTMLElement, module: any) => void, hasData?: boolean) {
            super();
            this.ModuleId = moduleId;
            this.Module = $YetaWF.getElementById(moduleId) as HTMLDivElement;
            this.registerModule(selector, userData, destroyModule, hasData);
        }
        public registerModule(selector: string, userData: any, destroyModule?: (tag: HTMLElement, module: any) => void, hasData?: boolean): void {
            ModuleBase.register(selector, hasData || false, userData, destroyModule);
        }
    }


    /** A module with internal data management. */
    export abstract class ModuleBaseDataImpl extends ModuleBaseNoDataImpl {

        constructor(moduleId: string, selector: string, userData: any, destroyModule?: (tag: HTMLElement, module: any) => void) {
            super(moduleId, selector, userData, destroyModule, true);
            $YetaWF.addObjectDataById(moduleId, this);
        }

        // Various ways to find the module object (using tag, selector or id)

        /**
         * Given an element within a module, find the containing module object.
         * @param elem The element within the module.
         * Returns null if not found.
         */
        public static getModuleFromTagCond<CLSS extends ModuleBaseDataImpl>(elem: HTMLElement): CLSS | null {
            let mod = ModuleBase.getModuleDivFromTagCond(elem);
            if (!mod)
                return null;
            let obj = $YetaWF.getObjectData(mod) as CLSS;
            if (obj.Module !== mod)
                throw `object data doesn't match module type - ${mod.outerHTML}`;
            return obj;
        }
        /**
         * Given an element within a module, find the containing module object.
         * @param elem The element within the module.
         */
        public static getModuleFromTag<CLSS extends ModuleBaseDataImpl>(elem: HTMLElement): CLSS {
            let obj = ModuleBaseDataImpl.getModuleFromTagCond<CLSS>(elem);
            if (obj == null)
                throw `Object not found - ${elem.outerHTML}`;
            return obj;
        }

        /**
         * Finds an module within tags using the provided module selector and returns the module object.
         * @param moduleSelector The module-specific selector.
         * @param tags The elements to search for the specified selector.
         * Returns null if not found.
         */
        public static getModuleFromSelectorCond<CLSS extends ModuleBaseDataImpl>(moduleSelector: string, tags: HTMLElement[]): CLSS | null {
            let mod = $YetaWF.getElement1BySelectorCond(moduleSelector, tags);
            if (mod == null)
                return null;
            let obj = $YetaWF.getObjectData(mod) as CLSS;
            return obj;
        }
        /**
         * Finds an module within tags using the provided module selector and returns the module object.
         * @param moduleSelector The module-specific selector.
         * @param tags The elements to search for the specified selector.
         */
        public static getModuleFromSelector<CLSS extends ModuleBaseDataImpl>(moduleSelector: string, tags: HTMLElement[]): CLSS {
            let mod = $YetaWF.getElement1BySelector(moduleSelector, tags);
            return $YetaWF.getObjectData(mod) as CLSS;
        }

        /**
         * Given an id of a module, returns the module object.
         * @param id The id of the module.
         * Returns null if not found.
         */
        public static getModuleByIdCond<CLSS extends ModuleBaseDataImpl>(id: string): CLSS | null {
            let mod = $YetaWF.getElementByIdCond(id);
            if (mod == null)
                return null;
            let obj = $YetaWF.getObjectData(mod) as CLSS;
            return obj;
        }
        /**
         * Given an id of an element, finds the module object.
         * @param id The id of the module.
         */
        public static getModuleById<CLSS extends ModuleBaseDataImpl>(id: string): CLSS {
            let mod = $YetaWF.getElementById(id);
            let obj = $YetaWF.getObjectData(mod) as CLSS;
            return obj;
        }

        public static getModules<CLSS extends ModuleBaseDataImpl>(selector: string, tags?: HTMLElement[]): CLSS[] {
            let objs: CLSS[] = [];
            let mods = ModuleBase.getModuleDivs(selector, tags);
            for (let mod of mods) {
                let obj = $YetaWF.getObjectData(mod) as CLSS;
                objs.push(obj);
            }
            return objs;
        }

        public destroy(): void {
            $YetaWF.removeObjectDataById(this.Module.id);
        }
    }

    // A <div> is being emptied. Destroy all modules the <div> may contain.
    $YetaWF.registerClearDiv(false, (tag: HTMLElement): boolean => {
        for (let moduleDef of ModuleBaseDataImpl.RegisteredModules) {
            if (moduleDef.HasData) {
                let list = $YetaWF.getElementsBySelector(moduleDef.Selector, [tag]);
                for (let module of list) {
                    let obj = $YetaWF.getObjectData(module) as ModuleBaseDataImpl;
                    if (obj.Module !== module)
                        throw `object data doesn't match module type - ${moduleDef.Selector} - ${module.outerHTML}`;
                    if (moduleDef.DestroyModule)
                        moduleDef.DestroyModule(tag, obj);
                    obj.destroy();
                }
            }
        }
        return true;
    });
}
