/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_TemplateDropDownList = {};
var _YetaWF_TemplateDropDownList = {};

YetaWF_TemplateDropDownList.initOne = function ($this) {
    var w = $this.width();
    if (w > 0 && $this.attr("data-needinit") !== undefined) {
        $this.removeAttr("data-needinit");
        $this.kendoDropDownList({});
        var avgw = $this.attr("data-charavgw");
        if (!avgw) throw "dropdowlist without avg char width";/*DEBUG*/
        $this.closest('.k-widget.yt_dropdownlist,.k-widget.yt_dropdownlist_base,.k-widget.yt_enum').width(w + 3 * avgw);
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
        if (dropdownlist.select() < 0)
            dropdownlist.select(0);
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
YetaWF_TemplateDropDownList.AjaxUpdate = function ($control, data, ajaxurl, onSuccess, onFailure) {
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
                $control.val(null);
                $control.kendoDropDownList({
                    dataTextField: "t",
                    dataValueField: "v",
                    dataSource: data.data,
                });
                $control.data("tooltips", data.tooltips);
                if (onSuccess !== undefined) {
                    onSuccess(data);
                } else {
                    $control.select(0);
                    $control.trigger('change');
                }
            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                eval(script);
                if (onFailure !== undefined)
                    onFailure(data);
            } else {
                if (onFailure !== undefined)
                    onFailure(data);
                throw "Unexpected data returned";
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            Y_Loading(false);
            Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
            if (onFailure !== undefined)
                onFailure(data);
        }
    });
}

// We need to delay initialization until a panel becomes visible so we can calculate the dropdown width
$(document).on('YetaWF_PropertyList_PanelSwitched', function (event, $panel) {
    var $ctls = $('select.yt_dropdownlist_base[data-needinit]');
    $ctls.each(function (index) {
        YetaWF_TemplateDropDownList.initOne($(this));
    });
});
$(document).on('change', 'select.yt_dropdownlist_base[data-val=true]', function () {
    if (typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) YetaWF_Forms.validateElement($(this));
});

// A <div> is being emptied. Destroy all date/time pickers the <div> may contain.
YetaWF_Basics.addClearDiv(function (tag) {
    var list = tag.querySelectorAll(".yt_dropdownlist_base select");
    var len = list.length;
    for (var i = 0; i < len; ++i) {
        var el = list[i];
        var dropdownlist = $(el).data("kendoDropDownList");
        if (dropdownlist) dropdownlist.destroy();
    }
});

