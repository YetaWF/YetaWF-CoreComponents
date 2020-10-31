/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF {

    export interface TemplateDefinition {
        Template: string;
        HasData: boolean;
        Selector: string;
        Display: boolean;
        UserData: any;
        DestroyControl?: (tag: HTMLElement, control: any) => void;
    }

    export abstract class ComponentBase {

        // Template registration

        public static RegisteredTemplates: TemplateDefinition[] = [];

        public static register(template: string, selector: string, hasData: boolean, userData: any, display?: boolean, destroyControl?: (tag: HTMLElement, control: any) => void): void {
            display = display ? true : false;
            let found = ComponentBase.RegisteredTemplates.find((e: TemplateDefinition): boolean => {
                return (e.Template === template && e.Display === display);
            });
            if (found)
                return;
            ComponentBase.RegisteredTemplates.push({
                Template: template, HasData: hasData, Selector: selector, UserData: userData, Display: display,
                DestroyControl: destroyControl
            });
        }

        public static getTemplateDefinitionCond(templateName: string, display?: boolean): TemplateDefinition | null {
            display = display ? true : false;
            let found = ComponentBase.RegisteredTemplates.find((e: TemplateDefinition): boolean => {
                return (e.Template === templateName && e.Display === display);
            });
            return found || null;
        }
        public static getTemplateDefinition(templateName: string, display?: boolean): TemplateDefinition {
            let found = ComponentBase.getTemplateDefinitionCond(templateName, display);
            if (!found)
                throw `Template ${templateName} not found`;
            return found;
        }

        public static getTemplateDefinitionFromTemplate(elem: HTMLElement): TemplateDefinition {
            let cls = $YetaWF.elementHasClassPrefix(elem, "yt_");
            if (cls.length === 0)
                throw `Template definition requested for element ${elem.outerHTML} that is not a template`;

            for (let cl of cls) {
                let templateDef: TemplateDefinition | null = null;
                if ($YetaWF.elementHasClass(elem, "t_display"))
                    templateDef = ComponentBase.getTemplateDefinitionCond(cl, true);
                if (!templateDef)
                    templateDef = ComponentBase.getTemplateDefinitionCond(cl, false);
                if (templateDef)
                    return templateDef;
            }
            throw `No template definition for element ${elem.outerHTML}`;
        }

        public static getTemplateFromControlNameCond(name: string, containers: HTMLElement[]): HTMLElement | null {
            let elem = $YetaWF.getElement1BySelectorCond(`[name='${name}']`, containers);
            if (!elem) {
                elem = $YetaWF.getElement1BySelectorCond(`[name^='${name}.']`, containers); // composite fields
                if (!elem)
                    return null;
                // we found an element (a composite field). This may be a template within another template (usually a propertylist)
                // so we use its parent element instead
                elem = elem.parentElement;
                if (!elem)
                    return null;
            }
            let template = ComponentBase.elementClosestTemplateCond(elem);
            if (!template)
                throw "No template found in getTemplateFromControlNameCond";
            return template;
        }
        public static getTemplateFromControlName(name: string, containers: HTMLElement[]): HTMLElement {
            let template = ComponentBase.getTemplateFromControlNameCond(name, containers);
            if (!template)
                throw "No template found in getTemplateFromControlName";
            return template;
        }
        public static getTemplateFromTagCond(elem: HTMLElement): HTMLElement | null {
            let template = ComponentBase.elementClosestTemplateCond(elem);
            return template;
        }
        public static getTemplateFromTag(elem: HTMLElement): HTMLElement {
            let template = ComponentBase.getTemplateFromTagCond(elem);
            if (!template)
                throw "No template found in getTemplateFromControlName";
            return template;
        }
        protected static elementClosestTemplateCond(elem: HTMLElement): HTMLElement | null {
            let template: HTMLElement | null = elem;
            while (template) {
                let cls = $YetaWF.elementHasClassPrefix(template, "yt_");
                if (cls.length > 0)
                    break;
                else
                    template = template.parentElement;
            }
            return template;
        }
    }


    /** A control without internal data management. Based on a native control. */
    export abstract class ComponentBaseNoDataImpl extends ComponentBase {

        public readonly Control: HTMLElement;
        public readonly ControlId: string;

        constructor(controlId: string, template: string, selector: string, userData: any, display?: boolean, destroyControl?: (tag: HTMLElement, control: any) => void, hasData?: boolean) {
            super();
            this.ControlId = controlId;
            this.Control = $YetaWF.getElementById(controlId);
            this.registerTemplate(template, selector, userData, display, destroyControl, hasData);
        }
        public registerTemplate(template: string, selector: string, userData: any, display?: boolean, destroyControl?: (tag: HTMLElement, control: any) => void, hasData?: boolean): void {
            ComponentBase.register(template, selector, hasData||false, userData, display, destroyControl);
        }
    }

    /** A control with internal data management. */
    export abstract class ComponentBaseDataImpl extends ComponentBaseNoDataImpl {

        constructor(controlId: string, template: string, selector: string, userData: any, display?: boolean, destroyControl?: (tag: HTMLElement, control: any) => void) {
            super(controlId, template, selector, userData, display, destroyControl, true);
            $YetaWF.addObjectDataById(controlId, this);
        }

        // Various ways to find the control object (using tag, selector or id)

        /**
         * Given an element within a component, find the containing component object.
         * @param elem The element within the component.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * Returns null if not found.
         */
        public static getControlFromTagCond<CLSS extends ComponentBaseDataImpl>(elem: HTMLElement, controlSelector: string): CLSS | null {
            let template = $YetaWF.elementClosestCond(elem, controlSelector);
            if (!template)
                return null;
            var control = $YetaWF.getElement1BySelectorCond(controlSelector, [template]) as HTMLElement;
            if (control == null)
                return null;
            var obj = $YetaWF.getObjectData(control) as CLSS;
            if (obj.Control !== control)
                throw `object data doesn't match control type - ${controlSelector} - ${control.outerHTML}`;
            return obj;
        }
        /**
         * Given an element within a component, find the containing component object.
         * @param elem The element within the component.
         * @param controlSelector The component-specific selector used to find the containing component object.
         */
        public static getControlFromTag<CLSS extends ComponentBaseDataImpl>(elem: HTMLElement, controlSelector: string): CLSS {
            var obj = ComponentBaseDataImpl.getControlFromTagCond<CLSS>(elem, controlSelector);
            if (obj == null)
                throw `Object matching ${controlSelector} not found`;
            return obj;
        }
        /**
         * Finds an element within tags using the provided selector and then finds the containing component object.
         * @param selector The selector used to find an element.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * @param tags The elements to search for the specified selector.
         * Returns null if not found.
         */
        public static getControlFromSelectorCond<CLSS extends ComponentBaseDataImpl>(selector: string, controlSelector: string, tags: HTMLElement[]): CLSS | null {
            var tag = $YetaWF.getElement1BySelectorCond(selector, tags);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        }
        /**
         * Finds an element within tags using the provided selector and then finds the containing component object.
         * @param selector The selector used to find an element.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * @param tags The elements to search for the specified selector.
         */
        public static getControlFromSelector<CLSS extends ComponentBaseDataImpl>(selector: string, controlSelector: string, tags: HTMLElement[]): CLSS {
            var tag = $YetaWF.getElement1BySelector(selector, tags);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        }
        /**
         * Given an id of an element, finds the containing component object.
         * @param id The id to find.
         * @param controlSelector The component-specific selector used to find the containing component object.
         * Returns null if not found.
         */
        public static getControlByIdCond<CLSS extends ComponentBaseDataImpl>(id: string, controlSelector: string): CLSS | null {
            var tag = $YetaWF.getElementByIdCond(id);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        }
        /**
         * Given an id of an element, finds the containing component object.
         * @param id The id to find.
         * @param controlSelector The component-specific selector used to find the containing component object.
         */
        public static getControlById<CLSS extends ComponentBaseDataImpl>(id: string, controlSelector: string): CLSS {
            var tag = $YetaWF.getElementById(id);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        }

        public static getControls<CLSS extends ComponentBaseDataImpl>(controlSelector: string, tags?: HTMLElement[]): CLSS[] {
            let objs: CLSS[] = [];
            let ctrls = $YetaWF.getElementsBySelector(controlSelector, tags);
            for (let ctrl of ctrls) {
                let obj = $YetaWF.getObjectData(ctrl) as CLSS;
                if (obj.Control !== ctrl)
                    throw `object data doesn't match control type - ${controlSelector} - ${ctrl.outerHTML}`;
                objs.push(obj);
            }
            return objs;
        }

        public destroy(): void {
            $YetaWF.removeObjectDataById(this.Control.id);
        }

        public registerTemplate(template: string, selector: string, userData: any, display?: boolean, destroyControl?: (tag: HTMLElement, control: any) => void): void {
            ComponentBase.register(template, selector, true, userData, display, destroyControl);
        }
    }

    // A <div> is being emptied. Destroy all controls the <div> may contain.
    $YetaWF.registerClearDiv(false, (tag: HTMLElement): boolean => {
        for (let templateDef of ComponentBaseDataImpl.RegisteredTemplates) {
            if (templateDef.HasData) {
                var list = $YetaWF.getElementsBySelector(templateDef.Selector, [tag]);
                for (let control of list) {
                    //if ($YetaWF.elementHasClass(control, "yt_propertylist"))
                    //    debugger;
                    var obj = $YetaWF.getObjectData(control) as ComponentBaseDataImpl;
                    if (obj.Control !== control)
                        throw `object data doesn't match control type - ${templateDef.Selector} - ${control.outerHTML}`;
                    if (templateDef.DestroyControl)
                        templateDef.DestroyControl(tag, obj);
                    obj.destroy();
                }
            }
        }
        return true;
    });
}





