/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    /// <summary>
    /// An instance of the LinkAltManager class is used to add and render all &lt;link rel='alternate'...&gt; tags for the current page.
    /// </summary>
    /// <remarks>The instance of the LinkAltManager class for the current HTTP request can be accessed using the YetaWF.Core.Support.YetaWFManager.Manager.LinkAltManager property.</remarks>
    public class LinkAltManager {

        internal LinkAltManager() { }

        private readonly Dictionary<string, string> _tags = new Dictionary<string, string>();

        /// <summary>
        /// Used to add a &lt;link rel='alternate'...&gt; tag to the current page.
        /// </summary>
        /// <param name="name">The name (used as key) for the link tag being added. It is not rendered and only used to avoid collisions between different link tags. The name should consist of the area name followed by an area-specific key. For example, the YetaWF.Text package would use a name of "YetaWF_Text_MyKey".</param>
        /// <param name="type">The type of link tag being added. This renders as the link's type attribute.</param>
        /// <param name="title">The title of the link tag being added. This renders as the link's title attribute.</param>
        /// <param name="href">The URL of the link being added. This renders as the link's href attribute.</param>
        /// <remarks>Link tags added using AddLinkAltTag are automatically rendered once the page has been completely processed.</remarks>
        public void AddLinkAltTag(string name, string type, string title, string href) {
            string tag = string.Format("<link rel='alternate' type='{0}' title='{1}' href='{2}'>",
                Utility.HAE(type), Utility.HAE(title), Utility.HAE(href));
            if (_tags.ContainsKey(name)) {
                if (_tags[name] != tag)
                    throw new InternalError($"Link alt tag {name} has already been added for this page with a different value");
            }
            _tags.Add(name, tag);
        }

        internal string Render() {
            StringBuilder sb = new StringBuilder();
            foreach (var tagEntry in _tags)
                sb.Append(tagEntry.Value);
            return sb.ToString();
        }
    }
}

