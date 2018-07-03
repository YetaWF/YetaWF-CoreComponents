/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

// We're saving some bootstrap functions that collide with jquery-ui

/* this is used to verify that the Bootstrap fixup is used */
var Y_YetaWFBootstrap_Fixup = true;

// bootstrap and jquery both use button() so we rename bootstrap's to bootstrapButton
$.fn.bootstrapButton = $.fn.button     // save the bootstrap function

// bootstrap and jquery both use tooltip() so we rename bootstrap's to bootstrapTooltip
$.fn.bootstrapTooltip = $.fn.tooltip    // save the bootstrap function
