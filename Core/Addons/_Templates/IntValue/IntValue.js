/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_IntValue = {};

YetaWF_IntValue.init = function ($tag) {
    'use strict';
    $('input.yt_intvalue.t_edit,input.yt_intvalue2.t_edit,input.yt_intvalue4.t_edit,input.yt_intvalue6.t_edit', $tag).each(function (index) {
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

YetaWF_Basics.whenReady.push({
    callback: YetaWF_IntValue.init
});

// A <div> is being emptied. Destroy all kendoNumericTextBox the <div> may contain.
YetaWF_Basics.addClearDiv(function (tag) {
    var list = tag.querySelectorAll("input.yt_intvalue.t_edit,input.yt_intvalue2.t_edit,input.yt_intvalue4.t_edit,input.yt_intvalue6.t_edit");
    var len = list.length;
    for (var i = 0; i < len; ++i) {
        var el = list[i];
        var numTextBox = $(el).data("kendoNumericTextBox");
        if (numTextBox) numTextBox.destroy();
    }
});
