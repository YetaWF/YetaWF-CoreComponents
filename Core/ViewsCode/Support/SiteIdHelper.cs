/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;
using YetaWF.Core.Site;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public static class SiteIdHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static Dictionary<int, StringTT> Sites = null;

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(SiteIdHelper), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderSiteIdDisplay<TModel>(this IHtmlHelper<TModel> htmlHelper, string name, int model, object HtmlAttributes = null)
#else
        public static HtmlString RenderSiteIdDisplay<TModel>(this HtmlHelper<TModel> htmlHelper, string name, int model, object HtmlAttributes = null)
#endif
        {
            if (Sites == null) {
                Sites = new Dictionary<int, StringTT>();
                foreach (SiteDefinition site in SiteDefinition.GetSites(0, 0, null, null).Sites) {
                    Sites.Add(site.Identity,
                        new StringTT {
                            Text = site.Identity.ToString(),
                            Tooltip = site.SiteDomain,
                        }
                    );
                }
            }
            StringTT stringTT;
            if (Sites.ContainsKey(model))
                stringTT = Sites[model];
            else {
                stringTT = new StringTT {
                    Text = __ResStr("none", "(none)"),
                    Tooltip = __ResStr("noneTT", "")
                };
            }
            return htmlHelper.RenderStringTTDisplay(name, stringTT);
        }
    }
}