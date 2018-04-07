/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Models;
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

    public class IntValue<TModel> : RazorTemplate<TModel> { }

    public static class IntValueHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static async Task<HtmlString> RenderIntValueAsync(this IHtmlHelper htmlHelper, string name, int? value, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#else
        public static async Task<HtmlString> RenderIntValueAsync(this HtmlHelper<object> htmlHelper, string name, int? value, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#endif
            await Manager.ScriptManager.AddKendoUICoreJsFileAsync("kendo.userevents.min.js");
            await Manager.ScriptManager.AddKendoUICoreJsFileAsync("kendo.numerictextbox.min.js");

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: Validation);

            // handle min/max
            Type containerType = htmlHelper.ViewData.ModelMetadata.ContainerType;
            string propertyName = htmlHelper.ViewData.ModelMetadata.PropertyName;
            PropertyData propData = ObjectSupport.GetPropertyData(containerType, propertyName);
            RangeAttribute rangeAttr = propData.TryGetAttribute<RangeAttribute>();
            if (rangeAttr != null) {
                tag.MergeAttribute("data-min", ((int)rangeAttr.Minimum).ToString("D"));
                tag.MergeAttribute("data-max", ((int)rangeAttr.Maximum).ToString("D"));
            }
            string noEntry = htmlHelper.GetControlInfo<string>("", "NoEntry", null);
            if (!string.IsNullOrWhiteSpace(noEntry))
                tag.MergeAttribute("data-noentry", noEntry);
            int step = htmlHelper.GetControlInfo<int>("", "Step", 1);
            tag.MergeAttribute("data-step", step.ToString());

            if (value != null)
                tag.MergeAttribute("value", ((int)value).ToString());

            return tag.ToHtmlString(TagRenderMode.SelfClosing);
        }
    }
}
