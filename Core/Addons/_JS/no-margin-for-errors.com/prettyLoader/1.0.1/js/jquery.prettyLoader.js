/* ------------------------------------------------------------------------
	Class: prettyLoader
	Use: A unified solution for AJAX loader
	Author: Stephane Caron (http://www.no-margin-for-errors.com)
	Version: 1.0.1
------------------------------------------------------------------------- */

(function($) {
	$.prettyLoader = {version: '1.0.1'};

	$.prettyLoader = function(settings) {
		settings = jQuery.extend({
			animation_speed: 'fast', /* fast/normal/slow/integer */
			bind_to_ajax: true, /* true/false */
			delay: false, /* false OR time in milliseconds (ms) */
			loader: '/images/prettyLoader/ajax-loader.gif', /* Path to your loader gif */
			offset_top: 13, /* integer */
			offset_left: 10 /* integer */
		}, settings);

	    //CUSTOMIZATON:
		var prettyLoaderOn = false;
	    // END CUSTOMIZATION

		scrollPos = _getScroll();

		imgLoader = new Image();
		imgLoader.onerror = function(){
			alert('Preloader image cannot be loaded. Make sure the path is correct in the settings and that the image is reachable.');
		};
		imgLoader.src = settings.loader;

		if(settings.bind_to_ajax)
			jQuery(document).ajaxStart(function(){ $.prettyLoader.show() }).ajaxStop(function(){ $.prettyLoader.hide() });

		var cur_x = 0;
	    var cur_y = 0;
	    //CUSTOMIZATON:
	    $(window).mousemove(function (e) {
	        cur_x = e.clientX;
	        cur_y = e.clientY;
	        if (!prettyLoaderOn) return;
	        $.prettyLoader.positionLoader(null);
	    });
	    $(window).scroll(function () { scrollPos = _getScroll(); $(window).triggerHandler('mousemove'); });
	    // END CUSTOMIZATION

	    $.prettyLoader.positionLoader = function (e) {

	        //CUSTOMIZATON:
		    if (!prettyLoaderOn) return;
		    // END CUSTOMIZATION

		    // CUSTOMIZATION
		    // END CUSTOMIZATION
		    // ORIGINAL CODE
		    //e = e ? e : window.event;
      		//cur_x = (e.clientX) ? e.clientX : cur_x;
      		//cur_y = (e.clientY) ? e.clientY : cur_y;

			left_pos = cur_x + settings.offset_left + scrollPos['scrollLeft'];
			top_pos = cur_y + settings.offset_top + scrollPos['scrollTop'];

			$('.prettyLoader').css({
				'top':top_pos,
				'left':left_pos
			});
		}

		$.prettyLoader.show = function (delay) {

            //CUSTOMIZATON:
		    if ($('.prettyLoader').length > 0) return;
		    prettyLoaderOn = true;
            //END CUSTOMIZATION
		    // ORIGINAL: if ($('.prettyLoader').size() > 0) return;

			// Get the scroll position
			scrollPos = _getScroll();

			// Build the loader container
			$('<div></div>')
				.addClass('prettyLoader')
				.addClass('prettyLoader_'+ settings.theme)
				.appendTo('body')
				.hide();

			// No png for IE6...sadly :(
			//if($.browser.msie && $.browser.version == 6)
			//	$('.prettyLoader').addClass('pl_ie6');

			// Build the loader image
			$('<img />')
				.attr('src',settings.loader)
				.appendTo('.prettyLoader');

			// Show it!
			$('.prettyLoader').fadeIn(settings.animation_speed);

		    // ORIGINAL CODE:
			//$(document).bind('click', $.prettyLoader.positionLoader);
			//$(document).bind('mousemove',$.prettyLoader.positionLoader);
			//$(window).scroll(function(){ scrollPos = _getScroll(); $(document).triggerHandler('mousemove'); });

		    //CUSTOMIZATON:
			$.prettyLoader.positionLoader(null); // force show
		    //END CUSTOMIZATION

			delay = (delay) ? delay : settings.delay;

			if(delay){
				setTimeout(function(){ $.prettyLoader.hide() }, delay);
			}
		};

		$.prettyLoader.hide = function () {
		    // ORIGINAL CODE:
		    //$(document).unbind('click', $.prettyLoader.positionLoader);
		    //CUSTOMIZATON:
		    prettyLoaderOn = false;
		    //END CUSTOMIZATION
            // ORIGINAL CODE:
			//$(document).unbind('mousemove',$.prettyLoader.positionLoader);

		    //CUSTOMIZATON:
		    //END CUSTOMIZATION
		    // ORIGINAL: $(window).unbind('scroll');

			$('.prettyLoader').fadeOut(settings.animation_speed,function(){
				$(this).remove();
			});
		};

		function _getScroll(){
			if (self.pageYOffset) {
				return {scrollTop:self.pageYOffset,scrollLeft:self.pageXOffset};
			} else if (document.documentElement && document.documentElement.scrollTop) { // Explorer 6 Strict
				return {scrollTop:document.documentElement.scrollTop,scrollLeft:document.documentElement.scrollLeft};
			} else if (document.body) {// all other Explorers
				return {scrollTop:document.body.scrollTop,scrollLeft:document.body.scrollLeft};
			};
		};

		return this;
	};

})(jQuery);

