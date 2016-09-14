/**
 * @license Copyright (c) 2003-2014, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.html or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function( config ) {
	// Define changes to default configuration here. For example:
	// config.language = 'fr'; TODO: deal with language sometime
    // config.uiColor = '#AADC6E';

    // allow <i></i> as used with font-awesome
    config.protectedSource.push(/<i[^>]*><\/i>/g);

    config.readOnly = true;
};
