/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Init all kendoMenus */

// Module Menu
YetaWF_Basics.whenReady.push({
    callback: function ($tag) {
        $('.yModuleMenu', $tag).kendoMenu({
            orientation: "vertical"
        })
        .css({
            width: 'auto'
        });
    }
});


