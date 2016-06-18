/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

// This assumes that bootstrap.js is loaded before jquery-ui
// bootstrap and jquery both use button() so we rename bootstrap's to bootstrapButton
$.fn.bootstrapButton = $.fn.button     // save the bootstrap function
$.fn.button = $.fn.button.noConflict();// return $.fn.button to previously assigned value

// bootstrap and jquery both use tooltip() so we rename bootstrap's to bootstrapTooltip
$.fn.bootstrapTooltip = $.fn.tooltip    // save the bootstrap function
$.fn.tooltip = $.fn.tooltip.noConflict() // return $.fn.tooltip to previously assigned value
