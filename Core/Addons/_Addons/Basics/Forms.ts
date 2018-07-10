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
        form: HTMLElement;          // form <div> to be processed
        callback: (entry: SubmitHandlerEntry) => void; // function to be called - the callback returns extra data appended to the submit url
        userdata: any;              // any data suitable to callback
    }
    export interface FormInfo {
        RequestVerificationToken: string;
        UniqueIdPrefix: string;
        ModuleGuid: string;
        QS: string;
    }

    export class Forms {

        // Partial Form

        /**
         * Initialize a partial form.
         */
        public initPartialForm(elemId: string): void {

            var partialForm = YetaWF_Basics.getElementById(elemId);

            // run registered actions (usually javascript initialization, similar to $doc.ready()
            YetaWF_Basics.processAllReady([partialForm]);
            YetaWF_Basics.processAllReadyOnce([partialForm]);

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

        // Submit

        public DATACLASS: string = 'yetawf_forms_data'; // add divs with this class to form for any data that needs to be submitted (will be removed before calling (pre)submit handlers.

        public submit(form: HTMLFormElement, useValidation: boolean, extraData?: string, successFunc?: (hasErrors: boolean) => void, failFunc?: () => void) {

            $('div.' + this.DATACLASS).remove();

            var $form = $(form) as JQuery<HTMLFormElement>;

            var onSubmitExtraData = extraData ? extraData : "";
            onSubmitExtraData = this.callPreSubmitHandler(form, onSubmitExtraData);

            if (useValidation)
                ($form as any).validate();

            YetaWF_Basics.setLoading(true);

            if (!useValidation || ($form as any).valid()) {

                // serialize the form
                var formData = this.serializeForm(form);
                // add extra data
                if (onSubmitExtraData)
                    formData = onSubmitExtraData + "&" + formData;
                // add the origin list in case we need to navigate back
                var originList = YVolatile.Basics.OriginList;
                if ($form.attr(YConfigs.Basics.CssSaveReturnUrl) != undefined) {// form says we need to save the return address on submit
                    var currUri = YetaWF_Basics.parseUrl(window.location.href);
                    currUri.removeSearch(YConfigs.Basics.Link_OriginList);// remove originlist from current URL
                    currUri.removeSearch(YConfigs.Basics.Link_InPopup);// remove popup info from current URL
                    originList = YVolatile.Basics.OriginList.slice(0);// copy saved originlist
                    var newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: YetaWF_Basics.isInPopup() };
                    originList.push(newOrigin);
                    if (originList.length > 5)// only keep the last 5 urls
                        originList = originList.slice(originList.length - 5);
                }
                // include the character dimension info
                {
                    var charSize = YetaWF_Basics.getCharSizeFromTag($form[0]);
                    formData = formData + "&" + YConfigs.Basics.Link_CharInfo + "=" + charSize.width.toString() + ',' + charSize.height.toString();
                }

                formData = formData + "&" + YConfigs.Basics.Link_OriginList + "=" + encodeURIComponent(JSON.stringify(originList));
                // add the status of the Pagecontrol
                if (YVolatile.Basics.PageControlVisible)
                    formData = formData + "&" + YConfigs.Basics.Link_PageControl + "=y";
                // add if we're in a popup
                if (YetaWF_Basics.isInPopup())
                    formData = formData + "&" + YConfigs.Basics.Link_InPopup + "=y";

                $.ajax({
                    url: form.action,
                    type: form.method,
                    data: formData,
                    success: (result: string, textStatus: string, jqXHR: JQuery.jqXHR) => {
                        YetaWF_Basics.setLoading(false);
                        YetaWF_Basics.processAjaxReturn(result, textStatus, jqXHR, $form[0], undefined, () => {
                            this.YPreSubmitHandler1 = [];
                            var $partForm = $('.' + YConfigs.Forms.CssFormPartial, $form);
                            if ($partForm.length > 0) {
                                // clean up everything that's about to be removed
                                YetaWF_Basics.processClearDiv($partForm[0]);
                                // preserve the original css classes on the partial form (PartialFormCss)
                                var cls = $partForm[0].className;
                                $partForm.replaceWith(result);
                                $partForm = $('.' + YConfigs.Forms.CssFormPartial, $form);
                                $partForm[0].className = cls;
                            }
                        });
                        this.callPostSubmitHandler(form);
                        if (successFunc) // executed on successful ajax submit
                            successFunc(this.hasErrors(form));
                    },
                    error: (jqXHR: JQuery.jqXHR, textStatus: string, errorThrown: string) => {
                        YetaWF_Basics.setLoading(false);
                        YetaWF_Basics.alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
                        if (failFunc)
                            failFunc();
                    },
                });
            } else {
                YetaWF_Basics.setLoading(false);
                // find the first field in a tab control that has an input validation error and activate that tab
                // This will not work for nested tabs. Only the lowermost tab will be activated.
                $("div.yt_propertylisttabbed", $form).each(function (index) {
                    var $tabctrl = $(this);
                    // get the first field in error (if any)
                    var $errField = $('.input-validation-error', $tabctrl).eq(0);
                    if ($errField.length > 0) {
                        // find out which tab panel we're on
                        var $ttabpanel = $errField.closest('div.t_tabpanel');
                        if ($ttabpanel.length == 0) throw "We found a validation error in a tab control, but we couldn't find the tab panel.";/*DEBUG*/
                        var panel = $ttabpanel.attr('data-tab') as number|undefined;
                        if (!panel) throw "We found a panel in a tab control without panel number (data-tab attribute).";/*DEBUG*/
                        // get the tab entry
                        var $te = $('ul.t_tabstrip > li', $tabctrl).eq(panel);
                        if ($te.length == 0) throw "We couldn't find the tab entry for panel " + panel;/*DEBUG*/
                        if (YVolatile.Forms.TabStyle === TabStyleEnum.JQuery)
                            $tabctrl.tabs("option", "active", panel);
                        else if (YVolatile.Forms.TabStyle === TabStyleEnum.Kendo)
                            $tabctrl.data("kendoTabStrip").activateTab($te);
                        else throw "Unknown tab style";/*DEBUG*/
                    }
                });
                var hasErrors = this.hasErrors(form);
                if (hasErrors)
                    this.showErrors(form);
                // call callback (if any)
                if (successFunc)
                    successFunc(this.hasErrors(form));
            }
            $('div.' + this.DATACLASS).remove();
            return false;
        };

        public submitTemplate(tag: HTMLElement, useValidation: boolean, templateName: string, templateAction: string, templateExtraData: string) {
            var qs = `${YConfigs.Basics.TemplateName}=${templateName}&${YConfigs.Basics.Link_SubmitIsApply}`;
            if (templateAction)
                qs += `&${YConfigs.Basics.TemplateAction}=${encodeURIComponent(templateAction)}`;
            if (templateExtraData)
                qs += `&${YConfigs.Basics.TemplateExtraData}=${encodeURIComponent(templateExtraData)}`;
            this.submit(this.getForm(tag), useValidation, qs);
        };

        public serializeFormArray(form: HTMLFormElement): JQuery.NameValuePair[] { //$$$
            // disable all fields that we don't want to submit (marked with YConfigs.Forms.CssFormNoSubmit)
            var $disabledFields = $('.' + YConfigs.Forms.CssFormNoSubmit, $(form)).not(':disabled');
            $disabledFields.attr('disabled', 'disabled');
            // disable all input fields in containers (usually grids) - we don't want to submit them - they're collected separately
            var $disabledGridFields = $(`.${YConfigs.Forms.CssFormNoSubmitContents} input,.${YConfigs.Forms.CssFormNoSubmitContents} select`, $(form)).not(':disabled');
            $disabledGridFields.attr('disabled', 'disabled');
            // serialize the form
            var formData = $(form).serializeArray();
            // and enable all the input fields we just disabled
            $disabledFields.removeAttr('disabled');
            $disabledGridFields.removeAttr('disabled');
            return formData;
        }

        public serializeForm(form: HTMLFormElement) : string {
            // disable all fields that we don't want to submit (marked with YConfigs.Forms.CssFormNoSubmit)
            var $disabledFields = $('.' + YConfigs.Forms.CssFormNoSubmit, form).not(':disabled');
            $disabledFields.attr('disabled', 'disabled');
            // disable all input fields in containers (usually grids) - we don't want to submit them - they're collected separately
            var $disabledGridFields = $(`.${YConfigs.Forms.CssFormNoSubmitContents} input,.${YConfigs.Forms.CssFormNoSubmitContents} select`, $(form)).not(':disabled');
            $disabledGridFields.attr('disabled', 'disabled');
            // serialize the form
            var formData = $(form).serialize();
            // and enable all the input fields we just disabled
            $disabledFields.removeAttr('disabled');
            $disabledGridFields.removeAttr('disabled');
            return formData;
        }

        // Pre/post submit

        // When a form is about to be submitted, all the functions in YPreSubmitHandler are called one by one
        // This is used to add control-specific data to the data submitted by the form
        // Usage:
        // YetaWF_Forms.addPreSubmitHandler(@Manager.InPartialForm ? 1 : 0, {
        //   form: form,                // form <div> to be processed
        //   callback: function() {}    // function to be called - the callback returns extra data appended to the submit url
        //   userdata: callback-data,   // any data suitable to callback
        // });

        private YPreSubmitHandlerAll: SubmitHandlerEntry[] = []; // done every time before submit (never cleared) - used on main forms
        private YPreSubmitHandler1: SubmitHandlerEntry[] = []; // done once before submit, then cleared - used in partial forms

        /**
         * Add a callback to be called when a form is about to be submitted.
         */
        public addPreSubmitHandler(inPartialForm: boolean, entry: SubmitHandlerEntry) {
            if (inPartialForm) {
                this.YPreSubmitHandler1.push(entry);
            } else {
                this.YPreSubmitHandlerAll.push(entry);
            }
        }

        /**
         * Call all callbacks for a form that is about to be submitted.
         */
        public callPreSubmitHandler(form: HTMLElement, onSubmitExtraData: any) {
            for (var index in this.YPreSubmitHandlerAll) {
                var entry = this.YPreSubmitHandlerAll[index]
                if (entry.form == form) {
                    // form specific
                    var extra = entry.callback(entry);
                    if (extra != undefined) {
                        if (onSubmitExtraData.length > 0)
                            onSubmitExtraData = onSubmitExtraData + "&";
                        onSubmitExtraData += extra;
                    }
                }
            }
            for (var index in this.YPreSubmitHandler1) {
                var entry = this.YPreSubmitHandler1[index];
                if (entry.form == form) {
                    var extra = entry.callback(entry);
                    if (extra != undefined) {
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
        // YetaWF_Forms.addPostSubmitHandler(@Manager.InPartialForm ? 1 : 0, {
        //   form: form,                // form <div> to be processed - may be null
        //   callback: function() {}    // function to be called
        //   userdata: callback-data,   // any data suitable to callback
        // });
        private YPostSubmitHandlerAll: SubmitHandlerEntry[] = []; // done every time after submit (never cleared) - used on main forms
        private YPostSubmitHandler1: SubmitHandlerEntry[] = []; // done once after submit, then cleared - used in partial forms

        /**
         * Add a callback to be called when a form has been successfully submitted.
         */
        public addPostSubmitHandler(inPartialForm: boolean, entry: SubmitHandlerEntry) {
            if (inPartialForm) {
                this.YPostSubmitHandler1.push(entry);
            } else {
                this.YPostSubmitHandlerAll.push(entry);
            }
        }

        /**
         * Call all callbacks for a form that has been successfully submitted.
         */
        public callPostSubmitHandler(form: HTMLElement, onSubmitExtraData?: string) {
            for (var index in this.YPostSubmitHandlerAll) {
                var entry = this.YPostSubmitHandlerAll[index];
                if (entry.form == null) {
                    // global
                    entry.callback(entry);
                } else if (entry.form[0] == form) {
                    // form specific
                    entry.callback(entry);
                }
            }
            for (var index in this.YPostSubmitHandler1) {
                var entry = this.YPostSubmitHandler1[index];
                if (entry.form == form)
                    entry.callback(entry);
            }
            this.YPostSubmitHandler1 = [];
            return onSubmitExtraData;
        }

        // Forms retrieval

        public getForm(tag: HTMLElement) : HTMLFormElement {
            var $form = $(tag).closest('form') as JQuery<HTMLFormElement>;
            if ($form.length == 0) throw "Can't locate enclosing form";/*DEBUG*/
            return $form[0];
        };
        public getFormCond(tag: HTMLElement) : HTMLFormElement | null {
            var $form = $(tag).closest('form') as JQuery<HTMLFormElement>;
            if ($form.length == 0) return null;
            return $form[0];
        };
        // get RequestVerificationToken, UniqueIdPrefix and ModuleGuid in query string format (usually for ajax requests)
        public getFormInfo(tag: HTMLElement) {
            var form = this.getForm(tag);
            var $form = $(form);
            var req: string | undefined = <string|undefined> $(`input[name='${YConfigs.Forms.RequestVerificationToken}']`, $form).val();
            if (!req || req.length == 0) throw "Can't locate " + YConfigs.Forms.RequestVerificationToken;/*DEBUG*/
            var pre: string | undefined = <string|undefined> $(`input[name='${YConfigs.Forms.UniqueIdPrefix}']`, $form).val();
            if (!pre || pre.length == 0) throw "Can't locate " + YConfigs.Forms.UniqueIdPrefix;/*DEBUG*/
            var guid: string | undefined = <string|undefined> $(`input[name='${YConfigs.Basics.ModuleGuid}']`, $form).val();
            if (!guid || guid.length == 0) throw "Can't locate " + YConfigs.Basics.ModuleGuid;/*DEBUG*/

            var charSize = YetaWF_Basics.getCharSizeFromTag(form);

            var qs : string = "&" + YConfigs.Forms.RequestVerificationToken + "=" + encodeURIComponent(req) +
                "&" + YConfigs.Forms.UniqueIdPrefix + "=" + encodeURIComponent(pre) +
                "&" + YConfigs.Basics.ModuleGuid + "=" + encodeURIComponent(guid) +
                "&" + YConfigs.Basics.Link_CharInfo + "=" + charSize.width.toString() + ',' + charSize.height.toString();

            var info: FormInfo = {
                RequestVerificationToken: req,
                UniqueIdPrefix: pre,
                ModuleGuid: guid,
                QS: qs
            };
            return info;
        };


        // submit form on change

        /**
         * Handles submitonchange/applyonchange
         */
        public initSubmitOnChange(): void {

            // submit

            $('body').on('keyup', '.ysubmitonchange select', (e) => {
                if (e.keyCode == 13) {
                    this.submitForm = this.getForm(e.currentTarget);
                    this.submitFormOnChange();
                }
            });
            $('body').on('change', '.ysubmitonchange select,.ysubmitonchange input[type="checkbox"]', (e) => {
                clearInterval(this.submitFormTimer);
                this.submitForm = this.getForm(e.currentTarget);
                this.submitFormTimer = setInterval(() => this.submitFormOnChange(), 1000);// wait 1 second and automatically submit the form
                YetaWF_Basics.setLoading(true);
            });

            // apply

            $('body').on('keyup', '.yapplyonchange select,.yapplyonchange input[type="checkbox"]', (e) => {
                if (e.keyCode == 13) {
                    this.submitForm = this.getForm(e.currentTarget);
                    this.applyFormOnChange();
                }
            });
            $('body').on('change', '.yapplyonchange select', (e) => {
                clearInterval(this.submitFormTimer);
                this.submitForm = this.getForm(e.currentTarget);
                this.submitFormTimer = setInterval(() => this.applyFormOnChange(), 1000);// wait 1 second and automatically submit the form
                YetaWF_Basics.setLoading(true);
            });
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

        /**
         * Initialize to handle Submit, Apply, Cancel buttons
         */
        public initHandleFormsButtons(): void {
            // Cancel the form when a Cancel button is clicked

            $(document).on('click', 'form .' + YConfigs.Forms.CssFormCancel, (e) => {

                if (YetaWF_Basics.isInPopup()) {
                    // we're in a popup, just close it
                    YetaWF_Basics.closePopup();
                } else {
                    // go to the last entry in the origin list, pop that entry and pass it in the url
                    var originList = YVolatile.Basics.OriginList;
                    if (originList.length > 0) {
                        var origin = originList.pop() as OriginListEntry;
                        var uri = YetaWF_Basics.parseUrl(origin.Url);
                        uri.removeSearch(YConfigs.Basics.Link_ToEditMode);
                        if (origin.EditMode != YVolatile.Basics.EditModeActive)
                            uri.addSearch(YConfigs.Basics.Link_ToEditMode, !YVolatile.Basics.EditModeActive ? "0":"1");
                        uri.removeSearch(YConfigs.Basics.Link_OriginList);
                        if (originList.length > 0)
                            uri.addSearch(YConfigs.Basics.Link_OriginList, JSON.stringify(originList));
                        if (!YetaWF_Basics.ContentHandling.setContent(uri, true))
                            window.location.assign(uri.toUrl());
                    } else {
                        // we don't know where to return so just close the browser
                        window.close();
                    }
                }
            });

            // Submit the form when an apply button is clicked

            $(document).on('click', `form input[type="button"][${YConfigs.Forms.CssDataApplyButton}]`, (e) => {
                e.preventDefault();
                var form = YetaWF_Forms.getForm(e.currentTarget);
                YetaWF_Forms.submit(form, true, YConfigs.Basics.Link_SubmitIsApply + "=y");
            });

            // Submit the form when a submit button is clicked

            $(document).on('submit', 'form.' + YConfigs.Forms.CssFormAjax, (e) => {
                e.preventDefault();
                YetaWF_Forms.submit(e.currentTarget as HTMLFormElement, true);
            });
        }
    }
}

var YetaWF_Forms: YetaWF.Forms = new YetaWF.Forms();

// initialize submit on change
YetaWF_Forms.initSubmitOnChange();
// initialize  Submit, Apply, Cancel button handling
YetaWF_Forms.initHandleFormsButtons();
