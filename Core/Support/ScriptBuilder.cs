/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace YetaWF.Core.Support {

    public class ScriptBuilder {

        public const string NL = "(+nl)";

        private readonly StringBuilder _sb = new StringBuilder();
        public void Append(string s) {
            if (s == null) return;
            _sb.Append(s);
        }
        public void Append(MvcHtmlString s) {
            if (s == null) return;
            _sb.Append(s.ToString());
        }
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
        public int Length {
            get { return _sb.Length; }
            set { _sb.Length = value; }
        }
        public void RemoveLast() {
            if (_sb.Length > 0)
                _sb.Remove(_sb.Length - 1, 1);
        }

        static public implicit operator MvcHtmlString(ScriptBuilder s) { return MvcHtmlString.Create(s.ToString()); }
    }
}
