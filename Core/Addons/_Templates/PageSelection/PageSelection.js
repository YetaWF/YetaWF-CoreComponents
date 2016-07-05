/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_PageSelection = {};

YetaWF_PageSelection.init = function (id) {
    'use strict';
    var $control = $('#' + id);
    if ($control.length != 1) throw "Can't find control";/*DEBUG*/
    var $select = $('.t_select select', $control);
    if ($select.length != 1) throw "Can't find hidden page selection dropdown";/*DEBUG*/
    var $link = $('.t_link a', $control);
    if ($link.length != 1) throw "Can't find link";/*DEBUG*/

    function updateLink(page, desc) {
        if (page == undefined || page == "" || page == "00000000-0000-0000-0000-000000000000") {
            desc = "";
            $('.t_link', $control).hide();
            //$desc.hide();
        } else {
            $('.t_link', $control).show();
            //$desc.show();
        }
        $link.attr("href", '/!Page/' + page);  // Globals.PageUrl
        //$desc.text(desc);
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
