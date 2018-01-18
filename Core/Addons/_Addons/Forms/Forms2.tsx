/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF_Core {
    export class Forms {
        public static ValidateElement(elem: HTMLElement): void {
            if (typeof YetaWF_Forms !== "undefined" && YetaWF_Forms !== undefined) YetaWF_Forms.validateElement($(elem));
        }
    }
}
