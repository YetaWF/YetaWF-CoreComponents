/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;
#if MVC6
using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;
#else
#endif

namespace YetaWF.Core.Support {

    public class HtmlBuilder {

        public HtmlBuilder() { }
        public HtmlBuilder(string s) { }

        private readonly StringBuilder _hb = new StringBuilder(4000);

        public void Append(string s) {
            if (s == null) return;
            _hb.Append(s);
        }
        public void Append(string s, params object[] parms) {
            if (s == null) return;
            _hb.AppendFormat(s, parms);
        }
        public new string ToString() {
            return _hb.ToString();
        }

        public int Length {
            get { return _hb.Length; }
            set { _hb.Length = value; }
        }

        public StringBuilder Replace(string oldStr, string newStr) {
            return _hb.Replace(oldStr, newStr);
        }
        public void Remove(int startIndex, int length) {
            _hb.Remove(startIndex, length);
        }
    }
}
