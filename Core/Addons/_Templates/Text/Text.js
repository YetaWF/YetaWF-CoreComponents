/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Text_Clip = {};
var _YetaWF_Text_ClipBoard = undefined;

YetaWF_Text_Clip.init = function() {
    'use strict';
    if (_YetaWF_Text_ClipBoard !== undefined) {
        _YetaWF_Text_ClipBoard.destroy();
        _YetaWF_Text_ClipBoard = undefined;
    }
    if ($('.yt_text_copy').length > 0) {
        var _YetaWF_Text_ClipBoard = new Clipboard('.yt_text_copy', {
            target: function (trigger) {
                return trigger.previousElementSibling;
            },
        });
        _YetaWF_Text_ClipBoard.on('success', function (e) {
            Y_Confirm("Copied to clipboard");//$$localize
        });
    }
};

$(document).ready(function () {
    'use strict';
    YetaWF_Text_Clip.init();
    if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {
        YetaWF_Forms.partialFormActionsAll.push({
            callback: YetaWF_Text_Clip.init
        });
    }
});

