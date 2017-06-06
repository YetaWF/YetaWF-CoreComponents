/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Currency = {};

YetaWF_Currency.init = function ($tag) {
    'use strict';
    $('input.yt_currency.t_edit', $tag).each(function (index) {
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
            format: YVolatile.Currency.CurrencyFormat,
            min: sd, max: ed,
            culture: YConfigs.Basics.Language
        });
    });
};

YetaWF_Basics.whenReady.push({
    callback: YetaWF_Currency.init
});
