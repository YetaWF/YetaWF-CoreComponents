/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Forms API, to be implemented by rendering-specific code - rendering code must define a YetaWF_FormsImpl object implementing IFormsImpl */

declare var YetaWF_FormsImpl: YetaWF.IFormsImpl;

namespace YetaWF {

    export interface IFormsImpl {
        /**
         * Initializes a partial form.
         */
        initPartialForm(partialForm: HTMLElement): void;
        /**
         * Validate one element.
         * If the contents are empty the field will be fully validated. If contents are present, the error indicator is reset.
         * Full validation takes place on blur (or using validateElementFully).
         */
        validateElement(ctrl: HTMLElement, hasValue?: (value: any) => boolean): void;
        /**
         * Validate one element.
         * Full validation takes place.
         */
        validateElementFully(ctrl: HTMLElement): void;
        /**
         * Re-validates all fields within the div, typically used after paging in a grid to let validation update all fields
         */
        updateValidation(div: HTMLElement): void;
        /**
         * Clear any validation errors within the div
         */
        clearValidation(div: HTMLElement): void;
        /**
         * Clear validation error for one element
         */
        clearValidation1(elem: HTMLElement): void;

        /**
         * Returns whether a div has form errors.
         */
        hasErrors(elem: HTMLElement): boolean;
        /**
         * Shows all div form errors in a popup.
         */
        showErrors(elem: HTMLElement): void;

        /**
         * Validates all fields in the current form.
         */
        validate(form: HTMLFormElement): boolean;
        /**
         * Returns whether all fields in the current form are valid.
         */
        isValid(form: HTMLFormElement): boolean;
        /**
         * Serializes the form and returns an object
         */
        serializeFormObject(form: HTMLFormElement): any;

        /**
         * If there is a validation error in the specified tab control, the tab is activated.
         */
        setErrorInNestedControls(tag: HTMLElement): void;
        /**
         * Resequences array indexes in forms fields.
         * This is very much a work in progress and doesn't handle all controls.
         * All fields prefix[index].name are resequenced based on their position within the tags array.
         * This is typically used after adding/reordering entries.
         * @param rows Array of tags containing input fields to resequence.
         * @param prefix The name prefix used in input fields.
         */
        resequenceFields(rows: HTMLElement[], prefix: string): void;
    }

    export interface NameValuePair {
        name: string;
        value: string;
    }

    export interface IConfigs {
        Forms: IConfigsForms;
    }
    export interface IConfigsForms {

        // Global form related items (not implementation specific)
        UniqueIdCounters: string;

        // Css used which is global to YetaWF (not implementation specific)

        CssFormPartial: string;
        CssForm: string;
        CssFormNoSubmit: string;
        CssFormNoValidate: string;
        CssFormNoSubmitContents: string;
        CssFormCancel: string;
        CssDataApplyButton: string;
        CssWarningIcon: string;

        CssWarningIconUrl: string;
    }
    export interface ILocs {
        Forms: ILocsForms;
    }
    export interface ILocsForms {
        AjaxError: string;
        AjaxErrorTitle: string;
        AjaxNotAuth: string;
        AjaxConnLost: string;
        FormErrors: string;
    }

    interface ModuleSubmitData {
        Model: any;
        __Apply: boolean;
        __Reload: boolean;
        UniqueIdCounters: UniqueIdInfo;
        __Pagectl: boolean;
        __InPopup: boolean;
    }

    export interface SubmitHandlerEntry {
        form: HTMLElement | null;   // form <div> to be processed
        callback: (entry: SubmitHandlerEntry) => string|null; // function to be called - the callback returns extra data appended to the submit url
        userdata: any;              // any data suitable to callback
    }
    export interface FormInfoJSON {
        ModuleGuid: string;
        UniqueIdCounters?: UniqueIdInfo;
    }

    export enum PanelAction {
        Apply = 0,
        MoveLeft = 1,
        MoveRight = 2,
        Add = 3,
        Insert = 4,
        Remove = 5,
    }

