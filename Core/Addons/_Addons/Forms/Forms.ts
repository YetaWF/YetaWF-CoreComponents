/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Forms API, to be implemented by rendering-specific code - rendering code must define a YetaWF_FormsImpl object implementing IFormsImpl */

declare var YetaWF_FormsImpl: YetaWF.IFormsImpl;

namespace YetaWF {

    export interface IFormsImpl {
        /**
         * Initializes a partialform.
         */
        initPartialForm(partialForm: HTMLElement): void;
        /**
         * Validates one elements.
         */
        validateElement(ctrl: HTMLElement): void;
        /**
         * Re-validate all fields within the div, typically used after paging in a grid to let jquery.validate update all fields
         */
        updateValidation(div: HTMLElement): void;
        /**
         * Returns whether a div has form errors.
         */
        hasErrors(elem: HTMLElement): boolean;
        /**
         * Shows all div form errors in a popup.
         */
        showErrors(elem: HTMLElement): void;

        /**
         * Validate all fields in the current form.
         */
        validate(form: HTMLFormElement): void;
        /**
         * Returns whether all fields in the current form are valid.
         */
        isValid(form: HTMLFormElement): boolean;
        /**
         * Serializes the form and returns a name/value pairs array
         */
        serializeFormArray(form: HTMLFormElement): NameValuePair[];
        /**
         * If there is a validation in the specified tab control, the tab is activated.
         */
        setErrorInTab(tabctrl: HTMLElement): void;
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
        UniqueIdPrefix: string;
        RequestVerificationToken: string;

        // Validation (not implementation specific) used by validation attributes
        ConditionPropertyName: string;
        ConditionPropertyValue: string;
        ConditionPropertyValueLow: string;
        ConditionPropertyValueHigh: string;

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
        FormErrors: string;
    }

