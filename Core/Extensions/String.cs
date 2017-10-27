/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF.Core.Extensions {

    public static class Extensions {

        public static string Truncate(this string text, int maxLength) {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }
        public static string TruncateStart(this string text, string trim) {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.StartsWith(trim)) return text.Substring(trim.Length);
            return text;
        }
        public static string TrimEnd(this string text, string trim) {
            while (text.EndsWith(trim))
                text = text.Substring(0, text.Length - trim.Length);
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
            int index = text.IndexOf(search);
            if (index < 0) return text;
            return text.Substring(0, index) + replace + text.Substring(index + search.Length);
        }
        public static bool ContainsIgnoreCase(this string text, string search) {
            return text.ToLower().Contains(search.ToLower());
        }
        public static bool IsHttp(this string text) {
            return text.StartsWith("http://");
        }
        public static bool IsHttps(this string text) {
            return text.StartsWith("https://");
        }
        public static bool IsAbsoluteUrl(this string text) {
            return text.StartsWith("http://") || text.StartsWith("https://") || text.StartsWith("//");
        }
        public static string AddQSSeparator(this string text) {
            if (string.IsNullOrWhiteSpace(text))
                return "?";
            if (text.Contains("?"))
                return "&";
            else
                return "?";
        }
        public static string AddUrlCacheBuster(this string text, string cacheBuster) {
            if (string.IsNullOrWhiteSpace(cacheBuster)) return "";
            if (text == null) return "";
            if (text.Contains("__yVrs=")|| text.Contains("/__yVrs/")) return "";
            if (text.Contains("?"))
                return string.Format("&__yVrs={0}", cacheBuster);
            else
                return string.Format("?__yVrs={0}", cacheBuster);
        }
        public static string AddUrlCacheBusterSegment(this string text, string cacheBuster) {
            if (string.IsNullOrWhiteSpace(cacheBuster)) return "";
            if (text == null) return "";
            if (text.Contains("/__yVrs/")) return "";
            return string.Format("/__yVrs/{0}", cacheBuster);
        }
    }
}
