/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_TemplateText = {};
var _YetaWF_TemplateText = {};

// Enable a text object
// $control refers to the <div class="yt_text t_edit">
YetaWF_TemplateText.Enable = function ($control, enabled) {
    'use strict';
    if (enabled)
        $control.removeAttr("disabled");
    else
        $control.attr("disabled", "disabled");
};

_YetaWF_TemplateText.clip = null;

// Initialize all text templates within $tag
YetaWF_TemplateText.init = function ($tag) {
    'use strict';

    $('input.yt_text,input.yt_text10,input.yt_text20,input.yt_text40,input.yt_text80,input.yt_text_base', $tag).not('.ybrowsercontrols').each(function (index) {
        var $this = $(this);
        var autocomplete = $this.attr('autocomplete');// preserve autocomplete
        $this.kendoMaskedTextBox({});
        $this.attr('autocomplete', autocomplete);
    });
    function initClip() {
        if (_YetaWF_TemplateText.clip == null && $('.yt_text_copy').length > 0) {
            _YetaWF_TemplateText.clip = new Clipboard('.yt_text_copy', {
                target: function (trigger) {
                    return trigger.previousElementSibling;
                },
            });
            _YetaWF_TemplateText.clip.on('success', function (e) {
                Y_Confirm(YLocs.Text.CopyToClip);
            });
        }
    };
    initClip();
};

YetaWF_Basics.whenReady.push({
    callback: YetaWF_TemplateText.init
});

