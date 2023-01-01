/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Text;
using YetaWF.Core.Support;

namespace YetaWF.Core.Pages {

    /// <summary>
    /// An instance of the MetatagsManager class is used to add and render all &lt;meta&gt; tags for the current page.
    /// </summary>
    /// <remarks>The instance of the MetatagsManager class for the current HTTP request can be accessed using the YetaWF.Core.Support.YetaWFManager.Manager.MetatagsManager property.</remarks>
    public class MetatagsManager {

        internal MetatagsManager(YetaWFManager manager) { Manager = manager; }
        private YetaWFManager Manager { get; set; }

        private readonly List<string> _tags = new List<string>();

        /// <summary>
        /// Adds a meta tag to the current page.
        /// </summary>
        /// <param name="type">The type of the meta tag being added. This renders as the tag's name attribute.</param>
        /// <param name="content">The content of the meta tag being added. This renders as the tag's content attribute.</param>
        /// <remarks>Used for rarely used tags.
        ///
        /// Many standard meta tags are automatically added based on site settings and page settings, like page keywords, description, noindex, nofollow, noarchive, nosnippet, robots.
        ///
        /// The "title" meta tag should not be set directly. Set YetaWF.Core.Support.YetaWFManager.Manager.PageTitle instead.
        /// For "keywords", set YetaWF.Core.Support.YetaWFManager.Manager.CurrentPage.Keywords instead. For "description" set YetaWF.Core.Support.YetaWFManager.Manager.CurrentPage.Description instead.</remarks>
        public void AddMetatag(string type, string content) {
            if (_tags.Contains(type))
                return;
            string tag = string.Format("<meta name='{0}' content='{1}'/>", Utility.HAE(type), Utility.HAE(content));
            _tags.Add(tag);
        }
        /// <summary>
        /// Adds a meta tag to the current page with variable substitution.
        /// </summary>
        /// <param name="vars">An instance of a YetaWF.Core.Support.Variables object describing the variables to substitute.</param>
        /// <param name="type">The type of the meta tag being added. This renders as the tag's name attribute.</param>
        /// <param name="content">The content of the meta tag being added. This renders as the tag's content attribute.</param>
        /// <remarks>Used for rarely used tags.
        ///
        /// Many standard meta tags are automatically added based on site settings and page settings, like page keywords, description, noindex, nofollow, noarchive, nosnippet, robots.
        ///
        /// The "title" meta tag should not be set directly. Set YetaWF.Core.Support.YetaWFManager.Manager.PageTitle instead.
        /// For "keywords", set YetaWF.Core.Support.YetaWFManager.Manager.CurrentPage.Keywords instead. For "description" set YetaWF.Core.Support.YetaWFManager.Manager.CurrentPage.Description instead.</remarks>
        private void AddMetatag(Variables vars, string type, string content) {
            if (_tags.Contains(type))
                throw new InternalError($"Metatag name={type} has already been added for this page.");
            content = vars.ReplaceVariables(content);// variable substitution
            string tag = string.Format("<meta name='{0}' content='{1}'/>", Utility.HAE(type), Utility.HAE(content));
            _tags.Add(tag);
        }

        internal string Render() {
            StringBuilder sb = new StringBuilder();
            Variables vars = new Variables(Manager) {  };
            // add built-in metatags
            string? title = Manager.PageTitle;
            if (!string.IsNullOrWhiteSpace(title))
                AddMetatag(vars, "title", title);
            if (!Manager.IsInPopup) {
                string? kwds = Manager.CurrentPage.Keywords;
                if (!string.IsNullOrWhiteSpace(kwds))
                    AddMetatag(vars, "keywords", kwds);
                string? desc = Manager.CurrentPage.Description;
                if (!string.IsNullOrWhiteSpace(desc))
                    AddMetatag(vars, "description", desc);
                string robots = "";
                if (Manager.CurrentPage.RobotNoIndex) robots += ",noindex";
                if (Manager.CurrentPage.RobotNoFollow) robots += ",nofollow";
                if (Manager.CurrentPage.RobotNoArchive) robots += ",noarchive";
                if (Manager.CurrentPage.RobotNoSnippet) robots += ",nosnippet";
                if (!string.IsNullOrWhiteSpace(robots))
                    AddMetatag(vars, "robots", robots.Substring(1));
                // for the home page, add the google verification
                if (Manager.CurrentPage.Url == "/" && !string.IsNullOrWhiteSpace(Manager.CurrentSite.GoogleVerification))
                    sb.Append(vars.ReplaceVariables(Manager.CurrentSite.GoogleVerification));
                if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.SiteMetaTags))
                    sb.Append(vars.ReplaceVariables(Manager.CurrentSite.SiteMetaTags));
                if (!string.IsNullOrWhiteSpace(Manager.CurrentPage.PageMetaTags))
                    sb.Append(vars.ReplaceVariables(Manager.CurrentPage.PageMetaTags));
                else if (!string.IsNullOrWhiteSpace(Manager.CurrentSite.PageMetaTags))
                    sb.Append(vars.ReplaceVariables(Manager.CurrentSite.PageMetaTags));
            }
            // add all explicitly added meta tags
            foreach (var tag in _tags)
                sb.Append(tag);
            return sb.ToString();
        }
    }
}
