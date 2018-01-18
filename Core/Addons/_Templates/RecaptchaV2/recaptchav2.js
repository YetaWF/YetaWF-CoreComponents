/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Core_RecaptchaV2 = {};

YetaWF_Core_RecaptchaV2.onLoad = function ($tag) {
    'use strict';
    if (typeof grecaptcha === 'undefined') {
        // keep trying until grecaptcha is available
        setTimeout(function() {
            YetaWF_Core_RecaptchaV2.onLoad($tag);
        }, 100);
        return;
    }
    $('.yt_recaptchav2', $tag).each(function () {
        grecaptcha.render(this, {
            'sitekey':YConfigs.RecaptchaV2.SiteKey,
            'theme': YConfigs.RecaptchaV2.Theme,
            'size': YConfigs.RecaptchaV2.Size,
        });
    });
};

YetaWF_Basics.whenReady.push({
    callback: function ($tag) {
        YetaWF_Core_RecaptchaV2.onLoad($tag);
    }
});