    export interface DetailsPreSubmit {
        form: HTMLFormElement;
        customEventData?: any;
    }
    export interface DetailsPostSubmit {
        success: boolean;
        form: HTMLFormElement;
        customEventData?: any;
        response?: any;
    }

    export class Forms {

        public static readonly EVENTPRESUBMIT: string = "form_presubmit";
        public static readonly EVENTPOSTSUBMIT: string = "form_postsubmit";

        // Partial Form

        /**
         * Initialize a partial form.
         */
        public initPartialForm(elemId: string): void {

            let partialForm = $YetaWF.getElementById(elemId);

            // run registered actions (usually javascript initialization, similar to $doc.ready()
            $YetaWF.sendCustomEvent(document.body, Content.EVENTNAVPAGELOADED, { containers : [partialForm] });
            $YetaWF.processAllReadyOnce([partialForm]);

            YetaWF_FormsImpl.initPartialForm(partialForm);

            // show error popup
            let hasErrors = this.hasErrors(partialForm);
            if (hasErrors)
                this.showErrors(partialForm);
        }
        /**
         * Validate one element.
         * If the contents are empty the field will be fully validated. If contents are present, the error indicator is reset.
         * Full validation takes place on blur (or using validateElementFully).
         */
        public validateElement(ctrl: HTMLElement, hasValue?: (value: any) => boolean): void {
            YetaWF_FormsImpl.validateElement(ctrl, hasValue);
        }
        /**
         * Validate one element.
         * Full validation takes place.
         */
        public validateElementFully(ctrl: HTMLElement): void {
            YetaWF_FormsImpl.validateElementFully(ctrl);
        }
        /**
         * Re-validate all fields within the div, typically used after paging in a grid to let validation update all fields
         */
        public updateValidation(div: HTMLElement): void {
            YetaWF_FormsImpl.updateValidation(div);
        }
        /**
         * Clear any validation errors within the div
         */
        public clearValidation(div: HTMLElement): void {
            YetaWF_FormsImpl.clearValidation(div);
        }
        /**
         * Clear any validation errors within the div
         */
        public clearValidation1(elem: HTMLElement): void {
            YetaWF_FormsImpl.clearValidation1(elem);
        }

        /**
         * Returns whether the form has errors.
         */
        public hasErrors(elem: HTMLElement): boolean {
            return YetaWF_FormsImpl.hasErrors(elem);
        }
        /**
         * Shows all form errors in a popup.
         */
        public showErrors(elem: HTMLElement): void {
            YetaWF_FormsImpl.showErrors(elem);
        }
        /**
         * Serializes the form and returns an object
         */
        public serializeFormObject(form: HTMLFormElement): any {
            return YetaWF_FormsImpl.serializeFormObject(form);
        }
        /**
         * Validate all fields in the current form.
         */
        public validate(form: HTMLFormElement): boolean {
            return YetaWF_FormsImpl.validate(form);
        }
        /**
         * Returns whether all fields in the current form are valid.
         */
        public isValid(form: HTMLFormElement): boolean {
            return YetaWF_FormsImpl.isValid(form);
        }
        /**
         * Resequences array indexes in forms fields.
         */
        public resequenceFields(rows: HTMLElement[], prefix: string): void {
            return YetaWF_FormsImpl.resequenceFields(rows, prefix);
        }

        // Submit

        public DATACLASS: string = "yetawf_forms_data"; // add divs with this class to form for any data that needs to be submitted (will be removed before calling (pre)submit handlers.

        /**
         * Submit a form.
         * @param form The form being submitted.
         * @param useValidation Defines whether validation is performed before submission.
         * @param extraData Optional additional form data submitted.
         * @param customEventData
         * @returns Optional event information sent with EVENTPRESUBMIT/EVENTPOSTSUBMIT events as event.detail.customEventData.
         */
        public submit(form: HTMLFormElement, useValidation: boolean, extraData?: any, customEventData?: any): void {
            let method = form.getAttribute("method");
            if (!method) return; // no method, don't submit
            this.submitExplicit(form, method, form.action, useValidation, extraData, customEventData);
        }

