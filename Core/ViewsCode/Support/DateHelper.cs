/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using System.Threading.Tasks;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class Date<TModel> : RazorTemplate<TModel> { }

    public static class DateHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static async Task IncludeAsync() {
            await Manager.ScriptManager.AddKendoUICoreJsFileAsync("kendo.calendar.min.js");
            // Manager.ScriptManager.AddKendoUICoreJsFile("kendo.popup.min.js"); // is now a prereq of kendo.window (2017.2.621)
            await Manager.ScriptManager.AddKendoUICoreJsFileAsync("kendo.datepicker.min.js");
            await Manager.ScriptManager.AddKendoUICoreJsFileAsync("kendo.timepicker.min.js");
        }

#if MVC6
        public static async Task<HtmlString> RenderDateAsync(this IHtmlHelper htmlHelper, string name, DateTime? model, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#else
        public static async Task<HtmlString> RenderDateAsync(this HtmlHelper<object> htmlHelper, string name, DateTime? model, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#endif
            await IncludeAsync();

            HtmlBuilder hb = new HtmlBuilder();
            hb.Append(htmlHelper.RenderHidden(name, "", HtmlAttributes: HtmlAttributes, Validation: Validation));

            TagBuilder tag = new TagBuilder("input");
            tag.Attributes.Add("name", "dtpicker");

            // handle min/max date
            Type containerType = htmlHelper.ViewData.ModelMetadata.ContainerType;
            string propertyName = htmlHelper.ViewData.ModelMetadata.PropertyName;
            PropertyData propData = ObjectSupport.GetPropertyData(containerType, propertyName);
            MinimumDateAttribute minAttr = propData.TryGetAttribute<MinimumDateAttribute>();
            MaximumDateAttribute maxAttr = propData.TryGetAttribute<MaximumDateAttribute>();
            if (minAttr != null) {
                tag.MergeAttribute("data-min-y", minAttr.MinDate.Year.ToString());
                tag.MergeAttribute("data-min-m", minAttr.MinDate.Month.ToString());
                tag.MergeAttribute("data-min-d", minAttr.MinDate.Day.ToString());
            }
            if (maxAttr != null) {
                tag.MergeAttribute("data-max-y", maxAttr.MaxDate.Year.ToString());
                tag.MergeAttribute("data-max-m", maxAttr.MaxDate.Month.ToString());
                tag.MergeAttribute("data-max-d", maxAttr.MaxDate.Day.ToString());
            }
            if (model != null)
                tag.MergeAttribute("value", Formatting.FormatDate((DateTime)model));
            hb.Append(tag.ToString(TagRenderMode.SelfClosing));

            return hb.ToHtmlString();
        }
        public static async Task<string> RenderDateJavascriptAsync(string gridId, string elemVarName) {
            await IncludeAsync();
            return string.Format("(new YetaWF_Core.TemplateDate.TemplateClass()).renderjqGridFilter('{0}', {1});", gridId, elemVarName);
        }
    }
}
