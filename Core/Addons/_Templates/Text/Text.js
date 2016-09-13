/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Text_Clip = {};
var _YetaWF_Text_ClipBoard = undefined;

YetaWF_Text_Clip.init = function() {
    'use strict';
    if ($('.yt_text_copy').length > 0) {
        var _YetaWF_Text_ClipBoard = new Clipboard('.yt_text_copy', {
            target: function (trigger) {
                return trigger.previousElementSibling;
            },
        });
        _YetaWF_Text_ClipBoard.on('success', function (e) {
            Y_Confirm(YLocs.Text.CopyToClip);
        });
    }
};

$(document).ready(function () {
    YetaWF_Text_Clip.init();
});

