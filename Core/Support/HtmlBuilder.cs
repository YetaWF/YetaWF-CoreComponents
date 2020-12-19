/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;

namespace YetaWF.Core.Support {

    /// <summary>
    /// This class is used to build HTML content.
    /// </summary>
    public class HtmlBuilder {

        /// <summary>
        /// Constructor.
        /// </summary>
        public HtmlBuilder() { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="s">A string defining the initial HTML contents.</param>
        public HtmlBuilder(string s) { }

        private readonly StringBuilder _hb = new StringBuilder(4000);

        /// <summary>
        /// Appends a string to the current HTML content.
        /// </summary>
        /// <param name="s"></param>
        public void Append(string? s) {
            if (s == null) return;
            _hb.Append(s);
        }
        /// <summary>
        /// Appends a string to the current HTML content.
        /// </summary>
        /// <param name="s">A composite format string.</param>
        /// <param name="parms">An array of objects to format.</param>
        public void Append(string? s, params object?[] parms) {
            if (s == null) return;
            _hb.AppendFormat(s, parms);
        }
        /// <summary>
        /// Converts the value of this instance to a System.String.
        /// </summary>
        /// <returns> A string whose value is the same as this instance.</returns>
        public new string ToString() {
            return _hb.ToString();
        }

        /// <summary>
        ///  Gets or sets the length of the current YetaWF.Core.Support.HtmlBuilder object.
        /// </summary>
        public int Length {
            get { return _hb.Length; }
            set { _hb.Length = value; }
        }

        /// <summary>
        /// Replaces all occurrences of a specified string in this instance with another specified string.
        /// </summary>
        /// <param name="oldStr">The string to replace.</param>
        /// <param name="newStr">The string that replaces oldValue, or null.</param>
        /// <returns></returns>
        public StringBuilder Replace(string oldStr, string? newStr) {
            return _hb.Replace(oldStr, newStr);
        }

        /// <summary>
        /// Removes the specified range of characters from this instance.
        /// </summary>
        /// <param name="startIndex">The zero-based position in this instance where removal begins.</param>
        /// <param name="length">The number of characters to remove.</param>
        public void Remove(int startIndex, int length) {
            _hb.Remove(startIndex, length);
        }
    }
}
