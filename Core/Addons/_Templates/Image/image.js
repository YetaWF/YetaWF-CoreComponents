/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_Image = {};

YetaWF_Image.init = function (divId) {
    var CLEAREDFILE = '(CLEARED)';

    var $control = $('#' + divId);
    if ($control.length != 1) throw "div not found";/*DEBUG*/

    var $img = $('.t_preview', $control);
    if ($img.length != 1) throw "preview image not found";/*DEBUG*/

    var $hidden = $('input[type="hidden"]', $control);
    if ($hidden.length != 1) throw "hidden field not found";/*DEBUG*/
    if (getFileName() == CLEAREDFILE) $hidden.val('');

    // set the preview image
    function setPreview(name) {
        var src = $img.attr('src');
        var currUri = new URI(src);
        currUri.removeSearch("Name");
        currUri.addSearch("Name", name);
        $img.attr('src', currUri.toString());
    };
    function getFileName() {
        return $hidden.val();
    }
    function clearFileName() {
        $hidden.val(CLEAREDFILE);
        setPreview('');
        $('.t_haveimage', $control).toggle(false);
    }
    function successfullUpload(js) {
        $hidden.val(js.filename);
        setPreview(js.filename);
        $('.t_haveimage', $control).toggle(js.filename.length > 0);
    }
    // set upload control settings
    var $upload = $('.yt_fileupload1', $control);
    if ($upload.length != 1) throw "upload control not found";/*DEBUG*/
    $upload.data({
        getFileName: getFileName,
        successfullUpload: successfullUpload,
    });

    // handle the clear button
    $('input.t_clear', $control).on('click', function () {
        var name = getFileName();
        YetaWF_FileUpload1.removeFile(name);
        clearFileName();
    });

    // init clear button
    if (getFileName().length == 0)
        clearFileName();
};

