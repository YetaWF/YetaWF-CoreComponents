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

            return await YetaWFCoreRendering.Render.RenderModuleActionAsync(this, mode, Id);

        }

        public string GetCompleteUrl(bool OnPage = false) {

            string url = Url;
            if (!string.IsNullOrWhiteSpace(url)) {
                // handle all args

                string urlOnly;
                QueryHelper query = QueryHelper.FromUrl(url, out urlOnly);
                if (NeedsModuleContext)
                    query.Remove(Basics.ModuleGuid);

                // add human readable args as URL segments
                QueryHelper qh = QueryHelper.FromAnonymousObject(QueryArgsHR);
                if (NeedsModuleContext)
                    qh.Remove(Basics.ModuleGuid);
                urlOnly = qh.ToUrlHumanReadable(urlOnly);

                // add query args
                qh = QueryHelper.FromAnonymousObject(QueryArgs);
                if (NeedsModuleContext)
                    qh.Remove(Basics.ModuleGuid);
                urlOnly = qh.ToUrl(urlOnly);

                // add query args dictionary
                if (QueryArgsDict != null) {
                    if (NeedsModuleContext)
                        QueryArgsDict.Remove(Basics.ModuleGuid);
                    urlOnly = QueryArgsDict.ToUrl(urlOnly);
                }

                // add module guid if needed
                if (NeedsModuleContext)
                    query.Add(Basics.ModuleGuid, GetOwningModuleGuid().ToString());
                url = query.ToUrl(urlOnly);

                // schema and anchor
                if (url.StartsWith("/")) {
                    url = Manager.CurrentSite.MakeUrl(url, PagePageSecurity: PageSecurity);
                    if (OnPage && PageSecurity == PageDefinition.PageSecurityType.Any)
                        url = url.Split(new char[] { ':' }, 2)[1];// remove http: or https:
                }
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