        /**
         * Submit a form.
         * @param form The form being submitted.
         * @param method The method used to submit the form (typically post)
         * @param action The action URL used to submit the form.
         * @param useValidation Defines whether validation is performed before submission.
         * @param extraData Optional additional form data submitted.
         * @param customEventData
         * @returns Optional event information sent with EVENTPRESUBMIT/EVENTPOSTSUBMIT events as event.detail.customEventData.
         */
        public submitExplicit(form: HTMLFormElement, method: string, action: string, useValidation: boolean, extraData?: any, customEventData?: any): void  {

            $YetaWF.pageChanged = false;// suppress navigate error

            let divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (let div of divs)
                $YetaWF.removeElement(div);

            $YetaWF.sendCustomEvent(document.body, Forms.EVENTPRESUBMIT, { form : form, customEventData: customEventData, });

            let formValid = true;
            //$$$$ if (useValidation)
            //$$$$     formValid = this.validate(form);

            $YetaWF.closeOverlays();

            if (!useValidation || formValid) {

                if (method.toLowerCase() === "get")
                    throw "FORM GET not supported";

                const uri = $YetaWF.parseUrl(action);
                // serialize the form
                let model = this.serializeFormObject(form);
                let formData: ModuleSubmitData = {
                    Model: model,
                    __Apply: false,
                    __Reload: false,
                    UniqueIdCounters: YVolatile.Basics.UniqueIdCounters,
                    __Pagectl: YVolatile.Basics.PageControlVisible,
                    __InPopup: $YetaWF.isInPopup(),
                };

                // add extra data
                if (extraData) {
                    if (extraData[YConfigs.Basics.Link_SubmitIsApply] != null) {
                        formData.__Apply = extraData[YConfigs.Basics.Link_SubmitIsApply];
                        delete model[YConfigs.Basics.Link_SubmitIsApply];
                    }
                    if (extraData[YConfigs.Basics.Link_SubmitIsReload] != null) {
                        formData.__Reload = extraData[YConfigs.Basics.Link_SubmitIsReload];
                        delete model[YConfigs.Basics.Link_SubmitIsReload];
                    }
                }
                if (extraData)
                    uri.addSearchSimpleObject(extraData);

                const formJson = $YetaWF.Forms.getJSONInfo(form);
                $YetaWF.postJSON(uri, formJson, null, formData, (success: boolean, responseText: string): void => {
                    if (success) {
                        if (responseText) {
                            let partForm = $YetaWF.getElement1BySelectorCond("." + YConfigs.Forms.CssFormPartial, [form]);
                            if (partForm) {
                                // clean up everything that's about to be removed
                                $YetaWF.processClearDiv(partForm);
                                // preserve the original css classes on the partial form (PartialFormCss)
                                let cls = partForm.className;
                                $YetaWF.setMixedOuterHTML(partForm, responseText);
                                partForm = $YetaWF.getElement1BySelectorCond("." + YConfigs.Forms.CssFormPartial, [form]);
                                if (partForm)
                                    partForm.className = cls;
                            }
                        }
                        $YetaWF.sendCustomEvent(form, Forms.EVENTPOSTSUBMIT, { success: !this.hasErrors(form), form : form, customEventData: customEventData, response: responseText });
                        $YetaWF.setFocus([form]);
                    } else {
                        $YetaWF.sendCustomEvent(form, Forms.EVENTPOSTSUBMIT, { success: false, form : form, customEventData: customEventData, });
                    }
                });
            } else {
                // find the first field in each tab control that has an input validation error and activate that tab
                // This will not work for nested tabs. Only the lowermost tab will be activated.
                YetaWF_FormsImpl.setErrorInNestedControls(form);
                let hasErrors = this.hasErrors(form);
                if (hasErrors)
                    this.showErrors(form);
                // call callback (if any)
                $YetaWF.sendCustomEvent(form, Forms.EVENTPOSTSUBMIT, { success: false, form : form, customEventData: customEventData, });
            }
            divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (let div of divs)
                $YetaWF.removeElement(div);
        }

