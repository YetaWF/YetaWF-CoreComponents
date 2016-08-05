/**
 * @license Copyright (c) 2003-2014, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.html or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function( config ) {
	// Define changes to default configuration here. For example:
	// config.language = 'fr'; TODO: deal with language sometime
    // config.uiColor = '#AADC6E';
    config.removePlugins = 'save,forms,templates,elementspath,resize'; //TODO:? font
    config.enterMode = CKEDITOR.ENTER_BR; // inserts <br /> instead of <p></p>
    // allow <i></i> as used with font-awesome
    config.protectedSource.push(/<i[^>]*><\/i>/g);
};
