/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public static class BooleanHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(BooleanHelper), name, defaultValue, parms); }
#if MVC6
        public static MvcHtmlString RenderBoolean(this IHtmlHelper htmlHelper, string name, bool value, object HtmlAttributes = null) {
#else
        public static MvcHtmlString RenderBoolean(this HtmlHelper<object> htmlHelper, string name, bool value, object HtmlAttributes = null) {
#endif
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes);
            tag.Attributes.Add("type", "checkbox");
            tag.Attributes.Add("value", "true");
            if (value)
                tag.Attributes.Add("checked", "checked");

            // add a hidden field so we always get "something" for check boxes (that means we have to deal with duplicates names)
            TagBuilder tagHidden = new TagBuilder("input");
            htmlHelper.FieldSetup(tagHidden, name, Validation: false);
            tagHidden.Attributes.Add("type", "hidden");
            tagHidden.Attributes.Add("value", "false");

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag) + tagHidden.ToString(TagRenderMode.StartTag));
        }


#if MVC6
        public static MvcHtmlString RenderBooleanDisplay(this IHtmlHelper<bool> htmlHelper, string name, bool value, object HtmlAttributes = null) {
#else
        public static MvcHtmlString RenderBooleanDisplay(this HtmlHelper<object> htmlHelper, string name, bool value, object HtmlAttributes = null) {
#endif
            TagBuilder tag = new TagBuilder("input");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: false, Anonymous: true);
            tag.Attributes.Add("type", "checkbox");
            tag.Attributes.Add("disabled", "disabled");
            if (value)
                tag.Attributes.Add("checked", "checked");
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.StartTag));
        }
    }

}
