/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Url = {};
var _YetaWF_Url = {};

YetaWF_Url.init = function (id) {
    'use strict';
    var $control = $('#' + id);
    if ($control.length != 1) throw "Can't find control";/*DEBUG*/

    $('select.t_select', $control).on('change', function () {
        var $type = _YetaWF_Url.getType($control);
        var $localPage = _YetaWF_Url.getLocalPage($control);
        var $remotePage = _YetaWF_Url.getRemotePage($control);
        var sel = $type.val();
        var urlString;
        if (sel == 1)
            urlString = $localPage.val();
        else
            urlString = $remotePage.val();
        YetaWF_Url.Update($control, urlString, false);
        YetaWF_Url.Enable($control, true);
    });
    $('.t_local select', $control).on('change', function () {
        var $hidden = _YetaWF_Url.getHidden($control);
        $hidden.val($('.t_local select', $control).val());
        _YetaWF_Url.updateLink($control);
    });
    $('.t_remote input', $control).on('change', function () {
        var $hidden = _YetaWF_Url.getHidden($control);
        $hidden.val($('.t_remote input', $control).val());
        _YetaWF_Url.updateLink($control);
    });
    // initial selection
    var $hidden = _YetaWF_Url.getHidden($control);
    YetaWF_Url.Update($control, $hidden.val(), true);
};

// Enable a url object
// $control refers to the <div class="yt_url t_edit">
YetaWF_Url.Enable = function ($control, enabled) {
    'use strict';
    var $type = _YetaWF_Url.getType($control);
    var $localPage = _YetaWF_Url.getLocalPage($control);
    var $remotePage = _YetaWF_Url.getRemotePage($control);
    if (enabled) {
        if ($localPage)
            $localPage.removeAttr('disabled');
        if ($remotePage)
            $remotePage.removeAttr('disabled');
        if ($localPage && $remotePage)
            $type.removeAttr('disabled');
    } else {
        if ($localPage)
            $localPage.attr('disabled', 'disabled');
        if ($remotePage)
            $remotePage.attr('disabled', 'disabled');
        $type.attr('disabled', 'disabled');
    }
    if (!($localPage && $remotePage))
        $type.attr('disabled', 'disabled');
};
// Load a value into a url object
// $control refers to the <div class="yt_url t_edit">
YetaWF_Url.Update = function ($control, urlString, initial) {
    'use strict';
    var $hidden = _YetaWF_Url.getHidden($control);
    var $type = _YetaWF_Url.getType($control);
    var $localPage = _YetaWF_Url.getLocalPage($control);
    var $remotePage = _YetaWF_Url.getRemotePage($control);
    if ($localPage && $remotePage) {
        var sel = $type.val();// use selection
        if (urlString != null && (urlString.startsWith('//') || urlString.startsWith('http'))) {
            if ($remotePage)
                sel = 2;
        } else if (initial) {
            $localPage.val(urlString);
            var actualSel = $localPage.val();
            if (urlString != actualSel) {
                sel = 2; // have to use remote even though it's a local page (but with args)
            } else {
                sel = 1;
            }
        }
    } else if ($localPage) {
        sel = 1;
    } else {
        sel = 2;
    }

    $hidden.val(urlString);
    $type.val(sel);
    if (sel == 1) {
        if ($localPage) {
            $localPage.val(urlString);
            $localPage.show();
        }
        if ($remotePage)
            $remotePage.hide();
    } else {
        if ($localPage)
            $localPage.hide();
        if ($remotePage) {
            $remotePage.val(urlString);
            $remotePage.show();
        }
    }
    _YetaWF_Url.updateLink($control);
};
YetaWF_Url.Clear = function ($control) {
    'use strict';
    YetaWF_Url.Update($control, "", true);
};
YetaWF_Url.Retrieve = function ($control) {
    'use strict';
    var $hidden = _YetaWF_Url.getHidden($control);
    return $hidden.val();
};
YetaWF_Url.HasChanged = function ($control, data) {
    'use strict';
    var $hidden = _YetaWF_Url.getHidden($control);
    return !StringYCompare(data, $hidden.val());
};


_YetaWF_Url.updateLink = function ($control) {
    'use strict';
    var $hidden = _YetaWF_Url.getHidden($control);
    var $link = _YetaWF_Url.getLink($control);
    var urlString = $hidden.val();
    if (urlString == undefined || urlString == "") {
        $link.hide();
    } else {
        var currUri = new URI(urlString);
        currUri.removeSearch(YGlobals.Link_TempNoEditMode);
        currUri.addSearch(YGlobals.Link_TempNoEditMode, "y");
        $link.attr("href", currUri.toString());

        $link.show();
    }
};

// get the hidden field storing the url
_YetaWF_Url.getHidden = function ($control) {
    'use strict';
    var $hidden = $('input[type="hidden"]', $control);
    if ($hidden.length != 1) throw "couldn't find hidden field";/*DEBUG*/
    return $hidden;
};
// get the user visible local/remote selector
_YetaWF_Url.getType = function ($control) {
    'use strict';
    var $sel = $('select.t_select', $control);
    if ($sel.length != 1) throw "couldn't find local/remote type selector";/*DEBUG*/
    return $sel;
};
// get the user visible local page selector
_YetaWF_Url.getLocalPage = function ($control) {
    'use strict';
    var $sel = $('.t_local select', $control);
    if ($sel.length != 1) return null;
    return $sel;
};
// get the user visible remote page selector
_YetaWF_Url.getRemotePage = function ($control) {
    'use strict';
    var $sel = $('.t_remote input', $control);
    if ($sel.length != 1) return null;
    return $sel;
};
// get the link to go to when the image is clicked
_YetaWF_Url.getLink = function ($control) {
    'use strict';
    var $link = $('.t_link a', $control);
    if ($link.length != 1) throw "couldn't find link";/*DEBUG*/
    return $link;
};
