﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
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

    public class Decimal<TModel> : RazorTemplate<TModel> { }

    public static class DecimalHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static async Task<HtmlString> RenderDecimalAsync(this IHtmlHelper htmlHelper, string name, Decimal? model, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#else
        public static async Task<HtmlString> RenderDecimalAsync(this HtmlHelper<object> htmlHelper, string name, Decimal? model, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
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
                tag.MergeAttribute("data-min", ((double)rangeAttr.Minimum).ToString("0.000"));
                tag.MergeAttribute("data-min", ((double)rangeAttr.Maximum).ToString("0.000"));
            }
            if (model != null)
                tag.MergeAttribute("value", ((decimal)model).ToString("0.00"));

            return tag.ToHtmlString(TagRenderMode.SelfClosing);
        }
    }
}
