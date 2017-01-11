﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;

namespace YetaWF.Core.Views.Shared {

    public class PageJQueryUISkins<TModel> : RazorTemplate<TModel> { }

    public static class JQueryUISkinsHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(JQueryUISkinsHelper), name, defaultValue, parms); }

        public static MvcHtmlString RenderJQueryUISkins(this HtmlHelper htmlHelper, string name, string selection, object HtmlAttributes = null) {
            // get all available skins
            SkinAccess skinAccess = new SkinAccess();
            List<SelectionItem<string>> list = (from theme in skinAccess.GetJQueryThemeList() select new SelectionItem<string>() {
                Text = theme.Name,
                Tooltip = theme.Description,
                Value = theme.Name,
            }).ToList();

            bool useDefault = ! htmlHelper.GetControlInfo<bool>("", "NoDefault");
            if (useDefault)
                list.Insert(0, new SelectionItem<string> {
                    Text = __ResStr("default", "(Site Default)"),
                    Tooltip = __ResStr("defaultTT", "Use the site defined default theme"),
                    Value = "",
                });
            else if (selection == null)
                selection = SkinAccess.GetJQueryUIDefaultSkin();

            // display the skins in a drop down
            return htmlHelper.RenderDropDownSelectionList(name, selection, list, HtmlAttributes: HtmlAttributes);
        }
    }
}
