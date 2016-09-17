/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.ResponseFilter
{
    /// <summary>
    /// The class responsible for output (HTML) compression.
    /// </summary>
    public class WhiteSpaceResponseFilter : MemoryStream
    {
        private readonly Stream _outputStream = null;

        public WhiteSpaceResponseFilter(YetaWFManager manager, Stream output) {
            _outputStream = output;
            Manager = manager;
        }
        protected YetaWFManager Manager { get; private set; }

        public override void Flush() {
            base.Flush();
            if (_buffer == "") return;

            string s = Compress(Manager, _buffer);
            _outputStream.Write(Encoding.UTF8.GetBytes(s), 0, Encoding.UTF8.GetByteCount(s));

            _buffer = "";
        }
        public override void Write(byte[] buffer, int offset, int count) {
            _buffer += Encoding.UTF8.GetString(buffer);
        }

        private string _buffer = "";

        private bool Aggressive { get; set; }

        private WhiteSpaceResponseFilter() {
            Aggressive = true;
        }

        // COMPRESS
        // COMPRESS
        // COMPRESS

        /// <summary>
        /// Compress the HTML input buffer which is normally the complete page.
        /// </summary>
        /// <param name="manager">The Manager instance.</param>
        /// <param name="inputBuffer">The input buffer containing HTML to be optimized.</param>
        /// <returns>Removes excessive whitespace, optimizes javascript while preserving Texarea and Pre tag contents.
        ///
        /// Optimizing is very aggressive and removes all whitespace between tags. This can cause unexpected side-effect. For example,
        /// when using spaces to add distance between objects (usually in templates, these will be lost. Such spacing must be accomplished using
        /// Css, not space character litter.
        ///
        /// Inside areas marked <!--LazyWSF--> and <!--LazyWSFEnd--> (Text Modules (display only)), whitespace between tags is preserved.
        /// Inside pre and textarea tags no optimization is performed.
        /// </returns>
        public static string Compress(YetaWFManager manager, string inputBuffer) {
            WhiteSpaceResponseFilter wsf = new WhiteSpaceResponseFilter();
            string output = wsf.ProcessScriptInput(manager, inputBuffer);
#if DEBUG
            output += string.Format("<!-- WhitespaceFilter optimized from {0} bytes to {1} -->", inputBuffer.Length, output.Length);
#endif
            return output;
        }

        private static readonly Regex scriptRe = new Regex("^(?'start'.*?)(?'scripttag'<script[^>]*?>)(?'script'.*?)</script\\s*>\\s*(?'end'.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private string ProcessScriptInput(YetaWFManager manager, string inputBuffer) {
            // We're in html (optimize scripts)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for ( ; ; ) {
                Match m = scriptRe.Match(contentInBuffer);
                if (!m.Success)
                    break;
                output.Append(ProcessTextModuleInput(m.Groups["start"].Value));
                output.Append(ProcessTextModuleInput(m.Groups["scripttag"].Value));
                string script = ScriptManager.TrimScript(manager, m.Groups["script"].Value);
                if (!string.IsNullOrEmpty(script)) {
                    //output.Append("\n//<![CDATA[\n");
                    output.Append(script);
                    //output.Append("\n//]]>\n");
                }
                output.Append("</script>");
                contentInBuffer = m.Groups["end"].Value;
            }
            output.Append(ProcessTextModuleInput(contentInBuffer));
            return output.ToString();
        }

        private static readonly Regex textModRe = new Regex(
            string.Format("^(?'start'.*?){0}(?'wsf'.*?){1}(?'end'.*)$", Regex.Escape(Globals.LazyHTMLOptimization), Regex.Escape(Globals.LazyHTMLOptimizationEnd)),
            RegexOptions.Compiled | RegexOptions.Singleline);

        private string ProcessTextModuleInput(string inputBuffer) {
            // We're in html (no scripts)
            // find all text modules (really yt_textarea t_display) output. We don't want to optimize these aggressively.
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for ( ; ; ) {
                Match m = textModRe.Match(contentInBuffer);
                if (!m.Success)
                    break;
                output.Append(ProcessTextAreaInput(m.Groups["start"].Value));
                Aggressive = true;
                output.Append(ProcessTextAreaInput(m.Groups["wsf"].Value));
                Aggressive = false;
                contentInBuffer = m.Groups["end"].Value;
            }
            output.Append(ProcessTextAreaInput(contentInBuffer));
            return output.ToString();
        }
        private static readonly Regex textareaRe = new Regex("^(?'start'.*?)\\s*(?'textareatag'<textarea[^>]*?>)(?'textarea'.*?)</textarea\\s*>\\s*(?'end'.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private string ProcessTextAreaInput(string inputBuffer)
        {
            // We're in html (no scripts)
            // skip <textarea> (we can't optimize these as otherwise source editing doesn't reflect what user entered)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for ( ; ; ) {
                Match m = textareaRe.Match(contentInBuffer);
                if (!m.Success)
                    break;
                output.Append(ProcessPreInput(m.Groups["start"].Value));
                output.Append(ProcessPreInput(m.Groups["textareatag"].Value));
                output.Append(m.Groups["textarea"].Value); // unmodified
                output.Append("</textarea>");
                contentInBuffer = m.Groups["end"].Value;
            }
            output.Append(ProcessPreInput(contentInBuffer));
            return output.ToString();
        }

        private static readonly Regex preRe = new Regex("^(?'start'.*?)\\s*(?'pretag'<pre[^>]*?>)(?'pre'.*?)</pre\\s*>\\s*(?'end'.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private string ProcessPreInput(string inputBuffer)
        {
            // We're in html (no scripts)
            // skip <pre> (we can't optimize these as otherwise formatted output doesn't reflect what user entered)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for (;;) {
                Match m = preRe.Match(contentInBuffer);
                if (!m.Success)
                    break;
                output.Append(ProcessRemainingInput(m.Groups["start"].Value));
                output.Append(ProcessRemainingInput(m.Groups["pretag"].Value));
                output.Append(m.Groups["pre"].Value); // unmodified
                output.Append("</pre>");
                contentInBuffer = m.Groups["end"].Value;
            }
            output.Append(ProcessRemainingInput(contentInBuffer));
            return output.ToString();
        }

        private static readonly Regex _tabsRe = new Regex("\\t", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _carriageReturnSafeRe = new Regex(">\\s*\\r\\n\\s*<", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _carriageReturn1SafeRe = new Regex("\\s*\\r\\n\\s*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _carriageReturn2SafeRe = new Regex("\\s*\\r\\s*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _carriageReturn3SafeRe = new Regex("\\s*\\n\\s*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _spaceBetweenTagsSafeRe = new Regex(">\\s+<", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _LeadWSSafeRe = new Regex("$\\s+", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _TrailWSSafeRe = new Regex("\\s+^", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex _spaceBetweenDivsUnsafeRe = new Regex(">\\s+<", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _multipleSpaces = new Regex("  ", RegexOptions.Compiled | RegexOptions.Multiline);

        private string ProcessRemainingInput(string inputBuffer) {
            if (inputBuffer == "") return "";
            inputBuffer = _tabsRe.Replace(inputBuffer, " ");
            inputBuffer = _carriageReturnSafeRe.Replace(inputBuffer, "> <");
            inputBuffer = _carriageReturn1SafeRe.Replace(inputBuffer, " ");
            inputBuffer = _carriageReturn2SafeRe.Replace(inputBuffer, " ");
            inputBuffer = _carriageReturn3SafeRe.Replace(inputBuffer, " ");
            inputBuffer = _spaceBetweenTagsSafeRe.Replace(inputBuffer, "> <");
            inputBuffer = _LeadWSSafeRe.Replace(inputBuffer, " ");
            inputBuffer = _TrailWSSafeRe.Replace(inputBuffer, " ");
            if (Aggressive) {
                while (_multipleSpaces.IsMatch(inputBuffer))
                    inputBuffer = _multipleSpaces.Replace(inputBuffer, " ");
                inputBuffer = _spaceBetweenDivsUnsafeRe.Replace(inputBuffer, "><");
            }
            return inputBuffer;
        }
    }
}