/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_DateTime = {};
var _YetaWF_DateTime = {};

YetaWF_DateTime.init = function (ctrlId) {
    'use strict';
    var $ctrl = $('#' + ctrlId);
    var $hidden = _YetaWF_DateTime.getHidden($ctrl);

    function setHidden($this, dateVal) {
        var s = "";
        if (dateVal != null)
            s = dateVal.toUTCString()
        $hidden.val(s)
    }

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
            setHidden($dt, this.value(), true);
            if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($hidden);
        },
    });
    var kdPicker = $dt.data("kendoDateTimePicker");
    setHidden($dt, kdPicker.value(), false);
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

$(document).ready(function () {
    $('body').on('focusout', '.yt_datetime.t_edit input[name="dtpicker"]', function () {
        var $ctrl = $(this).closest('.yt_datetime.t_edit');
        if ($ctrl.length != 1) throw "couldn't find control";/*DEBUG*/
        var $hidden = _YetaWF_DateTime.getHidden($ctrl);
        if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($hidden);
    });
});