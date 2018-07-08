/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Forms API, to be implemented by rendering-specific code - rendering code must define a YetaWF_FormsImpl object implementing IFormsImpl */

declare var YetaWF_FormsImpl: YetaWF.IFormsImpl;

namespace YetaWF {

    export interface IFormsImpl {
        /**
        * Initializes a partialform.
        */
        initPartialForm(elem: JQuery<HTMLElement>): void;
        /**
         * Validates one elements.
         */
        validateElement($ctrl: JQuery<HTMLElement>): void;
        /**
         * Re-validate all fields within the div, typically used after paging in a grid to let jquery.validate update all fields
         */
        updateValidation($div: JQuery<HTMLElement>): void;
        /**
        * Returns whether the form has errors.
        */
        hasErrors($form: JQuery<HTMLElement>): boolean;
        /**
         * Shows all form errors in a popup.
         */
        showErrors($form: JQuery<HTMLElement>): void;
    }

    export interface SubmitHandlerEntry {
        form: JQuery<HTMLElement>;  // form <div> to be processed
        callback: (entry: SubmitHandlerEntry) => void;       // function to be called - the callback returns extra data appended to the submit url
        userdata: any;              // any data suitable to callback
    }
    export interface FormInfo {
        RequestVerificationToken: string;
        UniqueIdPrefix: string;
        ModuleGuid: string;
        QS: string;
    }

    export class Forms /* implements IFormsImpl */ {  // doesn't need to implement IFormsImpl, used for type checking only

        // Partial Form

        /**
         * Initialize a partial form.
         */
        public initPartialForm($partialForm: JQuery<HTMLElement>): void {

            // run registered actions (usually javascript initialization, similar to $doc.ready()
            YetaWF_Basics.processAllReady($partialForm);
            YetaWF_Basics.processAllReadyOnce($partialForm);

            YetaWF_FormsImpl.initPartialForm($partialForm);

            // show error popup
            var hasErrors = this.hasErrors($partialForm);
            if (hasErrors)
                this.showErrors($partialForm);
        }
        /**
         * Validates one elements.
         */
        public validateElement($ctrl: JQuery<HTMLElement>): void {
            YetaWF_FormsImpl.validateElement($ctrl);
        }
        /**
         * Re-validate all fields within the div, typically used after paging in a grid to let jquery.validate update all fields
         */
        public updateValidation($div: JQuery<HTMLElement>): void {
            YetaWF_FormsImpl.updateValidation($div);
        }
        /**
        * Returns whether the form has errors.
        */
        public hasErrors($form: JQuery<HTMLElement>): boolean {
            return YetaWF_FormsImpl.hasErrors($form);
        }
        /**
         * Shows all form errors in a popup.
         */
        public showErrors($form: JQuery<HTMLElement>): void {
            YetaWF_FormsImpl.showErrors($form);
        }

        // Submit

        public DATACLASS: string = 'yetawf_forms_data'; // add divs with this class to form for any data that needs to be submitted (will be removed before calling (pre)submit handlers.

