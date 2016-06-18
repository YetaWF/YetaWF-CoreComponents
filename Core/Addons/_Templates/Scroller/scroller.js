/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

$(document).ready(function () {

    function getControl(elem) {
        var $elem = $(elem);
        var $control = $elem.closest('.yt_scroller');
        if ($control.length != 1) throw "Can't find scroller control";/*DEBUG*/
        return $control;
    }
    function updateButtons($control, direction) {
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
        $('.t_right', $control).css('background-position', index + skip < itemCount - 1 ? '-48px -48px' : '-48px 0px');
        if (index + skip >= itemCount - 1)
            $('.t_right', $control).attr('disabled', 'disabled');
        else
            $('.t_right', $control).removeAttr('disabled');
    }
    function scroll(direction, elem) {
        var $elem = $(elem);
        var $control = getControl($elem);
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

        updateButtons($control)

        var offs = index * itemwidth;
        $('.t_items', $control).animate({
            left: -offs,
        }, 250, function () { });
    }
    $('body').on('click', '.yt_scroller .t_left', function () {
        scroll(-1, this);
    });
    $('body').on('click', '.yt_scroller .t_right', function () {
        scroll(1, this);
    });

    $('.yt_scroller').each(function () {
        var $control = getControl(this);
        updateButtons($control);
    });
});