/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using System.Threading.Tasks;
using YetaWF.Core.Templates;
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
            List<SelectionItem<string>> states = await USState.ReadStatesListAsync();

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
            List<SelectionItem<string>> states = await USState.ReadStatesListAsync();
            if (model == null) model = "";
            string state = (from s in states where string.Compare(s.Value, model.ToUpper(), true) == 0 select s.Text).FirstOrDefault();
            return new HtmlString(state);
        }
    }
}
