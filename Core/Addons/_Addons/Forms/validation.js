﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

// validation for all forms

// REQUIRED
// REQUIRED
// REQUIRED

$.validator.addMethod('customrequired', function (value, element, parameters) {
    if (value == undefined || value == null || value.trim().length == 0) return false;
    return true;
});

$.validator.unobtrusive.adapters.add('customrequired', [YConfigs.Forms.ConditionPropertyName, YConfigs.Forms.ConditionPropertyValue], function (options) {
    options.rules['customrequired'] = { };
    options.messages['customrequired'] = options.message;
});

// SELECTIONREQUIRED
// SELECTIONREQUIRED
// SELECTIONREQUIRED

$.validator.addMethod('selectionrequired', function (value, element, parameters) {
    if (value == undefined || value == null || value.trim().length == 0 || value.trim() == "0") return false;
    return true;
});

$.validator.unobtrusive.adapters.add('selectionrequired', [YConfigs.Forms.ConditionPropertyName, YConfigs.Forms.ConditionPropertyValue], function (options) {
    options.rules['selectionrequired'] = {};
    options.messages['selectionrequired'] = options.message;
});

// REQUIREDIF
// REQUIREDIF
// REQUIREDIF

$.validator.addMethod('requiredif', function (value, element, parameters) {

    // get the target value (as a string)
    var conditionvalue = parameters['targetvalue'];
    conditionvalue = (conditionvalue == null ? '' : conditionvalue).toString();

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $(':input[name="' + name + '"]', $form);
    if ($ctrl.length < 1) throw "No control found for name " + name;/*DEBUG*/
    var ctrl = $ctrl[0];
    var tag = ctrl.tagName;
    var controltype = $ctrl.attr('type');

    if ($ctrl.length >= 2) {
        // for checkboxes there can be two controls by the same name (the second is a hidden control)
        if (tag != "INPUT" || controltype !== 'checkbox') throw "Multiple controls found for name " + name;/*DEBUG*/
    }

    // control types, e.g. radios
    var actualvalue;
    if (tag == "INPUT") {
        // regular input control
        if (controltype === 'checkbox') {
            // checkbox
            actualvalue = $ctrl.prop('checked').toString().toLowerCase();;
            conditionvalue = conditionvalue.toLowerCase();
        } else {
            // other
            actualvalue = $ctrl.val();
        }
    } else if (tag == "SELECT") {
        actualvalue = $ctrl.val();
    } else {
        throw "Unsupported tag " + tag;/*DEBUG*/
    }

    // if the condition is true, reuse the existing 
    // required field validator functionality
    if (conditionvalue === actualvalue)
        return $.validator.methods.required.call(this, value, element, parameters);
    return true;
});

$.validator.unobtrusive.adapters.add('requiredif', [YConfigs.Forms.ConditionPropertyName, YConfigs.Forms.ConditionPropertyValue], function (options) {
    options.rules['requiredif'] = {
        dependentproperty: options.params[YConfigs.Forms.ConditionPropertyName],
        targetvalue: options.params[YConfigs.Forms.ConditionPropertyValue]
    };
    options.messages['requiredif'] = options.message;
});

// REQUIREDIFNOT
// REQUIREDIFNOT
// REQUIREDIFNOT

$.validator.addMethod('requiredifnot', function (value, element, parameters) {

    // get the target value (as a string, 
    // as that's what the actual value will be)
    var conditionvalue = parameters['targetvalue'];
    conditionvalue = (conditionvalue == null ? '' : conditionvalue).toString();

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $(':input[name="' + name + '"]', $form);
    if ($ctrl.length > 1) throw "Multiple controls found for name " + name;/*DEBUG*/
    if ($ctrl.length < 1) throw "No control found for name " + name;/*DEBUG*/
    var ctrl = $ctrl[0];

    // control types, e.g. radios
    var actualvalue;
    if (ctrl.tagName == "INPUT") {
        // regular input control
        var controltype = $ctrl.attr('type');
        if (controltype === 'checkbox') {
            // checkbox
            actualvalue = $ctrl.prop('checked').toString().toLowerCase();;
            conditionvalue = conditionvalue.toLowerCase();
        } else {
            // other
            actualvalue = $ctrl.val();
        }
    } else if (ctrl.tagName == "SELECT") {
        actualvalue = $ctrl.val();
    } else {
        throw "Unsupported tag " + ctrl.tagName;/*DEBUG*/
    }
    // if the condition is false, reuse the existing 
    // required field validator functionality
    if (conditionvalue !== actualvalue)
        return $.validator.methods.required.call(this, value, element, parameters);
    return true;
});

