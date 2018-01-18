/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

/* Page Skin */

var YetaWF_Template_PageSkin = {};
var _YetaWF_Template_PageSkin = {};

_YetaWF_Template_PageSkin.FallbackPageFileName = "Default.cshtml";
_YetaWF_Template_PageSkin.FallbackPopupFileName = "Popup.cshtml";

YetaWF_Template_PageSkin.pageInit = function (id, collection) { };
YetaWF_Template_PageSkin.popupInit = function (id, collection) { };

$(document).on('change', '.yt_pageskin .t_collection select, .yt_popupskin .t_collection select', function (event) {

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
    YetaWF_TemplateDropDownList.AjaxUpdate($filename, data, ajaxurl);
});


