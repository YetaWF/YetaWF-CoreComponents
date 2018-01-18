/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

/* Modules */

// highlight on module entry/exit (in admin mode)
$(document).on('mouseenter', '.yModule', function (event) {

    //console.log("Entering module");

    var $t = $(this);
    // check if this module is already current
    if ($t.hasClass('yModule-current'))
        return;

    // find the module's edit icon
    var $edit = $('.yModuleMenuEditIcon', $t);
    if ($edit.length == 0) return; /// it just doesn't have a menu

    // find the module's menu
    var $menu = $('.yModuleMenu', $t);
    if ($menu.length > 1) throw "too many module menus found";/*DEBUG*/
    if ($menu.length < 1) throw "module menu not found";/*DEBUG*/

    // if the edit icon is already visible, we're done
    if ($edit.is(':visible')) // edit/menu is still visible
        return;

    // entered a new module - clear all module menus that may be open
    YetaWF_Modules_ClearMenus(true);

    // add a class to the module to identify it's the current module
    $t.addClass('yModule-current');

    //if (YVolatile.Basics.EditModeActive) { }

    // fade in edit icon
    $edit.fadeIn(500);

    YetaWF_Core_MenuHandler = setInterval(function () { YetaWF_Modules_ClearMenus(false); }, 1500);
});
$(document).on('mouseleave', '.yModule', function (event) {
    //console.log("Exiting module");

    var $t = $(this);
    $t.removeClass('yModule-current');
});

// Show/hide menu as we're hovering over the edit icon
$(document).on('mouseenter', '.yModuleMenuEditIcon', function (event) {

    //console.log("Entering edit icon");

    var $t = $(this);
    // find the module's menu
    var $menu = $('.yModuleMenu', $t);
    if ($menu.length != 1) throw "menu not found";/*DEBUG*/

    $menu.show();
});

var YetaWF_Core_MenuHandler = 0;

function YetaWF_Modules_ClearMenus(force) {
    if (!force) {
        // if we still have a current module, we can't clear menus
        if ($('.yModule.yModule-current').length > 0)
            return;
    }

    // clear the interval
    if (YetaWF_Core_MenuHandler != 0)
        clearInterval(YetaWF_Core_MenuHandler);
    YetaWF_Core_MenuHandler = 0;

    var $edit = $('.yModuleMenuEditIcon');
    if ($edit.length == 0) return; //throw "menu not found"; // modules don't necessarily have an edit menu

    // hide all module menus
    $('.yModuleMenu').hide(); // hide all menus
    // there is no longer a current module
    $('.yModule').removeClass('yModule-current'); // Globals.Css_Module

    if (force) {
        $edit.hide();
    } else {
        $edit.fadeOut(200);
    }
}