        public submitTemplate(tag: HTMLElement, useValidation: boolean, templateName: string, templateAction: PanelAction, templateExtraData: string) : void {
            let form = this.getForm(tag);
            if ($YetaWF.elementHasClass(form, YConfigs.Forms.CssFormNoSubmit)) return;
            let extraData = {};
            extraData[YConfigs.Basics.TemplateName] = templateName;
            extraData[YConfigs.Basics.Link_SubmitIsApply] = true;
            if (templateAction)
                extraData[YConfigs.Basics.TemplateAction] = templateAction;
            if (templateExtraData)
                extraData[YConfigs.Basics.TemplateExtraData] = templateExtraData;//$$$$
            this.submit(form, useValidation, extraData);
        }

        // Cancel

        /**
         * Cancels the current form (Cancel button handling).
         */
        public cancel(): void {
            this.goBack();
        }

        /**
         * Returns to the previous page.
         */
        public goBack(): void {
            if ($YetaWF.isInPopup()) {
                // we're in a popup, just close it
                $YetaWF.closePopup();
            } else {
                const state = history.state;
                if (state) {
                    history.back();
                } else {
                    // we don't know where to return so just close the browser
                    try {
                        window.close();
                    } catch (e) { }
                    try {
                        // TODO: use home page
                        let uri = $YetaWF.parseUrl("/");
                        $YetaWF.ContentHandling.setNewUri(uri);
                    } catch (e) { }
                }
            }
        }

        /**
         * Retrieve the form element containing the specified element tag.
         * An error occurs if no form can be found.
         * @param tag The element contained within a form.
         * @returns The form containing element tag.
         */
        public getForm(tag: HTMLElement): HTMLFormElement {
            return $YetaWF.elementClosest(tag, "form") as HTMLFormElement;
        }
        /**
         * Retrieve the form element containing the specified element tag.
         * @param tag The element contained within a form.
         * @returns The form containing element tag or null.
         */
        public getFormCond(tag: HTMLElement) : HTMLFormElement | null {
            let form = $YetaWF.elementClosestCond(tag, "form");
            if (!form) return null;
            return form as HTMLFormElement;
        }
        public getInnerForm(tag: HTMLElement): HTMLFormElement {
            return $YetaWF.getElement1BySelector("form", [tag]) as HTMLFormElement;
        }
        public getInnerFormCond(tag: HTMLElement): HTMLFormElement | null {
            return $YetaWF.getElement1BySelectorCond("form", [tag]) as HTMLFormElement | null;
        }
        // get ModuleGuid
        public getJSONInfo(tagInForm: HTMLElement, uniqueIdInfo?: UniqueIdInfo) : FormInfoJSON {
            let moduleGuid: string|null = null;
            const elem = $YetaWF.elementClosestCond(tagInForm, `[${YConfigs.Basics.CssModuleGuid}]`);
            if (elem) {
                moduleGuid = elem.getAttribute(YConfigs.Basics.CssModuleGuid) || "";
            } else {
                // we're not within a module or an element with an owning module
                moduleGuid = "";
            }
            const info: FormInfoJSON = {
                ModuleGuid: moduleGuid,
                UniqueIdCounters: uniqueIdInfo,
            };
            return info;
        }

        // Submit/apply on change/keydown

        public submitOnChange(elem: HTMLElement): void {
            clearInterval(this.submitFormTimer);
            this.submitForm = this.getForm(elem);
            this.submitFormTimer = setInterval(() : void => this.submitFormOnChange(), 1000);// wait 1 second and automatically submit the form
            $YetaWF.setLoading(true);
        }
        public submitOnReturnKey(elem: HTMLElement) : void {
            this.submitForm = this.getForm(elem);
            this.submitFormOnChange();
        }
        public applyOnChange(elem: HTMLElement) : void {
            clearInterval(this.submitFormTimer);
            this.submitForm = this.getForm(elem);
            this.submitFormTimer = setInterval((): void => this.applyFormOnChange(), 1000);// wait 1 second and automatically submit the form
            $YetaWF.setLoading(true);
        }
        public applyOnReturnKey(elem: HTMLElement) : void {
            this.submitForm = this.getForm(elem);
            this.applyFormOnChange();
        }
        public reloadOnChange(elem: HTMLElement): void {
            clearInterval(this.submitFormTimer);
            this.submitForm = this.getForm(elem);
            this.submitFormTimer = setInterval((): void => this.reloadFormOnChange(), 1000);// wait 1 second and automatically submit the form
            $YetaWF.setLoading(true);
        }
        public reloadOnReturnKey(elem: HTMLElement): void {
            this.submitForm = this.getForm(elem);
            this.reloadFormOnChange();
        }