    export interface SubmitHandlerEntry {
        form: HTMLElement | null;   // form <div> to be processed
        callback: (entry: SubmitHandlerEntry) => void; // function to be called - the callback returns extra data appended to the submit url
        userdata: any;              // any data suitable to callback
    }
    export interface FormInfo {
        RequestVerificationToken: string;
        UniqueIdPrefix: string;
        ModuleGuid: string;
        CharInfo: string;
        QS: string;
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
         * Validates one elements.
         */
        public validateElement(ctrl: HTMLElement): void {
            YetaWF_FormsImpl.validateElement(ctrl);
        }
        /**
         * Re-validate all fields within the div, typically used after paging in a grid to let jquery.validate update all fields
         */
        public updateValidation(div: HTMLElement): void {
            YetaWF_FormsImpl.updateValidation(div);
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
        public validate(form: HTMLFormElement): void {
            YetaWF_FormsImpl.validate(form);
        }
        /**
         * Returns whether all fields in the current form are valid.
         */
        public isValid(form: HTMLFormElement): boolean {
            return YetaWF_FormsImpl.isValid(form);
        }

        // Submit

        public DATACLASS: string = "yetawf_forms_data"; // add divs with this class to form for any data that needs to be submitted (will be removed before calling (pre)submit handlers.

        public submit(form: HTMLFormElement, useValidation: boolean, extraData?: string, successFunc?: (hasErrors: boolean) => void, failFunc?: () => void) : void  {

            if (!form.getAttribute("method")) return; // no method, don't submit

            var divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (let div of divs)
                $YetaWF.removeElement(div);

            if (useValidation)
                this.validate(form);

            $YetaWF.closeOverlays();
            $YetaWF.setLoading(true);

            if (!useValidation || this.isValid(form)) {

                var onSubmitExtraData = extraData ? extraData : "";
                onSubmitExtraData = this.callPreSubmitHandler(form, onSubmitExtraData);

                // serialize the form
                var formData = this.serializeForm(form);
                // add extra data
                if (onSubmitExtraData)
                    formData = onSubmitExtraData + "&" + formData;
                // add the origin list in case we need to navigate back
                var originList = YVolatile.Basics.OriginList;
                if (form.getAttribute(YConfigs.Basics.CssSaveReturnUrl)) {// form says we need to save the return address on submit
                    var currUri = $YetaWF.parseUrl(window.location.href);
                    currUri.removeSearch(YConfigs.Basics.Link_OriginList);// remove originlist from current URL
                    currUri.removeSearch(YConfigs.Basics.Link_InPopup);// remove popup info from current URL
                    originList = YVolatile.Basics.OriginList.slice(0);// copy saved originlist
                    var newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: $YetaWF.isInPopup() };
                    originList.push(newOrigin);
                    if (originList.length > 5)// only keep the last 5 urls
                        originList = originList.slice(originList.length - 5);
                }
                // include the character dimension info
                {
                    var charSize = $YetaWF.getCharSizeFromTag(form);
                    formData = formData + "&" + YConfigs.Basics.Link_CharInfo + "=" + charSize.width.toString() + "," + charSize.height.toString();
                }

                formData = formData + "&" + YConfigs.Basics.Link_OriginList + "=" + encodeURIComponent(JSON.stringify(originList));
                // add the status of the Pagecontrol
                if (YVolatile.Basics.PageControlVisible)
                    formData = formData + "&" + YConfigs.Basics.Link_PageControl + "=y";
                // add if we're in a popup
                if ($YetaWF.isInPopup())
                    formData = formData + "&" + YConfigs.Basics.Link_InPopup + "=y";

                var request: XMLHttpRequest = new XMLHttpRequest();
                request.open(form.method, form.action, true);
                request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                request.onreadystatechange = (ev: Event) : any => {
                    var req = request;
                    if (req.readyState === 4 /*DONE*/) {
                        $YetaWF.setLoading(false);
                        if ($YetaWF.processAjaxReturn(req.responseText, req.statusText, req, form, undefined, (result: string) => {
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
                            if (successFunc) // executed on successful ajax submit
                                successFunc(this.hasErrors(form));
                        })) {
                            // ok
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
                var elems = $YetaWF.getElementsBySelector("div.yt_propertylisttabbed", [form]);
                elems.forEach((tabctrl: HTMLElement, index: number) => {
                    YetaWF_FormsImpl.setErrorInTab(tabctrl);
                });
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

        public submitTemplate(tag: HTMLElement, useValidation: boolean, templateName: string, templateAction: string, templateExtraData: string) : void {
            var qs = `${YConfigs.Basics.TemplateName}=${templateName}&${YConfigs.Basics.Link_SubmitIsApply}`;
            if (templateAction)
                qs += `&${YConfigs.Basics.TemplateAction}=${encodeURIComponent(templateAction)}`;
            if (templateExtraData)
                qs += `&${YConfigs.Basics.TemplateExtraData}=${encodeURIComponent(templateExtraData)}`;
            this.submit(this.getForm(tag), useValidation, qs);
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
                } else if (entry.form[0] === form) {
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
        // get RequestVerificationToken, UniqueIdPrefix and ModuleGuid in query string format (usually for ajax requests)
        public getFormInfo(tag: HTMLElement, addAmpersand?: boolean, counter?: number) : FormInfo {
            var form = this.getForm(tag);
            var req = ($YetaWF.getElement1BySelector(`input[name='${YConfigs.Forms.RequestVerificationToken}']`, [form]) as HTMLInputElement).value;
            if (!req || req.length === 0) throw "Can't locate " + YConfigs.Forms.RequestVerificationToken;/*DEBUG*/
            var pre = ($YetaWF.getElement1BySelector(`input[name='${YConfigs.Forms.UniqueIdPrefix}']`, [form]) as HTMLInputElement).value;
            if (!pre || pre.length === 0) throw "Can't locate " + YConfigs.Forms.UniqueIdPrefix;/*DEBUG*/
            if (counter)
                pre += `_${counter}`;
            var guid = ($YetaWF.getElement1BySelector(`input[name='${YConfigs.Basics.ModuleGuid}']`, [form]) as HTMLInputElement).value;
            if (!guid || guid.length === 0) throw "Can't locate " + YConfigs.Basics.ModuleGuid;/*DEBUG*/
            var charSize = $YetaWF.getCharSizeFromTag(form);

            var qs: string = "";
            if (addAmpersand !== false)
                qs += "&";
            qs += YConfigs.Forms.RequestVerificationToken + "=" + encodeURIComponent(req) +
                "&" + YConfigs.Forms.UniqueIdPrefix + "=" + encodeURIComponent(pre) +
                "&" + YConfigs.Basics.ModuleGuid + "=" + encodeURIComponent(guid) +
                "&" + YConfigs.Basics.Link_CharInfo + "=" + charSize.width.toString() + "," + charSize.height.toString();

            var info: FormInfo = {
                RequestVerificationToken: req,
                UniqueIdPrefix: pre,
                ModuleGuid: guid,
                CharInfo: charSize.width.toString() + "," + charSize.height.toString(),
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
            this.submitFormTimer = setInterval(() => this.applyFormOnChange(), 1000);// wait 1 second and automatically submit the form
            $YetaWF.setLoading(true);
        }
        public applyOnReturnKey(elem: HTMLElement) : void {
            this.submitForm = this.getForm(elem);
            this.applyFormOnChange();
        }
        public reloadOnChange(elem: HTMLElement): void {
            clearInterval(this.submitFormTimer);
            this.submitForm = this.getForm(elem);
            this.submitFormTimer = setInterval(() => this.reloadFormOnChange(), 1000);// wait 1 second and automatically submit the form
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
            $YetaWF.registerEventHandlerBody("change", ".ysubmitonchange select,.ysubmitonchange input[type=\"checkbox\"]", (ev: Event) => {
                this.submitOnChange(ev.target as HTMLElement);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".ysubmitonchange select", (ev: KeyboardEvent) => {
                if (ev.keyCode === 13) {
                    this.submitOnChange(ev.target as HTMLElement);
                    return false;
                }
                return true;
            });

            // apply

            $YetaWF.registerEventHandlerBody("change", ".yapplyonchange select,.yapplyonchange input[type=\"checkbox\"]", (ev: Event) => {
                this.applyOnChange(ev.target as HTMLElement);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".yapplyonchange select", (ev: KeyboardEvent) => {
                if (ev.keyCode === 13) {
                    this.applyOnChange(ev.target as HTMLElement);
                    return false;
                }
                return true;
            });

            // reload

            $YetaWF.registerEventHandlerBody("change", ".yreloadonchange select,.yreloadonchange input[type=\"checkbox\"]", (ev: Event) => {
                this.reloadOnChange(ev.target as HTMLElement);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".yreloadonchange select", (ev: KeyboardEvent) => {
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

                if ($YetaWF.isInPopup()) {
                    // we're in a popup, just close it
                    $YetaWF.closePopup();
                } else {
                    // go to the last entry in the origin list, pop that entry and pass it in the url
                    var originList = YVolatile.Basics.OriginList;
                    if (originList.length > 0) {
                        var origin = originList.pop() as OriginListEntry;
                        var uri = $YetaWF.parseUrl(origin.Url);
                        uri.removeSearch(YConfigs.Basics.Link_ToEditMode);
                        if (origin.EditMode !== YVolatile.Basics.EditModeActive)
                            uri.addSearch(YConfigs.Basics.Link_ToEditMode, !YVolatile.Basics.EditModeActive ? "0":"1");
                        uri.removeSearch(YConfigs.Basics.Link_OriginList);
                        if (originList.length > 0)
                            uri.addSearch(YConfigs.Basics.Link_OriginList, JSON.stringify(originList));
                        if (!$YetaWF.ContentHandling.setContent(uri, true))
                            window.location.assign(uri.toUrl());
                    } else {
                        // we don't know where to return so just close the browser
                        try {
                            window.close();
                        } catch (e) { }
                    }
                }
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


