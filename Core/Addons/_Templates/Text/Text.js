/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

$(document).ready(function () {
    'use strict';
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

