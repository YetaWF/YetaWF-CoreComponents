/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
using System.Web;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {
    public class LinkAltManager {

        private readonly Dictionary<string, string> _tags = new Dictionary<string, string>();

        public void AddLinkAltTag(string name, string type, string title, string href) {
            string tag = string.Format("<link rel='alternate' type='{0}' title='{1}' href='{2}'>",
                Utility.HtmlAttributeEncode(type), Utility.HtmlAttributeEncode(title), Utility.HtmlAttributeEncode(href));
            if (_tags.ContainsKey(name)) {
                if (_tags[name] != tag)
                    throw new InternalError("Link alt tag {0} has already been added for this page with a different value", name);
            }
            _tags.Add(name, tag);
        }
        public string Render() {
            StringBuilder sb = new StringBuilder();
            foreach (var tagEntry in _tags)
                sb.Append(tagEntry.Value);
            return sb.ToString();
        }
    }
}

