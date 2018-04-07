/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Text;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;
#else
using System.Web;
using System.Web.Mvc;
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
#if MVC6
        public void Append(IHtmlContent content) {
            if (content == null) return;
            System.IO.StringWriter writer = new System.IO.StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            _hb.Append(writer.ToString());
        }
#else
        public void Append(IHtmlString content) {
            if (content == null) return;
            _hb.Append(content.ToHtmlString());
        }
#endif
        public void Append(string s, params object[] parms) {
            if (s == null) return;
            _hb.AppendFormat(s, parms);
        }
        public new string ToString() {
            return _hb.ToString();
        }
        public HtmlString ToHtmlString() {
            return new HtmlString(_hb.ToString());
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

        public void Append(Task<HtmlString> task) {
            throw new NotImplementedException();
        }
    }
}
