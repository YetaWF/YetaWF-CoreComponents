/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Date = {};

YetaWF_Date.init = function ($partialForm) {
    'use strict';

    function setHidden($this, dateVal) {
        var $tmplt = $this.closest('.yt_date.t_edit')
        if ($tmplt.length == 0) throw "Couldn't find containing template"/*DEBUG*/
        var $pk = $('input[name="dtpicker"]', $tmplt)
        if ($pk.length == 0) throw "Couldn't find date time picker"/*DEBUG*/

        var s = "";
        if (dateVal != null) {
            var utcDate = new Date(Date.UTC(dateVal.getFullYear(), dateVal.getMonth(), dateVal.getDate(), 0, 0, 0));
            s = utcDate.toUTCString()
        }
        var $inp = $('input[type="hidden"]', $tmplt)
        if ($inp.length == 0) throw "Couldn't find hidden input field"/*DEBUG*/
        $inp.val(s)
    }

    $('.yt_date.t_edit input[name="dtpicker"]', $partialForm).each(function (index) {
        var sd = new Date(1900, 1 - 1, 1);
        var ed = new Date(2199, 12 - 1, 31);
        var $this = $(this);
        if ($this.attr('data-min-y') != undefined) {
            sd = new Date($this.attr('data-min-y'), $this.attr('data-min-m') - 1, $this.attr('data-min-d'));
        }
        if ($this.attr('data-max-y') != undefined) {
            ed = new Date($this.attr('data-max-y'), $this.attr('data-max-m') - 1, $this.attr('data-max-d'));
        }
        $this.kendoDatePicker({
            animation: false,
            format: YVolatile.Date.DateFormat,
            min: sd, max: ed,
            culture: YConfigs.Basics.Language,
            change: function () {
                setHidden($this, this.value(), true)
            },
        });
        var kdPicker = $this.data("kendoDatePicker");
        setHidden($this, kdPicker.value(), false);
    });
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

$(document).ready(function () {
    'use strict';
    YetaWF_Date.init($('body'));
    if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {
        YetaWF_Forms.partialFormActionsAll.push({
            callback: YetaWF_Date.init
        });
    }
});

