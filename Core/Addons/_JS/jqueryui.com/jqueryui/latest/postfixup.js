/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

+function ($) {
    'use strict';
    // restore functions that may have been overwritten
    $.fn.button = YetaWF_Core_jqueryui.button;
    $.fn.tooltip = YetaWF_Core_jqueryui.tooltip;

    if (YVolatile.Skin.Bootstrap && YVolatile.Skin.BootstrapButtons) {
        if (Y_YetaWFBootstrap_Fixup != true)
            throw "Bootstrap fixup not used"
        // we're highjacking the button() function implemented by JQUERY so we can support button('enable'/'disable'); to be source compatible with
        // code assuming jquery-ui buttons even if Bootstrap buttons are used
        var jqbutton = $.fn.button
        $.fn.button = function (arg1, arg2, arg3) {
            if (this.hasClass('btn')) {
                // it's a Bootstrap button
                if (arg1 == 'enable') {
                    return this.removeAttr('disabled')
                } else if (arg1 == 'disable') {
                    return this.attr('disabled', 'disabled')
                } else
                    throw "For bootstrap buttons the function bootstrapButton() must be used instead of button()"
            } else {
                // otherwise it's a call to jquery
                return jqbutton.call(this, arg1, arg2, arg3)
            }
        }
    }
}(jQuery);
