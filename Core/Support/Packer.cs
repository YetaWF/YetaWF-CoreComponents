/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Text.RegularExpressions;
using Yahoo.Yui.Compressor;
using YetaWF.Core.Extensions;
using YetaWF.Core.IO;
using YetaWF.Core.Log;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Support {
    public class Packer {

        public Packer() { }

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public enum PackMode {
            JS = 0,
            CSS = 1,
        };

        /// <summary>
        /// Returns whether a file can be compressed.
        /// </summary>
        /// <param name="fullPathUrl">The file Url.</param>
        /// <param name="mode">The file mode.</param>
        /// <returns></returns>
        public bool CanCompress(string fullPathUrl, PackMode mode) {
            if (fullPathUrl.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                    fullPathUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) ||
                    fullPathUrl.StartsWith("//"))
                return false;
            return true;
        }

        /// <summary>
        /// Compile a js/css/less/sass file.
        /// </summary>
        /// <param name="fullPathUrl">The file Url.</param>
        /// <param name="mode">The file mode.</param>
        /// <param name="minify"></param>
        /// <param name="processCharSize"></param>
        /// <param name="dummy"></param>
        /// <param name="MarkNameCompiled"></param>
        /// <returns></returns>
        public string ProcessFile(string fullPathUrl, PackMode mode, bool minify, bool processCharSize, int dummy = 0, bool MarkNameCompiled = true) {

            if (!CanCompress(fullPathUrl, mode))
                return fullPathUrl;

            string fullPath = YetaWFManager.UrlToPhysical(fullPathUrl);
            if (!File.Exists(fullPath))
                throw new InternalError("File {0} not found - can't be processed", fullPath);

            if (mode == PackMode.JS && !minify) return fullPathUrl; // no processing for javascript files when we're not minifying

            string extension = Path.GetExtension(fullPath);

            if (fullPathUrl.EndsWith(".pack" + extension, StringComparison.InvariantCultureIgnoreCase))
                return fullPathUrl;
            if (fullPathUrl.EndsWith(".min" + extension, StringComparison.InvariantCultureIgnoreCase))
                return fullPathUrl;

            string minPathUrl = fullPathUrl;
            minPathUrl = minPathUrl.Remove(minPathUrl.Length - extension.Length);
            if (minify)
                minPathUrl = minPathUrl + (MarkNameCompiled ? Globals.Compiled : "") + ".min";
            string minPathUrlWithCharInfo = minPathUrl;

            bool process = false;
            switch (mode) {
                case PackMode.CSS:
                    if (!fullPathUrl.EndsWith(".css")) {
                        minPathUrl += extension + (MarkNameCompiled ? Globals.Compiled : "");
                        minPathUrlWithCharInfo += extension + (MarkNameCompiled ? Globals.Compiled : "");
                    }
                    if (processCharSize && !fullPathUrl.ContainsIgnoreCase(Globals.NugetContentsUrl)) {
                        // add character size to css
                        process = true;
                        minPathUrlWithCharInfo += string.Format("._ci_{0}_{1}", Manager.CharWidthAvg, Manager.CharHeight);
                    }
                    minPathUrl += ".css";
                    minPathUrlWithCharInfo += ".css";
                    break;
                case PackMode.JS:
                    minPathUrl += ".js";
                    minPathUrlWithCharInfo += ".js";
                    break;
            }

            string minPath = YetaWFManager.UrlToPhysical(minPathUrl);
            string minPathWithCharInfo = YetaWFManager.UrlToPhysical(minPathUrlWithCharInfo);

            // Make sure we don't have multiple threads processing the same file
            StringLocks.DoAction("YetaWF##Packer_" + fullPath, () => {
                if (File.Exists(minPathWithCharInfo)) {
                    if (File.GetLastWriteTimeUtc(minPathWithCharInfo) >= File.GetLastWriteTimeUtc(fullPath)) {
                        minPathUrl = minPathUrlWithCharInfo;
                        return;
                    }
                }
                if (File.Exists(minPath)) {
                    if (File.GetLastWriteTimeUtc(minPath) >= File.GetLastWriteTimeUtc(fullPath))
                        return;
                }
                Logging.AddLog("Processing {0} ({1})", minPath, mode.ToString());
                minPath = ProcessOneFile(fullPath, minPath, minPathWithCharInfo, process, mode, minify);
                if (minPath == null) {// empty file
                    Logging.AddLog("Processed and discarded {0}, because it's empty", fullPath);
                    minPathUrl = null;
                } else
                    minPathUrl = YetaWFManager.PhysicalToUrl(minPath);
            });
            return minPathUrl;
        }

        private string ProcessOneFile(string fullPath, string minPath, string minPathWithCharInfo, bool processCharSize, PackMode mode, bool minify) {
            string text = null;
            switch (mode) {
                default:
                    throw new InternalError("Invalid mode {0}", mode);
                case PackMode.JS: {
                    if (!minify)
                        throw new InternalError("Shouldn't do any processing for Javascript files when minify=false");
                    text = File.ReadAllText(fullPath);
                    text = RemoveDebugCode(text);
                    JavaScriptCompressor jsCompressor = new JavaScriptCompressor();
                    try {
                        text = jsCompressor.Compress(text);
                    } catch (Exception exc) {
                        EcmaScript.NET.EcmaScriptRuntimeException ecmaExcp = exc as EcmaScript.NET.EcmaScriptRuntimeException;
                        if (ecmaExcp != null) {
                            throw new InternalError(Logging.AddErrorLog("An error occurred compiling {0}({1}) - {2}", fullPath, ecmaExcp.LineNumber, ecmaExcp.LineSource));
                        } else {
                            throw new InternalError(Logging.AddErrorLog("An error occurred compiling {0}", fullPath, exc));
                        }
                    }
                    break;
                 }
                case PackMode.CSS:
                    if (minify || processCharSize || fullPath.EndsWith(".scss", StringComparison.InvariantCultureIgnoreCase) || fullPath.EndsWith(".less", StringComparison.InvariantCultureIgnoreCase)) {
                        text = File.ReadAllText(fullPath);
                        if (!string.IsNullOrWhiteSpace(text)) {
                            if (fullPath.EndsWith(".scss", StringComparison.InvariantCultureIgnoreCase))
                                text = CssManager.CompileNSass(fullPath, text);
                            else if (fullPath.EndsWith(".less", StringComparison.InvariantCultureIgnoreCase))
                                text = CssManager.CompileLess(fullPath, text);
                            if (processCharSize) {
                                string newText = ProcessCss(text, Manager.CharWidthAvg, Manager.CharHeight);
                                if (newText != text) {
                                    text = newText;
                                    minPath = minPathWithCharInfo;
                                }
                            }
                            if (minify) {
                                string folder = Path.GetDirectoryName(fullPath);
                                text = ProcessCssImport(text, folder);
                                CssCompressor cssCompressor = new CssCompressor();
                                text = cssCompressor.Compress(text);
                            }
                        }
                    }
                    break;
            }
            if (fullPath != minPath) {
                if (string.IsNullOrWhiteSpace(text)) {
                    // this was an empty file, discard it
                    File.Delete(minPath);
                    minPath = null;
                } else {
                    File.WriteAllText(minPath, text);
                }
            }
            return minPath;
        }

        private static readonly Regex varChRegex = new Regex("(?'num'[0-9\\.]+)\\s*ch(?'delim'(\\s*|;|\\}))", RegexOptions.Compiled | RegexOptions.Multiline);

        public string ProcessCss(string text, int avgCharWidth, int charHeight) {
            // replace all instances of nn ch with pixels
            text = varChRegex.Replace(text, match => ProcessChMatch(match, avgCharWidth));
            return text;
        }
        private string ProcessChMatch(Match match, int avgCharWidth) {
            string num = match.Groups["num"].Value;
            string delim = match.Groups["delim"].Value;
            string pix = match.ToString();
            if (num != ".") {
                try {
                    double f = Convert.ToDouble(num);
                    pix = string.Format("{0}px{1}", Math.Round(f * avgCharWidth, 0), delim);
                } catch (Exception) { }
            }
            return pix;
        }

        private static readonly Regex varJSUseRegex = new Regex(@"^\s*'use\s+strict';\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex varJSDebugRegex = new Regex(@"^.*?/\*debug\*/\s*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        /// <summary>
        /// Remove debug code from javascript
        /// </summary>
        /// <remarks>Removes 'use strict'; and lines with /*DEBUG*/</remarks>
        private string RemoveDebugCode(string fileText) {
            fileText = varJSUseRegex.Replace(fileText, "");// remove use strict;
            fileText = varJSDebugRegex.Replace(fileText, "");// remove /*DEBUG*/ lines
            return fileText;
        }

        private static readonly Regex varImport1Regex = new Regex(@"@import\s+url\(\s*(?'url'['""]?[^'""\)]*?['""])\s*\)(?'rem'[^;]*?);", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex varImport2Regex = new Regex(@"@import\s+(?'url'['""]?[^'""\)]*?['""])(?'rem'[^;]*?);", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Replace all @import url.. with contents of minimized files
        /// </summary>
        private string ProcessCssImport(string text, string folder) {
            text = varImport1Regex.Replace(text, match => ProcessImportMatch(match, folder));
            text = varImport2Regex.Replace(text, match => ProcessImportMatch(match, folder));
            return text;
        }

        private string ProcessImportMatch(Match match, string folder) {
            string url = match.Groups["url"].Value;
            url = url.Trim('"').Trim('\'');
            //string rem = match.Groups["rem"].Value;
            string textMatch = match.ToString();
            if (!url.EndsWith(".css")) return textMatch; // leave as-is if not .css
            if (url.StartsWith("http://") || url.StartsWith("https://")) return textMatch;
            if (url.StartsWith("/")) return textMatch;
            string file = Path.Combine(folder, url);
            string importText = File.ReadAllText(file);
            return ProcessCssImport(importText, Path.GetDirectoryName(file));
        }
    }
}
