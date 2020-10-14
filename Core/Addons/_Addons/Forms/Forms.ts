/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
         * Re-validates all fields within the div, typically used after paging in a grid to let jquery.validate update all fields
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
         * Serializes the form and returns a name/value pairs array
         */
        serializeFormArray(form: HTMLFormElement): NameValuePair[];
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

    export interface IVolatile {
        Forms: IVolatileForms;
    }
    export interface IVolatileForms {
        TabStyle: TabStyleEnum;
    }
    export enum TabStyleEnum {
        JQuery = 0,
        Kendo = 1,
    }

    export interface IConfigs {
        Forms: IConfigsForms;
    }
    export interface IConfigsForms {

        // Global form related items (not implementation specific)
        UniqueIdCounters: string;
        RequestVerificationToken: string;

        // Css used which is global to YetaWF (not implementation specific)

        CssFormPartial: string;
        CssFormAjax: string;
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

    export interface SubmitHandlerEntry {
        form: HTMLElement | null;   // form <div> to be processed
        callback: (entry: SubmitHandlerEntry) => void; // function to be called - the callback returns extra data appended to the submit url
        userdata: any;              // any data suitable to callback
    }
    export interface FormInfo {
        RequestVerificationToken: string;
        UniqueIdCounters: UniqueIdInfo;
        ModuleGuid: string;
        QS: string;
    }

    export enum PanelAction {
        Apply = 0,
        MoveLeft = 1,
        MoveRight = 2,
        Add = 3,
        Insert = 4,
        Remove = 5,
    }

    export class Forms {

        // Partial Form

        /**
         * Initialize a partial form.
         */
        public initPartialForm(elemId: string): void {

            var partialForm = $YetaWF.getElementById(elemId);

            // run registered actions (usually javascript initialization, similar to $doc.ready()
            $YetaWF.processAllReady([partialForm]);
            $YetaWF.processAllReadyOnce([partialForm]);

            YetaWF_FormsImpl.initPartialForm(partialForm);

            // show error popup
            var hasErrors = this.hasErrors(partialForm);
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
         * Re-validate all fields within the div, typically used after paging in a grid to let jquery.validate update all fields
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
         * Serializes the form and returns a name/value pairs array
         */
        public serializeFormArray(form: HTMLFormElement): NameValuePair[] {
            return YetaWF_FormsImpl.serializeFormArray(form);
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

        public submit(form: HTMLFormElement, useValidation: boolean, extraData?: string, successFunc?: (hasErrors: boolean) => void, failFunc?: () => void): void {
            let method = form.getAttribute("method");
            if (!method) return; // no method, don't submit
            let saveReturn = form.getAttribute(YConfigs.Basics.CssSaveReturnUrl) !== null;// form says we need to save the return address on submit
            this.submitExplicit(form, method, form.action, saveReturn, useValidation, extraData, successFunc, failFunc);
        }

        public submitExplicit(form: HTMLFormElement, method: string, action: string, saveReturn: boolean, useValidation: boolean, extraData?: string,
                successFunc?: (hasErrors: boolean) => void, failFunc?: () => void, rawJSONFunc?: (json:string) => void): void  {

            $YetaWF.pageChanged = false;// suppress navigate error

            var divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (let div of divs)
                $YetaWF.removeElement(div);

            var onSubmitExtraData = extraData ? extraData : "";
            onSubmitExtraData = this.callPreSubmitHandler(form, onSubmitExtraData);

            let formValid = true;
            if (useValidation)
                formValid = this.validate(form);

            $YetaWF.closeOverlays();
            $YetaWF.setLoading(true);

            if (!useValidation || formValid) {

                // serialize the form
                var formData = this.serializeForm(form);
                // add extra data
                if (onSubmitExtraData)
                    formData = onSubmitExtraData + "&" + formData;
                // add the origin list in case we need to navigate back
                var originList = YVolatile.Basics.OriginList;
                if (saveReturn) {
                    var currUri = $YetaWF.parseUrl(window.location.href);
                    currUri.removeSearch(YConfigs.Basics.Link_OriginList);// remove originlist from current URL
                    currUri.removeSearch(YConfigs.Basics.Link_InPopup);// remove popup info from current URL
                    originList = YVolatile.Basics.OriginList.slice(0);// copy saved originlist
                    var newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: $YetaWF.isInPopup() };
                    originList.push(newOrigin);
                    if (originList.length > 5)// only keep the last 5 urls
                        originList = originList.slice(originList.length - 5);
                }
                formData = formData + "&" + YConfigs.Basics.Link_OriginList + "=" + encodeURIComponent(JSON.stringify(originList));
                // add uniqueidcounters
                {
                    formData = formData + "&" + YConfigs.Forms.UniqueIdCounters + "=" + encodeURIComponent(JSON.stringify(YVolatile.Basics.UniqueIdCounters));
                }
                // add the status of the Pagecontrol
                if (YVolatile.Basics.PageControlVisible)
                    formData = formData + "&" + YConfigs.Basics.Link_PageControl + "=y";
                // add if we're in a popup
                if ($YetaWF.isInPopup())
                    formData = formData + "&" + YConfigs.Basics.Link_InPopup + "=y";

                if (method.toLowerCase() === "get")
                    action = `${action}?${formData}`;

                var request: XMLHttpRequest = new XMLHttpRequest();
                request.open(method, action, true);
                if (method.toLowerCase() === "post")
                    request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                request.onreadystatechange = (ev: Event) : any => {
                    var req = request;
                    if (req.readyState === 4 /*DONE*/) {
                        $YetaWF.setLoading(false);
                        if (rawJSONFunc && req.responseText && req.responseText[0] === "{") {
                            rawJSONFunc(req.responseText);
                            return;
                        }
                        if ($YetaWF.processAjaxReturn(req.responseText, req.statusText, req, form, undefined, (result: string): void => {
                            this.preSubmitHandler1 = [];
                            var partForm = $YetaWF.getElement1BySelectorCond("." + YConfigs.Forms.CssFormPartial, [form]);
                            if (partForm) {
                                // clean up everything that's about to be removed
                                $YetaWF.processClearDiv(partForm);
                                // preserve the original css classes on the partial form (PartialFormCss)
                                var cls = partForm.className;
                                $YetaWF.setMixedOuterHTML(partForm, req.responseText);
                                partForm = $YetaWF.getElement1BySelectorCond("." + YConfigs.Forms.CssFormPartial, [form]);
                                if (partForm)
                                    partForm.className = cls;
                            }
                            this.callPostSubmitHandler(form);
                            $YetaWF.setFocus([form]);
                        })) {
                            if (successFunc)
                                successFunc(this.hasErrors(form));
                        } else {
                            if (failFunc)
                                failFunc();
                        }
                    }
                };
                request.send(formData);

            } else {
                $YetaWF.setLoading(false);
                // find the first field in each tab control that has an input validation error and activate that tab
                // This will not work for nested tabs. Only the lowermost tab will be activated.
                YetaWF_FormsImpl.setErrorInNestedControls(form);
                var hasErrors = this.hasErrors(form);
                if (hasErrors)
                    this.showErrors(form);
                // call callback (if any)
                if (successFunc)
                    successFunc(this.hasErrors(form));
            }
            var divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (let div of divs)
                $YetaWF.removeElement(div);
        }

        public submitTemplate(tag: HTMLElement, useValidation: boolean, templateName: string, templateAction: PanelAction, templateExtraData: string) : void {
            var qs = `${YConfigs.Basics.TemplateName}=${templateName}&${YConfigs.Basics.Link_SubmitIsApply}=y`;
            if (templateAction)
                qs += `&${YConfigs.Basics.TemplateAction}=${encodeURIComponent(templateAction)}`;
            if (templateExtraData)
                qs += `&${YConfigs.Basics.TemplateExtraData}=${encodeURIComponent(templateExtraData)}`;
            let form = this.getForm(tag);
            if ($YetaWF.elementHasClass(form, YConfigs.Forms.CssFormNoSubmit)) return;
            this.submit(form, useValidation, qs);
        }

        public serializeForm(form: HTMLFormElement): string {
            var pairs = this.serializeFormArray(form);
            var formData: string = "";
            for (var entry of pairs) {
                if (formData !== "")
                    formData += "&";
                formData += encodeURIComponent(entry.name) + "=" + encodeURIComponent(entry.value);
            }
            return formData;
        }

        // Cancel

        public cancel(): void {
            if ($YetaWF.isInPopup()) {
                // we're in a popup, just close it
                $YetaWF.closePopup();
            } else {
                // go to the last entry in the origin list, pop that entry and pass it in the url
                var originList = YVolatile.Basics.OriginList;
                if (originList.length > 0) {
                    var origin = originList.pop() as OriginListEntry;
                    var uri = $YetaWF.parseUrl(origin.Url);
                    uri.removeSearch(YConfigs.Basics.Link_OriginList);
                    if (originList.length > 0)
                        uri.addSearch(YConfigs.Basics.Link_OriginList, JSON.stringify(originList));
                    $YetaWF.ContentHandling.setNewUri(uri);
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


        // Pre/post submit

        // When a form is about to be submitted, all the functions in YPreSubmitHandler are called one by one
        // This is used to add control-specific data to the data submitted by the form
        // Usage:
        // $YetaWF.Forms.addPreSubmitHandler(@Manager.InPartialForm ? 1 : 0, {
        //   form: form,                // form <div> to be processed
        //   callback: function() {}    // function to be called - the callback returns extra data appended to the submit url
        //   userdata: callback-data,   // any data suitable to callback
        // });

        private preSubmitHandlerAll: SubmitHandlerEntry[] = []; // done every time before submit (never cleared) - used on main forms
        private preSubmitHandler1: SubmitHandlerEntry[] = []; // done once before submit, then cleared - used in partial forms

        /**
         * Add a callback to be called when a form is about to be submitted.
         */
        public addPreSubmitHandler(inPartialForm: boolean, entry: SubmitHandlerEntry) : void {
            if (inPartialForm) {
                this.preSubmitHandler1.push(entry);
            } else {
                this.preSubmitHandlerAll.push(entry);
            }
        }

        /**
         * Call all callbacks for a form that is about to be submitted.
         */
        public callPreSubmitHandler(form: HTMLElement, onSubmitExtraData: string) : string {
            for (let entry of this.preSubmitHandlerAll) {
                if (entry.form === form) {
                    // form specific
                    var extra = entry.callback(entry);
                    if (extra !== undefined) {
                        if (onSubmitExtraData.length > 0)
                            onSubmitExtraData = onSubmitExtraData + "&";
                        onSubmitExtraData += extra;
                    }
                }
            }
            for (let entry of this.preSubmitHandler1) {
                if (entry.form === form) {
                    var extra = entry.callback(entry);
                    if (extra !== undefined) {
                        if (onSubmitExtraData.length > 0)
                            onSubmitExtraData = onSubmitExtraData + "&";
                        onSubmitExtraData += extra;
                    }
                }
            }
            return onSubmitExtraData;
        }

        // When a form has been successfully submitted, all the functions in YPostSubmitHandler are called one by one
        // Usage:
        // $YetaWF.Forms.addPostSubmitHandler(@Manager.InPartialForm ? 1 : 0, {
        //   form: form,                // form <div> to be processed - may be null
        //   callback: function() {}    // function to be called
        //   userdata: callback-data,   // any data suitable to callback
        // });
        private YPostSubmitHandlerAll: SubmitHandlerEntry[] = []; // done every time after submit (never cleared) - used on main forms
        private YPostSubmitHandler1: SubmitHandlerEntry[] = []; // done once after submit, then cleared - used in partial forms

        /**
         * Add a callback to be called when a form has been successfully submitted.
         */
        public addPostSubmitHandler(inPartialForm: boolean, entry: SubmitHandlerEntry) : void {
            if (inPartialForm) {
                this.YPostSubmitHandler1.push(entry);
            } else {
                this.YPostSubmitHandlerAll.push(entry);
            }
        }

        /**
         * Call all callbacks for a form that has been successfully submitted.
         */
        public callPostSubmitHandler(form: HTMLElement, onSubmitExtraData?: string) : void {
            for (let entry of this.YPostSubmitHandlerAll) {
                if (entry.form == null) {
                    // global
                    entry.callback(entry);
                } else if (entry.form === form) {
                    // form specific
                    entry.callback(entry);
                }
            }
            for (let entry of this.YPostSubmitHandler1) {
                if (entry.form === form)
                    entry.callback(entry);
            }
            this.YPostSubmitHandler1 = [];
        }

        // Forms retrieval
        public getForm(tag: HTMLElement): HTMLFormElement {
            return $YetaWF.elementClosest(tag, "form") as HTMLFormElement;
        }
        public getFormCond(tag: HTMLElement) : HTMLFormElement | null {
            var form = $YetaWF.elementClosestCond(tag, "form");
            if (!form) return null;
            return form as HTMLFormElement;
        }
        public getInnerForm(tag: HTMLElement): HTMLFormElement {
            return $YetaWF.getElement1BySelector("form", [tag]) as HTMLFormElement;
        }
        public getInnerFormCond(tag: HTMLElement): HTMLFormElement | null {
            return $YetaWF.getElement1BySelectorCond("form", [tag]) as HTMLFormElement | null;
        }
        // get RequestVerificationToken, UniqueIdCounters and ModuleGuid in query string format (usually for ajax requests)
        public getFormInfo(tag: HTMLElement, addAmpersand?: boolean) : FormInfo {
            var form = this.getForm(tag);
            var req = ($YetaWF.getElement1BySelector(`input[name='${YConfigs.Forms.RequestVerificationToken}']`, [form]) as HTMLInputElement).value;
            if (!req || req.length === 0) throw "Can't locate " + YConfigs.Forms.RequestVerificationToken;/*DEBUG*/
            var guid = ($YetaWF.getElement1BySelector(`input[name='${YConfigs.Basics.ModuleGuid}']`, [form]) as HTMLInputElement).value;
            if (!guid || guid.length === 0) throw "Can't locate " + YConfigs.Basics.ModuleGuid;/*DEBUG*/

            var qs: string = "";
            if (addAmpersand !== false)
                qs += "&";
            qs += YConfigs.Forms.RequestVerificationToken + "=" + encodeURIComponent(req) +
                "&" + YConfigs.Forms.UniqueIdCounters + "=" + JSON.stringify(YVolatile.Basics.UniqueIdCounters) +
                "&" + YConfigs.Basics.ModuleGuid + "=" + encodeURIComponent(guid);

            var info: FormInfo = {
                RequestVerificationToken: req,
                UniqueIdCounters: YVolatile.Basics.UniqueIdCounters,
                ModuleGuid: guid,
                QS: qs
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
            this.submit(this.submitForm, false, YConfigs.Basics.Link_SubmitIsApply + "=y");
        }
        private reloadFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm) return;
            this.submit(this.submitForm, false, YConfigs.Basics.Link_SubmitIsReload + "=y");
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
                var form = this.getForm(ev.target as HTMLElement);
                this.submit(form, true, YConfigs.Basics.Link_SubmitIsApply + "=y");
                return false;
            });

            // Submit the form when a submit button is clicked

            $YetaWF.registerEventHandlerBody("submit", "form." + YConfigs.Forms.CssFormAjax, (ev: Event) : boolean => {
                var form = this.getForm(ev.target as HTMLElement);
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


