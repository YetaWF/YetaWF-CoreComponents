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
         * Serializes the form and returns a name/value pairs array
         */
        Forms.prototype.serializeFormArray = function (form) {
            return YetaWF_FormsImpl.serializeFormArray(form);
        };
        /**
         * Serializes the form and returns an object
         */
        Forms.prototype.serializeFormObject = function (form) {
            return YetaWF_FormsImpl.serializeFormObject(form);
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
            var saveReturn = form.getAttribute(YConfigs.Basics.CssSaveReturnUrl) !== null; // form says we need to save the return address on submit
            this.submitExplicit(form, method, form.action, saveReturn, useValidation, extraData, customEventData);
        };
        /**
         * Submit a form.
         * @param form The form being submitted.
         * @param method The method used to submit the form (typically post)
         * @param action The action URL used to submit the form.
         * @param saveReturn Defines whether the return URL is saved on submit.
         * @param useValidation Defines whether validation is performed before submission.
         * @param extraData Optional additional form data submitted.
         * @param customEventData
         * @returns Optional event information sent with EVENTPRESUBMIT/EVENTPOSTSUBMIT events as event.detail.customEventData.
         */
        Forms.prototype.submitExplicit = function (form, method, action, saveReturn, useValidation, extraData, customEventData) {
            var _this = this;
            console.log("====> submitExplicit");
            var jsonSubmit = $YetaWF.getAttributeCond(form, "data-json-submit"); //$$$$
            $YetaWF.pageChanged = false; // suppress navigate error
            var divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (var _i = 0, divs_1 = divs; _i < divs_1.length; _i++) {
                var div = divs_1[_i];
                $YetaWF.removeElement(div);
            }
            $YetaWF.sendCustomEvent(document.body, Forms.EVENTPRESUBMIT, { form: form, customEventData: customEventData, });
            var formValid = true;
            //$$$$ if (useValidation)
            //$$$$     formValid = this.validate(form);
            $YetaWF.closeOverlays();
            if (!useValidation || formValid) {
                // calculate origin list in case we need to navigate back
                var originList = YVolatile.Basics.OriginList;
                if (saveReturn) {
                    var currUri = $YetaWF.parseUrl(window.location.href);
                    currUri.removeSearch(YConfigs.Basics.Link_OriginList); // remove originlist from current URL
                    currUri.removeSearch(YConfigs.Basics.Link_InPopup); // remove popup info from current URL
                    originList = YVolatile.Basics.OriginList.slice(0); // copy saved originlist
                    var newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: $YetaWF.isInPopup() };
                    originList.push(newOrigin);
                    if (originList.length > 5) // only keep the last 5 urls
                        originList = originList.slice(originList.length - 5);
                }
                if (jsonSubmit != null) {
                    if (method.toLowerCase() === "get")
                        throw "FORM GET not supported";
                    // eslint-disable
                    debugger;
                    var uri = $YetaWF.parseUrl(action);
                    // serialize the form
                    var model = this.serializeFormObject(form);
                    var formData = {
                        Model: model,
                        __Apply: false,
                        __Reload: false,
                        __OriginList: originList,
                        UniqueIdCounters: YVolatile.Basics.UniqueIdCounters,
                        __Pagectl: YVolatile.Basics.PageControlVisible,
                        __InPopup: $YetaWF.isInPopup(),
                    };
                    // add extra data
                    if (extraData)
                        formData = Object.assign(formData, extraData);
                    // if (extraData) {
                    //     if (extraData[YConfigs.Basics.Link_SubmitIsApply] != null) {
                    //         formData.__Apply = extraData[YConfigs.Basics.Link_SubmitIsApply];
                    //         delete model[YConfigs.Basics.Link_SubmitIsApply];
                    //     }
                    //     if (extraData[YConfigs.Basics.Link_SubmitIsReload] != null) {
                    //         formData.__Reload = extraData[YConfigs.Basics.Link_SubmitIsReload];
                    //         delete model[YConfigs.Basics.Link_SubmitIsReload];
                    //     }
                    // }
                    $YetaWF.postJSON(uri, null, formData, function (success, responseText) {
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
                    // serialize the form
                    var formData = this.serializeForm(form);
                    // add extra data
                    if (extraData) {
                        for (var _a = 0, extraData_1 = extraData; _a < extraData_1.length; _a++) {
                            var entry = extraData_1[_a];
                            var key = entry.key;
                            if (key === YConfigs.Basics.Link_SubmitIsApply)
                                formData += "&".concat(encodeURIComponent(entry.key), "=").concat(entry.value ? "y" : "n");
                            else if (key === YConfigs.Basics.Link_SubmitIsReload)
                                formData += "&".concat(encodeURIComponent(entry.key), "=").concat(entry.value ? "y" : "n");
                            else
                                formData += "&".concat(encodeURIComponent(entry.key), "=").concat(encodeURIComponent(entry.value));
                        }
                    }
                    // add the origin list in case we need to navigate back
                    formData = formData + "&" + YConfigs.Basics.Link_OriginList + "=" + encodeURIComponent(JSON.stringify(originList));
                    // add uniqueidcounters
                    formData = formData + "&" + YConfigs.Forms.UniqueIdCounters + "=" + encodeURIComponent(JSON.stringify(YVolatile.Basics.UniqueIdCounters));
                    // add the status of the Pagecontrol
                    if (YVolatile.Basics.PageControlVisible)
                        formData = formData + "&" + YConfigs.Basics.Link_PageControl + "=y";
                    // add if we're in a popup
                    if ($YetaWF.isInPopup())
                        formData = formData + "&" + YConfigs.Basics.Link_InPopup + "=y";
                    if (method.toLowerCase() === "get")
                        action = "".concat(action, "?").concat(formData);
                    $YetaWF.send(method, action, formData, function (success, responseText) {
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
            for (var _b = 0, divs_2 = divs; _b < divs_2.length; _b++) {
                var div = divs_2[_b];
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
            if ($YetaWF.isInPopup()) {
                // we're in a popup, just close it
                $YetaWF.closePopup();
            }
            else {
                // go to the last entry in the origin list, pop that entry and pass it in the url
                var originList = YVolatile.Basics.OriginList;
                if (originList.length > 0) {
                    var origin_1 = originList.pop();
                    var uri = $YetaWF.parseUrl(origin_1.Url);
                    uri.removeSearch(YConfigs.Basics.Link_OriginList);
                    if (originList.length > 0)
                        uri.addSearch(YConfigs.Basics.Link_OriginList, JSON.stringify(originList));
                    $YetaWF.ContentHandling.setNewUri(uri);
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
        // get RequestVerificationToken, UniqueIdCounters and ModuleGuid in query string format (usually for ajax requests)
        Forms.prototype.getFormInfo = function (tag, addAmpersand) {
            var form = this.getForm(tag);
            var req = $YetaWF.getElement1BySelector("input[name='".concat(YConfigs.Forms.RequestVerificationToken, "']"), [form]).value;
            if (!req || req.length === 0)
                throw "Can't locate " + YConfigs.Forms.RequestVerificationToken; /*DEBUG*/
            var guid = $YetaWF.getElement1BySelector("input[name='".concat(YConfigs.Basics.ModuleGuid, "']"), [form]).value;
            if (!guid || guid.length === 0)
                throw "Can't locate " + YConfigs.Basics.ModuleGuid; /*DEBUG*/
            var qs = "";
            if (addAmpersand !== false)
                qs += "&";
            qs += YConfigs.Forms.RequestVerificationToken + "=" + encodeURIComponent(req) +
                "&" + YConfigs.Forms.UniqueIdCounters + "=" + JSON.stringify(YVolatile.Basics.UniqueIdCounters) +
                "&" + YConfigs.Basics.ModuleGuid + "=" + encodeURIComponent(guid);
            var info = {
                RequestVerificationToken: req,
                UniqueIdCounters: YVolatile.Basics.UniqueIdCounters,
                ModuleGuid: guid,
                QS: qs
            };
            return info;
        };
        // get RequestVerificationToken and ModuleGuid (usually for ajax requests)
        Forms.prototype.getJSONInfo = function (tagInForm) {
            var req = null;
            var form = null;
            if (!form) {
                // get token from form containing the tag
                form = this.getFormCond(tagInForm);
            }
            if (!form) {
                // get token from module, then form containing the tag
                var mod = YetaWF.ModuleBase.getModuleDivFromTagCond(tagInForm);
                if (mod)
                    form = this.getInnerFormCond(mod);
            }
            if (!form)
                throw "Can't locate form";
            var reqVerElem = $YetaWF.getElement1BySelectorCond("input[name='".concat(YConfigs.Forms.RequestVerificationToken, "']"), [form]);
            if (reqVerElem)
                req = reqVerElem.value;
            if (!req || req.length === 0)
                throw "Can't locate " + YConfigs.Forms.RequestVerificationToken; /*DEBUG*/
            var guid = $YetaWF.getElement1BySelector("input[name='".concat(YConfigs.Basics.ModuleGuid, "']"), [form]).value;
            if (!guid || guid.length === 0)
                throw "Can't locate " + YConfigs.Basics.ModuleGuid; /*DEBUG*/
            var info = {};
            info[YConfigs.Forms.RequestVerificationToken] = req;
            info[YConfigs.Basics.ModuleGuid] = guid;
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
            $YetaWF.registerEventHandlerBody("submit", "form." + YConfigs.Forms.CssFormAjax, function (ev) {
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
