/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif
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

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(DropDownHelper), name, defaultValue, parms); }

        ///// <summary>
        /// Renders a dropdownlist (without tooltips) for selection.
        /// </summary>
        /// <typeparam name="TYPE">A Type.</typeparam>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="name">The name of the property being rendered.</param>
        /// <param name="selection">The selected value.</param>
        /// <param name="FuncToString">A method that converts TYPE to a string.</param>
        /// <param name="HtmlAttributes">Optional attributes.</param>
        /// <returns>An HTML string with the rendered dropdownlist.</returns>
        /// <remarks>
        /// The parent model containing the property has to offer a property "_List" with a List<Type> of possible values.
        /// </remarks>

#if MVC6
        public static HtmlString RenderDropDownList<TYPE>(this IHtmlHelper htmlHelper, string name, TYPE selection, Func<TYPE, string> FuncToString = null, object HtmlAttributes = null) {
#else
        public static HtmlString RenderDropDownList<TYPE>(this HtmlHelper<object> htmlHelper, string name, TYPE selection, Func<TYPE, string> FuncToString = null, object HtmlAttributes = null) {
#endif
            List<TYPE> list = htmlHelper.GetParentModelSupportProperty<List<TYPE>>(name, "List");
            List<SelectionItem<TYPE>> itemList = new List<SelectionItem<TYPE>>();
            foreach (TYPE l in list) {
                itemList.Add(new SelectionItem<TYPE> {
                    Text = (FuncToString != null) ? FuncToString(l) : l.ToString(),
                    Value = l,
                });
            }
            return htmlHelper.RenderDropDownSelectionList(name, selection, itemList, FuncToString, HtmlAttributes);
        }

        /// <summary>
        /// Renders a dropdownlist (with tooltips) for selection.
        /// </summary>
        /// <typeparam name="TYPE">The Type of the property.</typeparam>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="name">The name of the property being rendered.</param>
        /// <param name="selection">The selected value.</param>
        /// <param name="FuncToString">A method that converts TYPE to a string.</param>
        /// <param name="HtmlAttributes">Optional attributes.</param>
        /// <returns>An HTML string with the rendered dropdownlist.</returns>
        /// <remarks>
        /// The parent model containing the property has to offer a property "_List" with a List<SelectionItem<TYPE>> of possible values.
        /// </remarks>
#if MVC6
        public static HtmlString RenderDropDownSelectionList<TYPE>(this IHtmlHelper htmlHelper, string name, TYPE selection, Func<TYPE, string> FuncToString = null,
#else
        public static HtmlString RenderDropDownSelectionList<TYPE>(this HtmlHelper htmlHelper, string name, TYPE selection, Func<TYPE, string> FuncToString = null,
#endif
                object HtmlAttributes = null, bool BrowserControls = false, bool Validation = true) {
            List<SelectionItem<TYPE>> list = htmlHelper.GetParentModelSupportProperty<List<SelectionItem<TYPE>>>(name, "List");
            return htmlHelper.RenderDropDownSelectionList<TYPE>(name, selection, list, FuncToString, HtmlAttributes: HtmlAttributes, BrowserControls: BrowserControls, Validation: Validation);
        }
        /// <summary>
        /// Renders a dropdownlist (with tooltips) for selection.
        /// </summary>
        /// <typeparam name="TYPE">The Type of the property.</typeparam>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="name">The name of the property being rendered.</param>
        /// <param name="selection">The selected value.</param>
        /// <param name="list">A list of all items part of the dropdownlist.</param>
        /// <param name="FuncToString">A method that converts TYPE to a string.</param>
        /// <param name="HtmlAttributes">Optional attributes.</param>
        /// <returns></returns>
#if MVC6
        public static HtmlString RenderDropDownSelectionList<TYPE>(this IHtmlHelper htmlHelper, string name, TYPE selection, List<SelectionItem<TYPE>> list,
#else
        public static HtmlString RenderDropDownSelectionList<TYPE>(this HtmlHelper htmlHelper, string name, TYPE selection, List<SelectionItem<TYPE>> list,
#endif
            Func<TYPE, string> FuncToString = null, object HtmlAttributes = null, bool BrowserControls = false, bool Validation = true) {

            bool useKendo = !BrowserControls && !Manager.IsRenderingGrid;

            Manager.AddOnManager.AddTemplate("DropDownList");
            if (useKendo) {
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.data.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.popup.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.list.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.fx.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.userevents.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.draganddrop.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.mobile.scroller.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.virtuallist.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.dropdownlist.min.js");
            }

            TagBuilder tag = new TagBuilder("select");
            htmlHelper.FieldSetup(tag, name, HtmlAttributes: HtmlAttributes, Validation: Validation);
            tag.AddCssClass("yt_dropdownlist_base");
            string id = htmlHelper.MakeId(tag);
            if (useKendo)
                tag.Attributes.Add("data-needinit", "");

            HtmlBuilder tagHtml = new HtmlBuilder();
            ScriptBuilder sb = new ScriptBuilder();

            bool haveDesc = false;
            foreach (var item in list) {
                TagBuilder tagOpt = new TagBuilder("option");
                tagOpt.SetInnerText(item.Text);
                if (item.Value != null)
                    tagOpt.Attributes["value"] = FuncToString != null ? FuncToString(item.Value) : item.Value.ToString();
                else
                    tagOpt.Attributes["value"] = "";
                if (Equals(item.Value, selection))
                    tagOpt.Attributes["selected"] = "selected";
                string desc = (item.Tooltip != null) ? item.Tooltip.ToString() : null;
                if (!useKendo) {
                    if (!string.IsNullOrWhiteSpace(desc))
                        tagOpt.Attributes["title"] = desc;
                } else {
                    if (string.IsNullOrWhiteSpace(desc))
                        desc = "";
                    else
                        haveDesc = true;
                    sb.Append("{0},", YetaWFManager.JsonSerialize(desc));
                }
                tagHtml.Append(tagOpt.ToString(TagRenderMode.Normal));
            }

            if (useKendo) {
                if (!haveDesc) // if we don't have any descriptions, clear the tooltip array
                    sb = new ScriptBuilder();
                ScriptBuilder newSb = new ScriptBuilder();
                newSb.Append("$('#{0}').data('tooltips', [{1}]);", id, sb.ToString());
                newSb.Append("YetaWF_TemplateDropDownList.initOne($('#{0}'));", id);
                sb = newSb;
            }

            tag.SetInnerHtml(tagHtml.ToString());

            HtmlBuilder hb = new Support.HtmlBuilder();
            hb.Append(tag.ToString(TagRenderMode.Normal));
            hb.Append(Manager.ScriptManager.AddNow(sb.ToString()).ToHtmlString());
            return hb.ToHtmlString();
        }

        /// <summary>
        /// Render a JSON object with data and tooltips for a dropdownlist.
        /// </summary>
        /// <typeparam name="TYPE">The Type of the property.</typeparam>
        /// <param name="extraData">Optional data to be returned in JSON object as 'extra:' data.</param>
        /// <param name="list">A list of all items part of the dropdownlist.</param>
        /// <param name="FuncToString">A method that converts TYPE to a string.</param>
        /// <returns>A JSON object containing data and tooltips to update the contents of a dropdownlist.</returns>
        public static HtmlString RenderDataSource<TYPE>(string extraData, List<SelectionItem<TYPE>> list, Func<TYPE, string> FuncToString = null) {
            ScriptBuilder sb = new ScriptBuilder();
            sb.Append(Basics.AjaxJavascriptReturn);
            sb.Append(@"{""data"":[");
            foreach (SelectionItem<TYPE> item in list) {
                sb.Append(@"{{""t"":{0},""v"":{1}}},", YetaWFManager.JsonSerialize(item.Text.ToString()), YetaWFManager.JsonSerialize(FuncToString != null ? FuncToString(item.Value) : item.Value.ToString()));
            }
            if (list.Count > 0)
                sb.RemoveLast();
            sb.Append(@"],""tooltips"":[");
            if ((from i in list where i.Tooltip != null && !string.IsNullOrWhiteSpace(i.Tooltip.ToString()) select i).FirstOrDefault() != null) {
                foreach (SelectionItem<TYPE> item in list) {
                    sb.Append("{0},", YetaWFManager.JsonSerialize(item.Tooltip == null ? "" : item.Tooltip.ToString()));
                }
                if (list.Count > 0)
                    sb.RemoveLast();
            }
            if (!string.IsNullOrWhiteSpace(extraData)) {
                sb.Append(@"],""extra"":[");
                sb.Append("{0}", YetaWFManager.JsonSerialize(extraData));
            }
            sb.Append("]}");
            return sb.ToHtmlString();
        }

        /// <summary>
        /// Renders an enumerated value for selection.
        /// </summary>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="name">The name of the property being rendered.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="HtmlAttributes">Optional attributes.</param>
        /// <returns>An HTML string with the rendered dropdownlist.</returns>
        /// <remarks>
        /// A dropdownlist is rendered based on the enumerated type's EnumDescription attributes.</remarks>
#if MVC6
        public static HtmlString RenderEnum(this IHtmlHelper<object> htmlHelper, string name, object value, object HtmlAttributes = null) {
#else
        public static HtmlString RenderEnum(this HtmlHelper<object> htmlHelper, string name, object value, object HtmlAttributes = null) {
#endif
            List<SelectionItem<int>> items = new List<Shared.SelectionItem<int>>();

            Type enumType = value.GetType();
            EnumData enumData = ObjectSupport.GetEnumData(enumType);
            bool showValues = UserSettings.GetProperty<bool>("ShowEnumValue");

            bool showSelect = htmlHelper.GetControlInfo<bool>("", "ShowSelect", false);
            if (showSelect) {
                items.Add(new SelectionItem<int> {
                    Text = __ResStr("enumSelect", "(select)"),
                    Value = 0,
                    Tooltip = __ResStr("enumPlsSelect", "Please select one of the available options"),
                });
            }
            foreach (EnumDataEntry entry in enumData.Entries) {

                int enumVal = Convert.ToInt32(entry.Value);
                if (enumVal == 0 && showSelect) continue;

                string caption = entry.Caption;
                if (showValues)
                    caption = __ResStr("enumFmt", "{0} - {1}", enumVal, caption);

                items.Add(new SelectionItem<int> {
                    Text = caption,
                    Value = enumVal,
                    Tooltip = entry.Description,
                });
            }

            return htmlHelper.RenderDropDownSelectionList(name, Convert.ToInt32(value), items, HtmlAttributes: HtmlAttributes);
        }
        /// <summary>
        /// Renders an enumerated value for display.
        /// </summary>
        /// <param name="htmlHelper">HTML helper.</param>
        /// <param name="name">The name of the property being rendered.</param>
        /// <param name="value">The value of the property.</param>
        /// <param name="HtmlAttributes">Optional attributes.</param>
        /// <returns>An HTML string with the rendered dropdownlist.</returns>
        /// <remarks>
        /// A dropdownlist is rendered based on the enumerated type's EnumDescription attributes.</remarks>
#if MVC6
        public static HtmlString RenderEnumDisplay(this IHtmlHelper<object> htmlHelper, string name, object value, object HtmlAttributes = null) {
#else
        public static HtmlString RenderEnumDisplay(this HtmlHelper<object> htmlHelper, string name, object value, object HtmlAttributes = null) {
#endif
            bool showValues = UserSettings.GetProperty<bool>("ShowEnumValue");
            showValues = showValues && htmlHelper.GetControlInfo<bool>("", "ShowEnumValue", true);

            string desc;
            string caption = ObjectSupport.GetEnumDisplayInfo(value, out desc, ShowValue: showValues);

            if (HtmlAttributes != null || !string.IsNullOrWhiteSpace(desc)) {
                TagBuilder tag = new TagBuilder("span");
                IDictionary<string, object> htmlAttributes = FieldHelper.AnonymousObjectToHtmlAttributes(HtmlAttributes);
                htmlAttributes.Add(Basics.CssTooltipSpan, YetaWFManager.HtmlEncode(desc));
                tag.MergeAttributes(htmlAttributes, replaceExisting: true);
                tag.SetInnerText(caption);
                return tag.ToHtmlString(TagRenderMode.Normal);
            } else {
                return new HtmlString(caption);
            }
        }
    }
}