$.validator.unobtrusive.adapters.add('requiredifnot', [YConfigs.Forms.ConditionPropertyName, YConfigs.Forms.ConditionPropertyValue], function (options) {
    options.rules['requiredifnot'] = {
        dependentproperty: options.params[YConfigs.Forms.ConditionPropertyName],
        targetvalue: options.params[YConfigs.Forms.ConditionPropertyValue]
    };
    options.messages['requiredifnot'] = options.message;
});

// REQUIREDIFINRANGE
// REQUIREDIFINRANGE
// REQUIREDIFINRANGE

$.validator.addMethod('requiredifinrange', function (value, element, parameters) {

    // get the target value (as a int as that's what the actual value will be)
    var conditionvaluelow = parseInt(parameters['targetvaluelow'], 10);
    var conditionvaluehigh = parseInt(parameters['targetvaluehigh'], 10);

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $(':input[name="' + name + '"]', $form);
    if ($ctrl.length > 1) throw "Multiple controls found for name " + name;/*DEBUG*/
    if ($ctrl.length < 1) throw "No control found for name " + name;/*DEBUG*/
    var ctrl = $ctrl[0];

    // control types, e.g. radios
    var actualvalue;
    if (ctrl.tagName == "INPUT") {
        actualvalue = $ctrl.val();
    } else if (ctrl.tagName == "SELECT") {
        actualvalue = $ctrl.val();
    } else {
        throw "Unsupported tag " + ctrl.tagName;/*DEBUG*/
    }
    // if the condition is false, reuse the existing 
    // required field validator functionality
    if (actualvalue >= conditionvaluelow && actualvalue <= conditionvaluehigh)
        return $.validator.methods.required.call(this, value, element, parameters);
    return true;
});

$.validator.unobtrusive.adapters.add('requiredifinrange', [YConfigs.Forms.ConditionPropertyName, YConfigs.Forms.ConditionPropertyValueLow, YConfigs.Forms.ConditionPropertyValueHigh], function (options) {
    options.rules['requiredifinrange'] = {
        dependentproperty: options.params[YConfigs.Forms.ConditionPropertyName],
        targetvaluelow: options.params[YConfigs.Forms.ConditionPropertyValueLow],
        targetvaluehigh: options.params[YConfigs.Forms.ConditionPropertyValueHigh]
    };
    options.messages['requiredifinrange'] = options.message;
});

// REQUIREDIFSUPPLIED
// REQUIREDIFSUPPLIED
// REQUIREDIFSUPPLIED

$.validator.addMethod('requiredifsupplied', function (value, element, parameters) {

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $(':input[name="' + name + '"]', $form);
    if ($ctrl.length > 1) throw "Multiple controls found for name " + name;/*DEBUG*/
    if ($ctrl.length < 1) throw "No control found for name " + name;/*DEBUG*/
    var ctrl = $ctrl[0];

    var actualValue = "";
    // control types, e.g. radios
    if (ctrl.tagName == "INPUT") {
        // regular input control
        actualValue = $ctrl.val().trim();
    } else {
        throw "Unsupported tag " + ctrl.tagName;/*DEBUG*/
    }
    // if the dependent property is supplied, reuse the existing 
    // required field validator functionality
    if (actualValue != undefined && actualValue != "")
        return $.validator.methods.required.call(this, value, element, parameters);
    return true;
});

$.validator.unobtrusive.adapters.add('requiredifsupplied', [YConfigs.Forms.ConditionPropertyName], function (options) {
    options.rules['requiredifsupplied'] = {
        dependentproperty: options.params[YConfigs.Forms.ConditionPropertyName]
    };
    options.messages['requiredifsupplied'] = options.message;
});

// SAMEAS
// SAMEAS
// SAMEAS

$.validator.addMethod('sameas', function (value, element, parameters) {

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $(':input[name="' + name + '"]', $form);
    if ($ctrl.length > 1) throw "Multiple controls found for name " + name;/*DEBUG*/
    if ($ctrl.length < 1) throw "No control found for name " + name;/*DEBUG*/
    var ctrl = $ctrl[0];

    // control types, e.g. radios
    var actualvalue;
    if (ctrl.tagName == "INPUT") {
        actualvalue = $ctrl.val();
    } else {
        throw "Unsupported tag " + ctrl.tagName;/*DEBUG*/
    }
    // if the condition is true, reuse the existing 
    // required field validator functionality
    if (value === actualvalue)
        return $.validator.methods.required.call(this, value, element, parameters);
    return false;
});

$.validator.unobtrusive.adapters.add('sameas', [YConfigs.Forms.ConditionPropertyName], function (options) {
    options.rules['sameas'] = {
        dependentproperty: options.params[YConfigs.Forms.ConditionPropertyName],
    };
    options.messages['sameas'] = options.message;
});

