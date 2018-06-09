/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Components;

namespace YetaWF.Core.Modules {

    public partial class ModuleAction {

        public enum RenderModeEnum {
            NormalMenu = 0,
            NormalLinks = 1,
            IconsOnly = 2,
            LinksOnly = 3,
            Button = 4,
            ButtonIcon = 5,
        }
        public enum RenderEngineEnum {
            JqueryMenu = 0,
            BootstrapSmartMenu = 1,
        }

        // Render an action as button
        public async Task<YHtmlString> RenderAsButtonAsync(string id = null) {
            return await RenderAsync(RenderModeEnum.Button, Id: id);
        }
        public async Task<YHtmlString> RenderAsButtonIconAsync(string id = null) {
            return await RenderAsync(RenderModeEnum.ButtonIcon, Id: id);
        }
        // Render an action as icon
        public async Task<YHtmlString> RenderAsIconAsync(string id = null) {
            return await RenderAsync(RenderModeEnum.IconsOnly, Id: id);
        }
        // Render an action as link
        public async Task<YHtmlString> RenderAsLinkAsync(string id = null) {
            return await RenderAsync(RenderModeEnum.LinksOnly, Id: id);
        }
        // Render an action as normal link with icon
        public async Task<YHtmlString> RenderAsNormalLinkAsync(string id = null) {
            return await RenderAsync(RenderModeEnum.NormalLinks, Id: id);
        }

