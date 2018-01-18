/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    // adding k-textbox to the control makes it look like a kendo maskedtext box without the overhead of actually calling kendoMaskedTextBox
    $('input.yt_text,input.yt_text10,input.yt_text20,input.yt_text40,input.yt_text80,input.yt_text_base', $tag).not('.ybrowsercontrols').addClass('k-textbox');

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

