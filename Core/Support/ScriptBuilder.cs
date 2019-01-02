/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
#if MVC6
using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;
#else
using System.Web;
#endif

namespace YetaWF.Core.Support {

    public class ScriptBuilder {

        public const string NL = "(+nl)";

        private readonly StringBuilder _sb = new StringBuilder();
        public void Append(string s) {
            if (s == null) return;
            _sb.Append(s);
        }
#if MVC6
        public void Append(IHtmlContent content) {
            if (content == null) return;
            System.IO.StringWriter writer = new System.IO.StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            _sb.Append(writer.ToString());
        }
#else
        public void Append(IHtmlString content) {
            if (content == null) return;
            _sb.Append(content.ToHtmlString());
        }
#endif
        public void Append(string s, params object[] parms) {
            if (s == null) return;
            _sb.AppendFormat(s, parms);
        }
        public void Append(List<string> errorList, bool LeadingNL = false) {
            foreach (var s in errorList) {
                Append("{0}{1}", LeadingNL ? NL : "", s);
                LeadingNL = true;
            }
        }
        public new string ToString() {
            return _sb.ToString();
        }
        public HtmlString ToHtmlString() {
            return new HtmlString(_sb.ToString());
        }
        public YHtmlString ToYHtmlString() {
            return new YHtmlString(_sb.ToString());
        }
        public int Length {
            get { return _sb.Length; }
            set { _sb.Length = value; }
        }
        public void RemoveLast() {
            if (_sb.Length > 0)
                _sb.Remove(_sb.Length - 1, 1);
        }
    }
}
