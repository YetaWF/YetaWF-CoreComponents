/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Web.Mvc;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Views.Shared {

    public static class BooleanHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(BooleanHelper), name, defaultValue, parms); }

        public static MvcHtmlString RenderBoolean(this HtmlHelper<object> htmlHelper, string name, bool value, object HtmlAttributes = null) {

            string fullName = htmlHelper.FieldName(name);
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

        public static MvcHtmlString RenderBooleanDisplay(this HtmlHelper<object> htmlHelper, string name, bool value, object HtmlAttributes = null) {

            string fullName = htmlHelper.FieldName(name);
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
