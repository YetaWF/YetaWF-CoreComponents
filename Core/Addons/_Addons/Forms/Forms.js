"use strict";
/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    var PanelAction;
    (function (PanelAction) {
        PanelAction[PanelAction["Apply"] = 0] = "Apply";
        PanelAction[PanelAction["MoveLeft"] = 1] = "MoveLeft";
        PanelAction[PanelAction["MoveRight"] = 2] = "MoveRight";
        PanelAction[PanelAction["Add"] = 3] = "Add";
        PanelAction[PanelAction["Insert"] = 4] = "Insert";
        PanelAction[PanelAction["Remove"] = 5] = "Remove";
    })(PanelAction = YetaWF.PanelAction || (YetaWF.PanelAction = {}));
    var Forms = /** @class */ (function () {
        function Forms() {
            // Submit
            this.DATACLASS = "yetawf_forms_data"; // add divs with this class to form for any data that needs to be submitted (will be removed before calling (pre)submit handlers.
            this.submitFormTimer = undefined;
            this.submitForm = null;
        }
        // Partial Form
        /**
         * Initialize a partial form.
         */
        Forms.prototype.initPartialForm = function (elemId) {
            var partialForm = $YetaWF.getElementById(elemId);
            // run registered actions (usually javascript initialization, similar to $doc.ready()
            $YetaWF.sendCustomEvent(document.body, YetaWF.Content.EVENTNAVPAGELOADED, { containers: [partialForm] });
            $YetaWF.processAllReadyOnce([partialForm]);
            YetaWF_FormsImpl.initPartialForm(partialForm);
            // show error popup
            var hasErrors = this.hasErrors(partialForm);
            if (hasErrors)
                this.showErrors(partialForm);
        };
        /**
         * Validate one element.
         * If the contents are empty the field will be fully validated. If contents are present, the error indicator is reset.
         * Full validation takes place on blur (or using validateElementFully).
         */
        Forms.prototype.validateElement = function (ctrl, hasValue) {
            YetaWF_FormsImpl.validateElement(ctrl, hasValue);
        };
        /**
         * Validate one element.
         * Full validation takes place.
         */
        Forms.prototype.validateElementFully = function (ctrl) {
            YetaWF_FormsImpl.validateElementFully(ctrl);
        };
        /**
         * Re-validate all fields within the div, typically used after paging in a grid to let validation update all fields
         */
        Forms.prototype.updateValidation = function (div) {
            YetaWF_FormsImpl.updateValidation(div);
        };
        /**
         * Clear any validation errors within the div
         */
        Forms.prototype.clearValidation = function (div) {
            YetaWF_FormsImpl.clearValidation(div);
        };
        /**
         * Clear any validation errors within the div
         */
        Forms.prototype.clearValidation1 = function (elem) {
            YetaWF_FormsImpl.clearValidation1(elem);
        };
        /**
         * Returns whether the form has errors.
         */
        Forms.prototype.hasErrors = function (elem) {
            return YetaWF_FormsImpl.hasErrors(elem);
        };
        /**
         * Shows all form errors in a popup.
         */
        Forms.prototype.showErrors = function (elem) {
            YetaWF_FormsImpl.showErrors(elem);
        };
        /**
         * Serializes the form and returns an object
         */
        Forms.prototype.serializeFormObject = function (form) {
            return YetaWF_FormsImpl.serializeFormObject(form);
        };
        /**
         * Serializes the form and returns a name/value pairs array
         */
        Forms.prototype.serializeFormArray = function (form) {
            return YetaWF_FormsImpl.serializeFormArray(form);
        };
        /**
         * Validate all fields in the current form.
         */
        Forms.prototype.validate = function (form) {
            return YetaWF_FormsImpl.validate(form);
        };
        /**
         * Returns whether all fields in the current form are valid.
         */
        Forms.prototype.isValid = function (form) {
            return YetaWF_FormsImpl.isValid(form);
        };
        /**
         * Resequences array indexes in forms fields.
         */
        Forms.prototype.resequenceFields = function (rows, prefix) {
            return YetaWF_FormsImpl.resequenceFields(rows, prefix);
        };
        /**
         * Submit a form.
         * @param form The form being submitted.
         * @param useValidation Defines whether validation is performed before submission.
         * @param extraData Optional additional form data submitted.
         * @param customEventData
         * @returns Optional event information sent with EVENTPRESUBMIT/EVENTPOSTSUBMIT events as event.detail.customEventData.
         */
        Forms.prototype.submit = function (form, useValidation, extraData, customEventData) {
            var method = form.getAttribute("method");
            if (!method)
                return; // no method, don't submit
            this.submitExplicit(form, method, form.action, useValidation, extraData, customEventData);
        };
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
        Forms.prototype.submitExplicit = function (form, method, action, useValidation, extraData, customEventData) {
            var _this = this;
            $YetaWF.pageChanged = false; // suppress navigate error
            var divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (var _i = 0, divs_1 = divs; _i < divs_1.length; _i++) {
                var div = divs_1[_i];
                $YetaWF.removeElement(div);
            }
            $YetaWF.sendCustomEvent(document.body, Forms.EVENTPRESUBMIT, { form: form, customEventData: customEventData, });
            var formValid = true;
            if (useValidation)
                formValid = this.validate(form);
            $YetaWF.closeOverlays();
            if (!useValidation || formValid) {
                if (method.toLowerCase() !== "post")
                    throw "FORM ".concat(method, " not supported");
                var uri = $YetaWF.parseUrl(action);
                // serialize the form
                var model = this.serializeFormObject(form);
                var formData = {
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
                var formJson = $YetaWF.Forms.getJSONInfo(form);
                $YetaWF.postJSON(uri, formJson, null, formData, function (success, responseText) {
                    if (success) {
                        if (responseText) {
                            var partForm = $YetaWF.getElement1BySelectorCond("." + YConfigs.Forms.CssFormPartial, [form]);
                            if (partForm) {
                                // clean up everything that's about to be removed
                                $YetaWF.processClearDiv(partForm);
                                // preserve the original css classes on the partial form (PartialFormCss)
                                var cls = partForm.className;
                                $YetaWF.setMixedOuterHTML(partForm, responseText);
                                partForm = $YetaWF.getElement1BySelectorCond("." + YConfigs.Forms.CssFormPartial, [form]);
                                if (partForm)
                                    partForm.className = cls;
                            }
                        }
                        $YetaWF.sendCustomEvent(form, Forms.EVENTPOSTSUBMIT, { success: !_this.hasErrors(form), form: form, customEventData: customEventData, response: responseText });
                        $YetaWF.setFocus([form]);
                    }
                    else {
                        $YetaWF.sendCustomEvent(form, Forms.EVENTPOSTSUBMIT, { success: false, form: form, customEventData: customEventData, });
                    }
                });
            }
            else {
                // find the first field in each tab control that has an input validation error and activate that tab
                // This will not work for nested tabs. Only the lowermost tab will be activated.
                YetaWF_FormsImpl.setErrorInNestedControls(form);
                var hasErrors = this.hasErrors(form);
                if (hasErrors)
                    this.showErrors(form);
                // call callback (if any)
                $YetaWF.sendCustomEvent(form, Forms.EVENTPOSTSUBMIT, { success: false, form: form, customEventData: customEventData, });
            }
            divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (var _a = 0, divs_2 = divs; _a < divs_2.length; _a++) {
                var div = divs_2[_a];
                $YetaWF.removeElement(div);
            }
        };
        Forms.prototype.submitTemplate = function (tag, useValidation, templateName, templateAction, templateExtraData) {
            var form = this.getForm(tag);
            if ($YetaWF.elementHasClass(form, YConfigs.Forms.CssFormNoSubmit))
                return;
            var extraData = {};
            extraData[YConfigs.Basics.TemplateName] = templateName;
            extraData[YConfigs.Basics.Link_SubmitIsApply] = true;
            if (templateAction)
                extraData[YConfigs.Basics.TemplateAction] = templateAction;
            if (templateExtraData)
                extraData[YConfigs.Basics.TemplateExtraData] = templateExtraData; //$$$$
            this.submit(form, useValidation, extraData);
        };
        Forms.prototype.serializeForm = function (form) {
            var pairs = this.serializeFormArray(form);
            var formData = "";
            for (var _i = 0, pairs_1 = pairs; _i < pairs_1.length; _i++) {
                var entry = pairs_1[_i];
                if (formData !== "")
                    formData += "&";
                formData += encodeURIComponent(entry.name) + "=" + encodeURIComponent(entry.value);
            }
            return formData;
        };
        // Cancel
        /**
         * Cancels the current form (Cancel button handling).
         */
        Forms.prototype.cancel = function () {
            this.goBack();
        };
        /**
         * Returns to the previous page.
         */
        Forms.prototype.goBack = function () {
            if ($YetaWF.isInPopup()) {
                // we're in a popup, just close it
                $YetaWF.closePopup();
            }
            else {
                var state = history.state;
                if (state) {
                    history.back();
                }
                else {
                    // we don't know where to return so just close the browser
                    try {
                        window.close();
                    }
                    catch (e) { }
                    try {
                        // TODO: use home page
                        var uri = $YetaWF.parseUrl("/");
                        $YetaWF.ContentHandling.setNewUri(uri);
                    }
                    catch (e) { }
                }
            }
        };
        /**
         * Retrieve the form element containing the specified element tag.
         * An error occurs if no form can be found.
         * @param tag The element contained within a form.
         * @returns The form containing element tag.
         */
        Forms.prototype.getForm = function (tag) {
            return $YetaWF.elementClosest(tag, "form");
        };
        /**
         * Retrieve the form element containing the specified element tag.
         * @param tag The element contained within a form.
         * @returns The form containing element tag or null.
         */
        Forms.prototype.getFormCond = function (tag) {
            var form = $YetaWF.elementClosestCond(tag, "form");
            if (!form)
                return null;
            return form;
        };
        Forms.prototype.getInnerForm = function (tag) {
            return $YetaWF.getElement1BySelector("form", [tag]);
        };
        Forms.prototype.getInnerFormCond = function (tag) {
            return $YetaWF.getElement1BySelectorCond("form", [tag]);
        };
        // get ModuleGuid
        Forms.prototype.getJSONInfo = function (tagInForm, uniqueIdInfo) {
            var moduleGuid = null;
            var elem = $YetaWF.elementClosestCond(tagInForm, "[".concat(YConfigs.Basics.CssModuleGuid, "]"));
            if (elem) {
                moduleGuid = elem.getAttribute(YConfigs.Basics.CssModuleGuid) || "";
            }
            else {
                // we're not within a module or an element with an owning module
                moduleGuid = "";
            }
            var info = {
                ModuleGuid: moduleGuid,
                UniqueIdCounters: uniqueIdInfo || YVolatile.Basics.UniqueIdCounters,
            };
            return info;
        };
        // Submit/apply on change/keydown
        Forms.prototype.submitOnChange = function (elem) {
            var _this = this;
            clearInterval(this.submitFormTimer);
            this.submitForm = this.getForm(elem);
            this.submitFormTimer = setInterval(function () { return _this.submitFormOnChange(); }, 1000); // wait 1 second and automatically submit the form
            $YetaWF.setLoading(true);
        };
        Forms.prototype.submitOnReturnKey = function (elem) {
            this.submitForm = this.getForm(elem);
            this.submitFormOnChange();
        };
        Forms.prototype.applyOnChange = function (elem) {
            var _this = this;
            clearInterval(this.submitFormTimer);
            this.submitForm = this.getForm(elem);
            this.submitFormTimer = setInterval(function () { return _this.applyFormOnChange(); }, 1000); // wait 1 second and automatically submit the form
            $YetaWF.setLoading(true);
        };
        Forms.prototype.applyOnReturnKey = function (elem) {
            this.submitForm = this.getForm(elem);
            this.applyFormOnChange();
        };
        Forms.prototype.reloadOnChange = function (elem) {
            var _this = this;
            clearInterval(this.submitFormTimer);
            this.submitForm = this.getForm(elem);
            this.submitFormTimer = setInterval(function () { return _this.reloadFormOnChange(); }, 1000); // wait 1 second and automatically submit the form
            $YetaWF.setLoading(true);
        };
        Forms.prototype.reloadOnReturnKey = function (elem) {
            this.submitForm = this.getForm(elem);
            this.reloadFormOnChange();
        };
        Forms.prototype.submitFormOnChange = function () {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm)
                return;
            if ($YetaWF.elementHasClass(this.submitForm, YConfigs.Forms.CssFormNoSubmit))
                return;
            this.submit(this.submitForm, false);
        };
        Forms.prototype.applyFormOnChange = function () {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm)
                return;
            var extraData = {};
            extraData[YConfigs.Basics.Link_SubmitIsApply] = true;
            this.submit(this.submitForm, false, extraData);
        };
        Forms.prototype.reloadFormOnChange = function () {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm)
                return;
            var extraData = {};
            extraData[YConfigs.Basics.Link_SubmitIsReload] = true;
            this.submit(this.submitForm, false, extraData);
        };
        // submit form on change
        /**
         * Handles submitonchange/applyonchange
         */
        Forms.prototype.initSubmitOnChange = function () {
            var _this = this;
            // submit
            $YetaWF.registerEventHandlerBody("change", ".ysubmitonchange select,.ysubmitonchange input[type=\"checkbox\"]", function (ev) {
                _this.submitOnChange(ev.target);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".ysubmitonchange select", function (ev) {
                if (ev.keyCode === 13) {
                    _this.submitOnChange(ev.target);
                    return false;
                }
                return true;
            });
            // apply
            $YetaWF.registerEventHandlerBody("change", ".yapplyonchange select,.yapplyonchange input[type=\"checkbox\"]", function (ev) {
                _this.applyOnChange(ev.target);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".yapplyonchange select", function (ev) {
                if (ev.keyCode === 13) {
                    _this.applyOnChange(ev.target);
                    return false;
                }
                return true;
            });
            // reload
            $YetaWF.registerEventHandlerBody("change", ".yreloadonchange select,.yreloadonchange input[type=\"checkbox\"]", function (ev) {
                _this.reloadOnChange(ev.target);
                return false;
            });
            $YetaWF.registerEventHandlerBody("keyup", ".yreloadonchange select", function (ev) {
                if (ev.keyCode === 13) {
                    _this.reloadOnChange(ev.target);
                    return false;
                }
                return true;
            });
        };
        /**
         * Initialize to handle Submit, Apply, Cancel buttons
         */
        Forms.prototype.initHandleFormsButtons = function () {
            // Cancel the form when a Cancel button is clicked
            var _this = this;
            $YetaWF.registerEventHandlerBody("click", "form ." + YConfigs.Forms.CssFormCancel, function (ev) {
                _this.cancel();
                return false;
            });
            // Submit the form when an apply button is clicked
            $YetaWF.registerEventHandlerBody("click", "form input[type=\"button\"][".concat(YConfigs.Forms.CssDataApplyButton, "]"), function (ev) {
                var form = _this.getForm(ev.target);
                var extraData = {};
                extraData[YConfigs.Basics.Link_SubmitIsApply] = true;
                _this.submit(form, true, extraData);
                return false;
            });
            // Submit the form when a submit button is clicked
            $YetaWF.registerEventHandlerBody("submit", "form." + YConfigs.Forms.CssForm, function (ev) {
                var form = _this.getForm(ev.target);
                if ($YetaWF.elementHasClass(form, YConfigs.Forms.CssFormNoSubmit))
                    return false;
                _this.submit(form, true);
                return false;
            });
        };
        Forms.prototype.init = function () {
            // initialize submit on change
            this.initSubmitOnChange();
            // initialize  Submit, Apply, Cancel button handling
            this.initHandleFormsButtons();
        };
        Forms.EVENTPRESUBMIT = "form_presubmit";
        Forms.EVENTPOSTSUBMIT = "form_postsubmit";
        return Forms;
    }());
    YetaWF.Forms = Forms;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Forms.js.map
