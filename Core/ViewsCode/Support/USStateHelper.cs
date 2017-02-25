﻿/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Views.Shared {

    public class USState<TModel> : RazorTemplate<TModel> { }

    public static class USStateHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(USStateHelper), name, defaultValue, parms); }
#if MVC6
        public static MvcHtmlString RenderUSState(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#else
        public static MvcHtmlString RenderUSState(this HtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, string ModelNameOverride = null, bool Validation = true) {
#endif
            List<SelectionItem<string>> states = ReadStatesList();

            bool useDefault = !htmlHelper.GetControlInfo<bool>("", "NoDefault");
            if (useDefault) {
                states = (from s in states select s).ToList();//copy
                states.Insert(0, new SelectionItem<string> {
                    Text = __ResStr("default", "(select)"),
                    Tooltip = __ResStr("defaultTT", "Please make a selection"),
                    Value = "",
                });
            }
            return htmlHelper.RenderDropDownSelectionList<string>(name, model, states, HtmlAttributes: HtmlAttributes);
        }
#if MVC6
        public static MvcHtmlString RenderUSStateDisplay(this IHtmlHelper htmlHelper, string name, string model) {
#else
        public static MvcHtmlString RenderUSStateDisplay(this HtmlHelper htmlHelper, string name, string model) {
#endif
            List<SelectionItem<string>> states = ReadStatesList();
            string state = (from s in states where string.Compare(s.Value, model.ToUpper(), true) == 0 select s.Text).FirstOrDefault();
            return MvcHtmlString.Create(state);
        }

        private static List<SelectionItem<string>> ReadStatesList() {
            if (_statesList == null) {
                Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                string url = VersionManager.GetAddOnTemplateUrl(package.Domain, package.Product, "USState");
                string path = YetaWFManager.UrlToPhysical(url);
                string file = Path.Combine(path, "USStates.txt");
                _statesList = new List<SelectionItem<string>>();
                if (!File.Exists(file)) throw new InternalError("File {0} not found");

                string[] sts = File.ReadAllLines(file);
                foreach (var st in sts) {
                    string[] s = st.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length != 2)
                        throw new InternalError("Invalid input in US states list.");
                    _statesList.Add(new SelectionItem<string> { Text = s[1], Value = s[0].ToUpper() });
                }
            }
            return _statesList;
        }
        private static List<SelectionItem<string>> _statesList = null;
    }
}