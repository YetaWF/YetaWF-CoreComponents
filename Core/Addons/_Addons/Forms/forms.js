/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

// Form submit handling for all forms
// http://jqueryvalidation.org/documentation/
// http://bradwilson.typepad.com/blog/2010/10/mvc3-unobtrusive-validation.html

// Make sure all hidden fields are NOT ignored
$.validator.setDefaults({
    ignore: '.yNoValidate', // don't ignore hidden fields - ignore fields with .yNoValidate class
});
$.validator.unobtrusive.options = {
    errorElement: 'label'
};

var YetaWF_Forms = {};
var _YetaWF_Forms = {};

// AJAX PARTIALFORM INITIALIZATION
// AJAX PARTIALFORM INITIALIZATION
// AJAX PARTIALFORM INITIALIZATION

YetaWF_Forms.partialFormActionsAll = [];
YetaWF_Forms.partialFormActions1 = [];
// Usage (executed on every partial form, not deleted):
// YetaWF_Forms.partialFormActionsAll.push({
//   callback: ..., // javascript to execute
// });
// Usage (once for partial form then deleted):
// YetaWF_Forms.partialFormActions1.push({
//   callback: ..., // javascript to execute
// });
YetaWF_Forms.initPartialForm = function ($partialForm) {
    // run registered actions (usually javascript initialization, similar to $doc.ready()
    for (var entry in YetaWF_Forms.partialFormActionsAll) {
        YetaWF_Forms.partialFormActionsAll[entry].callback($partialForm);
    }
    for (var entry in YetaWF_Forms.partialFormActions1) {
        YetaWF_Forms.partialFormActions1[entry].callback($partialForm);
    }
    YetaWF_Forms.partialFormActions1 = [];

    // get all fields with errors (set server-side)
    var $errs = $('.field-validation-error', $partialForm);
    // add warning icons to validation errors
    $errs.each(function () {
        var $val = $(this);
        var name = $val.attr("data-valmsg-for");
        var $err = $('img.{0}[name="{1}"]'.format(YConfigs.Forms.CssWarningIcon, name), $val.closest('form'));
        $err.remove();
        $val.before('<img src="{0}" name={1} class="{2}" {3}="{4}"/>'.format(
            Y_HtmlEscape(YConfigs.Forms.CssWarningIconUrl), name, YConfigs.Forms.CssWarningIcon, YConfigs.Basics.CssTooltip, Y_HtmlEscape($val.text())));
    });

    // show error popup
    var hasErrors = _YetaWF_Forms.hasErrors($partialForm);
    if (hasErrors)
        _YetaWF_Forms.showErrors($partialForm);
};

// ERROR HANDLING
// ERROR HANDLING
// ERROR HANDLING

_YetaWF_Forms.hasErrors = function ($form)
{
    return $('.validation-summary-errors li', $form).length > 0;
};
_YetaWF_Forms.formErrorSummary = function ($form) {
    var $summary = $('.validation-summary-errors', $form);
    if ($summary.length != 1) throw "Error summary not found";/*DEBUG*/
    return $summary;
};

// When a form is about to be submitted, all the functions in YPreSubmitHandler are called one by one
// This is used to add control-specific data to the data submitted by the form
// Usage:
// YetaWF_Forms.addPreSubmitHandler(@Manager.InPartialForm ? 1 : 0, {
//   form: $form,               // form <div> to be processed
//   callback: function() {}    // function to be called - the callback returns extra data appended to the submit url
//   userdata: callback-data,   // any data suitable to callback
// });
_YetaWF_Forms.YPreSubmitHandlerAll = []; // done every time before submit (never cleared) - used on main forms
_YetaWF_Forms.YPreSubmitHandler1 = []; // done once before submit, then cleared - used in partial forms

YetaWF_Forms.addPreSubmitHandler = function(inPartialForm, func)
{
    if (inPartialForm) {
        _YetaWF_Forms.YPreSubmitHandler1.push(func);
    } else {
        _YetaWF_Forms.YPreSubmitHandlerAll.push(func);
    }
}

