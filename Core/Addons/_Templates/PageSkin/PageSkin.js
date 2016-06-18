/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

/* Page Skin */

var YetaWF_Template_PageSkin = {};
var _YetaWF_Template_PageSkin = {};

_YetaWF_Template_PageSkin.FallbackPageFileName = "Default.cshtml";
_YetaWF_Template_PageSkin.FallbackPopupFileName = "Popup.cshtml";

YetaWF_Template_PageSkin.pageInit = function (id, collection) { };
YetaWF_Template_PageSkin.popupInit = function (id, collection) { };

$(document).ready(function () {

    $("body").on('change', '.yt_pageskin .t_collection select, .yt_popupskin .t_collection select', function (event) {

        'use strict';

        var $this = $(this);

        var $ctl = $this.closest('.yt_pageskin,.yt_popupskin');
        if ($ctl.length != 1) throw "Couldn't find skin control";/*DEBUG*/
        var $coll = $('select[name$=".Collection"]', $ctl);
        if ($coll.length != 1) throw "Couldn't find skin collection control";/*DEBUG*/
        var $filename = $('select[name$=".FileName"]', $ctl);
        if ($filename.length != 1) throw "Couldn't find filename control";/*DEBUG*/
        var popup = $ctl.hasClass('yt_popupskin');

        var ajaxurl = $('input[name$=".AjaxUrl"]', $ctl).val();
        if (ajaxurl == "") throw "Couldn't find ajax url";/*DEBUG*/

        var data = { 'skinCollection': $(this).val() };
        // get a new list of skins
        $.ajax({
            url: ajaxurl,
            type: 'post',
            data: data,
            success: function (result, textStatus, jqXHR) {
                Y_Loading(false);
                if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                    eval(script);
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                    eval(script);
                } else {
                    var name = $filename.val();
                    $filename.html(result);
                    $filename.val(name);
                    if ($filename.val() == null) {
                        if (popup)
                            $filename.val(_YetaWF_Template_PageSkin.FallbackPopupFileName);
                        else
                            $filename.val(_YetaWF_Template_PageSkin.FallbackPageFileName);
                    }
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                Y_Loading(false);
                Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
            }
        });
    });
});

