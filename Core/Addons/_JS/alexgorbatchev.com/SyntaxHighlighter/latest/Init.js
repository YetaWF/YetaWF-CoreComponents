/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

/* Syntax highlighter autoload */

var AlexGorbatchevCom_SyntaxHighlighter = {};

var _AlexGorbatchevCom_SyntaxHighlighter = {};

AlexGorbatchevCom_SyntaxHighlighter.Init = function (addon) {

    // Autoload doesn't work in ajax, so we have to load all required brushes in page
    // We simply add them to the Addon's filelistJS.txt  TODO: Could add a config page to configure all available brushes
    //_AlexGorbatchevCom_SyntaxHighlighter.url = addon;

    //function path() {
    //    var args = arguments,
    //        result = []
    //    ;

    //    for (var i = 0; i < args.length; i++)
    //        result.push(args[i].replace('@', _AlexGorbatchevCom_SyntaxHighlighter.url + 'scripts/'));

    //    return result
    //};

    //_AlexGorbatchevCom_SyntaxHighlighter.path = path(
    //  'applescript            @shBrushAppleScript.js',
    //  'actionscript3 as3      @shBrushAS3.js',
    //  'bash shell             @shBrushBash.js',
    //  'coldfusion cf          @shBrushColdFusion.js',
    //  'cpp c                  @shBrushCpp.js',
    //  'c# c-sharp csharp      @shBrushCSharp.js',
    //  'css                    @shBrushCss.js',
    //  'delphi pascal          @shBrushDelphi.js',
    //  'diff patch pas         @shBrushDiff.js',
    //  'erl erlang             @shBrushErlang.js',
    //  'groovy                 @shBrushGroovy.js',
    //  'java                   @shBrushJava.js',
    //  'jfx javafx             @shBrushJavaFX.js',
    //  'js jscript javascript  @shBrushJScript.js',
    //  'perl pl                @shBrushPerl.js',
    //  'php                    @shBrushPhp.js',
    //  'text plain             @shBrushPlain.js',
    //  'py python              @shBrushPython.js',
    //  'ruby rails ror rb      @shBrushRuby.js',
    //  'sass scss              @shBrushSass.js',
    //  'scala                  @shBrushScala.js',
    //  'sql                    @shBrushSql.js',
    //  'vb vbnet               @shBrushVb.js',
    //  'xml xhtml xslt html    @shBrushXml.js'
    //);

    //SyntaxHighlighter.autoloader.apply(null, _AlexGorbatchevCom_SyntaxHighlighter.path);

    SyntaxHighlighter.config.strings.expandSource = YLocs.TextArea.msg_expandSource;
    SyntaxHighlighter.config.strings.help = YLocs.TextArea.msg_help;
    SyntaxHighlighter.config.strings.alert = YLocs.TextArea.msg_alert;
    SyntaxHighlighter.config.strings.noBrush = YLocs.TextArea.msg_noBrush;
    SyntaxHighlighter.config.strings.brushNotHtmlScript = YLocs.TextArea.msg_brushNotHtmlScript;
    SyntaxHighlighter.config.strings.viewSource = YLocs.TextArea.msg_viewSource;
    SyntaxHighlighter.config.strings.copyToClipboard = YLocs.TextArea.msg_copyToClipboard;
    SyntaxHighlighter.config.strings.copyToClipboardConfirmation = YLocs.TextArea.msg_copyToClipboardConfirmation;
    SyntaxHighlighter.config.strings.print = YLocs.TextArea.msg_print;

    SyntaxHighlighter.all();
};
YetaWF_Basics.whenReady.push({
    callback: function ($tag) {
        SyntaxHighlighter.highlight();
    }
});
