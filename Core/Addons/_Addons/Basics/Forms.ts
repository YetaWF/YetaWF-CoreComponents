/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Forms API, to be implemented by rendering-specific code - rendering code must define a YetaWF_FormsImpl object implementing IFormsImpl */

interface IFormsImpl {
    initPartialFormTS(elem: HTMLElement): void;
    initPartialForm(elem: JQuery<HTMLElement>): void;
}
declare var YetaWF_FormsImpl: IFormsImpl;

namespace YetaWF {

    export class Forms {

        /**
         * Initialize a partial form.
         */
        public initPartialFormTS(elem: HTMLElement): void {
            YetaWF_FormsImpl.initPartialFormTS(elem);
        }
        /**
         * Initialize a partial form.
         */
        public initPartialForm($elem: JQuery<HTMLElement>): void {
            YetaWF_FormsImpl.initPartialForm($elem);
        }

        // submit form on change

        /**
         * Handles submitonchange/applyonchange
         */
        public initSubmitOnChange(): void {

            // submit

            $('body').on('keyup', '.ysubmitonchange select', (e) => {
                if (e.keyCode == 13) {
                    this.submitForm = (YetaWF_Forms as any/*$$$*/).getForm(e.currentTarget);
                    this.submitFormOnChange();
                }
            });
            $('body').on('change', '.ysubmitonchange select,.ysubmitonchange input[type="checkbox"]', (e) => {
                clearInterval(this.submitFormTimer);
                this.submitForm = (YetaWF_Forms as any/*$$$*/).getForm(e.currentTarget);
                this.submitFormTimer = setInterval(() => this.submitFormOnChange(), 1000);// wait 1 second and automatically submit the form
                YetaWF_Basics.setLoading(true);
            });

            // apply

            $('body').on('keyup', '.yapplyonchange select,.yapplyonchange input[type="checkbox"]', (e) => {
                if (e.keyCode == 13) {
                    this.submitForm = (YetaWF_Forms as any/*$$$*/).getForm(e.currentTarget);
                    this.applyFormOnChange();
                }
            });
            $('body').on('change', '.yapplyonchange select', (e) => {
                clearInterval(this.submitFormTimer);
                this.submitForm = (YetaWF_Forms as any/*$$$*/).getForm(e.currentTarget);
                this.submitFormTimer = setInterval(() => this.applyFormOnChange(), 1000);// wait 1 second and automatically submit the form
                YetaWF_Basics.setLoading(true);
            });
        }

        private submitFormTimer: number | undefined = undefined;
        private submitForm = null;

        private submitFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            (YetaWF_Forms as any/*$$$*/).submit(this.submitForm, false);
        }
        private applyFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            (YetaWF_Forms as any/*$$$*/).submit(this.submitForm, false, YGlobals.Link_SubmitIsApply + "=y");
        }
    }
}

var YetaWF_Forms: YetaWF.Forms = new YetaWF.Forms();

// initialize submit on change
YetaWF_Forms.initSubmitOnChange();
