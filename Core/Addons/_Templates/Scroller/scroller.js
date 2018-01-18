/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

_YetaWF_Scroller = {};
_YetaWF_Scroller.getControl = function (elem) {
    'use strict';
    var $elem = $(elem);
    var $control = $elem.closest('.yt_scroller');
    if ($control.length != 1) throw "Can't find scroller control";/*DEBUG*/
    return $control;
};
_YetaWF_Scroller.updateButtons = function ($control, direction) {
    'use strict';
    var index = $control.attr('data-index');
    if (index == undefined) index = 0;
    index = parseInt(index);
    $('.t_left', $control).css('background-position', index == 0 ? '0px 0px' : '0px -48px');
    if (index == 0)
        $('.t_left', $control).attr('disabled', 'disabled');
    else
        $('.t_left', $control).removeAttr('disabled');
    var width = $control.innerWidth();
    var itemwidth = $('.t_item', $control).eq(0).outerWidth();
    var itemCount = $('.t_item', $control).length;
    var skip = Math.floor(width / itemwidth);
    $('.t_right', $control).css('background-position', index + skip < itemCount ? '-48px -48px' : '-48px 0px');
    if (index + skip >= itemCount)
        $('.t_right', $control).attr('disabled', 'disabled');
    else
        $('.t_right', $control).removeAttr('disabled');
};

_YetaWF_Scroller.scroll = function (direction, elem) {
    'use strict';

    var $elem = $(elem);
    var $control = _YetaWF_Scroller.getControl($elem);
    var width = $control.innerWidth();
    var itemwidth = $('.t_item', $control).eq(0).outerWidth();

    var index = $control.attr('data-index');
    if (index == undefined) index = 0;
    index = parseInt(index);
    var itemCount = $('.t_item', $control).length;

    var skip = Math.floor(width / itemwidth);
    if (skip < 1) skip = 1;
    index = index + skip * direction;
    //if (index >= itemCount - skip) index %= itemCount;
    //if (index < 0) index = itemCount + index;
    if (index >= itemCount) index = itemCount - skip;
    if (index < 0) index = 0;
    $control.attr('data-index', index);

    _YetaWF_Scroller.updateButtons($control)

    var offs = index * itemwidth;
    $('.t_items', $control).animate({
        left: -offs,
    }, 250, function () { });
}

$(document).on('click', '.yt_scroller .t_left', function () {
    _YetaWF_Scroller.scroll(-1, this);
});
$(document).on('click', '.yt_scroller .t_right', function () {
    _YetaWF_Scroller.scroll(1, this);
});

YetaWF_Basics.whenReady.push({
    callback: function ($tag) {
        $('.yt_scroller', $tag).each(function () {
            var $control = _YetaWF_Scroller.getControl(this);
            _YetaWF_Scroller.updateButtons($control);
        });
    }
});
