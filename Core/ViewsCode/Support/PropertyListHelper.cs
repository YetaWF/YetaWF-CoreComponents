/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
#endif

namespace YetaWF.Core.Views.Shared {

    public class PropertyList<TModel> : RazorTemplate<TModel> { }
    public class PropertyListTabbed<TModel> : RazorTemplate<TModel> { }

    public class PropertyListEntry {

        public PropertyListEntry(string name, object value, string uiHint, bool editable, bool restricted, string textAbove, string textBelow, bool suppressEmpty, ProcessIfAttribute procIfAttr, SubmitFormOnChangeAttribute.SubmitTypeEnum submit) {
            Name = name; Value = value; Editable = editable;
            Restricted = restricted;
            TextAbove = textAbove; TextBelow = textBelow;
            UIHint = uiHint;
            ProcIfAttr = procIfAttr;
            SuppressEmpty = suppressEmpty;
            SubmitType = submit;
        }
        public object Value { get; private set; }
        public string Name { get; private set; }
        public string TextAbove { get; private set; }
        public string TextBelow { get; private set; }
        public bool Editable { get; private set; }
        public bool Restricted { get; private set; }
        public string UIHint { get; private set; }
        public bool SuppressEmpty { get; private set; }
        public SubmitFormOnChangeAttribute.SubmitTypeEnum SubmitType { get; private set; }
        public ProcessIfAttribute ProcIfAttr { get; set; }
    };

    public static class PropertyListSupport {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PropertyListSupport), name, defaultValue, parms); }
