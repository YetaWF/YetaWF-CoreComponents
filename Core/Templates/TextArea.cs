/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class TextArea : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;

            // Syntax highlighter
            scripts.AddLocalization("TextArea", "msg_expandSource", this.__ResStr("expandSource", "+ expand source"));
            scripts.AddLocalization("TextArea", "msg_help", this.__ResStr("help", "?"));
            scripts.AddLocalization("TextArea", "msg_alert", this.__ResStr("alert", "SyntaxHighlighter\n\n"));
            scripts.AddLocalization("TextArea", "msg_noBrush", this.__ResStr("noBrush", "Can't find brush for "));
            scripts.AddLocalization("TextArea", "msg_brushNotHtmlScript", this.__ResStr("brushNotHtmlScript", "Brush wasn't made for html-script option "));
            scripts.AddLocalization("TextArea", "msg_viewSource", this.__ResStr("viewSource", "View Source"));
            scripts.AddLocalization("TextArea", "msg_copyToClipboard", this.__ResStr("copyToClipboard", "Copy to Clipboard"));
            scripts.AddLocalization("TextArea", "msg_copyToClipboardConfirmation", this.__ResStr("copyToClipboardConfirmation", "The code has been copied to your clipboard."));
            scripts.AddLocalization("TextArea", "msg_print", this.__ResStr("print", "Print"));

        }
    }
}
