/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// validation for all forms

// SELECTIONREQUIRED
// SELECTIONREQUIRED
// SELECTIONREQUIRED

$.validator.addMethod('selectionrequired', function (value, element, parameters) {
    if ($(element).hasClass('yNoValidate')) return true;
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
    'use strict';

    if ($(element).hasClass('yNoValidate')) return true;

    // get the target value (as a string)
    var conditionvalue = parameters['targetvalue'];
    conditionvalue = (conditionvalue == null ? '' : conditionvalue).toString();

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $('input[name="{0}"],select[name="{0}"]'.format(name), $form);
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
    if (conditionvalue.toLowerCase() === actualvalue.toLowerCase())
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
    'use strict';

    if ($(element).hasClass('yNoValidate')) return true;

    // get the target value (as a string,
    // as that's what the actual value will be)
    var conditionvalue = parameters['targetvalue'];
    conditionvalue = (conditionvalue == null ? '' : conditionvalue).toString();

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $('input[name="{0}"],select[name="{0}"]'.format(name), $form);
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
    if (conditionvalue.toLowerCase() !== actualvalue.toLowerCase())
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
    'use strict';

    if ($(element).hasClass('yNoValidate')) return true;

    // get the target value (as a int as that's what the actual value will be)
    var conditionvaluelow = parseInt(parameters['targetvaluelow'], 10);
    var conditionvaluehigh = parseInt(parameters['targetvaluehigh'], 10);

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $('input[name="{0}"],select[name="{0}"]'.format(name), $form);
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
    'use strict';

    if ($(element).hasClass('yNoValidate')) return true;

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $('input[name="{0}"],select[name="{0}"]'.format(name), $form);
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
    'use strict';

    if ($(element).hasClass('yNoValidate')) return true;

    // Get value of the target control - we can't use its Id because it could be non-unique, not predictable
    // use the name attribute instead
    // first, find the enclosing form
    var $form = YetaWF_Forms.getForm(element);

    var name = parameters['dependentproperty'];

    var $ctrl = $('input[name="{0}"],select[name="{0}"]'.format(name), $form);
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

// LISTNODUPLICATES
// LISTNODUPLICATES
// LISTNODUPLICATES

$.validator.addMethod('listnoduplicates', function (value, element, parameters) {
    'use strict';

    var $element = $(element);
    if ($element.hasClass('yNoValidate')) return true;

    // Lists are always in a grid. Because field names can be duplicates (occurs due to add/delete) we can use the element name for comparisons
    // instead we locate the jqgrid record id
    // verify we're in a grid control
    var $grid = $element.closest('.yt_grid');/*DEBUG*/
    if ($grid.length != 1) throw "Can't find grid control";/*DEBUG*/

    function getRowId($element) {
        // get the closest row (it has the record id)
        var $row = $element.closest('tr');
        if ($row.length != 1) throw "Can't find grid row";/*DEBUG*/
        var id = $row.attr("id");// record id (0..n)
        if (id == undefined) throw "Can't find record id";/*DEBUG*/
        return id;
    }
    var index = getRowId($element);// get the index of the element we're checking

    // extract the field name (usually fieldname[x].__name)
    var re1 = new RegExp(new RegExp('([a-z0-9]+\\[)([0-9]+)(\\].*)', 'i'));
    var result = element.name.match(re1);
    if (result == null) return false;
    value = value.trim().toUpperCase(); // value to find

    // find all matching fields with indexed names
    var $set = $('input[name^="{0}"]'.format(result[1])).filter('[name$="{0}"]'.format(result[3]));

    for (var i = 0 ; i < $set.length ; ++i) {
        if (getRowId($set.eq(i)) != index) {
            if ($set.eq(i).val().trim().toUpperCase() == value)
                return false;// duplicate found
        }
    }
    return true;
});

$.validator.unobtrusive.adapters.add('listnoduplicates', [YConfigs.Forms.ConditionPropertyName, YConfigs.Forms.ConditionPropertyValue], function (options) {
    options.rules['listnoduplicates'] = {};
    options.messages['listnoduplicates'] = options.message;
});

