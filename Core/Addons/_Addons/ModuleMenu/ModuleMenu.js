/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Init all module menus */

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


