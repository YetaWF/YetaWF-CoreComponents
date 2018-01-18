/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_TimeSpan = {};

YetaWF_TimeSpan.init = function (divId) {
    var $control = $('#' + divId);
    if ($control.length != 1) throw "div not found";/*DEBUG*/

    var $hidden = $('input[type="hidden"]', $control);
    if ($hidden.length != 1) throw "hidden field not found";/*DEBUG*/

    // update the hidden field with the new timespan value ddd.hh:mm:ss.mm
    function updateValue() {
        // we can assume that the input fields are numeric (as long as we're using the IntValue templates)
        // so we don't need to do any validation here
        var s = $('input[name$="Days"]', $control).val() + "." +
                $('input[name$="Hours"]', $control).val() + ":" +
                $('input[name$="Minutes"]', $control).val() + ":" +
                $('input[name$="Seconds"]', $control).val();
        $hidden.val(s);
    };

    $('input', $control).on('change', function () {
        updateValue();
    });
};

