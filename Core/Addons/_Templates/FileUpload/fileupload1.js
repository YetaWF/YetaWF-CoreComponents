/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_FileUpload1 = {};

YetaWF_FileUpload1.init = function (divId, serializeForm) {

    var $control = $('#' + divId);
    if ($control.length != 1) throw "div not found";/*DEBUG*/
    var $filename = $('input.t_filename', $control);
    if ($filename.length != 1) throw "filename control not found";/*DEBUG*/

    var saveUrl = $control.attr("data-saveurl");
    if (saveUrl == undefined) throw "data-saveurl not defined";/*DEBUG*/
    var removeUrl = $control.attr("data-removeurl");

    var $progressbar = $('.t_progressbar', $control);
    if ($progressbar.length > 0) {
        $progressbar.progressbar({
            max: 100,
            value: 0,
        });
        $progressbar.hide();
    }

    $('#' + divId).dmUploader({
        url: saveUrl,
        //dataType: 'json',  //don't use otherwise response is not recognized in case of errors
        //allowedTypes: '*',
        //extFilter: 'jpg,png,gif',
        fileName:'__filename',
        onInit: function () { },
        onBeforeUpload: function (id) {
            Y_Loading(true);
        },
        onExtraData: function (id, data) {
            if ($control.data().getFileName != undefined) {
                var filename = $control.data().getFileName();
                data.append('__lastInternalName', filename);// the previous real filename of the file to remove
            }
            if (serializeForm) {
                var $form = YetaWF_Forms.getForm($control);
                var formData = YetaWF_Forms.serializeFormArray($form);
                var i, l;
                for (i = 0, l = formData.length; i < l ; ++i) {
                    data.append(formData[i].name, formData[i].value);
                }
            }
        },
        onNewFile: function (id, file) {
            console.log('onNewFile #' + id + ' ' + file);
        },
        onComplete: function () {
            $progressbar.hide();
        },
        onUploadProgress: function (id, percent) {
            if ($progressbar.length > 0) {
                $progressbar.show();
                $progressbar.progressbar("value", percent);
            }
        },
        onUploadError: function (id, message) {
            Y_Loading(false);
            if (message == "")
                Y_Error(YLocs.FileUpload.StatusUploadNoResp);
            else
                Y_Error(YLocs.FileUpload.StatusUploadFailed.format(message));
        },
        onFileTypeError: function (file) {
            Y_Error(YLocs.FileUpload.FileTypeError);
        },
        onFileSizeError: function (file) {
            Y_Error(YLocs.FileUpload.FileSizeError);
        },
        onFallbackMode: function (message) {
            Y_Error(YLocs.FileUpload.FallbackMode);
        },
        onUploadSuccess: function (id, data) {
            //{
            //    "result":      "Y_Confirm(\"Image \\\"logo_233x133.jpg\\\" successfully uploaded\");",  
            //    "filename": "tempc8eb1eb6-31ef-4e5d-9100-9fab50761a81.jpg",
            //    "realFilename": "logo_233x133.jpg",
            //    "attributes": "233 x 123 (w x h)"
            //}
            Y_Loading(false);
            if (data.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                var script = data.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                eval(script);
                return;
            }
            if (data.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                var script = data.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                eval(script);
                return;
            }
            // result has quotes around it
            var js = JSON.parse(data);
            eval(js.result);
            if ($control.data().successfullUpload != undefined) {
                $control.data().successfullUpload(js);
            }
            //if ($control.data().setAttributes != undefined)
            //    filename = $control.data().setAttributes('');
        },
    });

    YetaWF_FileUpload1.removeFile = function (name) {
        if (removeUrl == undefined || removeUrl == "") throw "data-removeUrl not defined";/*DEBUG*/
        $.ajax({
            url: $control.attr("data-removeurl"),
            type: 'post',
            data: '__internalName=' + encodeURIComponent(name) + '&__filename=' + encodeURIComponent(name),
            success: function (result, textStatus, jqXHR) { },
            error: function (jqXHR, textStatus, errorThrown) {
                Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
            }
        });
    }
};

$(document).ready(function () {
    // trigger upload button
    $('body').on('click', '.yt_fileupload1 .t_upload', function (ev) {
        var $control = $(this).closest('.yt_fileupload1');
        $('input[type="file"]', $control).trigger('click');
    });
});