_YetaWF_Forms.callPreSubmitHandler = function($form, onSubmitExtraData) {
    for (var index in _YetaWF_Forms.YPreSubmitHandlerAll) {
        var entry = _YetaWF_Forms.YPreSubmitHandlerAll[index]
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
    for (var index in _YetaWF_Forms.YPreSubmitHandler1) {
        var entry = _YetaWF_Forms.YPreSubmitHandler1[index];
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
_YetaWF_Forms.YPostSubmitHandlerAll = []; // done every time after submit (never cleared) - used on main forms
_YetaWF_Forms.YPostSubmitHandler1 = []; // done once after submit, then cleared - used in partial forms

YetaWF_Forms.addPostSubmitHandler = function (inPartialForm, func) {
    if (inPartialForm) {
        _YetaWF_Forms.YPostSubmitHandler1.push(func);
    } else {
        _YetaWF_Forms.YPostSubmitHandlerAll.push(func);
    }
}

_YetaWF_Forms.callPostSubmitHandler = function ($form, onSubmitExtraData) {
    for (var index in _YetaWF_Forms.YPostSubmitHandlerAll) {
        var entry = _YetaWF_Forms.YPostSubmitHandlerAll[index];
        if (entry.form == null) {
            // global
            entry.callback(entry);
        } else if (entry.form[0] == $form[0]) {
            // form specific
            entry.callback(entry);
        }
    }
    for (var index in _YetaWF_Forms.YPostSubmitHandler1) {
        var entry = _YetaWF_Forms.YPostSubmitHandler1[index];
        if (entry.form[0] == $form[0])
            entry.callback(entry);
    }
    return onSubmitExtraData;
}

YetaWF_Forms.serializeFormArray = function ($form) {
    // disable all fields that we don't want to submit (marked with YConfigs.Forms.CssFormNoSubmit)
    var $disabledFields = $('.' + YConfigs.Forms.CssFormNoSubmit, $form).not(':disabled');
    $disabledFields.attr('disabled', 'disabled');
    // disable all input fields in containers (usually grids) - we don't want to submit them - they're collected separately
    var $disabledGridFields = $('.{0} input,.{0} select'.format(YConfigs.Forms.CssFormNoSubmitContents), $form).not(':disabled');
    $disabledGridFields.attr('disabled', 'disabled');
    // serialize the form
    var formData = $form.serializeArray();
    // and enable all the input fields we just disabled
    $disabledFields.removeAttr('disabled');
    $disabledGridFields.removeAttr('disabled');
    return formData;
}

YetaWF_Forms.serializeForm = function ($form) {
    // disable all fields that we don't want to submit (marked with YConfigs.Forms.CssFormNoSubmit)
    var $disabledFields = $('.' + YConfigs.Forms.CssFormNoSubmit, $form).not(':disabled');
    $disabledFields.attr('disabled', 'disabled');
    // disable all input fields in containers (usually grids) - we don't want to submit them - they're collected separately
    var $disabledGridFields = $('.{0} input,.{0} select'.format(YConfigs.Forms.CssFormNoSubmitContents), $form).not(':disabled');
    $disabledGridFields.attr('disabled', 'disabled');
    // serialize the form
    var formData = $form.serialize();
    // and enable all the input fields we just disabled
    $disabledFields.removeAttr('disabled');
    $disabledGridFields.removeAttr('disabled');
    return formData;
}

YetaWF_Forms.DATACLASS = 'yetawf_forms_data'; // add divs with this class to form for any data that needs to be submitted (will be removed before calling (pre)submit handlers

YetaWF_Forms.submit = function ($form, useValidation, extraData, successFunc, failFunc) {

    $('div.' + YetaWF_Forms.DATACLASS).remove();

    var form = $form.get(0);

    var onSubmitExtraData = extraData == undefined ? "" : extraData;
    onSubmitExtraData = _YetaWF_Forms.callPreSubmitHandler($form, onSubmitExtraData);

    if (useValidation)
        $form.validate();

    Y_Loading(true);

    if (!useValidation || $form.valid()) {

        // serialize the form
        var formData = YetaWF_Forms.serializeForm($form);
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
            var newOrigin = { Url: currUri.toString(), EditMode: YVolatile.Basics.EditModeActive != 0, InPopup: Y_InPopup() };
            originList.push(newOrigin);
            if (originList.length > 5)// only keep the last 5 urls
                originList = originList.slice(originList.length - 5);
        }
        // include the character dimension info
        {
            var width, height;
            var $mod = YetaWF_Basics.getModuleFromTag(form);
            width = $mod.attr('data-charwidthavg');
            height = $mod.attr('data-charheight');
            formData = formData + "&" + YGlobals.Link_CharInfo + "=" + width.toString() + ',' + height.toString();
        }

        formData = formData + "&" + YGlobals.Link_OriginList + "=" + encodeURIComponent(JSON.stringify(originList));
        // add the status of the Pagecontrol
        if (YVolatile.Basics.PageControlVisible)
            formData = formData + "&" + YGlobals.Link_PageControl + "=y";
        // add if we're in a popup
        if (Y_InPopup())
            formData = formData + "&" + YGlobals.Link_InPopup + "=y";

        $.ajax({
            url: form.action,
            type: form.method,
            data: formData,
            success: function (result, textStatus, jqXHR) {
                Y_Loading(false);
                YetaWF_Basics.processAjaxReturn(result, textStatus, jqXHR, $form, undefined, function () {
                    _YetaWF_Forms.YPreSubmitHandler1 = [];
                    $('.' + YConfigs.Forms.CssFormPartial, $form).replaceWith(result);
                });
                _YetaWF_Forms.callPostSubmitHandler($form);
                _YetaWF_Forms.YPostSubmitHandler1 = [];
                if (successFunc) // executed on successful ajax submit
                    successFunc(_YetaWF_Forms.hasErrors($form));
            },
            error: function (jqXHR, textStatus, errorThrown) {
                Y_Loading(false);
                Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
                if (failFunc)
                    failFunc();
            },
        });
    } else {
        Y_Loading(false);
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
                var panel = $ttabpanel.attr('data-tab');
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
        var hasErrors = _YetaWF_Forms.hasErrors($form);
        if (hasErrors)
            _YetaWF_Forms.showErrors($form);
        // call callback (if any)
        if (successFunc)
            successFunc(_YetaWF_Forms.hasErrors($form));
    }
    $('div.' + YetaWF_Forms.DATACLASS).remove();
    return false;
};

YetaWF_Forms.submitTemplate = function (obj, useValidation, templateName, templateAction, templateExtraData) {
    var qs = "{0}={1}&{2}=y".format(YConfigs.Basics.TemplateName, templateName, YGlobals.Link_SubmitIsApply);
    if (templateAction != undefined)
        qs += "&{0}={1}".format(YConfigs.Basics.TemplateAction, encodeURIComponent(templateAction));
    if (templateExtraData != undefined)
        qs += "&{0}={1}".format(YConfigs.Basics.TemplateExtraData, encodeURIComponent(templateExtraData));
    YetaWF_Forms.submit(YetaWF_Forms.getForm($(obj)), true, qs);
};

YetaWF_Forms.getForm = function (obj) {
    var $form = $(obj).closest('form');
    if ($form.length == 0) throw "Can't locate enclosing form";/*DEBUG*/
    return $form;
};

// get RequestVerificationToken, UniqueIdPrefix and ModuleGuid (usually for ajax requests)
YetaWF_Forms.getFormInfo = function (obj) {
    var $form = YetaWF_Forms.getForm(obj);
    var info = {};
    var s = $('input[name="' + YConfigs.Forms.RequestVerificationToken + '"]', $form).val();
    if (s == undefined || s.length == 0) throw "Can't locate " + YConfigs.Forms.RequestVerificationToken;/*DEBUG*/
    info.RequestVerificationToken = s;
    s = $('input[name="' + YConfigs.Forms.UniqueIdPrefix + '"]', $form).val();
    if (s == undefined || s.length == 0) throw "Can't locate " + YConfigs.Forms.UniqueIdPrefix;/*DEBUG*/
    info.UniqueIdPrefix = s;
    s = $('input[name="' + YConfigs.Basics.ModuleGuid + '"]', $form).val();
    if (s == undefined || s.length == 0) throw "Can't locate " + YConfigs.Basics.ModuleGuid;/*DEBUG*/
    info.ModuleGuid = s;
    info.QS = "&" + YConfigs.Forms.RequestVerificationToken + "=" + encodeURIComponent(info.RequestVerificationToken) +
              "&" + YConfigs.Forms.UniqueIdPrefix + "=" + encodeURIComponent(info.UniqueIdPrefix) +
              "&" + YConfigs.Basics.ModuleGuid + "=" + encodeURIComponent(info.ModuleGuid);
    return info;
};

YetaWF_Forms.updateValidation = function ($div) {
    // re-validate all fields within the div, typically used after paging in a grid
    // to let jquery.validate update all fields
    $.validator.unobtrusive.parse($div);
    $('input,select,textarea', $div).has("[data-val=true]").trigger('focusout');
};
YetaWF_Forms.validateElement = function ($ctrl) {
    var $form = YetaWF_Forms.getForm($ctrl);
    $form.validate().element($ctrl);
}

_YetaWF_Forms.showErrors = function ($form) {
    var $summary = _YetaWF_Forms.formErrorSummary($form);
    var $list = $('ul li', $summary);

    // only show unique messages (no duplicates)
    var list = [];
    $list.each(function () {
        list.push($(this).text());
    });
    var uniqueMsgs = [];
    $.each(list, function (i, el) {
        if ($.inArray(el, uniqueMsgs) === -1) uniqueMsgs.push(el);
    });

    // build output
    var s = "";
    $.each(uniqueMsgs, function (i, el) {
        s += el + '(+nl)';
    });
    _YetaWF_Forms.dontUpdateWarningIcons = true;
    Y_Error(YLocs.Forms.FormErrors + s);
    _YetaWF_Forms.dontUpdateWarningIcons = false;
};

$(document).ready(function () {

    // cancel the form
    $('form').on('click', '.' + YConfigs.Forms.CssFormCancel, function (e) {

        if (Y_InPopup()) {
            // we're in a popup, just close it
            Y_ClosePopup();
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
                window.location.assign(uri);
            } else {
                // we don't know where to return so just close the browser
                window.close();
            }
        }
    });

    $('form').on('click', 'input[type="button"][{0}]'.format(YConfigs.Forms.CssDataApplyButton), function (e) {
        e.preventDefault();
        var $form = YetaWF_Forms.getForm(this);
        YetaWF_Forms.submit($form, true, YGlobals.Link_SubmitIsApply + "=y");
    });

    // submit the form
    $('form.' + YConfigs.Forms.CssFormAjax).submit(function (e) {
        var $form = $(this);
        e.preventDefault();
        YetaWF_Forms.submit($form, true);
    });
});

// when we display a popup for the error summary, the focus loss causes validation to occur. We suppress updating icons if dontUpdateWarningIcons == true
_YetaWF_Forms.dontUpdateWarningIcons = false;

$(document).ready(function () {

    // running this overrides some jQuery Validate stuff so we can hook into its validations.
    // triggerElementValidationsOnFormValidation is optional and will fire off all of your
    // element validations WHEN the form validation runs ... it requires jquery.validate.unobtrusive
    $('form').addTriggersToJqueryValidate().triggerElementValidationsOnFormValidation();

    // You can bind to events that the forms/elements trigger on validation
    //$('form').bind('formValidation', function (event, element, result) {
    //    console.log(['validation ran for form:', element, 'and the result was:', result]);
    //});

    //// Or you can use the helper functions that we created for binding to these events
    //$('form').formValidation(function (element, result) {
    //    console.log(['validation ran for form:', element, 'and the result was:', result]);
    //});

    //$('input.something').elementValidation(function (element, result) {
    //    console.log(['validation ran for element:', element, 'and the result was:', result]);
    //});

    //$('input#address').elementValidationSuccess(function (element) {
    //    console.log(['validations just ran for this element and it was valid!', element]);
    //});

    $('body').on('elementValidationError', function (element) {
        if (_YetaWF_Forms.dontUpdateWarningIcons) return;
        var $input = $(element.target);
        var $form = YetaWF_Forms.getForm($input);
        var name = $input.attr("name");
        // remove the error icon
        var $err = $('img.{0}[name="{1}"]'.format(YConfigs.Forms.CssWarningIcon, name), $form);
        $err.remove();
        // find the validation message
        var $val = $('span.field-validation-error[data-valmsg-for="{0}"]'.format(name), $form);// get the validation message (which follows the input field but is hidden via CSS)
        // some templates incorrectly add  @Html.ValidationMessageFor(m => Model) to the rendered template - THIS IS WRONG
        // rather than going back and testing each template, we'll just use the first validation error for the field we find.
        if ($val.length < 1) throw "Validation message not found";/*DEBUG*/
        // insert a new error icon
        $val.eq(0).before('<img src="{0}" name="{1}" class="{2}" {3}="{4}"/>'.format(Y_HtmlEscape(YConfigs.Forms.CssWarningIconUrl), name, YConfigs.Forms.CssWarningIcon, YConfigs.Basics.CssTooltip, Y_HtmlEscape($val.text())));
    });
    $('body').on('elementValidationSuccess', function (element) {
        if (_YetaWF_Forms.dontUpdateWarningIcons) return;
        var $input = $(element.target);
        var $form = YetaWF_Forms.getForm($input);
        var name = $input.attr("name");
        // remove the error icon
        var $err = $('img.{0}[name="{1}"]'.format(YConfigs.Forms.CssWarningIcon, name), $form);
        $err.remove();
    });

    var $forms = $('form').filter('.yValidateImmediately');
    if ($forms.length > 0) {
        $forms.each(function () {
            $(this).validate();
            $(this).valid(); // force all fields to show valid/not valid
        });
    }
});
