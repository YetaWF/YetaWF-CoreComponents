/**
 * @license Copyright (c) 2003-2014, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.html or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function (config) {
    // Define changes to default configuration here. For example:
    // config.language = 'fr'; TODO: deal with language sometime
    // config.uiColor = '#AADC6E';
    config.startupMode = 'source';
    config.removePlugins = 'tableselect...,tabletools,indent,outdent,a11yhelp,basicstyles,bidi,blockquote,clipboard,colorbutton,colordialog,contextmenu,copyformatting,dialogadvtab,div,elementspath,enterkey,entities,filebrowser,find,flash,floatingspace,font,format,forms,horizontalrule,htmlwriter,iframe,image,indentblock,indentlist,justify,language,link,list,liststyle,magicline,newpage,pagebreak,pastefromword,pastetext,preview,print,removeformat,resize,save,scayt,selectall,showblocks,showborders,smiley,specialchar,stylescombo,tab,table,templates,undo,uploadwidget,uploadfile,uploadimage,wsc,widget,wysiwygarea';
    // keep about,maximize,toolbar,sourcearea,
    config.enterMode = CKEDITOR.ENTER_BR; // inserts <br /> instead of <p></p>
    config.filebrowserImageBrowseLinkUrl = '/__CKEditor/ImageBrowseLinkUrl';
    config.filebrowserImageBrowseUrl = '/__CKEditor/ImageBrowseLinkUrl';
    // allow <i></i> as used with font-awesome
    config.protectedSource.push(/<i[^>]*><\/i>/g);
}
