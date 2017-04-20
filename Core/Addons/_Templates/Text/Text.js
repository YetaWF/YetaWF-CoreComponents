/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_TemplateText = {};
YetaWF_TemplateText.init = function ($partialForm) {
    'use strict';
    $('input.yt_text,input.yt_text10,input.yt_text20,input.yt_text40,input.yt_text80,input.yt_text_base', $partialForm).not('.ybrowsercontrols').each(function (index) {
        var $this = $(this);
        var autocomplete = $this.attr('autocomplete');// preserve autocomplete
        $this.kendoMaskedTextBox({ });
        $this.attr('autocomplete', autocomplete);
    });
};

// Enable a text object
// $control refers to the <div class="yt_text t_edit">
YetaWF_TemplateText.Enable = function ($control, enabled) {
    'use strict';
    if (enabled)
        $control.removeAttr("disabled");
    else
        $control.attr("disabled", "disabled");
};

$(document).ready(function () {
    'use strict';

    YetaWF_TemplateText.init($('body'));
    if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {
        YetaWF_Forms.partialFormActionsAll.push({
            callback: YetaWF_TemplateText.init
        });
    }

    function initClip() {
        if ($('.yt_text_copy').length > 0) {
            var clipBoard = new Clipboard('.yt_text_copy', {
                target: function (trigger) {
                    return trigger.previousElementSibling;
                },
            });
            clipBoard.on('success', function (e) {
                Y_Confirm(YLocs.Text.CopyToClip);
            });
        }
    };
    initClip();
});

