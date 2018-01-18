/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// This saves functions that are in conflict with other libs (notably Bootstrap)
// the matching posfixup.js must run last

var YetaWF_Core_jqueryui = {};

+function ($) {
    'use strict';
    YetaWF_Core_jqueryui.button = $.fn.button;
    YetaWF_Core_jqueryui.tooltip = $.fn.tooltip;
}(jQuery);