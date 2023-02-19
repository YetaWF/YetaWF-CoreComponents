/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Endpoints.Support;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    public abstract class ModuleDefinition2 : ModuleDefinition {//$$$$ eventually rename ModuleDefinition2

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition2), name, defaultValue, parms); }

        public const string MethodRenderModuleAsync = "RenderModuleAsync";
        public const string MethodUpdateModuleAsync = "UpdateModuleAsync";

        public override bool JSONModule { get { return true; } }//$$$ eventually remove

        [DontSave]
        public bool IsApply { get; set; }
        [DontSave]
        public bool IsReload { get; set; }

        /// <summary>
        /// Gets the <see cref="ModelState"/> that contains the state of the model and of model-validation.
        /// </summary>
        public ModelState ModelState { 
            get {
                if (_modelState == null)
                    _modelState = new ModelState();
                return _modelState; 
            } 
        }
        private ModelState? _modelState = null;

        protected async Task<IResult> PartialViewAsync(object? model = null, ScriptBuilder? Script = null) {
            // Find view name
            string viewName;
            if (!string.IsNullOrWhiteSpace(DefaultViewName))
                viewName = DefaultViewName;
            else
                viewName = MakeFullViewName(ModuleName, AreaName);
            viewName += YetaWFViewExtender.PartialSuffix;

            return await YetaWF.Core.Endpoints.PartialView.RenderPartialView(Manager.CurrentContext, viewName, this, null, model, "application/html", Script: Script);
        }

        /// <summary>
        /// The type of processing used when closing a popup window, used with the FormProcessed method.
        /// </summary>
        protected enum OnPopupCloseEnum {
            /// <summary>
            /// No processing. The popup is not closed.
            /// </summary>
            Nothing = 0,
            /// <summary>
            /// No processing. The popup is closed.
            /// </summary>
            ReloadNothing = 1,
            /// <summary>
            /// The popup is closed and the parent page is reloaded.
            /// </summary>
            ReloadParentPage = 2,
            /// <summary>
            /// The popup is closed and the module is reloaded.
            /// </summary>
            ReloadModule = 3,
            /// <summary>
            /// The popup is closed and a new page is loaded.
            /// </summary>
            GotoNewPage = 4,
            /// <summary>
            /// The popup is not closed and the module is updated in place with the new model.
            /// </summary>
            UpdateInPlace = 5,
        }
        /// <summary>
        /// The type of processing used when closing a page, used with the FormProcessed method.
        /// </summary>
        protected enum OnCloseEnum {
            /// <summary>
            /// No processing. The page is not closed.
            /// </summary>
            Nothing = 0,
            /// <summary>
            /// The page is reloaded with the previous page save in the OriginList. If none is available, the Home page is loaded.
            /// </summary>
            Return = 1,
            /// <summary>
            /// A new page is loaded.
            /// </summary>
            GotoNewPage = 2,
            /// <summary>
            /// The page/module is updated in place with the new model.
            /// </summary>
            UpdateInPlace = 3,
            /// <summary>
            /// The current page is reloaded.
            /// </summary>
            ReloadPage = 4,
            /// <summary>
            /// The current page is closed, which will close the browser window.
            /// </summary>
            CloseWindow = 9,
        }
        /// <summary>
        /// The type of processing used when processing the Apply action for a form, used with the FormProcessed method.
        /// </summary>
        protected enum OnApplyEnum {
            /// <summary>
            /// Reload the current module.
            /// </summary>
            ReloadModule = 1,
            /// <summary>
            /// Reload the current page.
            /// </summary>
            ReloadPage = 2,
        }

        /// <summary>
        /// The form was successfully processed. This handles returning to a parent page or displaying a popup if a return page is not available.
        /// </summary>
        /// <param name="model">The model to display.</param>
        /// <param name="popupText">A message displayed in a popup. Specify null to suppress the popup.</param>
        /// <param name="popupTitle">The title for the popup if a message (popupText) is specified. If null is specified, a default title indicating success is supplied.</param>
        /// <param name="OnClose">The action to take when the page is closed. This is only used if a page is closed (as opposed to a popup or when the Apply button was processed).</param>
        /// <param name="OnPopupClose">The action to take when a popup is closed. This is only used if a popup is closed (as opposed to a page or when the Apply button was processed).</param>
        /// <param name="OnApply">The action to take when the Apply button was processed.</param>
        /// <param name="NextPage">The URL where the page is redirected (OnClose or OnPopupClose must request a matching action, otherwise this is ignored).</param>
        /// <param name="PreserveOriginList">Preserves the URL origin list. Only supported when <paramref name="NextPage"/> is used.</param>
        /// <param name="PreSaveJavaScript">Optional additional Javascript code that is returned as part of the ActionResult and runs before the form is saved.</param>
        /// <param name="PostSaveJavaScript">Optional additional Javascript code that is returned as part of the ActionResult and runs after the form is saved.</param>
        /// <param name="ForceRedirect">Force a real redirect bypassing Unified Page Set handling.</param>
        /// <param name="PopupOptions">TODO: This is not a good option, passes JavaScript/JSON to the client side for the popup window.</param>
        /// <param name="PageChanged">The new page changed status.</param>
        /// <param name="ForceApply">Force handling as Apply.</param>
        /// <param name="ExtraData">Additional data added to URL as _extraData argument. Length should be minimal, otherwise URL and Referer header may grow too large.</param>
        /// <param name="ForcePopup">The message is shown as a popup even if toasts are enabled.</param>
        /// <returns>An ActionResult to be returned by the controller.</returns>
        protected async Task<IResult> FormProcessedAsync(object? model, string? popupText = null, string? popupTitle = null,
                OnCloseEnum OnClose = OnCloseEnum.Return, OnPopupCloseEnum OnPopupClose = OnPopupCloseEnum.ReloadParentPage, OnApplyEnum OnApply = OnApplyEnum.ReloadModule,
                string? NextPage = null, bool PreserveOriginList = false, string? ExtraData = null,
                string? PreSaveJavaScript = null, string? PostSaveJavaScript = null, bool ForceRedirect = false, string? PopupOptions = null, bool ForceApply = false,
                bool? PageChanged = null,
                bool ForcePopup = false) {

            ScriptBuilder sb = new ScriptBuilder();

            if (PreSaveJavaScript != null)
                sb.Append(PreSaveJavaScript);

            popupText = string.IsNullOrWhiteSpace(popupText) ? null : Utility.JsonSerialize(popupText);
            popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
            PopupOptions ??= "null";

            if (PreserveOriginList && !string.IsNullOrWhiteSpace(NextPage)) {
                string url = NextPage;
                if (Manager.OriginList != null) {
                    QueryHelper qh = QueryHelper.FromUrl(url, out string urlOnly);
                    qh.Add(Globals.Link_OriginList, Utility.JsonSerialize(Manager.OriginList), Replace: true);
                    NextPage = qh.ToUrl(urlOnly);
                }
            }

            bool isApply = IsApply || IsReload || ForceApply;
            if (isApply) {
                NextPage = null;
                OnPopupClose = OnPopupCloseEnum.UpdateInPlace;
                OnClose = OnCloseEnum.UpdateInPlace;
            } else {
                if (Manager.IsInPopup) {
                    if (OnPopupClose == OnPopupCloseEnum.GotoNewPage) {
                        if (string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.ReturnToUrl;
                        if (string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.CurrentSite.HomePageUrl;
                    } else
                        NextPage = null;
                } else {
                    if (OnClose == OnCloseEnum.Return || OnClose == OnCloseEnum.GotoNewPage) {
                        if (OnClose == OnCloseEnum.Return && string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.ReturnToUrl;
                        if (string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.CurrentSite.HomePageUrl;
                    } else
                        NextPage = null;
                }
            }

            // handle NextPage (if any)
            if (ForceRedirect || !string.IsNullOrWhiteSpace(NextPage)) {

                string? url = NextPage;
                if (string.IsNullOrWhiteSpace(url))
                    url = Manager.CurrentSite.HomePageUrl;
                url = YetaWFEndpoints.AddUrlPayload(url, false, ExtraData);
                if (ForceRedirect)
                    url = QueryHelper.AddRando(url);
                url = Utility.JsonSerialize(url);

                if (Manager.IsInPopup) {
                    if (ForceRedirect) {
                        if (string.IsNullOrWhiteSpace(popupText)) {
                            sb.Append("$YetaWF.setLoading();window.parent.location.assign({0});", url);
                        } else {
                            sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.parent.location.assign({url}); }}, {PopupOptions});");
                        }
                    } else if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append($@"
$YetaWF.setLoading();
if (window.parent.$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

}} else
    window.parent.location.assign({url});");

                    } else {
                        sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    $YetaWF.setLoading();
    if (window.parent.$YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

    }} else
        window.parent.location.assign({url});
}});");
                    }
                } else {
                    if (ForceRedirect) {
                        if (isApply) {
                            sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.reload(true); }}, {PopupOptions});");
                        } else {
                            if (string.IsNullOrWhiteSpace(popupText)) {
                                sb.Append($@"
$YetaWF.setLoading();window.location.assign({url});");
                            } else {
                                sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.assign({url}); }}, {PopupOptions});");
                            }
                        }
                    } else if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append($@"
$YetaWF.setLoading();
if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

}} else
    window.location.assign({url});");
                    } else {
                        sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    $YetaWF.setLoading();
    if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

    }} else
        window.location.assign({url});
}});");
                    }
                }
            } else {
                if (Manager.IsInPopup) {
                    if (string.IsNullOrWhiteSpace(popupText)) {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                                throw new InternalError("No next page");
                            case OnPopupCloseEnum.Nothing:
                                sb.Append(PostSaveJavaScript);
                                break;
                            case OnPopupCloseEnum.ReloadNothing:
                                sb.Append($@"$YetaWF.closePopup(false);{PostSaveJavaScript}");
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                sb.Append("$YetaWF.closePopup(true);");
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                sb.Append($@"
window.parent.$YetaWF.refreshPage();
$YetaWF.closePopup(false);");
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    } else {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                                throw new InternalError("No next page");
                            case OnPopupCloseEnum.Nothing:
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ {PostSaveJavaScript} }}, {PopupOptions});");
                                break;
                            case OnPopupCloseEnum.ReloadNothing:
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.closePopup(false);{PostSaveJavaScript} }}, {PopupOptions});");
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.closePopup(true);{PostSaveJavaScript} }}, {PopupOptions});");
                                break;
                            case OnPopupCloseEnum.UpdateInPlace:
                                isApply = true;
                                break;
                            case OnPopupCloseEnum.ReloadModule:
                                // reload page, which reloads all modules (that are registered)
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ window.parent.$YetaWF.refreshPage(); $YetaWF.closePopup(false); }}, {PopupOptions});");
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    }
                } else {
                    switch (OnClose) {
                        case OnCloseEnum.GotoNewPage:
                            throw new InternalError("No next page");
                        case OnCloseEnum.Nothing:
                            if (!string.IsNullOrWhiteSpace(popupText)) {
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ {PostSaveJavaScript} }}, {PopupOptions});");
                            }
                            if (PageChanged != null)
                                sb.Append($@"$YetaWF.pageChanged = {((bool)PageChanged ? "true" : "false")} ;");
                            break;
                        case OnCloseEnum.UpdateInPlace:
                            isApply = true;
                            break;
                        case OnCloseEnum.Return:
                            if (Manager.OriginList == null || Manager.OriginList.Count == 0) {
                                if (string.IsNullOrWhiteSpace(popupText))
                                    sb.Append($@"window.close();{PostSaveJavaScript}");
                                else {
                                    if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true;");
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ window.close();{PostSaveJavaScript} }}, {PopupOptions});");
                                }
                            } else {
                                string url = Utility.JsonSerialize(Manager.ReturnToUrl);
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"
if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

}} else
    window.location.assign({url});");
                                } else {
                                    sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    if ($YetaWF.ContentHandling.setContent($YetaWF.parseUrl({url}), true, null, null, function (res) {{ {PostSaveJavaScript} }})) {{

    }} else
        window.location.assign({PopupOptions});
}});");
                                }
                            }
                            break;
                        case OnCloseEnum.CloseWindow:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append($@"window.close();{PostSaveJavaScript}");
                            else {
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ window.close();{PostSaveJavaScript} }}, {PopupOptions});");
                            }
                            break;
                        case OnCloseEnum.ReloadPage:
                            if (string.IsNullOrWhiteSpace(popupText))
                                sb.Append($@"$YetaWF.reloadPage(true);");
                            else {
                                if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.reloadPage(true);{PostSaveJavaScript} }}, {PopupOptions});");
                            }
                            break;
                        default:
                            throw new InternalError("Invalid OnClose value {0}", OnClose);
                    }
                }
                if (isApply) {
                    if (OnApply == OnApplyEnum.ReloadPage) {
                        if (string.IsNullOrWhiteSpace(popupText))
                            sb.Append($@"$YetaWF.reloadPage(true);{PostSaveJavaScript}");
                        else {
                            if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                            sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.reloadPage(true);{PostSaveJavaScript} }}, {PopupOptions});");
                        }
                    } else {
                        if (!string.IsNullOrWhiteSpace(popupText)) {
                            if (ForcePopup) sb.Append($@"YVolatile.Basics.ForcePopup = true; ");
                            sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ {PostSaveJavaScript} }}, {PopupOptions});");
                        } else
                            sb.Append(PostSaveJavaScript);
                        return await PartialViewAsync(model, Script: sb);
                    }
                }
            }
            return Results.Json($"{Basics.AjaxJavascriptReturn}{sb.ToString()}");
        }
    }
}
