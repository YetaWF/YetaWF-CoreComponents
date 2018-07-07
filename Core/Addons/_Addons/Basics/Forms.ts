/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Forms API, to be implemented by rendering-specific code - rendering code must define a YetaWF_FormsImpl object implementing IFormsImpl */

interface IFormsImpl {
    initPartialFormTS(elem: HTMLElement): void;
    initPartialForm(elem: JQuery<HTMLElement>): void;
}
declare var YetaWF_FormsImpl: IFormsImpl;

namespace YetaWF {

    export class Forms {
        public initPartialFormTS(elem: HTMLElement): void {
            YetaWF_FormsImpl.initPartialFormTS(elem);
        }
        /**
         * Deprecated
         */
        public initPartialForm($elem: JQuery<HTMLElement>): void {
            YetaWF_FormsImpl.initPartialForm($elem);
        }
    }
}

var YetaWF_Forms : YetaWF.Forms = new YetaWF.Forms();