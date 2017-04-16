/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_DateTime = {};

YetaWF_DateTime.init = function ($partialForm) {
    'use strict';

    function setHidden($this, dateVal) {
        var $tmplt = $this.closest('.yt_datetime.t_edit')
        if ($tmplt.length == 0) throw "Couldn't find containing template"/*DEBUG*/
        var $pk = $('input[name="dtpicker"]', $tmplt)
        if ($pk.length == 0) throw "Couldn't find date time picker"/*DEBUG*/

        var s = "";
        if (dateVal != null)
            s = dateVal.toUTCString()
        var $inp = $('input[type="hidden"]', $tmplt)
        if ($inp.length == 0) throw "Couldn't find hidden input field"/*DEBUG*/
        $inp.val(s)
    }

    $('.yt_datetime.t_edit input[name="dtpicker"]', $partialForm).each(function (index) {
        var sd = new Date(1900, 1-1, 1);
        var ed = new Date(2199, 12-1, 31);
        var $this = $(this);
        if ($this.attr('data-min-y') != undefined) {
            sd = new Date($this.attr('data-min-y'), $this.attr('data-min-m')-1, $this.attr('data-min-d'));
        }
        if ($this.attr('data-max-y') != undefined) {
            ed = new Date($this.attr('data-max-y'), $this.attr('data-max-m')-1, $this.attr('data-max-d'));
        }
        $this.kendoDateTimePicker({
            animation: false,
            format: YVolatile.DateTime.DateTimeFormat,
            min: sd, max: ed,
            culture: YConfigs.Basics.Language,
            change: function () {
                setHidden($this, this.value(), true)
            },
        });
        var kdPicker = $this.data("kendoDateTimePicker");
        setHidden($this, kdPicker.value(), false);
    });
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

$(document).ready(function () {
    'use strict';
    YetaWF_DateTime.init($('body'));
    if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {
        YetaWF_Forms.partialFormActionsAll.push({
            callback: YetaWF_DateTime.init
        });
    }
});

