using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Endpoints {

    public class YetaWFEndpoints {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(YetaWFEndpoints), name, defaultValue, parms); }

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        /// <summary>
        /// Format a partial Url for a package, derived from the package and endpoint class.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="type">The type of the class implementing the endpoint. The class name must end in "Endpoints".</param>
        /// <returns>A formatted partial Url to access the API endpoint.</returns>
        /// <exception cref="InternalError"></exception>
        protected static string GetPackageApiRoute(Package package, Type type) {
            string className = type.Name;
            if (!className.EndsWith("Endpoints"))
                throw new InternalError($"Class {className} is not an endpoint");
            className = className.Substring(0, className.Length - "Endpoints".Length);
            return $"{Globals.ApiPrefix}/{package.AreaName}/{className}/";
        }

        /// <summary>
        /// Format a Url for an enpoint, derived from the package, endpoint class and endpoint action.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="type">The type of the class implementing the endpoint. The class name must end in "Endpoints".</param>
        /// <param name="endpoint">The name of the endpoint action.</param>
        /// <returns>A formatted Url to access the API endpoint.</returns>
        /// <exception cref="InternalError"></exception>
        protected static string GetPackageApiEndpoint(Package package, Type type, string endpoint) {
            string className = type.Name;
            if (!className.EndsWith("Endpoints"))
                throw new InternalError($"Class {className} is not an endpoint");
            className = className.Substring(0, className.Length - "Endpoints".Length);
            return $"{Globals.ApiPrefix}/{package.AreaName}/{className}/{endpoint}";
        }

        public static async Task<ModuleDefinition> GetModuleAsync(Guid? moduleGuid = null) {
            ModuleDefinition? mod = null;
            if (moduleGuid is null) {
                string? s = null;
                if (Manager.IsGetRequest) {
                    s = Manager.RequestQueryString[Basics.ModuleGuid];
                } else if (Manager.IsPostRequest) {
                    s = Manager.RequestForm[Basics.ModuleGuid];
                    if (string.IsNullOrWhiteSpace(s))
                        s = Manager.RequestQueryString[Basics.ModuleGuid];
                }
                if (!string.IsNullOrWhiteSpace(s))
                    moduleGuid = new Guid(s);
            }
            if (moduleGuid is null) throw new InternalError("No module Guid available");
            mod = await ModuleDefinition.LoadAsync((Guid)moduleGuid);
            return mod ?? throw new InternalError("No ModuleDefinition available");
        }

        /// <summary>
        /// The type of form reload used with the Reload method.
        /// </summary>
        protected enum ReloadEnum {
            /// <summary>
            /// The entire page is reloaded.
            /// </summary>
            Page = 1,
            /// <summary>
            /// The entire module is reloaded. Not currently supported. Use Page to reload the entire page instead.
            /// </summary>
            Module = 2, // TODO: The entire module is not currently supported - use page reload instead
            /// <summary>
            /// Parts of the module are reloaded. E.g., in a grid control the data is reloaded.
            /// </summary>
            ModuleParts = 3
        }

        /// <summary>
        /// Returns a javascript result, indicating that the submission was successfully processed, causing a page or module reload, optionally with a popup message.
        /// </summary>
        /// <param name="PopupText">The optional text of the popup message to be displayed. If not specified, no popup will be shown.</param>
        /// <param name="PopupTitle">The optional title of the popup message to be displayed. If not specified, the default is "Success".</param>
        /// <param name="Reload">The method with which the current page or module is processed, i.e., by reloading the page or module.</param>
        protected static IResult Reload(ReloadEnum reload = ReloadEnum.Page, string ? PopupText = null, string? PopupTitle = null) {
            return Results.Json(reload switch {
                ReloadEnum.Module => Reload_Module(PopupText, PopupTitle),
                ReloadEnum.ModuleParts => Reload_ModuleParts(PopupText, PopupTitle),
                _ => Reload_Page(PopupText, PopupTitle),
            });
        }
        private static string Reload_Module(string? popupText, string? popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModule);
                return sb.ToString();
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("$YetaWF.message({0}, {1}, function() {{ $YetaWF.reloadModule(); }});", popupText, popupTitle);
                return sb.ToString();
            }
        }
        private static string Reload_ModuleParts(string? popupText, string? popupTitle) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                return sb.ToString();
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReloadModuleParts);
                sb.Append("$YetaWF.message({0}, {1});", popupText, popupTitle);
                return sb.ToString();
            }
        }
        protected static string Reload_Page(string? popupText = null, string? popupTitle = null) {
            ScriptBuilder sb = new ScriptBuilder();
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                sb.Append(Basics.AjaxJavascriptReloadPage);
                return sb.ToString();
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                sb.Append(Basics.AjaxJavascriptReturn);
                sb.Append("$YetaWF.message({0}, {1}, function() {{ $YetaWF.reloadPage(true); }});", popupText, popupTitle);
                return sb.ToString();
            }
        }
    }
}
