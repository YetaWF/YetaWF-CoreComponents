/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_ModuleSelection = {};

// Load a moduleselection UI object with data
// $control refers to the div class="yt_moduleselection t_edit"
YetaWF_ModuleSelection.Update = function ($control, data) {
    'use strict';
    var $select = _YetaWF_ModuleSelection.getSelect($control);
    var $link = _YetaWF_ModuleSelection.getLink($control);
    var $desc = _YetaWF_ModuleSelection.getDescription($control);
    YetaWF_TemplateDropDownList.Update($select, data);
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
    return (data != mod);
}
// Enable a moduleselection object
// $control refers to the <div class="yt_moduleselection t_edit">
YetaWF_ModuleSelection.Enable = function ($control, enabled) {
    'use strict';
    var $select = _YetaWF_ModuleSelection.getSelect($control);
    var $link = _YetaWF_ModuleSelection.getLink($control);
    var $desc = _YetaWF_ModuleSelection.getDescription($control);
    YetaWF_TemplateDropDownList.Enable($select, enabled);
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
    _YetaWF_ModuleSelection.getText($ms).val(null);
}


var _YetaWF_ModuleSelection = {};

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


YetaWF_ModuleSelection.init = function (id) {
    'use strict';
    var $control = $('#' + id);
    if ($control.length != 1) throw "Can't find control";/*DEBUG*/
    var $select = _YetaWF_ModuleSelection.getSelect($control);

    $select.on('change', function () {
        var $this = $(this);
        var val = $this.val();
        YetaWF_Forms.hideError(YetaWF_Forms.getForm($this), $this.attr("name")); // TODO: This could be removed when RequiredAttribute supports Guid client-side
        YetaWF_ModuleSelection.Update($control, val);
    });
    $select.trigger('change');
};


$(document).ready(function () {

    $("body").on('change', '.yt_moduleselection.t_edit .t_packages select', function (event) {
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
});

