/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_ModuleSelection = {};
var _YetaWF_ModuleSelection = {};

// Load a moduleselection UI object with data
// $control refers to the div class="yt_moduleselection t_edit"
// this assumes the package dropdownlist already has the correct package selected
YetaWF_ModuleSelection.Update = function ($control, data) {
    'use strict';
    var $select = _YetaWF_ModuleSelection.getSelect($control);
    YetaWF_TemplateDropDownList.Update($select, data);
    _YetaWF_ModuleSelection.showDescription($control, data);
};

// Load a moduleselection UI object with data
// $control refers to the div class="yt_moduleselection t_edit"
// this will select the correct package in the dropdownlist and select the module (the package is detected using ajax)
YetaWF_ModuleSelection.UpdateComplete = function ($control, modGuid) {
    'use strict';

    var $select = _YetaWF_ModuleSelection.getSelect($control);
    if (modGuid == null || modGuid == "" || modGuid == "00000000-0000-0000-0000-000000000000") {
        YetaWF_ModuleSelection.Clear($control);
        return;
    }

    var ajaxurl = $('input[name$=".AjaxUrlComplete"]', $control).val();
    if (ajaxurl == "") throw "Couldn't find ajax url";/*DEBUG*/

    var data = { 'modGuid': modGuid };
    // get a new list of modules
    YetaWF_TemplateDropDownList.AjaxUpdate($select, data, ajaxurl,
        function (data) { // success
            var $packages = _YetaWF_ModuleSelection.getPackages($control);
            YetaWF_TemplateDropDownList.Update($packages, data.extra);
            YetaWF_TemplateDropDownList.Update($select, modGuid);
            _YetaWF_ModuleSelection.showDescription($control, data);
            if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($select);
        },
        function (data) { // failure
            YetaWF_ModuleSelection.Clear($control);
            if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($select);
        }
    );
};

// Get data from moduleselection
// $control refers to the div class="yt_moduleselection t_edit"
YetaWF_ModuleSelection.Retrieve = function ($control) {
    return _YetaWF_ModuleSelection.getValue($control);
}
// Test whether the data has changed
// $control refers to the div class="yt_moduleselection t_edit"
YetaWF_ModuleSelection.HasChanged = function ($control, data) {
    var mod = _YetaWF_ModuleSelection.getValue($control);
    if ((data == null || data == "" || data == "00000000-0000-0000-0000-000000000000") && (mod == undefined || mod == "" || mod == "00000000-0000-0000-0000-000000000000"))
        return false;
    return (data != mod);
}
// Enable a moduleselection object
// $control refers to the <div class="yt_moduleselection t_edit">
YetaWF_ModuleSelection.Enable = function ($control, enabled) {
    'use strict';
    var $packages = _YetaWF_ModuleSelection.getPackages($control);
    YetaWF_TemplateDropDownList.Enable($packages, enabled);
    var $select = _YetaWF_ModuleSelection.getSelect($control);
    YetaWF_TemplateDropDownList.Enable($select, enabled);
    var $link = _YetaWF_ModuleSelection.getLink($control);
    var $desc = _YetaWF_ModuleSelection.getDescription($control);
    if (enabled) {
        if (_YetaWF_ModuleSelection.hasValue($control)) {
            $link.show();
            $desc.show();
        } else {
            $link.hide();
            $desc.hide();
        }
    } else {
        $link.hide();
        $desc.hide();
    }
}
// Clear a moduleselection object
// $control refers to the <div class="yt_moduleselection t_edit">
YetaWF_ModuleSelection.Clear = function ($control) {
    var $packages = _YetaWF_ModuleSelection.getPackages($control);
    YetaWF_TemplateDropDownList.Clear($packages);
    var $select = _YetaWF_ModuleSelection.getSelect($control);
    YetaWF_TemplateDropDownList.Clear($select);
    var $link = _YetaWF_ModuleSelection.getLink($control);
    var $desc = _YetaWF_ModuleSelection.getDescription($control);
    $link.hide();
    $desc.hide();
    $desc.text('');
}

_YetaWF_ModuleSelection.getPackages = function ($control) {
    'use strict';
    var $packages = $('.t_packages select', $control);
    if ($packages.length != 1) throw "Can't find packages selection dropdown";/*DEBUG*/
    return $packages;
};
_YetaWF_ModuleSelection.getSelect = function ($control) {
    'use strict';
    var $select = $('.t_select select', $control);
    if ($select.length != 1) throw "Can't find module selection dropdown";/*DEBUG*/
    return $select;
};
_YetaWF_ModuleSelection.getLink = function ($control) {
    'use strict';
    var $link = $('.t_link', $control);
    if ($link.length != 1) throw "Can't find link";/*DEBUG*/
    return $link;
};
_YetaWF_ModuleSelection.getDescription = function ($control) {
    'use strict';
    var $desc = $('.t_description', $control);
    if ($desc.length != 1) throw "Can't find description";/*DEBUG*/
    return $desc;
};
_YetaWF_ModuleSelection.hasValue = function ($control) {
    var $select = _YetaWF_ModuleSelection.getSelect($control);
    var mod = $select.val();
    return (mod !== undefined && mod != "" && mod != "00000000-0000-0000-0000-000000000000");
};
_YetaWF_ModuleSelection.getValue = function ($control) {
    var $select = _YetaWF_ModuleSelection.getSelect($control);
    return $select.val();
};
_YetaWF_ModuleSelection.getDescriptionText = function ($control) {
    var ix = $('.t_select select option:selected', $control).index();
    return YetaWF_TemplateDropDownList.getTitle($('.t_select select', $control), ix);
};
_YetaWF_ModuleSelection.showDescription = function ($control, data) {
    var $desc = _YetaWF_ModuleSelection.getDescription($control);
    var $link = _YetaWF_ModuleSelection.getLink($control);
    if (_YetaWF_ModuleSelection.hasValue($control)) {
        $('a', $link).attr("href", '/!Mod/' + data); // Globals.ModuleUrl
        $link.show();
        var desc = _YetaWF_ModuleSelection.getDescriptionText($control);
        $desc.text(desc);
        $desc.show();
    } else {
        $link.hide();
        $desc.hide();
        $desc.text('');
    }
}

YetaWF_ModuleSelection.init = function (id) {
    'use strict';
    var $control = $('#' + id);
    if ($control.length != 1) throw "Can't find control";/*DEBUG*/
    var $select = _YetaWF_ModuleSelection.getSelect($control);

    var val = $select.val();
    YetaWF_ModuleSelection.Update($control, val);
};

$(document).on('change', '.yt_moduleselection.t_edit .t_packages select', function (event) {
    'use strict';
    var $this = $(this);

    var $control = $this.closest('.yt_moduleselection');
    if ($control.length != 1) throw "Couldn't find module selection control";/*DEBUG*/
    var $select = _YetaWF_ModuleSelection.getSelect($control);

    var ajaxurl = $('input[name$=".AjaxUrl"]', $control).val();
    if (ajaxurl == "") throw "Couldn't find ajax url";/*DEBUG*/

    var data = { 'AreaName': $(this).val() };
    // get a new list of modules
    YetaWF_TemplateDropDownList.AjaxUpdate($select, data, ajaxurl);
});
$(document).on('change', '.yt_moduleselection.t_edit .t_select select', function (event) {
    var $this = $(this);
    var data = $this.val();
    var $control = $(this).closest('.yt_moduleselection');
    _YetaWF_ModuleSelection.showDescription($control, data);
});

