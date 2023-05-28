/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Components;
using YetaWF.Core.Endpoints;
using YetaWF.Core.Endpoints.Support;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using static YetaWF.Core.Endpoints.ModuleEndpoints;

namespace YetaWF.Core.Modules {

    public partial class ModuleDefinition {

        /* private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition), name, defaultValue, parms); } */

        public const string MethodRenderModuleAsync = "RenderModuleAsync";
        public const string MethodUpdateModuleAsync = "UpdateModuleAsync";

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

        protected async Task<object?> GetObjectFromModelAsync(Type type, string? fieldPrefix = null) {
            if (fieldPrefix?.Contains('.') ?? false)
                throw new InternalError($"Nested objects are not supported - {fieldPrefix}");
            object model;
            if (fieldPrefix != null) {
                JsonElement jsModel = (System.Text.Json.JsonElement)_dataIn.Model;
                if (!jsModel.TryGetProperty(fieldPrefix, out JsonElement jsSubModel))
                    return null;
                model = Utility.JsonDeserialize(jsSubModel.ToString()!, type);
            } else {
                model = Utility.JsonDeserialize(_dataIn.Model.ToString()!, type);
            }
            await ModelState.EvaluateModel(model, fieldPrefix ?? string.Empty, true, true, _dataIn.__TemplateName, _dataIn.__TemplateAction, _dataIn.__TemplateExtraData);
            return model;
        }
        protected async Task<T?> GetObjectFromModelAsync<T>(string? fieldPrefix = null) {
            return (T?) await GetObjectFromModelAsync(typeof(T), fieldPrefix);
        }
        internal ModuleSubmitData _dataIn = null!;


