/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class Date<TModel> : RazorTemplate<TModel> { }

    public static class DateHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public static void Include() {
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.calendar.min.js");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.popup.min.js");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.datepicker.min.js");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.timepicker.min.js");
        }

        public static MvcHtmlString RenderDate(this HtmlHelper<object> htmlHelper, string name, DateTime? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {

            Include();

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

            return hb.ToMvcHtmlString();
        }
        public static string RenderDateJavascript(string jqElem) {

            Include();

            ScriptBuilder sb = new ScriptBuilder();

            // Build a kendo date picker
            // We have to add it next to the jqgrid provided input field
            // We can't use the jqgrid provided element as a kendodatepicker because jqgrid gets confused and
            // uses the wrong sorting opion. So we add the datepicker next to the "official" input field (which we hide)
            TagBuilder tag = new TagBuilder("input");
            tag.Attributes.Add("name", "dtpicker");

            // add the kendodatepicker after the input field
            sb.Append("var $dtpick = $('{0}');", tag.ToString());
            sb.Append("{0}.after($dtpick);", jqElem);

            sb.Append("YetaWF_Date.renderjqGridFilter({0}, $dtpick);", jqElem);

            // Hide the jqgrid provided input element (we update the date in this hidden element)
            sb.Append("{0}.hide();", jqElem);

            return sb.ToString();
        }
    }
}
