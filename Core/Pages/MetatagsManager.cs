/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
using System.Web;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {
    public class MetatagsManager {

        public MetatagsManager(YetaWFManager manager) { Manager = manager; }
        protected YetaWFManager Manager { get; private set; }

        private readonly List<string> _tags = new List<string>();

        public void AddMetatag(string type, string content) {
            if (_tags.Contains(type))
                throw new InternalError("Metatag name={0} has already been added for this page.", type);
            string tag = string.Format("<meta name='{0}' content='{1}'/>", HttpUtility.HtmlAttributeEncode(type), HttpUtility.HtmlAttributeEncode(content));
            _tags.Add(tag);
        }
        public string Render() {
            StringBuilder sb = new StringBuilder();
            // add built-in metatags
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.Title))
                AddMetatag("title", Manager.CurrentPage.Title);
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.Keywords))
                AddMetatag("keywords", Manager.CurrentPage.Keywords);
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.Description))
                AddMetatag("description", Manager.CurrentPage.Description);
            string robots = "";
            if (Manager.CurrentPage.RobotNoIndex) robots += ",noindex";
            if (Manager.CurrentPage.RobotNoFollow) robots += ",nofollow";
            if (Manager.CurrentPage.RobotNoArchive) robots += ",noarchive";
            if (Manager.CurrentPage.RobotNoSnippet) robots += ",nosnippet";
            if (!string.IsNullOrWhiteSpace(robots))
                AddMetatag("robots", robots.Substring(1));
            // for the home page, add the google verification
            if (Manager.CurrentPage.Url == "/" && !string.IsNullOrWhiteSpace(Manager.CurrentSite.GoogleVerification))
                sb.Append(Manager.CurrentSite.GoogleVerification);
            // add all meta tags
            foreach (var tag in _tags)
                sb.Append(tag);
            string s = sb.ToString();
            if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.SiteMetaTags))
                s += Manager.CurrentSite.SiteMetaTags;
            if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.PageMetaTags))
                s += Manager.CurrentPage.PageMetaTags;
            else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.PageMetaTags))
                s += Manager.CurrentSite.PageMetaTags;
            return s;
        }
    }
}
