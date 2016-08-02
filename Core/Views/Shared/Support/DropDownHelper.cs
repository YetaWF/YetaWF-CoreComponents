/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class SelectionItem<TYPE> {
        public MultiString Text { get; set; }
        public TYPE Value { get; set; }
        public MultiString Tooltip { get; set; }
    }

    public static class DropDownHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(DropDownHelper), name, defaultValue, parms); }

        // Model has a property "_List" with List<SelectionItem<TYPE>> of possible values

        public static MvcHtmlString RenderDropDownSelectionList<TYPE>(this HtmlHelper htmlHelper, string name, TYPE selection, Func<TYPE, string> FuncToString = null, object HtmlAttributes = null) {
            List<SelectionItem<TYPE>> list = htmlHelper.GetParentModelSupportProperty<List<SelectionItem<TYPE>>>(name, "List");
            return htmlHelper.RenderDropDownSelectionList<TYPE>(name, selection, list, FuncToString, HtmlAttributes);
        }

        public static MvcHtmlString RenderDropDownSelectionList<TYPE>(this HtmlHelper htmlHelper, string name, TYPE selection, List<SelectionItem<TYPE>> list, Func<TYPE, string> FuncToString = null, object HtmlAttributes = null) {

            TagBuilder tag = new TagBuilder("select");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes);

            tag.InnerHtml = htmlHelper.RenderOptions<TYPE>(selection, list, FuncToString).ToString();

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString RenderOptions<TYPE>(this HtmlHelper htmlHelper, TYPE selection, List<SelectionItem<TYPE>> list, Func<TYPE, string> FuncToString = null) {
            HtmlBuilder tagHtml = new HtmlBuilder();
            foreach (var item in list)
                tagHtml.Append(ListItemToOption<TYPE>(item, Equals(item.Value, selection), FuncToString));
            return tagHtml.ToMvcHtmlString();
        }

        public static string ListItemToOption<TYPE>(SelectionItem<TYPE> item, bool selected, Func<TYPE, string> FuncToString) {
            TagBuilder tag = new TagBuilder("option") {
                InnerHtml = YetaWFManager.HtmlEncode(item.Text)
            };
            if (item.Value != null)
                tag.Attributes["value"] = FuncToString != null ? FuncToString(item.Value) : item.Value.ToString();
            if (selected)
                tag.Attributes["selected"] = "selected";
            if (item.Tooltip != null)
                tag.Attributes["title"] = item.Tooltip;
            return tag.ToString();
        }

        // Model has a property "_List" with List<Type> of possible values

        public static MvcHtmlString RenderDropDownList<TYPE>(this HtmlHelper<object> htmlHelper, string name, TYPE selection, Func<TYPE, string> FuncToString = null, object HtmlAttributes = null) {

            List<TYPE> list = htmlHelper.GetParentModelSupportProperty<List<TYPE>>(name, "List");

            TagBuilder tag = new TagBuilder("select");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes);

            HtmlBuilder tagHtml = new HtmlBuilder();
            foreach (var item in list)
                tagHtml.Append(ListItemToOption<TYPE>(item, Equals(item, selection), FuncToString));
            tag.InnerHtml = tagHtml.ToString();

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        private static string ListItemToOption<TYPE>(TYPE item, bool selected, Func<TYPE, string> FuncToString) {
            TagBuilder tag = new TagBuilder("option") {
                InnerHtml = YetaWFManager.HtmlEncode(FuncToString!=null ? FuncToString(item) : item.ToString())
            };
            if (selected)
                tag.Attributes["selected"] = "selected";
            return tag.ToString();
        }

        // Enum

        public static MvcHtmlString RenderEnum(this HtmlHelper<object> htmlHelper, string name, object value, object HtmlAttributes = null) {

            TagBuilder tag = new TagBuilder("select");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes);

            Type enumType = value.GetType();
            EnumData enumData = ObjectSupport.GetEnumData(enumType);
            bool showValues = UserSettings.GetProperty<bool>("ShowEnumValue");

            bool showSelect = htmlHelper.GetControlInfo<bool>("", "ShowSelect", false);

            HtmlBuilder tagHtml = new HtmlBuilder();
            foreach (EnumDataEntry entry in enumData.Entries) {
                object v = entry.Value;
                string desc = entry.Description;
                string caption = entry.Caption;

                int enumVal = Convert.ToInt32(v);
                if (showValues)
                    caption = __ResStr("enumFmt", "{0} - {1}", enumVal, caption);

                if (enumVal == 0 && showSelect) {
                    caption = __ResStr("enumSelect", "(select)");
                    showSelect = false;
                }

                TagBuilder tagOpt = new TagBuilder("option") {
                    InnerHtml = YetaWFManager.HtmlEncode(caption)
                };
                tagOpt.Attributes.Add("value", enumVal.ToString());
                if (Equals(value, v))
                    tagOpt.Attributes["selected"] = "selected";
                if (desc != caption)
                    tagOpt.Attributes["title"] = desc;

                tagHtml.Append(tagOpt.ToString(TagRenderMode.Normal));
            }
            if (showSelect) {
                TagBuilder tagOpt = new TagBuilder("option") {
                    InnerHtml = YetaWFManager.HtmlEncode(__ResStr("enumSelect", "(select)")),
                };
                tagOpt.Attributes.Add("value", "0");
                if (Equals(value, 0))
                    tagOpt.Attributes["selected"] = "selected";
                tagOpt.Attributes["title"] = __ResStr("enumPlsSelect", "Please select one of the available options");

                tagHtml.Append(tagOpt.ToString(TagRenderMode.Normal));
            }

            tag.InnerHtml = tagHtml.ToString();
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString RenderEnumDisplay(this HtmlHelper<object> htmlHelper, string name, object value, object HtmlAttributes = null) {

            Type enumType = value.GetType();
            EnumData enumData = ObjectSupport.GetEnumData(enumType);

            string desc = value.ToString();
            string caption = "";

            bool showValues = UserSettings.GetProperty<bool>("ShowEnumValue");
            showValues = showValues && htmlHelper.GetControlInfo<bool>("", "ShowEnumValue", true);

            // try to get enum caption/description
            foreach (EnumDataEntry entry in enumData.Entries) {
                object v = entry.Value;
                if (Equals(value, v)) {
                    desc = entry.Description;
                    caption = entry.Caption;
                    if (showValues)
                        caption = __ResStr("enumFmt", "{0} - {1}", (int) v, caption);
                    break;
                }
            }
            if (HtmlAttributes != null || !string.IsNullOrWhiteSpace(desc)) {
                TagBuilder tag = new TagBuilder("span");
                IDictionary<string, object> htmlAttributes = FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes);
                htmlAttributes.Add(Basics.CssTooltip, YetaWFManager.HtmlEncode(desc));
                tag.MergeAttributes(htmlAttributes, replaceExisting: true);
                tag.SetInnerText(caption);
                return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
            } else {
                return MvcHtmlString.Create(caption);
            }
        }
    }

}
