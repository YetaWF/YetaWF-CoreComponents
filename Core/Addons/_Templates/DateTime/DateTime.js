/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_DateTime = {};
var _YetaWF_DateTime = {};

YetaWF_DateTime.init = function (ctrlId) {
    'use strict';
    var $ctrl = $('#' + ctrlId);
    var $hidden = _YetaWF_DateTime.getHidden($ctrl);

    var $dt = $('input[name="dtpicker"]', $ctrl);
    var sd = new Date(1900, 1-1, 1);
    var ed = new Date(2199, 12-1, 31);
    if ($dt.attr('data-min-y') != undefined) {
        sd = new Date($dt.attr('data-min-y'), $dt.attr('data-min-m') - 1, $dt.attr('data-min-d'));
    }
    if ($dt.attr('data-max-y') != undefined) {
        ed = new Date($dt.attr('data-max-y'), $dt.attr('data-max-m') - 1, $dt.attr('data-max-d'));
    }
    $dt.kendoDateTimePicker({
        animation: false,
        format: YVolatile.DateTime.DateTimeFormat,
        min: sd, max: ed,
        culture: YConfigs.Basics.Language,
        change: function () {
            _YetaWF_DateTime.setHidden($hidden, this.value());
            if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($hidden);
        },
    });
    var kdPicker = $dt.data("kendoDateTimePicker");
    _YetaWF_DateTime.setHidden($hidden, kdPicker.value());
};

YetaWF_DateTime.renderjqGridFilter = function ($jqElem, $dtpick) {
    'use strict';
    // init date picker
    $dtpick.kendoDateTimePicker({
        animation: false,
        format: YVolatile.DateTime.DateTimeFormat,
        //sb.Append("min: sd, max: ed,");
        culture: YConfigs.Basics.Language,
        change: function () {
            var s = ''
            var dateVal = this.value()
            if (dateVal != null)
                s = dateVal.toUTCString()
            $jqElem.val(s)
        },
    });
};

// get the hidden field storing the datetime
_YetaWF_DateTime.getHidden = function ($control) {
    'use strict';
    var $hidden = $('input[type="hidden"]', $control);
    if ($hidden.length != 1) throw "couldn't find hidden field";/*DEBUG*/
    return $hidden;
};
_YetaWF_DateTime.setHidden = function ($hidden, dateVal) {
    var s = "";
    try {
        s = dateVal.toUTCString()
    } catch(e) {
        s = dateVal;//even though it's invalid, update the hidden field for validation
    }
    $hidden.val(s)
}

$(document).ready(function () {
    $('body').on('change keyup', '.yt_datetime.t_edit input[name="dtpicker"]', function () {
        var $ctrl = $(this).closest('.yt_datetime.t_edit');
        if ($ctrl.length != 1) throw "couldn't find control";/*DEBUG*/
        var kdPicker = $(this).data("kendoDateTimePicker");
        var val = kdPicker.value();
        if (val == null) // if the datetime picker has an invalid value, still propagate the actual value entered to hidden control for validation
            val = $(this).val();
        var $hidden = _YetaWF_DateTime.getHidden($ctrl);
        _YetaWF_DateTime.setHidden($hidden, val);
        if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($hidden);
    });
});