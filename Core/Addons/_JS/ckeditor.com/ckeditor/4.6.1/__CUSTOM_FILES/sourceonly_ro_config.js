/**
 * @license Copyright (c) 2003-2014, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.html or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function (config) {
    // Define changes to default configuration here. For example:
    // config.language = 'fr'; TODO: deal with language sometime
    // config.uiColor = '#AADC6E';
    config.startupMode = 'source';
    config.removePlugins = 'indent,outdent,a11yhelp,basicstyles,bidi,blockquote,clipboard,colorbutton,colordialog,contextmenu,dialogadvtab,div,elementspath,enterkey,entities,filebrowser,find,flash,floatingspace,font,format,forms,horizontalrule,htmlwriter,iframe,image,indentblock,indentlist,justify,language,link,list,liststyle,magicline,newpage,pagebreak,pastefromword,pastetext,preview,print,removeformat,resize,save,scayt,selectall,showblocks,showborders,smiley,specialchar,stylescombo,tab,table,tabletools,templates,undo,wsc,wysiwygarea';
    // allow <i></i> as used with font-awesome
    config.protectedSource.push(/<i[^>]*><\/i>/g);

    config.readOnly = true;
}