#if MVC6
        public static HtmlString RenderTabStripStart(this IHtmlHelper htmlHelper, string controlId) {
#else
        public static HtmlString RenderTabStripStart(this HtmlHelper htmlHelper, string controlId) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("<ul class='t_tabstrip'>");
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderTabStripEnd(this IHtmlHelper htmlHelper, string controlId) {
#else
        public static HtmlString RenderTabStripEnd(this HtmlHelper htmlHelper, string controlId) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("</ul>");
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderTabPaneStart(this IHtmlHelper htmlHelper, string controlId, int panel, string cssClass = "") {
#else
        public static HtmlString RenderTabPaneStart(this HtmlHelper htmlHelper, string controlId, int panel, string cssClass = "") {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            if (!string.IsNullOrWhiteSpace(cssClass)) cssClass = " " + cssClass;
            hb.Append("<div class='t_table t_cat t_tabpanel{0}' data-tab='{1}' id='{2}'>", cssClass, panel, controlId + "_tab" + panel.ToString());
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderTabPaneEnd(this IHtmlHelper htmlHelper, string controlId, int panel) {
#else
        public static HtmlString RenderTabPaneEnd(this HtmlHelper htmlHelper, string controlId, int panel) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("</div>");
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderTabEntry(this IHtmlHelper htmlHelper, string controlId, string label, string tooltip, int count) {
#else
        public static HtmlString RenderTabEntry(this HtmlHelper htmlHelper, string controlId, string label, string tooltip, int count) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            if (Manager.CurrentSite.TabStyle == YetaWF.Core.Site.TabStyleEnum.JQuery) {
                string tabId = controlId + "_tab" + count.ToString();
                hb.Append("<li data-tab='{0}'><a href='#{1}' {2}='{3}'>{4}</a></li>", count, tabId, Basics.CssTooltip, YetaWFManager.HtmlAttributeEncode(tooltip), YetaWFManager.HtmlEncode(label));
            } else {
                hb.Append("<li data-tab='{0}' {1}='{2}'>{3}</li>", count, Basics.CssTooltip, YetaWFManager.HtmlAttributeEncode(tooltip), YetaWFManager.HtmlEncode(label));
            }
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderTabInit(this IHtmlHelper htmlHelper, string controlId, object model = null) {
#else
        public static HtmlString RenderTabInit(this HtmlHelper htmlHelper, string controlId, object model = null) {
#endif
            ScriptBuilder sb = new ScriptBuilder();
            // About tab switching and YetaWF_PropertyList_PanelSwitched
            // This event occurs even for the first tab, but event handlers may not yet be attached.
            // This event is only intended to notify you that an OTHER tab is now active, which may have updated div dimensions because they're now
            // visible. Divs on the first tab are already visible.  DO NOT use this event for initialization purposes.

            int activeTab = 0;
            string activeTabId = null;
            if (model != null) {
                // check if the model has an _ActiveTab property in which case we'll activate the tab and keep track of the active tab so it can be returned on submit
                if (ObjectSupport.TryGetPropertyValue<int>(model, "_ActiveTab", out activeTab, 0)) {
                    // add a hidden field for _ActiveTab property
                    string name = htmlHelper.FieldName("");
                    activeTabId = Manager.UniqueId();
                    sb.Append(@"$('#{0}').append(""<input name='{1}._ActiveTab' type='hidden' value='{2}' id='{3}'/>"");", controlId, name, activeTab, activeTabId);
                }
            }
            if (Manager.CurrentSite.TabStyle == YetaWF.Core.Site.TabStyleEnum.JQuery) {
                sb.Append("$('#{0}').tabs({{\n", controlId);
                sb.Append("active: {0},", activeTab);
                sb.Append("activate: function(ev,ui) { if (ui.newPanel!=undefined) {");
                sb.Append("$('#{0}').trigger('YetaWF_PropertyList_PanelSwitched', ui.newPanel);\n", controlId);
                if (!string.IsNullOrWhiteSpace(activeTabId))
                    sb.Append("$('#{0}').val((ui.newTab.length > 0) ? ui.newTab.attr('data-tab') : -1);", activeTabId);
                sb.Append("}}\n");
                sb.Append("})");
                sb.Append(";\n");
            } else if (Manager.CurrentSite.TabStyle == YetaWF.Core.Site.TabStyleEnum.Kendo) {
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.data.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.tabstrip.min.js");
                // mark the active tab with .k-state-active before initializing the tabstrip
                sb.Append("var $tabs = $('#{0}>ul>li');", controlId);
                sb.Append("$tabs.removeClass('k-state-active');");
                sb.Append("$tabs.eq({0}).addClass('k-state-active');", activeTab);
                // init tab control
                sb.Append("var $ts = $('#{0}');", controlId);
                sb.Append("var tabStrip = $ts.kendoTabStrip({{\n", controlId);
                sb.Append("animation: false,\n");
                sb.Append("activate: function(ev) { if (ev.contentElement!=undefined) {");
                sb.Append("$('#{0}').trigger('YetaWF_PropertyList_PanelSwitched', $(ev.contentElement));", controlId);
                if (!string.IsNullOrWhiteSpace(activeTabId))
                    sb.Append("$('#{0}').val($(ev.item).attr('data-tab'));", activeTabId);
                sb.Append("}}\n");
                sb.Append("}).data('kendoTabStrip');\n");
            } else
                throw new InternalError("Unknown tab control style");

            if (Manager.CurrentSite.JSLocation == Site.JSLocationEnum.Top)
                return Manager.ScriptManager.AddNow(sb.ToString()).ToHtmlString();
            else {
                Manager.ScriptManager.AddLast(sb.ToString());
                return new HtmlString("");
            }
        }

        // Returns all categories implemented by this object - these are decorated with the [CategoryAttribute]
        public static List<string> GetCategories(object obj) {

            // get all properties that are shown
            List<PropertyData> props = GetProperties(obj.GetType()).ToList();

            // get the list of categories
            List<string> categories = new List<string>();
            foreach (PropertyData prop in props)
                categories.AddRange(prop.Categories);
            categories = categories.Distinct().ToList();

            // order (if there is a CategoryOrder property)
            PropertyInfo piCat = ObjectSupport.TryGetProperty(obj.GetType(), "CategoryOrder");
            if (piCat != null) {
                List<string> orderedCategories = (List<string>) piCat.GetValue(obj);
                List<string> allCategories = new List<string>();
                // verify that all returned categories in the list of ordered categories actually exist
                foreach (var oCat in orderedCategories) {
                    if (categories.Contains(oCat))
                        allCategories.Add(oCat);
                    //else
                        //throw new InternalError("No properties exist in category {0} found in CategoryOrder for type {1}.", oCat, obj.GetType().Name);
                }
                // if any are missing, add them to the end of the list
                foreach (var cat in categories) {
                    if (!allCategories.Contains(cat))
                        allCategories.Add(cat);
                }
                categories = allCategories;
            }
            return categories;
        }
        public static List<PropertyListEntry> GetPropertiesByCategory(object obj, string category = null, int dummy = 0, bool Sorted = false, bool GridUsage = false) {

            List<PropertyListEntry> properties = new List<PropertyListEntry>();
            Type objType = obj.GetType();
            var props = GetProperties(objType, GridUsage: GridUsage);
            foreach (var prop in props) {
                if (!string.IsNullOrWhiteSpace(category) && !prop.Categories.Contains(category))
                    continue;
                SuppressIfEqualAttribute supp = prop.TryGetAttribute<SuppressIfEqualAttribute>();
                if (supp != null) { // possibly suppress this property
                    if (supp.IsEqual(obj))
                        continue;// suppress this as requested
                }
                SuppressIfNotEqualAttribute suppn = prop.TryGetAttribute<SuppressIfNotEqualAttribute>();
                if (suppn != null) { // possibly suppress this property
                    if (suppn.IsNotEqual(obj))
                        continue;// suppress this as requested
                }
                bool editable = prop.PropInfo.CanWrite;
                if (editable) {
                    if (prop.ReadOnly)
                        editable = false;
                }
                SuppressEmptyAttribute suppressEmptyAttr = null;
                suppressEmptyAttr = prop.TryGetAttribute<SuppressEmptyAttribute>();

                SubmitFormOnChangeAttribute submitFormOnChangeAttr = null;
                submitFormOnChangeAttr = prop.TryGetAttribute<SubmitFormOnChangeAttribute>();

                ProcessIfAttribute procIfAttr = null;
                procIfAttr = prop.TryGetAttribute<ProcessIfAttribute>();

                bool restricted = false;
                if (Manager.IsDemo) {
                    ExcludeDemoModeAttribute exclDemoAttr = prop.TryGetAttribute<ExcludeDemoModeAttribute>();
                    if (exclDemoAttr != null)
                        restricted = true;
                }
                if (GridUsage)
                    properties.Add(new PropertyListEntry(prop.Name, prop.GetPropertyValue<object>(obj), prop.UIHint, editable, restricted, null, null, suppressEmptyAttr != null, null, submitFormOnChangeAttr != null? submitFormOnChangeAttr.Value : SubmitFormOnChangeAttribute.SubmitTypeEnum.None));
                else
                    properties.Add(new PropertyListEntry(prop.Name, prop.GetPropertyValue<object>(obj), prop.UIHint, editable, restricted, prop.TextAbove, prop.TextBelow, suppressEmptyAttr != null, procIfAttr, submitFormOnChangeAttr != null ? submitFormOnChangeAttr.Value : SubmitFormOnChangeAttribute.SubmitTypeEnum.None));
            }
            if (Sorted)
                return (from p in properties orderby p.Name ascending select p).ToList<PropertyListEntry>();
            else
                return properties;
        }

        // returns all properties for an object that have a description, in sorted order
        private static IEnumerable<PropertyData> GetProperties(Type objType, bool GridUsage = false) {
            if (GridUsage)
                return ObjectSupport.GetPropertyData(objType);
            else
                return from property in ObjectSupport.GetPropertyData(objType)
                       where property.Description != null  // This means it has to be a DescriptionAttribute (not a resource redirect)
                       orderby property.Order
                       select property;
        }

        public static List<PropertyListEntry> GetHiddenProperties(object obj) {

            List<PropertyListEntry> properties = new List<PropertyListEntry>();
            List<PropertyData> props = ObjectSupport.GetPropertyData(obj.GetType());
            foreach (var prop in props) {
                if (!prop.PropInfo.CanRead) continue;
                if (prop.UIHint != "Hidden")
                    continue;
                properties.Add(new PropertyListEntry(prop.Name, prop.GetPropertyValue<object>(obj), "Hidden", false, false, null, null, false, null, SubmitFormOnChangeAttribute.SubmitTypeEnum.None));
            }
            return properties;
        }
#if MVC6
        public static HtmlString RenderPropertyListDisplay(this IHtmlHelper htmlHelper, string name, object model, int dummy = 0, bool ReadOnly = false) {
#else
        public static HtmlString RenderPropertyListDisplay(this HtmlHelper<object> htmlHelper, string name, object model, int dummy = 0, bool ReadOnly = false) {
#endif
            return htmlHelper.RenderPropertyList(name, model, null, ReadOnly: true);
        }
#if MVC6
        public static HtmlString RenderPropertyList(this IHtmlHelper htmlHelper, string name, object model, string id = null, int dummy = 0, bool ReadOnly = false) {
#else
        public static HtmlString RenderPropertyList(this HtmlHelper<object> htmlHelper, string name, object model, string id = null, int dummy = 0, bool ReadOnly = false) {
#endif
            HtmlBuilder hb = new HtmlBuilder();
            Type modelType = model.GetType();
            ClassData classData = ObjectSupport.GetClassData(modelType);
            RenderHeader(hb, classData);

            hb.Append(RenderHidden(htmlHelper, model));
            bool showVariables = YetaWF.Core.Localize.UserSettings.GetProperty<bool>("ShowVariables");

            // property table
            HtmlBuilder hbProps = new HtmlBuilder();
            string divId = string.IsNullOrWhiteSpace(id) ? Manager.UniqueId() : id;
            hbProps.Append("<div id='{0}' class='yt_propertylist t_table {1}'>", divId, ReadOnly ? "t_display" : "t_edit");
            hbProps.Append(RenderList(htmlHelper, model, null, showVariables, ReadOnly));
            hbProps.Append("</div>");

            if (!string.IsNullOrWhiteSpace(classData.Legend)) {
                TagBuilder tagFieldSet = new TagBuilder("fieldset");
                TagBuilder tagLegend = new TagBuilder("legend");
                tagLegend.SetInnerText(classData.Legend);
                tagFieldSet.SetInnerHtml(tagLegend.ToString(TagRenderMode.Normal) + hbProps.ToString());
                hb.Append(tagFieldSet.ToString(TagRenderMode.Normal));
            } else {
                hb.Append(hbProps.ToHtmlString());
            }
            RenderFooter(hb, classData);

            if (!ReadOnly) {
                string script = htmlHelper.GetControlSets(model, divId);
                if (!string.IsNullOrWhiteSpace(script)) {
                    ScriptBuilder sb = new ScriptBuilder();
                    sb.Append("YetaWF_PropertyList.init('{0}', {1}, {2});", divId, script, Manager.InPartialView ? 1 : 0);
                    Manager.ScriptManager.AddLastDocumentReady(sb);
                }
            }

            return hb.ToHtmlString();
        }

        private static void RenderHeader(HtmlBuilder hb, ClassData classData) {
            if (!string.IsNullOrWhiteSpace(classData.Header)) {
                hb.Append("<div class='y_header'>");
                if (classData.Header.StartsWith("-"))
                    hb.Append(classData.Header.Substring(1));
                else
                    hb.Append(YetaWFManager.HtmlEncode(classData.Header));
                hb.Append("</div>");
            }
        }
        private static void RenderFooter(HtmlBuilder hb, ClassData classData) {
            if (!string.IsNullOrWhiteSpace(classData.Footer)) {
                hb.Append("<div class='y_footer'>");
                if (classData.Footer.StartsWith("-"))
                    hb.Append(classData.Footer.Substring(1));
                else
                    hb.Append(YetaWFManager.HtmlEncode(classData.Footer));
                hb.Append("</div>");
            }
        }
#if MVC6
        private static HtmlString RenderList(IHtmlHelper htmlHelper, object model, string category, bool showVariables, bool readOnly)
#else
        private static HtmlString RenderList(HtmlHelper<object> htmlHelper, object model, string category, bool showVariables, bool readOnly)
#endif
        {
            bool focusSet = Manager.WantFocus ? false : true;
            List<PropertyListEntry> properties = PropertyListSupport.GetPropertiesByCategory(model, category);
            HtmlBuilder hb = new HtmlBuilder();
            ScriptBuilder sb = new ScriptBuilder();

            foreach (PropertyListEntry property in properties) {
                bool labelDone = false;
                HtmlString shtmlDisp = null;
                if (property.Restricted) {
                    shtmlDisp = new HtmlString(__ResStr("demo", "This property is not available in Demo Mode"));
                } else if (readOnly || !property.Editable) {
#if MVC6
                    shtmlDisp = new HtmlString(htmlHelper.Display(property.Name).AsString());
#else
                    shtmlDisp = htmlHelper.Display(property.Name);
#endif
                    string s = shtmlDisp.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(s)) {
                        if (property.SuppressEmpty)
                            continue;
                        shtmlDisp = new HtmlString("&nbsp;");
                    }
                }
                hb.Append("<div class='t_row t_{0}'>", property.Name.ToLower());
                if (!string.IsNullOrWhiteSpace(property.TextAbove)) {
                    labelDone = true;
                    HtmlString hs = htmlHelper.ExtLabel(property.Name, ShowVariable: showVariables, SuppressIfEmpty: true);
                    if (hs != HtmlStringExtender.Empty) {
                        hb.Append("<div class='t_labels'>");
                        hb.Append(hs);
                        hb.Append("</div>");
                    }
                    hb.Append("<div class='t_vals t_textabove'>");
                    if (property.TextAbove.StartsWith("-"))
                        hb.Append(property.TextAbove.Substring(1));
                    else
                        hb.Append(YetaWFManager.HtmlEncode(property.TextAbove));
                    hb.Append("</div>");
                }
                if (labelDone) {
                    hb.Append("<div class='t_labels t_fillerabove'>&nbsp;</div>");
                } else {
                    HtmlString hs = htmlHelper.ExtLabel(property.Name, ShowVariable: showVariables, SuppressIfEmpty: true);
                    if (hs != HtmlStringExtender.Empty) {
                        hb.Append("<div class='t_labels'>");
                        hb.Append(hs);
                        hb.Append("</div>");
                    }
                }
                if (!readOnly && property.Editable && !property.Restricted) {
                    string cls = "t_vals" + (!focusSet ? " focusonme" : "");
                    switch (property.SubmitType) {
                        default:
                        case SubmitFormOnChangeAttribute.SubmitTypeEnum.None:
                            break;
                        case SubmitFormOnChangeAttribute.SubmitTypeEnum.Submit:
                            cls += " ysubmitonchange";
                            break;
                        case SubmitFormOnChangeAttribute.SubmitTypeEnum.Apply:
                            cls += " yapplyonchange";
                            break;
                    }
                    focusSet = true;
                    hb.Append("<div class='{0}'>", cls);
                    hb.Append(htmlHelper.Editor(property.Name));
                    hb.Append(htmlHelper.ValidationMessage(property.Name));
                    hb.Append("</div>");
                } else {
                    hb.Append("<div class='t_vals t_val'>");
                    hb.Append(shtmlDisp);
                    hb.Append("</div>");
                }
                if (!string.IsNullOrWhiteSpace(property.TextBelow)) {
                    hb.Append("<div class='t_labels t_fillerbelow'>&nbsp;</div>");
                    hb.Append("<div class='t_vals t_textbelow'>");
                    if (property.TextBelow.StartsWith("-"))
                        hb.Append(property.TextBelow.Substring(1));
                    else
                        hb.Append(YetaWFManager.HtmlEncode(property.TextBelow));
                    hb.Append("</div>");
                }
                hb.Append("</div>");
            }
            return hb.ToHtmlString();
        }
        /// <summary>
        /// Generate the control sets based on a model's ProcessIf attributes.
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper object.</param>
        /// <param name="model">The model for which the control set is generated.</param>
        /// <param name="id">The HTML id of the property list.</param>
        /// <returns>The data used client-side to show/hide properties and to enable/disable validation.</returns>
#if MVC6
        public static string GetControlSets(this IHtmlHelper htmlHelper, object model, string id) {
#else
        public static string GetControlSets(this HtmlHelper<object> htmlHelper, object model, string id) {
#endif
            List<PropertyListEntry> properties = PropertyListSupport.GetPropertiesByCategory(model, null);
            ScriptBuilder sb = new ScriptBuilder();
            List<string> selectionControls = new List<string>();

            sb.Append("{");
            sb.Append("'Id':{0},", YetaWFManager.JsonSerialize(id));
            sb.Append("'Dependents':[");
            foreach (PropertyListEntry property in properties) {
                if (property.ProcIfAttr != null) {
                    if (!selectionControls.Contains(property.ProcIfAttr.Name))
                        selectionControls.Add(property.ProcIfAttr.Name);
                    sb.Append("{");
                    sb.Append("'Prop':{0},'ControlProp':{1},'Disable':{2},'Values':[",
                        YetaWFManager.JsonSerialize(property.Name), YetaWFManager.JsonSerialize(property.ProcIfAttr.Name), property.ProcIfAttr.Disable ? 1 : 0);
                    foreach (object obj in property.ProcIfAttr.Objects) {
                        int i = Convert.ToInt32(obj);
                        sb.Append("{0},", i);
                    }
                    sb.Append("]},");
                }
            }
            sb.Append("],");

            if (selectionControls.Count == 0) return null;

            sb.Append("'Controls':[");
            foreach (string selectionControl in selectionControls) {
                sb.Append("{0},", YetaWFManager.JsonSerialize(selectionControl));
            }
            sb.Append("],");
            sb.Append("}");
            return sb.ToString();
        }

#if MVC6
        private static HtmlString RenderHidden(IHtmlHelper htmlHelper, object model)
#else
        private static HtmlString RenderHidden(HtmlHelper<object> htmlHelper, object model)
#endif
        {
            HtmlBuilder hb = new HtmlBuilder();
            List<PropertyListEntry> properties = PropertyListSupport.GetHiddenProperties(model);
            foreach (var property in properties) {
                hb.Append(htmlHelper.Display(property.Name));
            }
            return hb.ToHtmlString();
        }
#if MVC6
        public static HtmlString RenderPropertyListTabbedDisplay(this IHtmlHelper htmlHelper, string name, object model, int dummy = 0, bool ReadOnly = false) {
#else
        public static HtmlString RenderPropertyListTabbedDisplay(this HtmlHelper<object> htmlHelper, string name, object model, int dummy = 0, bool ReadOnly = false) {
#endif
            return htmlHelper.RenderPropertyListTabbed(name, model, null, ReadOnly: true);
        }
#if MVC6
        public static HtmlString RenderPropertyListTabbed(this IHtmlHelper htmlHelper, string name, object model, string id = null, int dummy = 0, bool ReadOnly = false) {
#else
        public static HtmlString RenderPropertyListTabbed(this HtmlHelper<object> htmlHelper, string name, object model, string id = null, int dummy = 0, bool ReadOnly = false) {
#endif
            Manager.AddOnManager.AddTemplate("PropertyList"); /*we're using the same javascript as the regular propertylist template */

            List<string> categories = PropertyListSupport.GetCategories(model);
            if (categories.Count <= 1) // if there is only one tab, show as regular property list
                return RenderPropertyList(htmlHelper, name, model, id, ReadOnly: ReadOnly);

            HtmlBuilder hb = new HtmlBuilder();
            Type modelType = model.GetType();

            ClassData classData = ObjectSupport.GetClassData(modelType);
            RenderHeader(hb, classData);

            string divId = string.IsNullOrWhiteSpace(id) ? Manager.UniqueId() : id;
            hb.Append("<div id='{0}' class='yt_propertylisttabbed {1}'>", divId, ReadOnly ? "t_display" : "t_edit");

            hb.Append(RenderHidden(htmlHelper, model));
            bool showVariables = YetaWF.Core.Localize.UserSettings.GetProperty<bool>("ShowVariables");

            // tabstrip
            hb.Append(htmlHelper.RenderTabStripStart(divId));
            int tabEntry = 0;
            foreach (string category in categories) {
                string cat = category;
                if (classData.Categories.ContainsKey(cat))
                    cat = classData.Categories[cat];
                hb.Append(htmlHelper.RenderTabEntry(divId, cat, "", tabEntry));
                ++tabEntry;
            }
            hb.Append(htmlHelper.RenderTabStripEnd(divId));

            // panels
            int panel = 0;
            foreach (string category in categories) {
                hb.Append(htmlHelper.RenderTabPaneStart(divId, panel));
                hb.Append(RenderList(htmlHelper, model, category, showVariables, ReadOnly));
                hb.Append(htmlHelper.RenderTabPaneEnd(divId, panel));
                ++panel;
            }

            hb.Append("</div>");
            hb.Append(htmlHelper.RenderTabInit(divId));

            RenderFooter(hb, classData);

            if (!ReadOnly) {
                string script = htmlHelper.GetControlSets(model, divId);
                if (!string.IsNullOrWhiteSpace(script)) {
                    ScriptBuilder sb = new ScriptBuilder();
                    sb.Append("YetaWF_PropertyList.init('{0}', {1}, {2});", divId, script, Manager.InPartialView ? 1 : 0);
                    Manager.ScriptManager.AddLastDocumentReady(sb);
                }
            }

            return hb.ToHtmlString();
        }

        public static void CorrectModelState(object model, ModelStateDictionary ModelState, string prefix = "") {
            if (model == null) return;
            Type modelType = model.GetType();
            if (ModelState.Keys.Count() == 0) return;
            List<PropertyData> props = ObjectSupport.GetPropertyData(modelType);
            foreach (var prop in props) {
                if (!ModelState.Keys.Contains(prefix + prop.Name)) {
                    // check if the property name is for a class
                    string subPrefix = prefix + prop.Name + ".";
                    if ((from k in ModelState.Keys where k.StartsWith(subPrefix) select k).FirstOrDefault() != null) {
                        object subObject = prop.PropInfo.GetValue(model);
                        CorrectModelState(subObject, ModelState, subPrefix);
                    }
                    continue;
                }
                // Only one of the Requiredxxx and Suppressxxx attributes is supported
                {
                    RequiredIfInRangeAttribute reqIfInRange = prop.TryGetAttribute<RequiredIfInRangeAttribute>();
                    if (reqIfInRange != null) {
                        if (!reqIfInRange.InRange(model)) {
                            ModelState.Remove(prefix + prop.Name);
                            continue;
                        }
                    }
                }
                {
                    RequiredIfNotAttribute reqIfNot = prop.TryGetAttribute<RequiredIfNotAttribute>();
                    if (reqIfNot != null) {
                        if (!reqIfNot.IsNot(model)) {
                            ModelState.Remove(prefix + prop.Name);
                            continue;
                        }
                    }
                }
                {
                    RequiredIfAttribute reqIf = prop.TryGetAttribute<RequiredIfAttribute>();
                    if (reqIf != null) {
                        if (!reqIf.Is(model)) {
                            ModelState.Remove(prefix + prop.Name);
                            continue;
                        }
                    }
                }
                {
                    RequiredIfSupplied reqIfSupplied = prop.TryGetAttribute<RequiredIfSupplied>();
                    if (reqIfSupplied != null) {
                        if (!reqIfSupplied.IsSupplied(model)) {
                            ModelState.Remove(prefix + prop.Name);
                            continue;
                        }
                    }
                }
                {
                    SuppressIfEqualAttribute suppIfEqual = prop.TryGetAttribute<SuppressIfEqualAttribute>();
                    if (suppIfEqual != null) {
                        if (suppIfEqual.IsEqual(model)) {
                            ModelState.Remove(prefix + prop.Name);
                            continue;
                        }
                    }
                }
                {
                    SuppressIfNotEqualAttribute suppIfNotEqual = prop.TryGetAttribute<SuppressIfNotEqualAttribute>();
                    if (suppIfNotEqual != null) {
                        if (suppIfNotEqual.IsNotEqual(model)) {
                            ModelState.Remove(prefix + prop.Name);
                            continue;
                        }
                    }
                }
                {
                    ProcessIfAttribute procIf = prop.TryGetAttribute<ProcessIfAttribute>();
                    if (procIf != null) {
                        if (procIf.Processing(model))
                            continue; // we're processing this
                        // we're not processing this
                        ModelState.Remove(prefix + prop.Name);
                        continue;
                    }
                }
            }
        }
    }
}
