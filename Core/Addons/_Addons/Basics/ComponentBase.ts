/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF {

    export abstract class ComponentBase<T extends HTMLElement> {

        public readonly Control: T;

        constructor(controlId: string) {
            this.Control = $YetaWF.getElementById(controlId) as T;
        }

        public static getControlBaseFromTag<T extends ComponentBase<HTMLElement>>(elem: HTMLElement, controlSelector: string): T {
            var control = $YetaWF.elementClosest(elem, controlSelector) as HTMLElement;
            var obj = $YetaWF.getObjectData(control) as T;
            if (obj.Control !== control) throw `object data doesn't match control type - {control.outerHTML}`;
            return obj;
        }
        public static getControlBaseFromSelector<T extends ComponentBase<HTMLElement>>(selector: string, controlSelector: string, tags: HTMLElement[]): T {
            var tag = $YetaWF.getElement1BySelector(selector, tags);
            return ComponentBase.getControlBaseFromTag(tag, controlSelector);
        }
        public static getControlBaseById<T extends ComponentBase<HTMLElement>>(id: string, controlSelector: string): T {
            var tag = $YetaWF.getElementById(id);
            return ComponentBase.getControlBaseFromTag(tag, controlSelector);
        }

        /**
         * A <div> is being emptied. Call the callback for the control type described by controlSelector.
         */
        public static clearDiv<T extends ComponentBase<HTMLElement>>(tag: HTMLElement, controlSelector: string, callback: (control: T) => void): void {
            var list = $YetaWF.getElementsBySelector(controlSelector, [tag]);
            for (let el of list) {
                callback(ComponentBase.getControlBaseFromTag(el, controlSelector) as T);
            }
        }
    }

}

