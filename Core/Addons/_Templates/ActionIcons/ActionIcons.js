/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_TemplateActionIcons = {};
var _YetaWF_TemplateActionIcons = {};

_YetaWF_TemplateActionIcons.menusOpen = 0;

_YetaWF_TemplateActionIcons.openMenu = function ($idButton, $idMenu) {
    _YetaWF_TemplateActionIcons.closeMenus();
    $idMenu.appendTo($('body'));
    $idMenu.show();
    ++_YetaWF_TemplateActionIcons.menusOpen;
    $idMenu.position({
        my: "left top",
        at: "left bottom",
        of: $idButton,
        collision: "flip"
    })
};
_YetaWF_TemplateActionIcons.closeMenus = function () {
    // find all action menus after grid (there really should only be one)
    if (_YetaWF_TemplateActionIcons.menusOpen == 0) return;
    _YetaWF_TemplateActionIcons.menusOpen = 0;
    var $menus = $('.yGridActionMenu');
    if ($menus.length == 0) return;
    $menus.each(function () {
        var $menu = $(this);
        $menu.hide();
        var idButton = $menu.attr('id').replace('_menu', '_btn');
        var $idButton = $('#' + idButton);
        if ($idButton.length > 0) // there can be a case without button if we switched to a new page
            $menu.appendTo($idButton.parent());
    });
};

YetaWF_TemplateActionIcons.initMenu = function (id, $idButton, $idMenu) {
    $idButton.kendoButton().on('click', function (ev) {
        var vis = $idMenu.is(':visible');
        _YetaWF_TemplateActionIcons.closeMenus();
        if (!vis)
            _YetaWF_TemplateActionIcons.openMenu($idButton, $idMenu);
        ev.preventDefault();
        return false;
    });
    $idMenu.kendoMenu({
        orientation: "vertical"
    }).hide();
};
// Handle clicks elsewhere so we can close the menus
$(document).on('click mousedown', function (ev) {
    if (ev.which != 1) return;
    if (_YetaWF_TemplateActionIcons.menusOpen > 0) {
        // delay closing to handle the event
        setTimeout(function () {
            _YetaWF_TemplateActionIcons.closeMenus();
        }, 300);
    }
});
// Handle Escape key to close any open menus
$(document).on('keydown', function (ev) {
    if (ev.which != 27) return;
    _YetaWF_TemplateActionIcons.closeMenus();
});
// last chance - handle a new page (UPS) and close open menus
$(document).on("YetaWF_Basics_NewPage", function (ev, url) {
    _YetaWF_TemplateActionIcons.closeMenus();
});
// A <div> is being emptied. Destroy all actionicons the <div> may contain.
YetaWF_Basics.addClearDiv(function (tag) {
    //var list = tag.querySelectorAll("button.yt_actionicons");
    //var len = list.length;
    //for (var i = 0; i < len; ++i) {
    //    var el = list[i];
    //    var button = $(el).data("kendoButton");
    //    if (!button) throw "No kendo object found";/*DEBUG*/
    //    button.destroy();
    //}
    var list = tag.querySelectorAll("ul.yGridActionMenu");
    var len = list.length;
    for (var i = 0; i < len; ++i) {
        var el = list[i];
        var menu = $(el).data("kendoMenu");
        if (!menu) throw "No kendo object found";/*DEBUG*/
        menu.destroy();
    }
});
