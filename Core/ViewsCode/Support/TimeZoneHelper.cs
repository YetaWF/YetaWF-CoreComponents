/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/DevTests#License */

using System;
using System.Collections.Generic;
using System.Linq;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class TimeZone<TModel> : RazorTemplate<TModel> { }

    public static class TimeZoneHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(TimeZoneHelper), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderTimeZoneDD(this IHtmlHelper htmlHelper, string name, string model, object HtmlAttributes = null) {
#else
        public static HtmlString RenderTimeZoneDD(this HtmlHelper htmlHelper, string name, string model, object HtmlAttributes = null) {
#endif
            List<TimeZoneInfo> tzis = TimeZoneInfo.GetSystemTimeZones().ToList();
            DateTime dt = DateTime.Now;// Need local time

            bool showDefault = htmlHelper.GetControlInfo<bool>(name, "ShowDefault", true);

            List<SelectionItem<string>> list;
            list = (
                from tzi in tzis orderby tzi.DisplayName
                    orderby tzi.DisplayName
                    select
                        new SelectionItem<string> {
                            Text = tzi.DisplayName,
                            Value = tzi.Id,
                            Tooltip = tzi.IsDaylightSavingTime(dt) ? tzi.DaylightName : tzi.StandardName,
                        }).ToList<SelectionItem<string>>();
            if (showDefault) {
                if (string.IsNullOrWhiteSpace(model))
                    model = TimeZoneInfo.Local.Id;
            } else
                list.Insert(0, new SelectionItem<string> { Text = __ResStr("select", "(select)"), Value = "" });
            return htmlHelper.RenderDropDownSelectionList<string>(name, model, list, HtmlAttributes: HtmlAttributes);
        }
#if MVC6
        public static HtmlString RenderTimeZoneDisplay(this IHtmlHelper htmlHelper, string name, string model) {
#else
        public static HtmlString RenderTimeZoneDisplay(this HtmlHelper htmlHelper, string name, string model) {
#endif
            TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(model);
            if (tzi == null) {
                return new HtmlString(__ResStr("unknown", "(unknown)"));
            } else {
                HtmlBuilder hb = new HtmlBuilder();
                TagBuilder tag = new TagBuilder("div");
                tag.SetInnerText(tzi.DisplayName);
                tag.Attributes.Add("title", tzi.IsDaylightSavingTime(DateTime.Now/*need local time*/) ? tzi.DaylightName : tzi.StandardName);
                hb.Append(tag.ToString(TagRenderMode.Normal));

                return hb.ToHtmlString();
            }
        }
    }
}
