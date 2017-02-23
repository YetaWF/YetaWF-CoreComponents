/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Text;
#if MVC6
using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Support {
    public class HtmlBuilder : HtmlString {

        public HtmlBuilder() : base("") { }
        public HtmlBuilder(string s) : base(s) { }

        private readonly StringBuilder _hb = new StringBuilder(4000);
        public void Append(string s) {
            if (s == null) return;
            _hb.Append(s);
        }
        public void Append(MvcHtmlString s) {
            if (s == null) return;
            _hb.Append(s.ToString());
        }
#if MVC6
        public void Append(IHtmlContent content) {
            if (content == null) return;
            System.IO.StringWriter writer = new System.IO.StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            _hb.Append(writer.ToString());
        }
#else
#endif
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
#if MVC6
        public string ToHtmlString() {
            return this.ToString();
        }
#else
#endif
    }
}