        public submit($form: JQuery<HTMLFormElement>, useValidation: boolean, extraData?: string, successFunc?: (hasErrors: boolean) => void, failFunc?: () => void) {

            $('div.' + this.DATACLASS).remove();

            var form = $form.get(0) as HTMLFormElement;

            var onSubmitExtraData = extraData ? extraData : "";
            onSubmitExtraData = this.callPreSubmitHandler($form, onSubmitExtraData);

            if (useValidation)
                ($form as any).validate();

            YetaWF_Basics.setLoading(true);

            if (!useValidation || ($form as any).valid()) {

                // serialize the form
                var formData = this.serializeForm($form);
                // add extra data
                if (onSubmitExtraData)
                    formData = onSubmitExtraData + "&" + formData;
                // add the origin list in case we need to navigate back
                var originList = YVolatile.Basics.OriginList;
                if ($form.attr(YConfigs.Basics.CssSaveReturnUrl) != undefined) {// form says we need to save the return address on submit
                    var currUri = new URI(window.location.href);
                    currUri.removeSearch(YGlobals.Link_OriginList);// remove originlist from current URL
                    currUri.removeSearch(YGlobals.Link_InPopup);// remove popup info from current URL
                    originList = YVolatile.Basics.OriginList.slice(0);// copy saved originlist
                    var newOrigin = { Url: currUri.toString(), EditMode: YVolatile.Basics.EditModeActive != 0, InPopup: YetaWF_Basics.isInPopup() };
                    originList.push(newOrigin);
                    if (originList.length > 5)// only keep the last 5 urls
                        originList = originList.slice(originList.length - 5);
                }
                // include the character dimension info
                {
                    var charSize = YetaWF_Basics.getCharSizeFromTag($form);
                    formData = formData + "&" + YGlobals.Link_CharInfo + "=" + charSize.width.toString() + ',' + charSize.height.toString();
                }

                formData = formData + "&" + YGlobals.Link_OriginList + "=" + encodeURIComponent(JSON.stringify(originList));
                // add the status of the Pagecontrol
                if (YVolatile.Basics.PageControlVisible)
                    formData = formData + "&" + YGlobals.Link_PageControl + "=y";
                // add if we're in a popup
                if (YetaWF_Basics.isInPopup())
                    formData = formData + "&" + YGlobals.Link_InPopup + "=y";

                $.ajax({
                    url: form.action,
                    type: form.method,
                    data: formData,
                    success: (result: string, textStatus: string, jqXHR: JQuery.jqXHR) => {
                        YetaWF_Basics.setLoading(false);
                        YetaWF_Basics.processAjaxReturn(result, textStatus, jqXHR, $form, undefined, () => {
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
                        this.callPostSubmitHandler($form);
                        if (successFunc) // executed on successful ajax submit
                            successFunc(this.hasErrors($form));
                    },
                    error: (jqXHR: JQuery.jqXHR, textStatus: string, errorThrown: string) => {
                        YetaWF_Basics.setLoading(false);
                        YetaWF_Basics.Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
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
                        if (YVolatile.Forms.TabStyle == 0)//jquery ui
                            $tabctrl.tabs("option", "active", panel);
                        else if (YVolatile.Forms.TabStyle == 1)//Kendo UI
                            $tabctrl.data("kendoTabStrip").activateTab($te);
                        else throw "Unknown tab style";/*DEBUG*/
                    }
                });
                var hasErrors = this.hasErrors($form);
                if (hasErrors)
                    this.showErrors($form);
                // call callback (if any)
                if (successFunc)
                    successFunc(this.hasErrors($form));
            }
            $('div.' + this.DATACLASS).remove();
            return false;
        };

        public submitTemplate(tag: HTMLElement, useValidation: boolean, templateName: string, templateAction: string, templateExtraData: string) {
            var qs = `${YConfigs.Basics.TemplateName}=${templateName}&${YGlobals.Link_SubmitIsApply}`;
            if (templateAction)
                qs += `&${YConfigs.Basics.TemplateAction}=${encodeURIComponent(templateAction)}`;
            if (templateExtraData)
                qs += `&${YConfigs.Basics.TemplateExtraData}=${encodeURIComponent(templateExtraData)}`;
            this.submit(this.getForm($(tag)), useValidation, qs);
        };

        public serializeFormArray($form: JQuery<HTMLFormElement>): JQuery.NameValuePair[] {
            // disable all fields that we don't want to submit (marked with YConfigs.Forms.CssFormNoSubmit)
            var $disabledFields = $('.' + YConfigs.Forms.CssFormNoSubmit, $form).not(':disabled');
            $disabledFields.attr('disabled', 'disabled');
            // disable all input fields in containers (usually grids) - we don't want to submit them - they're collected separately
            var $disabledGridFields = $(`.${YConfigs.Forms.CssFormNoSubmitContents} input,.${YConfigs.Forms.CssFormNoSubmitContents} select`, $form).not(':disabled');
            $disabledGridFields.attr('disabled', 'disabled');
            // serialize the form
            var formData = $form.serializeArray();
            // and enable all the input fields we just disabled
            $disabledFields.removeAttr('disabled');
            $disabledGridFields.removeAttr('disabled');
            return formData;
        }

        public serializeForm = function ($form: JQuery<HTMLFormElement>) {
            // disable all fields that we don't want to submit (marked with YConfigs.Forms.CssFormNoSubmit)
            var $disabledFields = $('.' + YConfigs.Forms.CssFormNoSubmit, $form).not(':disabled');
            $disabledFields.attr('disabled', 'disabled');
            // disable all input fields in containers (usually grids) - we don't want to submit them - they're collected separately
            var $disabledGridFields = $(`.${YConfigs.Forms.CssFormNoSubmitContents} input,.${YConfigs.Forms.CssFormNoSubmitContents} select`, $form).not(':disabled');
            $disabledGridFields.attr('disabled', 'disabled');
            // serialize the form
            var formData = $form.serialize();
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
        //   form: $form,               // form <div> to be processed
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
        public callPreSubmitHandler($form: JQuery<HTMLElement>, onSubmitExtraData: any) {
            for (var index in this.YPreSubmitHandlerAll) {
                var entry = this.YPreSubmitHandlerAll[index]
                if (entry.form[0] == $form[0]) {
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
                if (entry.form[0] == $form[0]) {
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
        //   form: $form,               // form <div> to be processed - may be null
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
        public callPostSubmitHandler($form: JQuery<HTMLElement>, onSubmitExtraData?: string) {
            for (var index in this.YPostSubmitHandlerAll) {
                var entry = this.YPostSubmitHandlerAll[index];
                if (entry.form == null) {
                    // global
                    entry.callback(entry);
                } else if (entry.form[0] == $form[0]) {
                    // form specific
                    entry.callback(entry);
                }
            }
            for (var index in this.YPostSubmitHandler1) {
                var entry = this.YPostSubmitHandler1[index];
                if (entry.form[0] == $form[0])
                    entry.callback(entry);
            }
            this.YPostSubmitHandler1 = [];
            return onSubmitExtraData;
        }

        // Forms retrieval

        public getForm(tag: HTMLElement | JQuery<HTMLElement>) : JQuery<HTMLFormElement> {
            var $form = $(tag).closest('form') as JQuery<HTMLFormElement>;
            if ($form.length == 0) throw "Can't locate enclosing form";/*DEBUG*/
            return $form;
        };
        public getFormCond(tag: HTMLElement | JQuery<HTMLElement>) : JQuery<HTMLFormElement> | null {
            var $form = $(tag).closest('form') as JQuery<HTMLFormElement>;
            if ($form.length == 0) return null;
            return $form;
        };
        // get RequestVerificationToken, UniqueIdPrefix and ModuleGuid in query string format (usually for ajax requests)
        public getFormInfo(tag: HTMLElement | JQuery<HTMLElement>) {
            var $form = this.getForm(tag);
            var req: string | undefined = <string|undefined> $('input[name="' + YConfigs.Forms.RequestVerificationToken + '"]', $form).val();
            if (req == undefined || req.length == 0) throw "Can't locate " + YConfigs.Forms.RequestVerificationToken;/*DEBUG*/
            var pre: string | undefined = <string|undefined> $('input[name="' + YConfigs.Forms.UniqueIdPrefix + '"]', $form).val();
            if (pre == undefined || pre.length == 0) throw "Can't locate " + YConfigs.Forms.UniqueIdPrefix;/*DEBUG*/
            var guid: string | undefined = <string|undefined> $('input[name="' + YConfigs.Basics.ModuleGuid + '"]', $form).val();
            if (guid == undefined || guid.length == 0) throw "Can't locate " + YConfigs.Basics.ModuleGuid;/*DEBUG*/

            var charSize = YetaWF_Basics.getCharSizeFromTag($form);

            var qs : string = "&" + YConfigs.Forms.RequestVerificationToken + "=" + encodeURIComponent(req) +
                "&" + YConfigs.Forms.UniqueIdPrefix + "=" + encodeURIComponent(pre) +
                "&" + YConfigs.Basics.ModuleGuid + "=" + encodeURIComponent(guid) +
                "&" + YGlobals.Link_CharInfo + "=" + charSize.width.toString() + ',' + charSize.height.toString();

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
        private submitForm: JQuery<HTMLFormElement> | null = null;

        private submitFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            this.submit(this.submitForm as JQuery<HTMLFormElement>, false);
        }
        private applyFormOnChange(): void {
            clearInterval(this.submitFormTimer);
            this.submit(this.submitForm as JQuery<HTMLFormElement>, false, YGlobals.Link_SubmitIsApply + "=y");
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
                        var origin = originList.pop();
                        var uri = new URI(origin.Url);
                        uri.removeSearch(YGlobals.Link_ToEditMode);
                        if (origin.EditMode != YVolatile.Basics.EditModeActive)
                            uri.addSearch(YGlobals.Link_ToEditMode, !YVolatile.Basics.EditModeActive);
                        uri.removeSearch(YGlobals.Link_OriginList);
                        if (originList.length > 0)
                            uri.addSearch(YGlobals.Link_OriginList, JSON.stringify(originList));
                        if (!YetaWF_Basics.ContentHandling.setContent(uri, true))
                            window.location.assign(uri as any);
                    } else {
                        // we don't know where to return so just close the browser
                        window.close();
                    }
                }
            });

            // Submit the form when an apply button is clicked

            $(document).on('click', `form input[type="button"][${YConfigs.Forms.CssDataApplyButton}]`, (e) => {
                e.preventDefault();
                var $form = YetaWF_Forms.getForm(e.currentTarget);
                YetaWF_Forms.submit($form, true, YGlobals.Link_SubmitIsApply + "=y");
            });

            // Submit the form when a submit button is clicked

            $(document).on('submit', 'form.' + YConfigs.Forms.CssFormAjax, (e) => {
                var $form = $(e.currentTarget) as JQuery<HTMLFormElement>;
                e.preventDefault();
                YetaWF_Forms.submit($form, true);
            });
        }
    }
}

var YetaWF_Forms: YetaWF.Forms = new YetaWF.Forms();

// initialize submit on change
YetaWF_Forms.initSubmitOnChange();
// initialize  Submit, Apply, Cancel button handling
YetaWF_Forms.initHandleFormsButtons();
