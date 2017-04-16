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
            else
                return text + "&";
        }
    }
}
