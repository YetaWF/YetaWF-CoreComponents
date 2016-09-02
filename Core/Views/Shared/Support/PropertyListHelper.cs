/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

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

    public interface IPropertyListSupport {
        List<string> GetCategories();
        List<PropertyListEntry> GetProperties(string category = null);
        List<PropertyListEntry> GetHiddenProperties();
    }

    public static class PropertyListSupport {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PropertyListSupport), name, defaultValue, parms); }

        public static MvcHtmlString RenderTabStripStart(this HtmlHelper htmlHelper, string controlId) {
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("<ul class='t_tabstrip'>");
            return hb.ToMvcHtmlString();
        }
        public static MvcHtmlString RenderTabStripEnd(this HtmlHelper htmlHelper, string controlId) {
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("</ul>");
            return hb.ToMvcHtmlString();
        }
        public static MvcHtmlString RenderTabPaneStart(this HtmlHelper htmlHelper, string controlId, int panel, string cssClass = "") {
            HtmlBuilder hb = new HtmlBuilder();
            if (!string.IsNullOrWhiteSpace(cssClass)) cssClass = " " + cssClass;
            hb.Append("<div class='t_table t_cat t_tabpanel{0}' data-tab='{1}' id='{2}'>", cssClass, panel, controlId + "_tab" + panel.ToString());
            return hb.ToMvcHtmlString();
        }
        public static MvcHtmlString RenderTabPaneEnd(this HtmlHelper htmlHelper, string controlId, int panel) {
            HtmlBuilder hb = new HtmlBuilder();
            hb.Append("</div>");
            return hb.ToMvcHtmlString();
        }

        public static MvcHtmlString RenderTabEntry(this HtmlHelper htmlHelper, string controlId, string label, string tooltip, int count) {
            HtmlBuilder hb = new HtmlBuilder();
            if (Manager.CurrentSite.TabStyle == YetaWF.Core.Site.TabStyleEnum.JQuery) {
                string tabId = controlId + "_tab" + count.ToString();
                hb.Append("<li data-tab='{0}'><a href='#{1}' {2}='{3}'>{4}</a></li>", count, tabId, Basics.CssTooltip, YetaWFManager.HtmlAttributeEncode(tooltip), YetaWFManager.HtmlEncode(label));
            } else {
                hb.Append("<li data-tab='{0}' {1}='{2}'>{3}</li>", count, Basics.CssTooltip, YetaWFManager.HtmlAttributeEncode(tooltip), YetaWFManager.HtmlEncode(label));
            }
            return hb.ToMvcHtmlString();
        }

        public static MvcHtmlString RenderTabInit(this HtmlHelper htmlHelper, string controlId) {
            ScriptBuilder sb = new ScriptBuilder();
            // About tab switching and YetaWF_PropertyList_PanelSwitched
            // This event occurs even for the first tab, but event handlers may not yet be attached.
            // This event is only intended to notify you that an OTHER tab is now active, which may have updated div dimensions because they're now
            // visible. Divs on the first tab are already visible.  DO NOT use this event for initialization purposes.
            if (Manager.CurrentSite.TabStyle == YetaWF.Core.Site.TabStyleEnum.JQuery) {
                sb.Append("$('#{0}').tabs({{\n", controlId);
                sb.Append("activate: function(ev,ui) {{ if (ui.newPanel!=undefined) $('#{0}').trigger('YetaWF_PropertyList_PanelSwitched', ui.newPanel); }}\n", controlId);
                sb.Append("});\n");
                // switch to the first tab
                // TODO: ? tabStrip.activateTab($("#@ControlId li").eq(0));
            } else if (Manager.CurrentSite.TabStyle == YetaWF.Core.Site.TabStyleEnum.Kendo) {
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.data.min.js");
                Manager.ScriptManager.AddKendoUICoreJsFile("kendo.tabstrip.min.js");
                sb.Append("var tabStrip = $('#{0}').kendoTabStrip({{\n", controlId);
                sb.Append("animation: false,\n");
                sb.Append("activate: function(ev) {{ if (ev.contentElement!=undefined) $('#{0}').trigger('YetaWF_PropertyList_PanelSwitched', $(ev.contentElement)); }}\n", controlId);
                sb.Append("}).data('kendoTabStrip');\n");
                sb.Append("// switch to the first tab\n");
                sb.Append("tabStrip.activateTab($('#{0} li').eq(0));\n", controlId);
            } else
                throw new InternalError("Unknown tab control style");
            return Manager.ScriptManager.AddNow(sb.ToString()).ToMvcHtmlString();
        }

        // Returns all categories implemented by this object - these are decorated with the [CategoryAttribute]
        public static List<string> GetCategories(object obj) {

            // use interface if object offers it
            IPropertyListSupport interf = obj as IPropertyListSupport;
            if (interf != null) return interf.GetCategories();

            // otherwise use default implementation to get the categories

            // get all properties that are shown
            var props = GetProperties(obj.GetType());

            // get the list of categories
            List<string> categories = (from property in props
                        let categoryAttribute = property.TryGetAttribute<CategoryAttribute>()
                        where categoryAttribute != null
                        select categoryAttribute.Value).Distinct().ToList();

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

            // use interface if object offers it
            IPropertyListSupport interf = obj as IPropertyListSupport;
            if (interf != null) return interf.GetProperties(category);

            // otherwise use default implementation
            List<PropertyListEntry> properties = new List<PropertyListEntry>();
            Type objType = obj.GetType();
            var props = GetProperties(objType, GridUsage: GridUsage);
            foreach (var prop in props) {
                if (category != null) {
                    CategoryAttribute cat = prop.TryGetAttribute<CategoryAttribute>();
                    if (cat == null || cat.Value != category)
                        continue;
                }
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
                       where !string.IsNullOrWhiteSpace(property.Description)  // This means it has to be a DescriptionAttribute (not a resource redirect)
                       orderby property.Order
                       select property;
        }

        public static List<PropertyListEntry> GetHiddenProperties(object obj) {

            // use interface if object offers it
            IPropertyListSupport interf = obj as IPropertyListSupport;
            if (interf != null) return interf.GetHiddenProperties();

            // otherwise use default implementation
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

        public static MvcHtmlString RenderPropertyListDisplay(this HtmlHelper<object> htmlHelper, string name, object model, int dummy = 0, bool ReadOnly = false) {
            return htmlHelper.RenderPropertyList(name, model, null, ReadOnly: true);
        }
        public static MvcHtmlString RenderPropertyList(this HtmlHelper<object> htmlHelper, string name, object model, string id = null, int dummy = 0, bool ReadOnly = false) {
            HtmlBuilder hb = new HtmlBuilder();
            Type modelType = model.GetType();
            ClassData classData = ObjectSupport.GetClassData(modelType);
            RenderHeader(hb, classData);

            hb.Append(RenderHidden(htmlHelper, model));
            bool showVariables = YetaWF.Core.Localize.UserSettings.GetProperty<bool>("ShowVariables");

            // property table
            HtmlBuilder hbProps = new HtmlBuilder();
            string divId = string.IsNullOrWhiteSpace(id) ? Manager.UniqueId() : id;
            hbProps.Append("<div id='{0}' class='yt_propertylist t_table t_edit'>", divId);
            hbProps.Append(RenderList(htmlHelper, model, null, showVariables, ReadOnly));
            hbProps.Append("</div>");

            if (!string.IsNullOrWhiteSpace(classData.Legend)) {
                TagBuilder tagFieldSet = new TagBuilder("fieldset");
                TagBuilder tagLegend = new TagBuilder("legend");
                tagLegend.SetInnerText(classData.Legend);
                tagFieldSet.InnerHtml = tagLegend.ToString() + hbProps.ToString();
                hb.Append(tagFieldSet.ToString());
            } else {
                hb.Append(hbProps);
            }
            RenderFooter(hb, classData);
            return hb.ToMvcHtmlString();
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
        private static MvcHtmlString RenderList(HtmlHelper<object> htmlHelper, object model, string category, bool showVariables, bool readOnly) {
            bool focusSet = Manager.WantFocus ? false : true;
            List<PropertyListEntry> properties = PropertyListSupport.GetPropertiesByCategory(model, category);
            HtmlBuilder hb = new HtmlBuilder();
            ScriptBuilder sb = new ScriptBuilder();

            foreach (PropertyListEntry property in properties) {
                bool labelDone = false;
                MvcHtmlString shtmlDisp = null;
                if (property.Restricted) {
                    shtmlDisp = MvcHtmlString.Create(__ResStr("demo", "This property is not available in Demo Mode"));
                } else if (readOnly || !property.Editable) {
                    shtmlDisp = htmlHelper.Display(property.Name);
                    string s = shtmlDisp.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(s)) {
                        if (property.SuppressEmpty)
                            continue;
                        shtmlDisp = MvcHtmlString.Create("&nbsp;");
                    }
                }
                hb.Append("<div class='t_row t_{0}'>", property.Name.ToLower());
                if (!string.IsNullOrWhiteSpace(property.TextAbove)) {
                    labelDone = true;
                    hb.Append("<div class='t_labels'>");
                    hb.Append(htmlHelper.ExtLabel(property.Name, ShowVariable: showVariables));
                    hb.Append("</div>");
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
                    hb.Append("<div class='t_labels'>");
                    hb.Append(htmlHelper.ExtLabel(property.Name, ShowVariable: showVariables));
                    hb.Append("</div>");
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
            return hb.ToMvcHtmlString();
        }
        /// <summary>
        /// Generate the control sets based on a model's ProcessIf attributes.
        /// </summary>
        /// <param name="htmlHelper">HtmlHelper object.</param>
        /// <param name="model">The model for which the control set is generated.</param>
        /// <param name="id">The HTML id of the property list.</param>
        /// <returns>The data used client-side to show/hide properties and to enable/disable validation.</returns>
        public static string GetControlSets(this HtmlHelper<object> htmlHelper, object model, string id) {

            List<PropertyListEntry> properties = PropertyListSupport.GetPropertiesByCategory(model, null);
            ScriptBuilder sb = new ScriptBuilder();
            List<string> selectionControls = new List<string>();

            sb.Append("{");
            sb.Append("'Id':{0},", YetaWFManager.Jser.Serialize(id));
            sb.Append("'Dependents':[");
            foreach (PropertyListEntry property in properties) {
                if (property.ProcIfAttr != null) {
                    if (!selectionControls.Contains(property.ProcIfAttr.Name))
                        selectionControls.Add(property.ProcIfAttr.Name);
                    sb.Append("{");
                    sb.Append("'Prop':{0},'ControlProp':{1},'Disable':{2},'Values':[",
                        YetaWFManager.Jser.Serialize(property.Name), YetaWFManager.Jser.Serialize(property.ProcIfAttr.Name), property.ProcIfAttr.Disable ? 1 : 0);
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
                sb.Append("{0},", YetaWFManager.Jser.Serialize(selectionControl));
            }
            sb.Append("],");
            sb.Append("}");
            return sb.ToString();
        }

        private static MvcHtmlString RenderHidden(HtmlHelper<object> htmlHelper, object model)
        {
            HtmlBuilder hb = new HtmlBuilder();
            List<PropertyListEntry> properties = PropertyListSupport.GetHiddenProperties(model);
            foreach (var property in properties) {
                hb.Append(htmlHelper.Display(property.Name));
            }
            return hb.ToMvcHtmlString();
        }
        public static MvcHtmlString RenderPropertyListTabbedDisplay(this HtmlHelper<object> htmlHelper, string name, object model, int dummy = 0, bool ReadOnly = false) {
            return htmlHelper.RenderPropertyListTabbed(name, model, null, ReadOnly: true);
        }
        public static MvcHtmlString RenderPropertyListTabbed(this HtmlHelper<object> htmlHelper, string name, object model, string id = null, int dummy = 0, bool ReadOnly = false) {

            List<string> categories = PropertyListSupport.GetCategories(model);
            if (categories.Count == 0) throw new InternalError("Unsupported model in PropertyListTabbed template - No categories defined");
            if (categories.Count == 1) // if there is only one tab, show as regular property list
                return RenderPropertyList(htmlHelper, name, model, id, ReadOnly: ReadOnly);

            HtmlBuilder hb = new HtmlBuilder();
            Type modelType = model.GetType();

            ClassData classData = ObjectSupport.GetClassData(modelType);
            RenderHeader(hb, classData);

            string divId = string.IsNullOrWhiteSpace(id) ? Manager.UniqueId() : id;
            hb.Append("<div id='{0}' class='yt_propertylisttabbed t_edit'>", divId);

            hb.Append(RenderHidden(htmlHelper, model));
            bool showVariables = YetaWF.Core.Localize.UserSettings.GetProperty<bool>("ShowVariables");

            // tabstrip
            hb.Append(htmlHelper.RenderTabStripStart(divId));
            int tabEntry = 0;
            foreach (string category in categories) {
                hb.Append(htmlHelper.RenderTabEntry(divId, category, "", tabEntry));
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

            return hb.ToMvcHtmlString();
        }

        public static void CorrectModelState(object model, ModelStateDictionary ModelState, string prefix = "") {
            if (model == null) return;
            Type modelType = model.GetType();
            if (ModelState.Keys.Count == 0) return;
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
