/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_TemplateDropDownList = {};
var _YetaWF_TemplateDropDownList = {};

YetaWF_TemplateDropDownList.initOne = function ($this) {
    var w = $this.width();
    if (w != 0 && $this.attr("data-needinit") !== undefined) {
        $this.removeAttr("data-needinit");
        $this.kendoDropDownList({ });
        $this.closest('.k-widget.yt_dropdownlist,.k-widget.yt_dropdownlist_base,.k-widget.yt_enum').width(w + 3 * YVolatile.Basics.CharWidthAvg);
    }
}
// Enable a dropdownlist object
// $control refers to the <div class="yt_dropdownlist...">
YetaWF_TemplateDropDownList.Enable = function ($control, enabled) {
    if ($control.attr("data-needinit") !== undefined) {
        if (enabled)
            $control.removeAttr("disabled");
        else
            $control.attr("disabled","disabled");
    } else {
        var dropdownlist = $control.data("kendoDropDownList");
        dropdownlist.enable(enabled);
    }
}
// Update a dropdownlist object
// $control refers to the <div class="yt_dropdownlist...">
YetaWF_TemplateDropDownList.Update = function ($control, value) {
    if ($control.attr("data-needinit") !== undefined)
        $control.val(value);
    else {
        var dropdownlist = $control.data("kendoDropDownList");
        dropdownlist.value(value);
    }
}
// Clear a dropdownlist object (select the first item)
// $control refers to the <div class="yt_dropdownlist...">
YetaWF_TemplateDropDownList.Clear = function ($control) {
    if ($control.attr("data-needinit") !== undefined)
        $control.prop('selectedIndex', 0);
    else {
        var dropdownlist = $control.data("kendoDropDownList");
        dropdownlist.select(0);
    }
}

// retrieve the tooltip for the nth item (index) in the dropdown list $this
YetaWF_TemplateDropDownList.getTitle = function ($this, index) {
    var tts = $this.data("tooltips");
    if (tts === undefined || tts == null) return null;
    if (index < 0 || index >= tts.length) return null;
    return tts[index];
}
YetaWF_TemplateDropDownList.getTitleFromId = function (id, index) {
    return YetaWF_TemplateDropDownList.getTitle($('#{0}'.format(id)), index);
}

// Send data to server using ajaxurl and update the dropdownlist with the returned data object (text,value & tooltips)
YetaWF_TemplateDropDownList.AjaxUpdate = function ($control, data, ajaxurl) {
    'use strict';
    $.ajax({
        url: ajaxurl,
        type: 'post',
        data: data,
        success: function (result, textStatus, jqXHR) {
            Y_Loading(false);
            if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                var data = JSON.parse(script);
                var ctl = $control.data("kendoDropDownList");
                $control.kendoDropDownList({
                    dataTextField: "t",
                    dataValueField: "v",
                    dataSource: data.data,
                });
                $control.data("tooltips", data.tooltips);
            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                eval(script);
            } else
                throw "Unexpected data returned";
        },
        error: function (jqXHR, textStatus, errorThrown) {
            Y_Loading(false);
            Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
        }
    });
}

$(document).ready(function () {
    //'use strict';
    // We need to delay initialization until a panel becomes visible so we can calculate the dropdown width
    $("body").on('YetaWF_PropertyList_PanelSwitched', function (event, $panel) {
        var $ctls = $('select.yt_dropdownlist[data-needinit],select.yt_dropdownlist_base[data-needinit],select.yt_enum[data-needinit]');
        $ctls.each(function (index) {
            YetaWF_TemplateDropDownList.initOne($(this));
        });
    });
});