        private submitFormTimer: number | undefined = undefined;
        private submitForm: HTMLFormElement | null = null;

        private submitFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm) return;
            if ($YetaWF.elementHasClass(this.submitForm, YConfigs.Forms.CssFormNoSubmit)) return;
            this.submit(this.submitForm, false);
        }
        private applyFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm) return;
            let extraData = {};
            extraData[YConfigs.Basics.Link_SubmitIsApply] = true;
            this.submit(this.submitForm, false, extraData);
        }
        private reloadFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm) return;
            let extraData = {};
            extraData[YConfigs.Basics.Link_SubmitIsReload] = true;
            this.submit(this.submitForm, false, extraData);
        }

        // submit form on change

        /**
         * Handles submitonchange/applyonchange
         */
        public initSubmitOnChange(): void {

            // submit
            $YetaWF.registerEventHandlerBody("change", ".ysubmitonchange select,.ysubmitonchange input[type=\"checkbox\"]", (ev: Event): boolean => {
                this.submitOnChange(ev.target as HTMLElement);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".ysubmitonchange select", (ev: KeyboardEvent): boolean => {
                if (ev.keyCode === 13) {
                    this.submitOnChange(ev.target as HTMLElement);
                    return false;
                }
                return true;
            });

            // apply

            $YetaWF.registerEventHandlerBody("change", ".yapplyonchange select,.yapplyonchange input[type=\"checkbox\"]", (ev: Event): boolean => {
                this.applyOnChange(ev.target as HTMLElement);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".yapplyonchange select", (ev: KeyboardEvent): boolean => {
                if (ev.keyCode === 13) {
                    this.applyOnChange(ev.target as HTMLElement);
                    return false;
                }
                return true;
            });

            // reload

            $YetaWF.registerEventHandlerBody("change", ".yreloadonchange select,.yreloadonchange input[type=\"checkbox\"]", (ev: Event): boolean => {
                this.reloadOnChange(ev.target as HTMLElement);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".yreloadonchange select", (ev: KeyboardEvent): boolean => {
                if (ev.keyCode === 13) {
                    this.reloadOnChange(ev.target as HTMLElement);
                    return false;
                }
                return true;
            });
        }

        /**
         * Initialize to handle Submit, Apply, Cancel buttons
         */
        public initHandleFormsButtons(): void {
            // Cancel the form when a Cancel button is clicked

            $YetaWF.registerEventHandlerBody("click", "form ." + YConfigs.Forms.CssFormCancel, (ev: MouseEvent):boolean => {
                this.cancel();
                return false;
            });

            // Submit the form when an apply button is clicked
            $YetaWF.registerEventHandlerBody("click", `form input[type="button"][${YConfigs.Forms.CssDataApplyButton}]`, (ev: MouseEvent) : boolean => {
                let form = this.getForm(ev.target as HTMLElement);
                let extraData = {};
                extraData[YConfigs.Basics.Link_SubmitIsApply] = true;
                this.submit(form, true, extraData);
                return false;
            });

            // Submit the form when a submit button is clicked

            $YetaWF.registerEventHandlerBody("submit", "form." + YConfigs.Forms.CssForm, (ev: Event) : boolean => {
                let form = this.getForm(ev.target as HTMLElement);
                if ($YetaWF.elementHasClass(form, YConfigs.Forms.CssFormNoSubmit)) return false;
                this.submit(form, true);
                return false;
            });
        }
        public init() : void {
            // initialize submit on change
            this.initSubmitOnChange();
            // initialize  Submit, Apply, Cancel button handling
            this.initHandleFormsButtons();
        }
    }
}


