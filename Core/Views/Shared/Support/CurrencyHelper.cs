/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class Currency<TModel> : RazorTemplate<TModel> { }

    public static class CurrencyHelper {

        public static MvcHtmlString RenderCurrency(this HtmlHelper<object> htmlHelper, string name, Decimal? model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {

            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, ModelNameOverride: ModelNameOverride, Validation: Validation);

            // handle min/max
            Type containerType = htmlHelper.ViewData.ModelMetadata.ContainerType;
            string propertyName = htmlHelper.ViewData.ModelMetadata.PropertyName;
            PropertyData propData = ObjectSupport.GetPropertyData(containerType, propertyName);
            RangeAttribute rangeAttr = propData.TryGetAttribute<RangeAttribute>();
            if (rangeAttr != null) {
                tag.MergeAttribute("data-min", ((double) rangeAttr.Minimum).ToString("F3"));
                tag.MergeAttribute("data-max", ((double) rangeAttr.Maximum).ToString("F3"));
            }
            if (model != null)
                tag.MergeAttribute("value", Formatting.FormatAmount((decimal) model));

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.SelfClosing));
        }
    }
}
