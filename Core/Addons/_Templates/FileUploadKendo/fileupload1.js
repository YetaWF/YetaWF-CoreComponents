/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_FileUpload1 = {};

YetaWF_FileUpload1.init = function (divId, serializeForm) {

    var $control = $('#'+divId);
    if ($control.length != 1) throw "div not found";/*DEBUG*/
    var $filename = $('input.t_filename', $control);
    if ($filename.length != 1) throw "filename control not found";/*DEBUG*/

    var saveUrl = $control.attr("data-saveurl");
    if (saveUrl == undefined) throw "data-saveurl not defined";/*DEBUG*/
    var removeUrl = $control.attr("data-removeurl");

    YetaWF_FileUpload1.removeFile = function(name) {
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

    $filename.kendoUpload({
        //template: kendo.template($('.t_template', $control).html()),
        multiple: false,
        showFileList: false,
        async: {
            saveUrl: saveUrl,
            removeUrl: removeUrl,
            saveField: '__filename',
            removeField: '__filename',
            autoUpload: true,
        },
        localization: {
            select: $control.attr("data-selectbuttontext"),
            dropFilesHere: $control.attr("data-dropfilestext"),
            cancel: YLocs.FileUpload,
            headerStatusUploaded: YLocs.FileUpload.HeaderStatusUploaded,
            headerStatusUploading: YLocs.FileUpload.HeaderStatusUploading,
            remove: YLocs.FileUpload.RemoveButtonText,
            retry: YLocs.FileUpload.RetryButtonText,
            statusFailed: YLocs.FileUpload.StatusFailedText,
            statusUploaded: YLocs.FileUpload.StatusUploadedText,
            statusUploading: YLocs.FileUpload.StatusUploadingText,
            uploadSelectedFiles: YLocs.FileUpload.UploadFilesButtonText,
        },
        upload: function (e) {
            if ($control.data().getFileName != undefined) {
                var filename = $control.data().getFileName();
                e.data = { '__lastInternalName': filename }; // the previous real filename of the file to remove
            }
            var $form = YetaWF_Forms.getForm($control);
            var formData = YetaWF_Forms.serializeFormArray($form);

            throw "THE FOLLOWING IS UNTESTED";//TODO: Test with Kendo UI Pro
            // We want to merge all form data with e.data which supposedly has __filename and other parms as an object
            if (serializeForm)
                $.extend(e.data, formData);

            Y_Loading(true);
        },
        remove: function (e) {
            if ($control.data().getFileName == undefined) {
                e.preventDefault();
            } else {
                if (removeUrl == undefined || removeUrl == "") throw "data-removeUrl not defined";/*DEBUG*/
                var filename = $control.data().getFileName();
                e.data = { '__internalName': filename }; // the real filename of the file to remove
                Y_Loading(true);
            }
        },
        success: function onSuccess(e) {
            //{
            //    "result":      "Y_Confirm(\"Image \\\"logo_233x133.jpg\\\" successfully uploaded\");",  
            //    "filename": "tempc8eb1eb6-31ef-4e5d-9100-9fab50761a81.jpg",
            //    "realFilename": "logo_233x133.jpg",
            //    "attributes": "233 x 123 (w x h)"
            //}
            Y_Loading(false);
            var result = e.XMLHttpRequest.responseText;
            if (result == "") {//success
                if ($control.data().successfullUpload != undefined) {
                    $control.data().successfullUpload({ 'filename': '' });
                }
                //if ($control.data().setAttributes != undefined)
                //    filename = $control.data().setAttributes('');
                return;
            }
            if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                eval(script);
                return;
            }
            if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                eval(script);
                return;
            }
            // result has quotes around it
            var js = JSON.parse(result);
            js = JSON.parse(js);
            //if ($control.data().setAttributes != undefined)
            //    filename = $control.data().setAttributes(js.attributes);
            eval(js.result);
            if ($control.data().successfullUpload != undefined)
                $control.data().successfullUpload(js);
        },
        error: function onError(e) {
            Y_Loading(false);
            var result = e.XMLHttpRequest.responseText;
            if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                eval(script);
            } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                eval(script);
            } else if (e.XMLHttpRequest.status == 404) {
                Y_Error(YLocs.FileUpload.FileTooLarge);
            } else
                Y_Error(YLocs.FileUpload.UnexpectedStatus.format(e.XMLHttpRequest.status));
            return false;
        },
    });
};