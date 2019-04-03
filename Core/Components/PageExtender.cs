/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// This static class implements extension methods for YetaWF pages.
    /// </summary>
    public static class YetaWFPageExtender {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Tests whether a valid page exists.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <returns>Returns true if a valid page can be found.</returns>
        /// <remarks>This is used by the framework for debugging/testing purposes only.</remarks>
        public static bool IsSupported(string pageName) {
            Type pageType;
            if (!YetaWFComponentBaseStartup.GetPages().TryGetValue(pageName, out pageType))
                return false;
            return true;
        }

        /// <summary>
        /// Renders a page.
        /// </summary>
        /// <param name="htmlHelper">The HtmlHelper instance.</param>
        /// <param name="pageName">The name of the page.</param>
        /// <returns>Returns HTML with the rendered page.</returns>
        public static async Task<YHtmlString> ForPageAsync(this YHtmlHelper htmlHelper, string pageName) {

            Type pageType;
            if (!YetaWFComponentBaseStartup.GetPages().TryGetValue(pageName, out pageType))
                throw new InternalError($"Page {pageName} not found");
            YetaWFPageBase page = (YetaWFPageBase)Activator.CreateInstance(pageType);
            page.SetRenderInfo(htmlHelper);

            IYetaWFPage iPage = (IYetaWFPage)page;

            // Add support for this page
            //await Manager.AddOnManager.TryAddAddOnNamedAsync(page.Package.AreaName, pageName);// RFFU page addons?

            // render page contents first so all data like title, meta becomes available
            string contents = (await iPage.RenderPageBodyAsync()).ToString();

#if DEBUG
            if (contents.ToString().Contains("System.Threading.Tasks.Task"))
                throw new InternalError($"Page {pageName} contains System.Threading.Tasks.Task - check for missing \"await\" - generated HTML: \"{contents}\"");
            if (contents.Contains("Microsoft.AspNetCore.Mvc.Rendering"))
                throw new InternalError($"Page {pageName} contains Microsoft.AspNetCore.Mvc.Rendering - check for missing \"ToString()\" - generated HTML: \"{contents}\"");
#endif

            HtmlBuilder hb = new HtmlBuilder();
            hb.Append((await iPage.RenderPageHeaderAsync()).ToString());
            hb.Append(contents);
            hb.Append("</html>");

            return hb.ToYHtmlString();
        }

        /// <summary>
        /// Returns the names of all panes available in this page.
        /// </summary>
        /// <returns>Returns a collection of pane names available in this page.</returns>
        public static List<string> GetPanes(string pageViewName) {

            Type pageType;
            if (!YetaWFComponentBaseStartup.GetPages().TryGetValue(pageViewName, out pageType))
                throw new InternalError($"Page {pageViewName} not found");
            YetaWFPageBase page = (YetaWFPageBase)Activator.CreateInstance(pageType);

            IYetaWFPage iPage = (IYetaWFPage)page;
            List<string> panes = iPage.GetPanes();
            if (panes.Count == 0)
                throw new InternalError("No panes defined in {0}", pageViewName);
            return panes;
        }
    }
}
