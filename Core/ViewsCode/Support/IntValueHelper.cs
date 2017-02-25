﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Models;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class IntValue<TModel> : RazorTemplate<TModel> { }

    public static class IntValueHelper {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
#if MVC6
        public static MvcHtmlString RenderIntValue(this IHtmlHelper htmlHelper, string name, int? value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#else
        public static MvcHtmlString RenderIntValue(this HtmlHelper<object> htmlHelper, string name, int? value, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
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

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }
    }
}