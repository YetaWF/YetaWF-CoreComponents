/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;

namespace YetaWF.Core.Extensions {

    public static partial class Extensions {

        public static string Truncate(this string text, int maxLength) {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }
        public static string TruncateWithEllipse(this string text, int maxLength) {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
        public static string TruncateStart(this string text, string trim) {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.StartsWith(trim, StringComparison.OrdinalIgnoreCase)) return text.Substring(trim.Length);
            return text;
        }
        public static string TrimEnd(this string text, string trim) {
            while (text.EndsWith(trim))
                text = text.Substring(0, text.Length - trim.Length);
            return text;
        }
        public static string ReplaceStart(this string text, string startText, string newText) {
            if (text.StartsWith(startText, StringComparison.OrdinalIgnoreCase))
                return text.ReplaceFirst(startText, newText);
            return text;
        }
        public static string Mid(ref string s, int index, char c) {
            char[] chars = s.ToCharArray();
            chars[index] = c;
            s = new string(chars);
            return s;
        }
        public static string RemoveStartingAt(this string text, char c) {
            int ix = text.IndexOf(c);
            if (ix < 0) return text;
            return text.Truncate(ix);
        }
        public static string RemoveStartingAtLast(this string text, char c) {
            int ix = text.LastIndexOf(c);
            if (ix < 0) return text;
            return text.Truncate(ix);
        }
        public static string RemoveEndingAt(this string text, char c) {
            int ix = text.IndexOf(c);
            if (ix < 0) return text;
            return text.Substring(ix);
        }
        public static string RemoveEndingAtIncluding(this string text, char c) {
            int ix = text.IndexOf(c);
            if (ix < 0) return text;
            if (ix >= text.Length) return "";
            return text.Substring(ix + 1);
        }
        public static string RemoveEndingAtLast(this string text, char c) {
            int ix = text.LastIndexOf(c);
            if (ix < 0) return text;
            return text.Substring(ix);
        }
        public static string RemoveEndingAtLastIncluding(this string text, char c) {
            int ix = text.LastIndexOf(c);
            if (ix < 0) return text;
            if (ix >= text.Length) return "";
            return text.Substring(ix + 1);
        }
        public static string ReplaceFirst(this string text, string search, string replace) {
            int index = text.IndexOf(search, StringComparison.Ordinal);
            if (index < 0) return text;
            return text.Substring(0, index) + replace + text.Substring(index + search.Length);
        }
        public static bool ContainsIgnoreCase(this string text, string search) {
            return text.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Remove part of a string starting at <para>start</para> searching for the end by matching a character in the string to the list of strings in <para>endChars</para>.
        /// The search starts at <para>offset</para>.
        /// </summary>
        /// <returns>Returns the string with the substring removed.</returns>
        public static string RemoveUpTo(this string text, int start, int offset, List<char> endChars) {
            for (int len = text.Length; offset < len; ++offset) {
                char c = text[offset];
                if (endChars.Contains(c)) {
                    if (start > 0)
                        return text.Substring(0, start) + text.Substring(offset);
                    else
                        return text.Substring(offset);
                }
            }
            return text;
        }
        public static bool IsHttp(this string text) {
            return text.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsHttps(this string text) {
            return text.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsAbsoluteUrl(this string text) {
            return text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("//", StringComparison.OrdinalIgnoreCase);
        }
        public static string AddQSSeparator(this string text) {
            if (string.IsNullOrWhiteSpace(text))
                return "?";
            if (text.Contains("?", StringComparison.OrdinalIgnoreCase))
                return "&";
            else
                return "?";
        }
        public static string AddSlashIfNone(this string text) {
            if (string.IsNullOrWhiteSpace(text))
                return "/";
            if (!text.EndsWith("/"))
                return $"{text}/";
            else
                return text;
        }
        public static string RemoveTrailingSlash(this string text) {
            if (string.IsNullOrWhiteSpace(text))
                return text;
            if (text.EndsWith("/"))
                return text.Substring(0, text.Length - 1);
            else
                return text;
        }
        public static string AddUrlCacheBuster(this string text, string? cacheBuster) {
            if (string.IsNullOrWhiteSpace(cacheBuster)) return "";
            if (text == null) return "";
            if (text.Contains("__yVrs=", StringComparison.OrdinalIgnoreCase) || text.Contains("/__yVrs/", StringComparison.OrdinalIgnoreCase)) return "";
            if (text.Contains("?", StringComparison.OrdinalIgnoreCase))
                return string.Format("&__yVrs={0}", cacheBuster);
            else
                return string.Format("?__yVrs={0}", cacheBuster);
        }
        public static string AddUrlCacheBusterSegment(this string text, string? cacheBuster) {
            if (string.IsNullOrWhiteSpace(cacheBuster)) return "";
            if (text == null) return "";
            if (text.Contains("/__yVrs/", StringComparison.OrdinalIgnoreCase)) return "";
            return string.Format("/__yVrs/{0}", cacheBuster);
        }
    }
}
