"use strict";
/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    var TabStyleEnum;
    (function (TabStyleEnum) {
        TabStyleEnum[TabStyleEnum["JQuery"] = 0] = "JQuery";
        TabStyleEnum[TabStyleEnum["Kendo"] = 1] = "Kendo";
    })(TabStyleEnum = YetaWF.TabStyleEnum || (YetaWF.TabStyleEnum = {}));
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
            // Partial Form
            // Submit
            this.DATACLASS = "yetawf_forms_data"; // add divs with this class to form for any data that needs to be submitted (will be removed before calling (pre)submit handlers.
            // Pre/post submit
            // When a form is about to be submitted, all the functions in YPreSubmitHandler are called one by one
            // This is used to add control-specific data to the data submitted by the form
            // Usage:
            // $YetaWF.Forms.addPreSubmitHandler(@Manager.InPartialForm ? 1 : 0, {
            //   form: form,                // form <div> to be processed
            //   callback: function() {}    // function to be called - the callback returns extra data appended to the submit url
            //   userdata: callback-data,   // any data suitable to callback
            // });
            this.preSubmitHandlerAll = []; // done every time before submit (never cleared) - used on main forms
            this.preSubmitHandler1 = []; // done once before submit, then cleared - used in partial forms
            // When a form has been successfully submitted, all the functions in YPostSubmitHandler are called one by one
            // Usage:
            // $YetaWF.Forms.addPostSubmitHandler(@Manager.InPartialForm ? 1 : 0, {
            //   form: form,                // form <div> to be processed - may be null
            //   callback: function() {}    // function to be called
            //   userdata: callback-data,   // any data suitable to callback
            // });
            this.YPostSubmitHandlerAll = []; // done every time after submit (never cleared) - used on main forms
            this.YPostSubmitHandler1 = []; // done once after submit, then cleared - used in partial forms
            this.submitFormTimer = undefined;
            this.submitForm = null;
        }
        /**
         * Initialize a partial form.
         */
        Forms.prototype.initPartialForm = function (elemId) {
            var partialForm = $YetaWF.getElementById(elemId);
            // run registered actions (usually javascript initialization, similar to $doc.ready()
            $YetaWF.processAllReady([partialForm]);
            $YetaWF.processAllReadyOnce([partialForm]);
            YetaWF_FormsImpl.initPartialForm(partialForm);
            // show error popup
            var hasErrors = this.hasErrors(partialForm);
            if (hasErrors)
                this.showErrors(partialForm);
        };
        /**
         * Validates one elements.
         */
        Forms.prototype.validateElement = function (ctrl) {
            YetaWF_FormsImpl.validateElement(ctrl);
        };
        /**
         * Re-validate all fields within the div, typically used after paging in a grid to let jquery.validate update all fields
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
         * Validate all fields in the current form.
         */
        Forms.prototype.validate = function (form) {
            YetaWF_FormsImpl.validate(form);
        };
        /**
         * Returns whether all fields in the current form are valid.
         */
        Forms.prototype.isValid = function (form) {
            return YetaWF_FormsImpl.isValid(form);
        };
        Forms.prototype.submit = function (form, useValidation, extraData, successFunc, failFunc) {
            var _this = this;
            if (!form.getAttribute("method"))
                return; // no method, don't submit
            var divs = $YetaWF.getElementsBySelector("div." + this.DATACLASS);
            for (var _i = 0, divs_1 = divs; _i < divs_1.length; _i++) {
                var div = divs_1[_i];
                $YetaWF.removeElement(div);
            }
            var onSubmitExtraData = extraData ? extraData : "";
            onSubmitExtraData = this.callPreSubmitHandler(form, onSubmitExtraData);
            if (useValidation)
                this.validate(form);
            $YetaWF.closeOverlays();
            $YetaWF.setLoading(true);
            if (!useValidation || this.isValid(form)) {
                // serialize the form
                var formData = this.serializeForm(form);
                // add extra data
                if (onSubmitExtraData)
                    formData = onSubmitExtraData + "&" + formData;
                // add the origin list in case we need to navigate back
                var originList = YVolatile.Basics.OriginList;
                if (form.getAttribute(YConfigs.Basics.CssSaveReturnUrl) !== null) { // form says we need to save the return address on submit
                    var currUri = $YetaWF.parseUrl(window.location.href);
                    currUri.removeSearch(YConfigs.Basics.Link_OriginList); // remove originlist from current URL
                    currUri.removeSearch(YConfigs.Basics.Link_InPopup); // remove popup info from current URL
                    originList = YVolatile.Basics.OriginList.slice(0); // copy saved originlist
                    var newOrigin = { Url: currUri.toUrl(), EditMode: YVolatile.Basics.EditModeActive, InPopup: $YetaWF.isInPopup() };
                    originList.push(newOrigin);
                    if (originList.length > 5) // only keep the last 5 urls
                        originList = originList.slice(originList.length - 5);
                }
                formData = formData + "&" + YConfigs.Basics.Link_OriginList + "=" + encodeURIComponent(JSON.stringify(originList));
                // include the character dimension info
                {
                    var charSize = $YetaWF.getCharSizeFromTag(form);
                    formData = formData + "&" + YConfigs.Basics.Link_CharInfo + "=" + charSize.width.toString() + "," + charSize.height.toString();
                }
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
                var request = new XMLHttpRequest();
                request.open(form.method, form.action, true);
                request.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
                request.onreadystatechange = function (ev) {
                    var req = request;
                    if (req.readyState === 4 /*DONE*/) {
                        $YetaWF.setLoading(false);
                        if ($YetaWF.processAjaxReturn(req.responseText, req.statusText, req, form, undefined, function (result) {
                            _this.preSubmitHandler1 = [];
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
                            _this.callPostSubmitHandler(form);
                            if (successFunc) // executed on successful ajax submit
                                successFunc(_this.hasErrors(form));
                        })) {
                            // ok
                        }
                        else {
                            if (failFunc)
                                failFunc();
                        }
                    }
                };
                request.send(formData);
            }
            else {
                $YetaWF.setLoading(false);
                // find the first field in each tab control that has an input validation error and activate that tab
                // This will not work for nested tabs. Only the lowermost tab will be activated.
                var elems = $YetaWF.getElementsBySelector("div.yt_propertylist.t_tabbed", [form]);
                elems.forEach(function (tabctrl, index) {
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
            for (var _a = 0, divs_2 = divs; _a < divs_2.length; _a++) {
                var div = divs_2[_a];
                $YetaWF.removeElement(div);
            }
        };
        Forms.prototype.submitTemplate = function (tag, useValidation, templateName, templateAction, templateExtraData) {
            var qs = YConfigs.Basics.TemplateName + "=" + templateName + "&" + YConfigs.Basics.Link_SubmitIsApply + "=y";
            if (templateAction)
                qs += "&" + YConfigs.Basics.TemplateAction + "=" + encodeURIComponent(templateAction);
            if (templateExtraData)
                qs += "&" + YConfigs.Basics.TemplateExtraData + "=" + encodeURIComponent(templateExtraData);
            var form = this.getForm(tag);
            if ($YetaWF.elementHasClass(form, YConfigs.Forms.CssFormNoSubmit))
                return;
            this.submit(form, useValidation, qs);
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
        Forms.prototype.cancel = function () {
            if ($YetaWF.isInPopup()) {
                // we're in a popup, just close it
                $YetaWF.closePopup();
            }
            else {
                // go to the last entry in the origin list, pop that entry and pass it in the url
                var originList = YVolatile.Basics.OriginList;
                if (originList.length > 0) {
                    var origin = originList.pop();
                    var uri = $YetaWF.parseUrl(origin.Url);
                    uri.removeSearch(YConfigs.Basics.Link_ToEditMode);
                    if (origin.EditMode !== YVolatile.Basics.EditModeActive)
                        uri.addSearch(YConfigs.Basics.Link_ToEditMode, !YVolatile.Basics.EditModeActive ? "0" : "1");
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
                        var uri_1 = $YetaWF.parseUrl("/");
                        $YetaWF.ContentHandling.setNewUri(uri_1);
                    }
                    catch (e) { }
                }
            }
        };
        /**
         * Add a callback to be called when a form is about to be submitted.
         */
        Forms.prototype.addPreSubmitHandler = function (inPartialForm, entry) {
            if (inPartialForm) {
                this.preSubmitHandler1.push(entry);
            }
            else {
                this.preSubmitHandlerAll.push(entry);
            }
        };
        /**
         * Call all callbacks for a form that is about to be submitted.
         */
        Forms.prototype.callPreSubmitHandler = function (form, onSubmitExtraData) {
            for (var _i = 0, _a = this.preSubmitHandlerAll; _i < _a.length; _i++) {
                var entry = _a[_i];
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
            for (var _b = 0, _c = this.preSubmitHandler1; _b < _c.length; _b++) {
                var entry = _c[_b];
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
        };
        /**
         * Add a callback to be called when a form has been successfully submitted.
         */
        Forms.prototype.addPostSubmitHandler = function (inPartialForm, entry) {
            if (inPartialForm) {
                this.YPostSubmitHandler1.push(entry);
            }
            else {
                this.YPostSubmitHandlerAll.push(entry);
            }
        };
        /**
         * Call all callbacks for a form that has been successfully submitted.
         */
        Forms.prototype.callPostSubmitHandler = function (form, onSubmitExtraData) {
            for (var _i = 0, _a = this.YPostSubmitHandlerAll; _i < _a.length; _i++) {
                var entry = _a[_i];
                if (entry.form == null) {
                    // global
                    entry.callback(entry);
                }
                else if (entry.form === form) {
                    // form specific
                    entry.callback(entry);
                }
            }
            for (var _b = 0, _c = this.YPostSubmitHandler1; _b < _c.length; _b++) {
                var entry = _c[_b];
                if (entry.form === form)
                    entry.callback(entry);
            }
            this.YPostSubmitHandler1 = [];
        };
        // Forms retrieval
        Forms.prototype.getForm = function (tag) {
            return $YetaWF.elementClosest(tag, "form");
        };
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
            var req = $YetaWF.getElement1BySelector("input[name='" + YConfigs.Forms.RequestVerificationToken + "']", [form]).value;
            if (!req || req.length === 0)
                throw "Can't locate " + YConfigs.Forms.RequestVerificationToken; /*DEBUG*/
            var guid = $YetaWF.getElement1BySelector("input[name='" + YConfigs.Basics.ModuleGuid + "']", [form]).value;
            if (!guid || guid.length === 0)
                throw "Can't locate " + YConfigs.Basics.ModuleGuid; /*DEBUG*/
            var charSize = $YetaWF.getCharSizeFromTag(form);
            var qs = "";
            if (addAmpersand !== false)
                qs += "&";
            qs += YConfigs.Forms.RequestVerificationToken + "=" + encodeURIComponent(req) +
                "&" + YConfigs.Forms.UniqueIdCounters + "=" + JSON.stringify(YVolatile.Basics.UniqueIdCounters) +
                "&" + YConfigs.Basics.ModuleGuid + "=" + encodeURIComponent(guid) +
                "&" + YConfigs.Basics.Link_CharInfo + "=" + charSize.width.toString() + "," + charSize.height.toString();
            var info = {
                RequestVerificationToken: req,
                UniqueIdCounters: YVolatile.Basics.UniqueIdCounters,
                ModuleGuid: guid,
                CharInfo: charSize.width.toString() + "," + charSize.height.toString(),
                QS: qs
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
            this.submit(this.submitForm, false, YConfigs.Basics.Link_SubmitIsApply + "=y");
        };
        Forms.prototype.reloadFormOnChange = function () {
            clearInterval(this.submitFormTimer);
            if (!this.submitForm)
                return;
            this.submit(this.submitForm, false, YConfigs.Basics.Link_SubmitIsReload + "=y");
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
            $YetaWF.registerEventHandlerBody("click", "form input[type=\"button\"][" + YConfigs.Forms.CssDataApplyButton + "]", function (ev) {
                var form = _this.getForm(ev.target);
                _this.submit(form, true, YConfigs.Basics.Link_SubmitIsApply + "=y");
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
        return Forms;
    }());
    YetaWF.Forms = Forms;
})(YetaWF || (YetaWF = {}));

//# sourceMappingURL=Forms.js.map
