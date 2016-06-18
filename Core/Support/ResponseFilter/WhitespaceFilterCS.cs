/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.ResponseFilter
{
    public class WhiteSpaceResponseFilter : MemoryStream
    {
        private readonly Stream _outputStream = null;

        public WhiteSpaceResponseFilter(YetaWFManager manager, Stream output)
        {
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

        // COMPRESS
        // COMPRESS
        // COMPRESS

        private static readonly Regex scriptRe = new Regex("^(?'start'.*?)(?'scripttag'<script[^>]*?>)(?'script'.*?)</script\\s*>(?'end'.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static string Compress(YetaWFManager manager, string inputBuffer) {
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
again:
            Match m = scriptRe.Match(contentInBuffer);
            if (m.Success) {
                output.Append(ProcessTextAreaInput(m.Groups["start"].Value));
                output.Append(ProcessTextAreaInput(m.Groups["scripttag"].Value));
                string script = ScriptManager.TrimScript(manager, m.Groups["script"].Value);
                if (!string.IsNullOrEmpty(script)) {
                    //output.Append("\n//<![CDATA[\n");
                    output.Append(script);
                    //output.Append("\n//]]>\n");
                }
                output.Append("</script>");
                contentInBuffer = m.Groups["end"].Value;
                goto again;
            }
            output.Append(ProcessTextAreaInput(contentInBuffer));
            return output.ToString();
        }

        private static readonly Regex textareaRe = new Regex("^(?'start'.*?)\\s*(?'textareatag'<textarea[^>]*?>)(?'textarea'.*?)</textarea\\s*>\\s*(?'end'.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static string ProcessTextAreaInput(string inputBuffer)
        {
            // We're in html (no scripts)
            // skip <textarea> (we can't optimize these as otherwise source editing doesn't reflect what user entered)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
again:
            Match m = textareaRe.Match(contentInBuffer);
            if (m.Success) {
                output.Append(ProcessPreInput(m.Groups["start"].Value));
                output.Append(ProcessPreInput(m.Groups["textareatag"].Value));
                output.Append(m.Groups["textarea"].Value); // unmodified
                output.Append("</textarea>");
                contentInBuffer = m.Groups["end"].Value;
                goto again;
            }
            output.Append(ProcessPreInput(contentInBuffer));
            return output.ToString();
        }

        private static readonly Regex preRe = new Regex("^(?'start'.*?)\\s*(?'pretag'<pre[^>]*?>)(?'pre'.*?)</pre\\s*>\\s*(?'end'.*)$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static string ProcessPreInput(string inputBuffer)
        {
            // We're in html (no scripts)
            // skip <pre> (we can't optimize these as otherwise formatted output doesn't reflect what user entered)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
again:
            Match m = preRe.Match(contentInBuffer);
            if (m.Success)
            {
                output.Append(ProcessRemainingInput(m.Groups["start"].Value));
                output.Append(ProcessRemainingInput(m.Groups["pretag"].Value));
                output.Append(m.Groups["pre"].Value); // unmodified
                output.Append("</pre>");
                contentInBuffer = m.Groups["end"].Value;
                goto again;
            }
            output.Append(ProcessRemainingInput(contentInBuffer));
            return output.ToString();
        }

        private static readonly Regex _tabsRe = new Regex("\\t", RegexOptions.Compiled | RegexOptions.Multiline);
        //private static readonly Regex _carriageReturn1Re = new Regex(">\\r\\n<", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _carriageReturn2Re = new Regex(">\\s*\\r\\n\\s*<", RegexOptions.Compiled | RegexOptions.Multiline);
        //private static readonly Regex _carriageReturn1SafeRe = new Regex("\\s*\\r\\n\\s*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _carriageReturn2SafeRe = new Regex("\\s*\\r\\s*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _carriageReturn3SafeRe = new Regex("\\s*\\n\\s*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _LeadWSRe = new Regex("$\\s+", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _TrailWSRe = new Regex("\\s+^", RegexOptions.Compiled | RegexOptions.Multiline);
        //private static readonly Regex _multipleSpaces = new Regex("  ", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _spaceBetweenTags = new Regex(">\\s+<", RegexOptions.Compiled | RegexOptions.Multiline);

        private static string ProcessRemainingInput(string inputBuffer)
        {
            // strip out all whitespace... kill all tabs, replace carriage returns with a space, and compress multiple spaces
            inputBuffer = _tabsRe.Replace(inputBuffer, string.Empty);
            //strInput = _carriageReturn1Re.Replace(strInput, "><");
            inputBuffer = _carriageReturn2Re.Replace(inputBuffer, "> <");
            //strInput = _carriageReturn1SafeRe.Replace(strInput, " ");
            inputBuffer = _carriageReturn2SafeRe.Replace(inputBuffer, " ");
            inputBuffer = _carriageReturn3SafeRe.Replace(inputBuffer, " ");
            //while (_multipleSpaces.IsMatch(strInput))
            //    strInput = _multipleSpaces.Replace(strInput, " ");
            inputBuffer = _spaceBetweenTags.Replace(inputBuffer, "> <");
            inputBuffer = _LeadWSRe.Replace(inputBuffer, " ");
            inputBuffer = _TrailWSRe.Replace(inputBuffer, " ");
            return inputBuffer;
        }
    }
}