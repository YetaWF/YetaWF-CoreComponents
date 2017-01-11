/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_IntValue = {};

YetaWF_IntValue.init = function ($partialForm) {
    'use strict';
    $('input.yt_intvalue.t_edit,input.yt_intvalue2.t_edit,input.yt_intvalue4.t_edit,input.yt_intvalue6.t_edit', $partialForm).each(function (index) {
        var $this = $(this);
        var ed = $this.attr('data-max')
        if (ed === undefined)
            ed = Number.MAX_SAFE_INTEGER;
        var sd = $this.attr('data-min');
        if (sd === undefined)
            sd = 0;
        var noentry = $this.attr('data-noentry');
        if (noentry === undefined)
            noentry = '';
        var stp = $this.attr('data-step');
        if (stp === undefined)
            stp = '';
        $this.kendoNumericTextBox({
            decimals: 0, format: 'n0',
            min: sd, max: ed,
            placeholder: noentry,
            step:stp,
            downArrowText: "",
            upArrowText: "",
        });
    });
};

$(document).ready(function () {
    'use strict';
    YetaWF_IntValue.init($('body'));
    if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {
        YetaWF_Forms.partialFormActionsAll.push({
            callback: YetaWF_IntValue.init
        });
    }
});