        /// <summary>
        /// Check if this action renders anything (based on authorization)
        /// </summary>
        public async Task<bool> RendersSomethingAsync() {
            // check if we're in the right mode
            if (!DontCheckAuthorization && !_AuthorizationEvaluated) {
                if (!await IsAuthorizedAsync())
                    return false;
            }
            if (this.Mode == ActionModeEnum.Edit) {
                if (!Manager.EditMode)
                    return false;
            } else if (this.Mode == ActionModeEnum.View) {
                if (Manager.EditMode)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Render an action.
        /// </summary>
        /// <remarks>HasSubmenu doesn't render the submenu, it merely adds the attributes reflecting that there is a submenu</remarks>
        public async Task<YHtmlString> RenderAsync(RenderModeEnum mode, string Id = null) {

            // check if we're in the right mode
            if (!await RendersSomethingAsync()) return new YHtmlString("");

            if (!string.IsNullOrWhiteSpace(ConfirmationText) && (Style != ActionStyleEnum.Post && Style != ActionStyleEnum.Nothing))
                throw new InternalError("When using ConfirmationText, the Style property must be set to Post");
            if (!string.IsNullOrWhiteSpace(PleaseWaitText) && (Style != ActionStyleEnum.Normal && Style != ActionStyleEnum.Post))
                throw new InternalError("When using PleaseWaitText, the Style property must be set to Normal or Post");
            if (CookieAsDoneSignal && Style != ActionStyleEnum.Normal)
                throw new InternalError("When using CookieAsDoneSignal, the Style property must be set to Normal");

            ActionStyleEnum style = Style;
            if (style == ActionStyleEnum.OuterWindow)
                if (!Manager.IsInPopup)
                    style = ActionStyleEnum.Normal;

            if (style == ActionStyleEnum.Popup || style == ActionStyleEnum.PopupEdit)
                if (Manager.IsInPopup)
                    style = ActionStyleEnum.NewWindow;

            if (style == ActionStyleEnum.Popup || style == ActionStyleEnum.PopupEdit || style == ActionStyleEnum.ForcePopup)
                await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF", "Core", "Popups");// this is needed for popup support //$$$$$this probably needs to move

            YHtmlString text = null;
            switch (style) {
                default:
                case ActionStyleEnum.Normal:
                    text = await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id);
                    break;
                case ActionStyleEnum.NewWindow:
                    text = await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id, NewWindow: true);
                    break;
                case ActionStyleEnum.Popup:
                case ActionStyleEnum.ForcePopup:
                    text = await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id, Popup: Manager.CurrentSite.AllowPopups);
                    break;
                case ActionStyleEnum.PopupEdit:
                    text = await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id, Popup: Manager.CurrentSite.AllowPopups, PopupEdit: Manager.CurrentSite.AllowPopups);
                    break;
                case ActionStyleEnum.OuterWindow:
                    text = await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id, OuterWindow: true);
                    break;
                case ActionStyleEnum.Nothing:
                    text = await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id, Nothing: true);
                    break;
                case ActionStyleEnum.Post:
                    text = await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id, Post: true);
                    break;
            }
            return text;
        }

        public string GetCompleteUrl(bool OnPage = false) {
            string qs = "";
            string url = Url;
            if (!string.IsNullOrWhiteSpace(url)) {
                // handle all args
                // add human readable args as URL segments
                QueryHelper query = QueryHelper.FromAnonymousObject(QueryArgsHR);
                url = query.ToUrlHumanReadable(url);
                query = QueryHelper.FromAnonymousObject(QueryArgs);
                if (NeedsModuleContext) //TODO: Url may already contain Basics.ModuleGuid
                    query.Add(Basics.ModuleGuid, GetOwningModuleGuid().ToString());
                url = query.ToUrl(url);
                if (QueryArgsDict != null)
                    url = QueryArgsDict.ToUrl(url);

                if (url.StartsWith("/")) {
                    url = Manager.CurrentSite.MakeUrl(url + qs, PagePageSecurity: PageSecurity);
                    if (OnPage && PageSecurity == PageDefinition.PageSecurityType.Any)
                        url = url.Split(new char[] { ':' }, 2)[1];// remove http: or https:
                } else
                    url += qs;
                if (!string.IsNullOrWhiteSpace(AnchorId))
                    url += "#" + AnchorId;
                return url;
            }
            return "";
        }

        // AUTHORIZATION
        // AUTHORIZATION
        // AUTHORIZATION

        public async Task<bool> IsAuthorizedAsync() {
            if (Resource.ResourceAccess.IsBackDoorWideOpen()) return true;
            if (AuthorizationIgnore)
                return true;
            if (LimitToRole != 0 && !Manager.HasSuperUserRole) {
                // action is limited to one role
                if (Manager.HaveUser) {
                    // we have a user - check if it's limited to something other than users
                    if (LimitToRole != Resource.ResourceAccess.GetUserRoleId()) {
                        if (Manager.UserRoles == null || !Manager.UserRoles.Contains(LimitToRole))
                            return false;
                    }
                } else {
                    // we don't have a user - check if it's limited to something other than anonymous users
                    if (LimitToRole != Resource.ResourceAccess.GetAnonymousRoleId())
                        return false;
                }
            }
            // validate SubModule
            if (SubModule != null && SubModule != Guid.Empty) {
                ModuleDefinition mod = await ModuleDefinition.LoadAsync((Guid)SubModule, AllowNone: true);
                if (mod == null) return false;// can't find module, not authorized
                if (!mod.IsAuthorized(ModuleDefinition.RoleDefinition.View))
                    return false;
                return true;
            }

            // validate by Url
            if (!string.IsNullOrEmpty(Url)) {
                string url = Url;
                // the url could start with http://ourdomain or https://ourdomain
                if (url.StartsWith(Manager.CurrentSite.SiteUrlHttp, System.StringComparison.OrdinalIgnoreCase))
                    url = url.Substring(Manager.CurrentSite.SiteUrlHttp.Length - 1);
                else if (url.StartsWith(Manager.CurrentSite.SiteUrlHttps, System.StringComparison.OrdinalIgnoreCase))
                    url = url.Substring(Manager.CurrentSite.SiteUrlHttps.Length - 1);
                if (url.StartsWith("/")) {
                    if (Manager.UserAuthorizedUrls != null && Manager.UserAuthorizedUrls.Contains(url)) return true;
                    if (Manager.UserNotAuthorizedUrls != null && Manager.UserNotAuthorizedUrls.Contains(url)) return false;
                    PageDefinition page = await PageDefinition.LoadFromUrlAsync(url);
                    if (page != null) {
                        PageSecurity = page.PageSecurity;
                        if (Manager.EditMode)
                            return AddUserUrl(url, page.IsAuthorized_Edit());
                        else
                            return AddUserUrl(url, page.IsAuthorized_View());
                    }
                    ModuleDefinition module = await ModuleDefinition.FindDesignedModuleAsync(url);
                    if (module == null)
                        module = await ModuleDefinition.LoadByUrlAsync(url);
                    if (module != null) {
                        PageSecurity = module.ModuleSecurity;
                        if (Manager.EditMode)
                            return AddUserUrl(url, module.IsAuthorized(ModuleDefinition.RoleDefinition.Edit));
                        else
                            return AddUserUrl(url, module.IsAuthorized(ModuleDefinition.RoleDefinition.View));
                    }
                    AddUserUrl(url, true);
                }
                // not a url for us, so we'll allow it
                return true;
            }
            // no url
            return true;
        }

        private bool AddUserUrl(string url, bool authorized) {
            if (authorized) {
                if (Manager.UserAuthorizedUrls == null) Manager.UserAuthorizedUrls = new List<string>();
                Manager.UserAuthorizedUrls.Add(url);
            } else {
                if (Manager.UserNotAuthorizedUrls == null) Manager.UserNotAuthorizedUrls = new List<string>();
                Manager.UserNotAuthorizedUrls.Add(url);
            }
            return authorized;
        }
    }
}
