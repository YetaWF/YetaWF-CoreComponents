/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using System.Threading.Tasks;
using YetaWF.Core.IO;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Views.Shared {

    public class USState<TModel> : RazorTemplate<TModel> { }

    public static class USStateHelper {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(USStateHelper), name, defaultValue, parms); }
#if MVC6
        public static async Task<HtmlString> RenderUSStateAsync(this IHtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#else
        public static async Task<HtmlString> RenderUSStateAsync(this HtmlHelper htmlHelper, string name, string model, int dummy = 0, object HtmlAttributes = null, bool Validation = true) {
#endif
            List<SelectionItem<string>> states = await ReadStatesListAsync();

            bool useDefault = !htmlHelper.GetControlInfo<bool>("", "NoDefault");
            if (useDefault) {
                states = (from s in states select s).ToList();//copy
                states.Insert(0, new SelectionItem<string> {
                    Text = __ResStr("default", "(select)"),
                    Tooltip = __ResStr("defaultTT", "Please make a selection"),
                    Value = "",
                });
            }
            return await htmlHelper.RenderDropDownSelectionListAsync<string>(name, model, states, HtmlAttributes: HtmlAttributes);
        }
#if MVC6
        public static async Task<HtmlString> RenderUSStateDisplayAsync(this IHtmlHelper htmlHelper, string name, string model) {
#else
        public static async Task<HtmlString> RenderUSStateDisplayAsync(this HtmlHelper htmlHelper, string name, string model) {
#endif
            List<SelectionItem<string>> states = await ReadStatesListAsync();
            if (model == null) model = "";
            string state = (from s in states where string.Compare(s.Value, model.ToUpper(), true) == 0 select s.Text).FirstOrDefault();
            return new HtmlString(state);
        }

        private static async Task<List<SelectionItem<string>>> ReadStatesListAsync() {
            using (await _lockObject.LockAsync()) { // short-term lock to build cached states list
                if (_statesList == null) {
                    Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
                    string url = VersionManager.GetAddOnTemplateUrl(package.Domain, package.Product, "USState");
                    string path = YetaWFManager.UrlToPhysical(url);
                    string file = Path.Combine(path, "USStates.txt");
                    _statesList = new List<SelectionItem<string>>();
                    if (!await FileSystem.FileSystemProvider.FileExistsAsync(file)) throw new InternalError("File {0} not found", file);

                    List<string> sts = await FileSystem.FileSystemProvider.ReadAllLinesAsync(file);
                    foreach (var st in sts) {
                        string[] s = st.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (s.Length != 2)
                            throw new InternalError("Invalid input in US states list - {0}", file);
                        _statesList.Add(new SelectionItem<string> { Text = s[1], Value = s[0].ToUpper() });
                    }
                }
            }
            return _statesList;
        }
        private static AsyncLock _lockObject = new AsyncLock();
        private static List<SelectionItem<string>> _statesList = null;
    }
}