        public async Task<IResult> PartialViewAsync(object? model = null, ScriptBuilder? Script = null, string? ViewName = null, bool UseAreaViewName = true) {

            ViewName = EvaluateViewName(ViewName, UseAreaViewName);
            ViewName += YetaWFViewExtender.PartialSuffix;//$$$$

            return await YetaWF.Core.Endpoints.PartialView.RenderPartialView(Manager.CurrentContext, ViewName, this, null, model, "application/html", Script: Script);
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
            /// The page is reloaded with the previous page save in the history. If none is available, the Home page is loaded.
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
        /// <param name="NextPage">The URL where the page is redirected (OnClose or OnPopupClose will then default to OnCloseEnum.GotoNewPage or OnPopupCloseEnum.GotoNewPage).</param>
        /// <param name="PreSaveJavaScript">Optional additional Javascript code that is returned as part of the ActionResult and runs before the form is saved.</param>
        /// <param name="PostSaveJavaScript">Optional additional Javascript code that is returned as part of the ActionResult and runs after the form is saved.</param>
        /// <param name="ForceReload">Force a real reload bypassing Unified Page Set handling.</param>
        /// <param name="PopupOptions">TODO: This is not a good option, passes JavaScript/JSON to the client side for the popup window.</param>
        /// <param name="PageChanged">The new page changed status.</param>
        /// <param name="ForceApply">Force handling as Apply.</param>
        /// <param name="ExtraData">Additional data added to URL as _extraData argument. Length should be minimal, otherwise URL and Referer header may grow too large.</param>
        /// <param name="ForcePopup">The message is shown as a popup even if toasts are enabled.</param>
        /// <returns>An IResult to be returned by the endpoint.</returns>
        protected async Task<IResult> FormProcessedAsync(object? model, string? popupText = null, string? popupTitle = null,
                OnCloseEnum OnClose = OnCloseEnum.Return, OnPopupCloseEnum OnPopupClose = OnPopupCloseEnum.ReloadParentPage, OnApplyEnum OnApply = OnApplyEnum.ReloadModule,
                string? NextPage = null, string? ExtraData = null,
                string? PreSaveJavaScript = null, string? PostSaveJavaScript = null, bool ForceReload = false, string? PopupOptions = null, bool ForceApply = false,
                bool? PageChanged = null,
                bool ForcePopup = false,
                string? ViewName = null, bool UseAreaViewName = true) {

            ScriptBuilder sb = new ScriptBuilder();

            if (PreSaveJavaScript != null)
                sb.Append(PreSaveJavaScript);

            popupText = string.IsNullOrWhiteSpace(popupText) ? null : Utility.JsonSerialize(popupText);
            popupTitle = Utility.JsonSerialize(popupTitle ?? __ResStr("completeTitle", "Success"));
            PopupOptions ??= "null";

            bool isApply = IsApply || IsReload || ForceApply;
            if (isApply) {
                NextPage = null;
                OnPopupClose = OnPopupCloseEnum.UpdateInPlace;
                OnClose = OnCloseEnum.UpdateInPlace;
            } else {
                if (Manager.IsInPopup) {
                    if (OnPopupClose == OnPopupCloseEnum.GotoNewPage || !string.IsNullOrWhiteSpace(NextPage)) {
                        OnPopupClose = OnPopupCloseEnum.GotoNewPage;
                        if (string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.CurrentSite.HomePageUrl;
                    } else
                        NextPage = null;
                } else {
                    if (OnClose == OnCloseEnum.GotoNewPage || !string.IsNullOrWhiteSpace(NextPage)) {
                        OnClose = OnCloseEnum.GotoNewPage;
                        if (string.IsNullOrWhiteSpace(NextPage))
                            NextPage = Manager.CurrentSite.HomePageUrl;
                    } else
                        NextPage = null;
                }
            }

            // handle NextPage (if any)
            if (ForceReload) {
                if (string.IsNullOrWhiteSpace(NextPage)) {
                    // handle ForceReload without NextPage
                    if (Manager.IsInPopup) {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.UpdateInPlace:
                                // nothing to do
                                break;
                            case OnPopupCloseEnum.ReloadParentPage:
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"$YetaWF.setLoading(); window.location.reload();");
                                } else {
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.reload(); }}, {PopupOptions});");
                                }
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    } else {
                        switch (OnClose) {
                            case OnCloseEnum.UpdateInPlace: // apply
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"$YetaWF.setLoading(); window.location.reload();");
                                } else {
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.reload(); }}, {PopupOptions});");
                                }
                                break;
                            case OnCloseEnum.ReloadPage:
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"$YetaWF.setLoading(); window.location.reload();");
                                } else {
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.reload(); }}, {PopupOptions});");
                                }
                                break;
                            case OnCloseEnum.Return:
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"$YetaWF.setLoading(); $YetaWF.Forms.goBack(); window.location.reload();");
                                } else {
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); $YetaWF.Forms.goBack(); window.location.reload(); }}, {PopupOptions});");
                                }
                                break;
                            default:
                                throw new InternalError("Invalid OnClose value {0}", OnClose);
                        }
                    }
                } else {
                    // handle ForceReload with NextPage
                    string? url = NextPage;
                    url = QueryHelper.AddRando(url);
                    url = Utility.JsonSerialize(url);

                    if (Manager.IsInPopup) {
                        switch (OnPopupClose) {
                            case OnPopupCloseEnum.GotoNewPage:
                            case OnPopupCloseEnum.ReloadParentPage:
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"$YetaWF.setLoading(); window.location.assign({url});");
                                } else {
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.assign({url}); }}, {PopupOptions});");
                                }
                                break;
                            default:
                                throw new InternalError("Invalid OnPopupClose value {0}", OnPopupClose);
                        }
                    } else {
                        switch (OnClose) {
                            case OnCloseEnum.GotoNewPage:
                            case OnCloseEnum.ReloadPage:
                            case OnCloseEnum.Return:
                                if (string.IsNullOrWhiteSpace(popupText)) {
                                    sb.Append($@"$YetaWF.setLoading(); window.location.assign({url});");
                                } else {
                                    sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.setLoading(); window.location.assign({url}); }}, {PopupOptions});");
                                }
                                break;
                            default:
                                throw new InternalError("Invalid OnClose value {0}", OnClose);
                        }
                    }
                }
            } else if (!string.IsNullOrWhiteSpace(NextPage)) {

                string url = NextPage;
                url = YetaWFEndpoints.AddUrlPayload(url, false, ExtraData);
                if (ForceReload)
                    url = QueryHelper.AddRando(url);
                url = Utility.JsonSerialize(url);

                if (Manager.IsInPopup) {
                    if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append($@"
$YetaWF.setLoading();
window.parent.$YetaWF.loadUrl({url}, true, null, null, function (res) {{ {PostSaveJavaScript} }});");

                    } else {
                        sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    $YetaWF.setLoading();
    window.parent.$YetaWF.loadUrl({url}, function (res) {{ {PostSaveJavaScript} }});
}});");
                    }
                } else {
                    if (string.IsNullOrWhiteSpace(popupText)) {
                        sb.Append($@"
$YetaWF.setLoading();
$YetaWF.loadUrl({url}, true, null, null, function (res) {{ {PostSaveJavaScript} }});");
                    } else {
                        sb.Append($@"
{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}
$YetaWF.message({popupText}, {popupTitle}, function() {{
    $YetaWF.setLoading();
    $YetaWF.loadUrl({url}, true, null, null, function (res) {{ {PostSaveJavaScript} }});
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
                            sb.Append($@"{(ForcePopup ? "YVolatile.Basics.ForcePopup = true;" : null)}");
                            if (string.IsNullOrWhiteSpace(popupText)) {
                                sb.Append($@"$YetaWF.Forms.goBack();{PostSaveJavaScript}");
                            } else {
                                sb.Append($@"$YetaWF.message({popupText}, {popupTitle}, function() {{ $YetaWF.Forms.goBack();{PostSaveJavaScript} }}, {PopupOptions});");
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
                        return await PartialViewAsync(model, Script: sb, ViewName: ViewName, UseAreaViewName: UseAreaViewName);
                    }
                }
            }
            return Results.Json($"{Basics.AjaxJavascriptReturn}{sb.ToString()}");
        }

        /// <summary>
        /// Redirects to the specified URL, aborting page rendering.
        /// </summary>
        /// <param name="url">The URL where the page is redirected.</param>
        /// <returns>An ActionInfo to be returned by the endpoint.</returns>
        /// <remarks>
        /// The Redirect method can be used for GET within content rendering.
        /// </remarks>
        protected ActionInfo RedirectToUrl(string url) {
            Manager.CurrentResponse.StatusCode = StatusCodes.Status307TemporaryRedirect;
            Manager.CurrentResponse.Headers.Add("Location", url);
            return ActionInfo.Empty;
        }

        /// <summary>
        /// A result that results in a 403 Not Authorized exception.
        /// </summary>
        /// <param name="message">The message text to be shown on an error page (GET requests only) along with the 403 exception.</param>
        protected async Task<ActionInfo> UnauthorizedAsync(string? message = null) {

            message ??= __ResStr("notAuth", "Not Authorized");

            if (!Manager.IsPostRequest) throw new InternalError($"{nameof(UnauthorizedAsync)} is only supported for GET requests");
            Manager.CurrentResponse.StatusCode = StatusCodes.Status403Forbidden;
            return await RenderAsync(message, ViewName: "ShowMessage", UseAreaViewName: false);
        }
    }
}
