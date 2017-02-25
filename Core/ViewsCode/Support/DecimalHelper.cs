﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class Decimal<TModel> : RazorTemplate<TModel> { }

    public static class DecimalHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static MvcHtmlString RenderDecimal(this IHtmlHelper htmlHelper, string name, Decimal? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#else
        public static MvcHtmlString RenderDecimal(this HtmlHelper<object> htmlHelper, string name, Decimal? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#endif
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.userevents.min.js");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.numerictextbox.min.js");

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: Validation);

            // handle min/max
            Type containerType = htmlHelper.ViewData.ModelMetadata.ContainerType;
            string propertyName = htmlHelper.ViewData.ModelMetadata.PropertyName;
            PropertyData propData = ObjectSupport.GetPropertyData(containerType, propertyName);
            RangeAttribute rangeAttr = propData.TryGetAttribute<RangeAttribute>();
            if (rangeAttr != null) {
                tag.MergeAttribute("data-min", ((double) rangeAttr.Minimum).ToString("0.000"));
                tag.MergeAttribute("data-min", ((double) rangeAttr.Maximum).ToString("0.000"));
            }
            if (model != null)
                tag.MergeAttribute("value", ((decimal)model).ToString("0.00"));

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }
    }
}