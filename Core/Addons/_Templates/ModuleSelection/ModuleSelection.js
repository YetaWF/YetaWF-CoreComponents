/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_ModuleSelection = {};

YetaWF_ModuleSelection.init = function (id) {
    var $control = $('#' + id);
    if ($control.length != 1) throw "Can't find control";/*DEBUG*/
    var $select = $('.t_select select', $control);
    if ($select.length != 1) throw "Can't find hidden module selection dropdown";/*DEBUG*/
    var $link = $('.t_link a', $control);
    if ($link.length != 1) throw "Can't find link";/*DEBUG*/
    var $desc = $('.t_description', $control);
    if ($desc.length != 1) throw "Can't find description";/*DEBUG*/

    function updateLink(mod, desc) {
        if (mod == undefined || mod == "" || mod == "00000000-0000-0000-0000-000000000000") {
            desc = "";
            $('.t_link', $control).hide();
            $desc.hide();
        } else {
            $('.t_link', $control).show();
            $desc.show();
        }
        $link.attr("href", YGlobals.ModuleUrl + mod);
        $desc.text(desc);
    }

    $('.t_select select', $control).on('change', function () {
        var $this = $(this);
        var val = $this.val();
        var desc = $('.t_select select option', $control).filter(":selected").attr('title');
        YetaWF_Forms.hideError(YetaWF_Forms.getForm($this), $this.attr("name")); // TODO: This could be removed when RequiredAttribute supports Guid  client-side
        updateLink(val, desc);
    });

    $('.t_select select', $control).trigger('change');
};

