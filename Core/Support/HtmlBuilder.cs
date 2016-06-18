/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;
using System.Web;
using System.Web.Mvc;

namespace YetaWF.Core.Support {
    public class HtmlBuilder : IHtmlString {

        private readonly StringBuilder _hb = new StringBuilder(4000);
        public void Append(string s) {
            if (s == null) return;
            _hb.Append(s);
        }
        public void Append(MvcHtmlString s) {
            if (s == null) return;
            _hb.Append(s.ToString());
        }
        public void Append(string s, params object[] parms) {
            if (s == null) return;
            _hb.AppendFormat(s, parms);
        }
        public new string ToString() {
            return _hb.ToString();
        }
        public MvcHtmlString ToMvcHtmlString() {
            return MvcHtmlString.Create(_hb.ToString());
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

        static public implicit operator MvcHtmlString(HtmlBuilder h) { return MvcHtmlString.Create(h.ToString()); }

        public string ToHtmlString() {
            return this.ToString();
        }
    }
}
