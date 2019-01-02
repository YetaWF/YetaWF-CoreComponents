/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF {

    /** A simple control without internal data management. */
    export abstract class ComponentBaseImpl {

        public readonly Control: HTMLElement;
        public readonly ControlId: string;

        constructor(controlId: string) {
            this.ControlId = controlId;
            this.Control = $YetaWF.getElementById(controlId);
        }
    }

    /** A control with internal data management. clearDiv must be called to clean up, typically used with $YetaWF.registerClearDiv. */
    export abstract class ComponentBaseDataImpl extends ComponentBaseImpl {

        constructor(controlId: string) {
            super(controlId);

            $YetaWF.addObjectDataById(controlId, this);
        }

        // Various ways to find the control object (using tag, selector or id)

        public static getControlFromTagCond<CLSS extends ComponentBaseDataImpl>(elem: HTMLElement, controlSelector: string): CLSS | null {
            var control = $YetaWF.elementClosestCond(elem, controlSelector) as HTMLElement;
            if (control == null)
                return null;
            var obj = $YetaWF.getObjectData(control) as CLSS;
            if (obj.Control !== control)
                throw `object data doesn't match control type - ${control.outerHTML}`;
            return obj;
        }
        public static getControlFromTag<CLSS extends ComponentBaseDataImpl>(elem: HTMLElement, controlSelector: string): CLSS {
            var obj = ComponentBaseDataImpl.getControlFromTagCond<CLSS>(elem, controlSelector);
            if (obj == null)
                throw `Object matching ${controlSelector} not found`;
            return obj;
        }
        public static getControlFromSelectorCond<CLSS extends ComponentBaseDataImpl>(selector: string, controlSelector: string, tags: HTMLElement[]): CLSS | null {
            var tag = $YetaWF.getElement1BySelectorCond(selector, tags);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        }
        public static getControlFromSelector<CLSS extends ComponentBaseDataImpl>(selector: string, controlSelector: string, tags: HTMLElement[]): CLSS {
            var tag = $YetaWF.getElement1BySelector(selector, tags);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        }
        public static getControlByIdCond<CLSS extends ComponentBaseDataImpl>(id: string, controlSelector: string): CLSS | null {
            var tag = $YetaWF.getElementByIdCond(id);
            if (tag == null)
                return null;
            return ComponentBaseDataImpl.getControlFromTagCond(tag, controlSelector);
        }
        public static getControlById<CLSS extends ComponentBaseDataImpl>(id: string, controlSelector: string): CLSS {
            var tag = $YetaWF.getElementById(id);
            return ComponentBaseDataImpl.getControlFromTag(tag, controlSelector);
        }

        public destroy(): void {
            $YetaWF.removeObjectDataById(this.Control.id);
        }

        /**
         * A <div> is being emptied. Destroy all controls described by controlSelector (the control type) and call the optional callback.
         */
        public static clearDiv<CLSS extends ComponentBaseDataImpl>(tag: HTMLElement, controlSelector: string, callback?: (control: CLSS) => void): void {
            var list = $YetaWF.getElementsBySelector(controlSelector, [tag]);
            for (let el of list) {
                var control = ComponentBaseDataImpl.getControlFromTag<CLSS>(el, controlSelector);
                if (callback)
                    callback(control);
                control.destroy();
            }
        }
    }
}





