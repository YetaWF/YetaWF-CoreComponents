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
            return $"{Globals.ApiPrefix}{GetPackageApplicationRoute(package, type)}";
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
            return $"{Globals.ApiPrefix}{GetPackageApplicationEndpoint(package, type, endpoint)}";
        }

        /// <summary>
        /// Format a partial application Url for a package, derived from the package and endpoint class.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="type">The type of the class implementing the endpoint. The class name must end in "Endpoints".</param>
        /// <returns>A formatted partial Url to access the API endpoint.</returns>
        /// <exception cref="InternalError"></exception>
        protected static string GetPackageApplicationRoute(Package package, Type type) {
            string className = type.Name;
            if (!className.EndsWith("Endpoints"))
                throw new InternalError($"Class {className} is not an endpoint");
            className = className.Substring(0, className.Length - "Endpoints".Length);
            return $"/{package.AreaName}/{className}/";
        }

        /// <summary>
        /// Format an application Url for an enpoint, derived from the package, endpoint class and endpoint action.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="type">The type of the class implementing the endpoint. The class name must end in "Endpoints".</param>
        /// <param name="endpoint">The name of the endpoint action.</param>
        /// <returns>A formatted Url to access the API endpoint.</returns>
        /// <exception cref="InternalError"></exception>
        protected static string GetPackageApplicationEndpoint(Package package, Type type, string endpoint) {
            string className = type.Name;
            if (!className.EndsWith("Endpoints"))
                throw new InternalError($"Class {className} is not an endpoint");
            className = className.Substring(0, className.Length - "Endpoints".Length);
            return $"/{package.AreaName}/{className}/{endpoint}";
        }

        public static async Task<ModuleDefinition> GetModuleAsync(Guid? moduleGuid = null) {
            if (moduleGuid is null) throw new InternalError("No module Guid available");
            ModuleDefinition? mod = await ModuleDefinition.LoadAsync((Guid)moduleGuid);
            return mod ?? throw new InternalError("No ModuleDefinition available");
        }
        public static async Task<TMod> GetModuleAsync<TMod>(Guid? moduleGuid = null) where TMod: ModuleDefinition {
            ModuleDefinition mod = await GetModuleAsync(moduleGuid);
            return (TMod)mod;
        }


        /// <summary>
        /// The type of form reload used with the Reload method.
        /// </summary>
        protected enum ReloadEnum {
            /// <summary>
            /// Nothing is reloaded.
            /// </summary>
            Nothing = 0,
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
        /// Returns a javascript result, indicating that the submission was successfully processed and displays a popup message.
        /// </summary>
        /// <param name="PopupText">The text of the popup message to be displayed.</param>
        /// <param name="PopupTitle">The optional title of the popup message to be displayed. If not specified, the default is "Success".</param>
        protected static IResult Done(string? PopupText = null, string? PopupTitle = null, bool ForcePopup = false) {
            if (string.IsNullOrWhiteSpace(PopupText))
                return Results.Json(Reload(string.Empty, string.Empty, null, null, ForcePopup));
            else
                return Results.Json(Reload(string.Empty, "$YetaWF.message({0}, {1});", PopupText, PopupTitle, ForcePopup));
        }

        /// <summary>
        /// Returns a javascript result, indicating that the submission was successfully processed, causing a page or module reload, optionally with a popup message.
        /// </summary>
        /// <param name="PopupText">The optional text of the popup message to be displayed. If not specified, no popup will be shown.</param>
        /// <param name="PopupTitle">The optional title of the popup message to be displayed. If not specified, the default is "Success".</param>
        /// <param name="reload">The method with which the current page or module is processed, i.e., by reloading the page or module.</param>
        protected static IResult Reload(ReloadEnum reload = ReloadEnum.Page, string ? PopupText = null, string? PopupTitle = null, bool ForcePopup = false) {
            return Results.Json(reload switch {
                ReloadEnum.Module => Reload(Basics.AjaxJavascriptReloadModule,
                    "$YetaWF.message({0}, {1}, function() {{ $YetaWF.reloadModule(); }});",
                    PopupText, PopupTitle, ForcePopup),
                ReloadEnum.ModuleParts => Reload(Basics.AjaxJavascriptReloadModuleParts,
                    "$YetaWF.message({0}, {1});",
                    PopupText, PopupTitle, ForcePopup),
                _ => Reload(Basics.AjaxJavascriptReloadPage,
                    "$YetaWF.message({0}, {1}, function() {{ $YetaWF.reloadPage(true); }});",
                    PopupText, PopupTitle, ForcePopup),
            });
        }
        private static string Reload(string prepend, string javascript, string? popupText, string? popupTitle, bool ForcePopup = false) {
            ScriptBuilder sb = new ScriptBuilder();
            if (!string.IsNullOrEmpty(prepend))
                sb.Append(prepend);
            else
                sb.Append(Basics.AjaxJavascriptReturn);
            if (string.IsNullOrWhiteSpace(popupText)) {
                // we don't want a message or an alert
                return sb.ToString();
            } else {
                popupText = Utility.JsonSerialize(popupText);
                popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
                if (ForcePopup)
                    sb.Append("YVolatile.Basics.ForcePopup = true;");
                sb.Append(javascript, popupText, popupTitle);
                return sb.ToString();
            }
        }

        public static IResult Redirect(string url, string? PopupText = null, string? PopupTitle = null, bool SetCurrentEditMode = false, string? ExtraJavascript = null, bool ForceFullPage = false) {
            if (Manager.IsPostRequest) {
                url = AddUrlPayload(url, SetCurrentEditMode, null);

                ScriptBuilder sb = new ScriptBuilder();
                sb.Append(Basics.AjaxJavascriptReturn);

                if (!string.IsNullOrEmpty(PopupText)) {
                    PopupText = Utility.JsonSerialize(PopupText);
                    PopupTitle = Utility.JsonSerialize(PopupTitle ?? __ResStr("completeTitle", "Success"));
                    sb.Append("$YetaWF.message({0}, {1}, function() {{", PopupText, PopupTitle);
                }

                url = Utility.JsonSerialize(url);
                if (!ForceFullPage) {
                    sb.Append(
                        "$YetaWF.setLoading();" +
                        (string.IsNullOrWhiteSpace(ExtraJavascript) ? "" : ExtraJavascript) +
                        $"$YetaWF.loadUrl({url});");
                } else {
                    sb.Append(
                        "$YetaWF.setLoading();" +
                        ExtraJavascript +
                        $"window.location.assign({url});");
                }

                if (!string.IsNullOrEmpty(PopupText)) {
                    sb.Append(" }});");
                }
                return Results.Json(sb.ToString());
            } else {
                return Results.Redirect(url);
            }
        }

        public static string AddUrlPayload(string url, bool SetCurrentEditMode, string? ExtraData) {

            QueryHelper qhUrl = QueryHelper.FromUrl(url, out string urlOnly);
            // If we're coming from a referring page with edit/noedit, we need to propagate that to the redirect
            if (SetCurrentEditMode) { // forced set edit mode
                qhUrl.Remove(Globals.Link_EditMode);
                if (Manager.EditMode)
                    qhUrl.Add(Globals.Link_EditMode, "y");
            } else if (!qhUrl.HasEntry(Globals.Link_EditMode)) {
                // current url has no edit/noedit preference
                if (Manager.EditMode) {
                    // in edit mode, force edit again
                    qhUrl.Add(Globals.Link_EditMode, "y");
                } else {
                    // not in edit mode, use referrer mode
                    string referrer = Manager.ReferrerUrl;
                    if (!string.IsNullOrWhiteSpace(referrer)) {
                        QueryHelper qhRef = QueryHelper.FromUrl(referrer, out string refUrlOnly);
                        if (qhRef.HasEntry(Globals.Link_EditMode)) { // referrer is edit
                            qhUrl.Add(Globals.Link_EditMode, "y", Replace: true);
                        }
                    }
                }
            }
            qhUrl.Remove(Globals.Link_PageControl);
            if (Manager.PageControlShown)
                qhUrl.Add(Globals.Link_PageControl, "y");
            if (!string.IsNullOrWhiteSpace(ExtraData))
                qhUrl.Add("_ExtraData", ExtraData, Replace: true);

            return qhUrl.ToUrl(urlOnly);
        }
    }
}
