/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.IO;
using System.Text;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.ResponseFilter {
    /// <summary>
    /// The class responsible for output (HTML) compression.
    /// </summary>
    /// <remarks>
    /// Enabling compression doesn't really help much. YetaWF generates fairly compact HTML so savings are typically just 1-10%.
    /// This code does JavaScript compression and eliminates excessive spacing (line " ", \r,\n), while preserving
    /// formatting in &lt;textarea&gt;, &lt;pre&gt; and &lt;script&gt; tags.
    /// </remarks>
    public class WhiteSpaceResponseFilter : MemoryStream {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Defines whether aggressive optimization is used.
        /// </summary>
        /// <remarks>
        /// Currently aggressive optimization removes whitespace between tags.
        /// </remarks>
        private bool Aggressive { get; set; }

        private WhiteSpaceResponseFilter() {
            Aggressive = true;
        }

        // COMPRESS
        // COMPRESS
        // COMPRESS

        /// <summary>
        /// Compresses the HTML input buffer which is normally the complete page.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML to be optimized.</param>
        /// <returns>Returns the compressed output.</returns>
        /// <remarks>Removes excessive whitespace, optimizes JavaScript while preserving textarea and pre tag contents.
        ///
        /// Optimizing is very aggressive and removes all whitespace between tags. This can cause unexpected side-effect. For example,
        /// when using spaces to add distance between objects (usually in templates, these will be lost. Such spacing must be accomplished using
        /// CSS, not space character litter.
        ///
        /// Inside areas marked &lt;!--LazyWSF--&gt; and &lt;!--LazyWSFEnd--&gt; (Text Modules (display only)), whitespace between tags is preserved.
        /// Inside pre and textarea tags no optimization is performed.
        /// </remarks>
        public static string Compress(string inputBuffer) {
            // only compress when deployed
            if (!YetaWFManager.Deployed) return inputBuffer;
            // if no compression is requested, still compress static pages (overriding no compression)
            if (!Manager.CurrentSite.Compression && !Manager.RenderStaticPage)
                return inputBuffer;
            using (WhiteSpaceResponseFilter wsf = new WhiteSpaceResponseFilter()) {
                string output = wsf.ProcessAllInputCheckLazy(inputBuffer).ToString();
#if DEBUG
                output += string.Format("<!-- WhitespaceFilter optimized from {0} bytes to {1} -->", inputBuffer.Length, output.Length);
#endif
                return output;
            }
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
                    contentInBuffer = contentInBuffer.Substring(ix);

                    ix = contentInBuffer.IndexOf(Globals.LazyHTMLOptimizationEnd);
                    if (ix >= 0) {
                        ix += Globals.LazyHTMLOptimizationEnd.Length;
                        Aggressive = false;
                        output.Append(ProcessScriptInput(contentInBuffer.Substring(0, ix)));
                        Aggressive = true;
                        contentInBuffer = contentInBuffer.Substring(ix);
                    } else
                        break;
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
        /// <summary>Find &lt;script&gt;>...&lt;/script&gt; and compress tags and JavaScript.</summary>
        private StringBuilder ProcessScriptInput(string inputBuffer) {
            // We're in html (optimize scripts)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for (;;) {
                int ix = contentInBuffer.IndexOf("<script");
                if (ix >= 0) {
                    output.Append(ProcessTextAreaInput(contentInBuffer.Substring(0, ix)));
                    contentInBuffer = contentInBuffer.Substring(ix);
                    ix = contentInBuffer.IndexOf("</script>");
                    if (ix >= 0) {
                        ix += "</script>".Length;
                        string script = ScriptManager.TrimScript(Manager, contentInBuffer.Substring(0, ix));
                        if (!string.IsNullOrEmpty(script))
                            output.Append(script);
                        contentInBuffer = contentInBuffer.Substring(ix);
                    } else
                        break;
                } else
                    break;
            }
            output.Append(ProcessTextAreaInput(contentInBuffer));
            return output;
        }

        /// <summary>
        /// Compress the HTML input buffer, looking for &lt;textarea&gt;...&lt;/textarea&gt;.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML.</param>
        /// <returns>Compressed output.</returns>
        /// <summary>Find &lt;textarea&gt;>...&lt;/textarea&gt; and compress tags. Textarea contents are not compressed so formatting is preserved.</summary>
        private StringBuilder ProcessTextAreaInput(string inputBuffer) {
            // We're in html (no scripts)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for (;;) {
                int ix = contentInBuffer.IndexOf("<textarea");
                if (ix >= 0) {
                    output.Append(ProcessPreInput(contentInBuffer.Substring(0, ix)));
                    contentInBuffer = contentInBuffer.Substring(ix);

                    ix = contentInBuffer.IndexOf("</textarea>");
                    if (ix >= 0) {
                        ix += "</textarea>".Length;
                        output.Append(contentInBuffer.Substring(0, ix));// unmodified
                        contentInBuffer = contentInBuffer.Substring(ix);
                    } else
                        break;
                } else
                    break;
            }
            output.Append(ProcessPreInput(contentInBuffer));
            return output;
        }

        /// <summary>
        /// Compress the HTML input buffer, looking for &lt;pre&gt;...&lt;/pre&gt;.
        /// </summary>
        /// <param name="inputBuffer">The input buffer containing HTML.</param>
        /// <returns>Compressed output.</returns>
        /// <summary>Find &lt;pre&gt;>...&lt;/pre&gt; and compress tags. Pre contents are not compressed so formatting is preserved.</summary>
        private StringBuilder ProcessPreInput(string inputBuffer) {
            // We're in html (no scripts, no textarea)
            string contentInBuffer = inputBuffer;
            StringBuilder output = new StringBuilder();
            for (;;) {
                int ix = contentInBuffer.IndexOf("<pre");
                if (ix >= 0) {
                    output.Append(ProcessRemainingInput(contentInBuffer.Substring(0, ix)));
                    contentInBuffer = contentInBuffer.Substring(ix);

                    ix = contentInBuffer.IndexOf("</pre>");
                    if (ix >= 0) {
                        ix += "</pre>".Length;
                        output.Append(contentInBuffer.Substring(0, ix));// unmodified
                        contentInBuffer = contentInBuffer.Substring(ix);
                    } else
                        break;
                } else
                    break;
            }
            output.Append(ProcessRemainingInput(contentInBuffer));
            return output;
        }

        private string ProcessRemainingInput(string inputBuffer) {
            if (string.IsNullOrEmpty(inputBuffer)) return "";
            StringBuilder sb = new StringBuilder(inputBuffer);
            sb.Replace('\t', ' ');
            sb.Replace('\r', ' ');
            sb.Replace('\n', ' ');
            for (int oldLen = sb.Length; ;) {
                sb.Replace("  ", " ");
                int newLen = sb.Length;
                if (oldLen == newLen)
                    break;
                oldLen = newLen;
            }
            if (Aggressive)
                sb.Replace("> <", "><");
            return sb.ToString();
        }
    }
}