/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Date = {};
var _YetaWF_Date = {};

YetaWF_Date.init = function (ctrlId) {
    'use strict';
    var $ctrl = $('#' + ctrlId);
    var $hidden = _YetaWF_Date.getHidden($ctrl);

    function setHidden($this, dateVal) {
        var s = "";
        if (dateVal != null) {
            var utcDate = new Date(Date.UTC(dateVal.getFullYear(), dateVal.getMonth(), dateVal.getDate(), 0, 0, 0));
            s = utcDate.toUTCString()
        }
        $hidden.val(s)
    }

    var $dt = $('input[name="dtpicker"]', $ctrl);
    var sd = new Date(1900, 1 - 1, 1);
    var ed = new Date(2199, 12 - 1, 31);
    if ($dt.attr('data-min-y') != undefined) {
        sd = new Date($dt.attr('data-min-y'), $dt.attr('data-min-m') - 1, $dt.attr('data-min-d'));
    }
    if ($dt.attr('data-max-y') != undefined) {
        ed = new Date($dt.attr('data-max-y'), $dt.attr('data-max-m') - 1, $dt.attr('data-max-d'));
    }
    $dt.kendoDatePicker({
        animation: false,
        format: YVolatile.Date.DateFormat,
        min: sd, max: ed,
        culture: YConfigs.Basics.Language,
        change: function () {
            setHidden($dt, this.value(), true)
        },
    });
    var kdPicker = $dt.data("kendoDatePicker");
    setHidden($dt, kdPicker.value(), false);
};

YetaWF_Date.renderjqGridFilter = function ($jqElem, $dtpick) {
    'use strict';
    // init date picker
    $dtpick.kendoDatePicker({
        animation: false,
        format: YVolatile.Date.DateFormat,
        //sb.Append("min: sd, max: ed,");
        culture: YConfigs.Basics.Language,
        change: function () {
            var s = ''
            var dateVal = this.value()
            if (dateVal != null) {
                var utcDate = new Date(Date.UTC(dateVal.getFullYear(), dateVal.getMonth(), dateVal.getDate(), 0, 0, 0));
                s = utcDate.toUTCString()
            }
            $jqElem.val(s)
        },
    });
};

// get the hidden field storing the date
_YetaWF_Date.getHidden = function ($control) {
    'use strict';
    var $hidden = $('input[type="hidden"]', $control);
    if ($hidden.length != 1) throw "couldn't find hidden field";/*DEBUG*/
    return $hidden;
};

$(document).ready(function () {
    $('body').on('focusout', '.yt_date.t_edit input[name="dtpicker"]', function () {
        var $ctrl = $(this).closest('.yt_date.t_edit');
        if ($ctrl.length != 1) throw "couldn't find control";/*DEBUG*/
        var $hidden = _YetaWF_Date.getHidden($ctrl);
        if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($hidden);
    });
});

