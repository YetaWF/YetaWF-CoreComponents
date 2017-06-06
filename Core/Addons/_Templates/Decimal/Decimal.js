/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_Decimal = {};

YetaWF_Decimal.init = function ($tag) {
    $('input.yt_decimal.t_edit', $tag).each(function (index) {
        var sd = 0.0;
        var ed = 99999999.99;
        var $this = $(this);
        if ($this.attr('data-min') != undefined) {
            sd = $this.attr('data-min');
        }
        if ($this.attr('data-max') != undefined) {
            ed = $this.attr('data-max');
        }
        $this.kendoNumericTextBox({
            format: "0.00",
            min: sd, max: ed,
            culture: YConfigs.Basics.Language
        });
    });
};

YetaWF_Basics.whenReady.push({
    callback: YetaWF_Decimal.init
});
