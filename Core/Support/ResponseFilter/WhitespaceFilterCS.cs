/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.ResponseFilter {
    /// <summary>
    /// The class responsible for output (HTML) compression.
    /// </summary>
    /// <remarks>
    /// Enabling compression doesn't really help much. YetaWF generates fairly compact html so savings are typically just 1-10%.
    /// This code does javascript compression and eliminates excessive spacing (line " ", \r,\n), while preserving
    /// formatting in &lt;textarea&gt;, &lt;pre&gt; and &lt;script&gt; tags.
    /// </remarks>
    public class WhiteSpaceResponseFilter : MemoryStream {

        protected YetaWFManager Manager { get; private set; }
        private readonly Stream _outputStream = null;

        public WhiteSpaceResponseFilter(YetaWFManager manager, Stream output) {
            _outputStream = output;
            Manager = manager;
        }

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

        /// <summary>
        /// Defines whether aggressive optimization is used.
        /// </summary>
        /// <remarks>
        /// Currently aggressive optimization removes whitespace between tags.
        /// </remarks>
        private bool Aggressive { get; set; }

        private WhiteSpaceResponseFilter(YetaWFManager manager) {
            Manager = manager;
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
        /// <summary>Removes excessive whitespace, optimizes javascript while preserving texarea and pre tag contents.
        ///
        /// Optimizing is very aggressive and removes all whitespace between tags. This can cause unexpected side-effect. For example,
        /// when using spaces to add distance between objects (usually in templates, these will be lost. Such spacing must be accomplished using
        /// Css, not space character litter.
        ///
        /// Inside areas marked &lt;!--LazyWSF--&gt; and &lt;!--LazyWSFEnd--&gt; (Text Modules (display only)), whitespace between tags is preserved.
        /// Inside pre and textarea tags no optimization is performed.
        /// </summary>
        /// <returns>Compressed output.</returns>
        public static string Compress(YetaWFManager manager, string inputBuffer) {
            WhiteSpaceResponseFilter wsf = new WhiteSpaceResponseFilter(manager);
            string output = wsf.ProcessAllInputCheckLazy(inputBuffer).ToString();
#if DEBUG
            output += string.Format("<!-- WhitespaceFilter optimized from {0} bytes to {1} -->", inputBuffer.Length, output.Length);
#endif
            return output;
        }

        /// <summary>
        /// Compress the HTML input buffer, looking for &lt;!--LazyWSF--&gt; or &lt;!--LazyWSFEnd--&gt;.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML.</param>
        /// <returns>Compressed output.</returns>
        /// <summary>Find areas marked &lt;!--LazyWSF--&gt; or &lt;!--LazyWSFEnd--&lt; and set aggressive optimization.</summary>
        private StringBuilder ProcessAllInputCheckLazy(string inputBuffer) {
            // We're in html
            // find all areas mark for lazy optimization. We don't want to optimize these aggressively.
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for (;;) {
                int ix = contentInBuffer.IndexOf(Globals.LazyHTMLOptimization);
                if (ix >= 0) {
                    output.Append(ProcessScriptInput(contentInBuffer.Substring(0, ix)));
                    contentInBuffer = contentInBuffer.Substring(ix + Globals.LazyHTMLOptimization.Length);
                }
                ix = contentInBuffer.IndexOf(Globals.LazyHTMLOptimizationEnd);
                if (ix >= 0) {
                    Aggressive = false;
                    output.Append(ProcessScriptInput(contentInBuffer.Substring(0, ix)));
                    Aggressive = true;
                    contentInBuffer = contentInBuffer.Substring(ix + Globals.LazyHTMLOptimizationEnd.Length);
                } else
                    break;
            }
            output.Append(ProcessScriptInput(contentInBuffer));
            return output;
        }

        /// <summary>
        /// Compress the HTML input buffer, looking for &lt;script&gt;...&lt;/script&gt;.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML.</param>
        /// <returns>Compressed output.</returns>
        /// <summary>Find &lt;script&gt;>...&lt;/script&gt; and compress tags and javascript.</summary>
        private StringBuilder ProcessScriptInput(string inputBuffer) {
            // We're in html (optimize scripts)
            StringBuilder output = new StringBuilder();
            int currStart = 0;
            Match m = scriptRe.Match(inputBuffer);
            for (; m.Success ;) {
                int start = m.Captures[0].Index;
                int end = m.Captures[0].Index + m.Captures[0].Length;
                if (currStart < start)
                    output.Append(ProcessTextAreaInput(inputBuffer.Substring(currStart, start - currStart)));
                output.Append(ProcessTextAreaInput(m.Groups["scripttag"].Value));
                string script = ScriptManager.TrimScript(Manager, m.Groups["script"].Value);
                if (!string.IsNullOrEmpty(script)) {
                    //output.Append("\n//<![CDATA[\n");
                    output.Append(script);
                    //output.Append("\n//]]>\n");
                }
                output.Append("</script>");
                currStart = end;
                m = m.NextMatch();
            }
            output.Append(ProcessTextAreaInput(inputBuffer.Substring(currStart)));
            return output;
        }

        private static readonly Regex scriptRe = new Regex("(?'scripttag'<script[^>]*?>)(?'script'.*?)</script\\s*>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Compress the HTML input buffer, looking for &lt;textarea&gt;...&lt;/textarea&gt;.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML.</param>
        /// <returns>Compressed output.</returns>
        /// <summary>Find &lt;textarea&gt;>...&lt;/textarea&gt; and compress tags. Textarea contents are not compressed so formatting is preserved.</summary>
        private StringBuilder ProcessTextAreaInput(string inputBuffer) {
            // We're in html (no scripts)
            // skip <textarea> (we can't optimize these as otherwise source editing doesn't reflect what user entered)
            StringBuilder output = new StringBuilder();
            int currStart = 0;
            Match m = textareaRe.Match(inputBuffer);
            for (; m.Success ;) {
                int start = m.Captures[0].Index;
                int end = m.Captures[0].Index + m.Captures[0].Length;
                if (currStart < start)
                    output.Append(ProcessPreInput(inputBuffer.Substring(currStart, start - currStart)));
                output.Append(ProcessPreInput(m.Groups["textareatag"].Value));
                output.Append(m.Groups["textarea"].Value); // unmodified
                output.Append("</textarea>");
                currStart = end;
                m = m.NextMatch();
            }
            output.Append(ProcessPreInput(inputBuffer.Substring(currStart)));
            return output;
        }

        private static readonly Regex textareaRe = new Regex("(?'textareatag'<textarea[^>]*?>)(?'textarea'.*?)</textarea\\s*>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Compress the HTML input buffer, looking for &lt;pre&gt;...&lt;/pre&gt;.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML.</param>
        /// <returns>Compressed output.</returns>
        /// <summary>Find &lt;pre&gt;>...&lt;/pre&gt; and compress tags. Pre contents are not compressed so formatting is preserved.</summary>
        private StringBuilder ProcessPreInput(string inputBuffer) {
            // We're in html (no scripts, no textarea)
            // skip <pre> (we can't optimize these as otherwise formatted output doesn't reflect what user entered)
            StringBuilder output = new StringBuilder();
            int currStart = 0;
            Match m = preRe.Match(inputBuffer);
            for (; m.Success ;) {
                int start = m.Captures[0].Index;
                int end = m.Captures[0].Index + m.Captures[0].Length;
                if (currStart < start)
                    output.Append(ProcessRemainingInput(inputBuffer.Substring(currStart, start - currStart)));
                output.Append(ProcessRemainingInput(m.Groups["pretag"].Value));
                output.Append(m.Groups["pre"].Value); // unmodified
                output.Append("</pre>");
                currStart = end;
                m = m.NextMatch();
            }
            output.Append(ProcessRemainingInput(inputBuffer.Substring(currStart)));
            return output;
        }

        private static readonly Regex preRe = new Regex("(?'pretag'<pre[^>]*?>)(?'pre'.*?)</pre\\s*>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// White space compression.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML.</param>
        /// <returns>Compressed output.</returns>
        private string ProcessRemainingInput(string inputBuffer) {
            if (inputBuffer == "") return "";
            inputBuffer = inputBuffer.Replace('\t', ' ');
            inputBuffer = inputBuffer.Replace('\r', ' ');
            inputBuffer = inputBuffer.Replace('\n', ' ');
            while (inputBuffer.Contains("  ")) // multiple spaces -> 1 space
                inputBuffer = inputBuffer.Replace("  ", " ");
            if (Aggressive)
                inputBuffer = inputBuffer.Replace("> <", "><");
            return inputBuffer;
        }
    }
}