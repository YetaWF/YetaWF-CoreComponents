/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;
using YetaWF.Core.Addons;
using YetaWF.Core.Identity;
using YetaWF.Core.Localize;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

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
        public MvcHtmlString RenderAsButton(string id = null) {
            return Render(RenderModeEnum.Button, Id: id);
        }
        public MvcHtmlString RenderAsButtonIcon(string id = null) {
            return Render(RenderModeEnum.ButtonIcon, Id: id);
        }
        // Render an action as icon
        public MvcHtmlString RenderAsIcon(string id = null) {
            return Render(RenderModeEnum.IconsOnly, Id: id);
        }
        // Render an action as link
        public MvcHtmlString RenderAsLink(string id = null) {
            return Render(RenderModeEnum.LinksOnly, Id: id);
        }

        /// <summary>
        /// Check if this action renders anything (based on authorization)
        /// </summary>
        public bool RendersSomething {
            get {
                // check if we're in the right mode
                if (!DontCheckAuthorization && !_AuthorizationEvaluated) {
                    if (!IsAuthorized)
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
        }

        /// <summary>
        /// Render an action
        /// </summary>
        /// <remarks>HasSubmenu doesn't render the submenu, it merely adds the attributes reflecting that there is a submenu</remarks>
        public MvcHtmlString Render(RenderModeEnum mode, int dummy = 0, string Id = null, RenderEngineEnum RenderEngine = RenderEngineEnum.JqueryMenu,
                bool HasSubmenu = false) {

            // check if we're in the right mode
            if (!RendersSomething) return MvcHtmlString.Empty;

            if (!string.IsNullOrWhiteSpace(ConfirmationText) && (Style != ActionStyleEnum.Post && Style != ActionStyleEnum.Nothing))
                throw new InternalError("When using ConfirmationText, the Style property must be set to Post");
            if (!string.IsNullOrWhiteSpace(PleaseWaitText) && (Style != ActionStyleEnum.Normal && Style != ActionStyleEnum.Post))
                throw new InternalError("When using PleaseWaitText, the Style property must be set to Normal or Post");
            if (CookieAsDoneSignal && Style != ActionStyleEnum.Normal)
                throw new InternalError("When using CookieAsDoneSignal, the Style property must be set to Normal");

            Manager.AddOnManager.AddTemplate("ActionIcons");// this is needed because we're not always used by templates

            ActionStyleEnum style = Style;
            if (style == ActionStyleEnum.OuterWindow)
                if (!Manager.IsInPopup)
                    style = ActionStyleEnum.Normal;

            if (style == ActionStyleEnum.Popup || style == ActionStyleEnum.PopupEdit)
                if (Manager.IsInPopup)
                    style = ActionStyleEnum.NewWindow;

            if (style == ActionStyleEnum.Popup || style == ActionStyleEnum.PopupEdit)
                Manager.AddOnManager.AddAddOn("YetaWF", "Core", "Popups");// this is needed for popup support

            TagBuilder tag = null;
            switch (style) {
                default:
                case ActionStyleEnum.Normal:
                    tag = Render_ALink(RenderEngine, mode, Id, HasSubmenu);
                    break;
                case ActionStyleEnum.NewWindow:
                    tag = Render_ALink(RenderEngine, mode, Id, HasSubmenu, NewWindow: true);
                    break;
                case ActionStyleEnum.Popup:
                case ActionStyleEnum.ForcePopup:
                    tag = Render_ALink(RenderEngine, mode, Id, HasSubmenu, Popup: Manager.CurrentSite.AllowPopups);
                    break;
                case ActionStyleEnum.PopupEdit:
                    tag = Render_ALink(RenderEngine, mode, Id, HasSubmenu, Popup: Manager.CurrentSite.AllowPopups, PopupEdit: Manager.CurrentSite.AllowPopups);
                    break;
                case ActionStyleEnum.OuterWindow:
                    tag = Render_ALink(RenderEngine, mode, Id, HasSubmenu, OuterWindow: true);
                    break;
                case ActionStyleEnum.Nothing:
                    tag = Render_ALink(RenderEngine, mode, Id, HasSubmenu, Nothing: true);
                    break;
                case ActionStyleEnum.Post:
                    tag = Render_ALink(RenderEngine, mode, Id, HasSubmenu, Post: true);
                    break;
            }
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        private TagBuilder Render_ALink(RenderEngineEnum renderEngine, RenderModeEnum mode, string id, bool hasSubmenu,
            bool NewWindow = false, bool Popup = false, bool PopupEdit = false, bool Post = false, bool Nothing = false, bool OuterWindow = false) {

            TagBuilder tag = new TagBuilder("a");
            if (!string.IsNullOrWhiteSpace(Tooltip))
                tag.MergeAttribute(Basics.CssTooltip, Tooltip);
            if (!string.IsNullOrWhiteSpace(Name))
                tag.MergeAttribute("data-name", Name);
            if (!Displayed)
                tag.MergeAttribute("style", "display:none");
            if (hasSubmenu) {
                if (renderEngine == RenderEngineEnum.BootstrapSmartMenu) {
                    tag.AddCssClass("dropdown-toggle");
                    tag.Attributes.Add("data-toggle", "dropdown-toggle");
                }
                tag.Attributes.Add("aria-haspopup", "true");
                tag.Attributes.Add("aria-expanded", "false");
            }

            if (!string.IsNullOrWhiteSpace(id))
                tag.Attributes.Add("id", id);

            if (!string.IsNullOrWhiteSpace(CssClass))
                tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(CssClass));
            string extraClass;
            switch (mode) {
                default:
                case RenderModeEnum.Button: extraClass = "y_act_button"; break;
                case RenderModeEnum.ButtonIcon: extraClass = "y_act_buttonicon"; break;
                case RenderModeEnum.IconsOnly: extraClass = "y_act_icon"; break;
                case RenderModeEnum.LinksOnly: extraClass = "y_act_link"; break;
                case RenderModeEnum.NormalLinks: extraClass = "y_act_normlink"; break;
                case RenderModeEnum.NormalMenu: extraClass = "y_act_normmenu"; break;
            }
            tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(extraClass));

            if (SubModule == null || SubModule == Guid.Empty) {
                string url = GetCompleteUrl(OnPage: true);
                if (!string.IsNullOrWhiteSpace(url)) {
                    tag.MergeAttribute("href", YetaWFManager.UrlEncodePath(url));
                    if (Manager.CurrentPage != null) {
                        string currUrl = Manager.CurrentPage.EvaluatedCanonicalUrl;
                        if (!string.IsNullOrWhiteSpace(currUrl) && currUrl != "/") {// this doesn't work on home page because everything matches
                            if (this.Url == currUrl)
                                tag.AddCssClass("t_currenturl");
                            if (currUrl.StartsWith(this.Url))
                                tag.AddCssClass("t_currenturlpart");
                        }
                    }
                } else
                    tag.MergeAttribute("href", "javascript:void(0);");
            } else
                tag.MergeAttribute("href", "javascript:void(0);");

            if (!string.IsNullOrWhiteSpace(ConfirmationText)) {
                if (Category == ActionCategoryEnum.Delete) {
                    // confirm deletions?
                    if (UserSettings.GetProperty<bool>("ConfirmDelete"))
                        tag.MergeAttribute(Basics.CssConfirm, ConfirmationText);
                } else {
                    // confirm actions?
                    if (UserSettings.GetProperty<bool>("ConfirmActions"))
                        tag.MergeAttribute(Basics.CssConfirm, ConfirmationText);
                }
            }
            if (!string.IsNullOrWhiteSpace(PleaseWaitText)) {
                tag.MergeAttribute(Basics.CssPleaseWait, PleaseWaitText);
            }
            if (CookieAsDoneSignal)
                tag.Attributes.Add(Basics.CookieDoneCssAttr, "");

            if (SaveReturnUrl)
                tag.Attributes.Add(Basics.CssSaveReturnUrl, "");
            if (!AddToOriginList)
                tag.Attributes.Add(Basics.CssDontAddToOriginList, "");

            if (NeedsModuleContext)
                tag.Attributes.Add(Basics.CssAddModuleContext, "");

            if (Post)
                tag.Attributes.Add(Basics.PostAttr, "");
            if (DontFollow || Post)
                tag.Attributes.Add("rel", "nofollow"); // this is so bots don't follow this assuming it's a simple page (Post actions can't betrieved with GET/HEAD anyway)
            if (OuterWindow)
                tag.Attributes.Add(Basics.CssOuterWindow, "");
            if (!Nothing)
                tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Basics.CssActionLink));
            if (NewWindow)
                tag.MergeAttribute("target", "_blank");
            if (Popup) {
                tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Basics.CssPopupLink));
                if (PopupEdit)
                    tag.Attributes.Add(Basics.CssAttrDataSpecialEdit, "");
            }
            if (mode == RenderModeEnum.Button || mode == RenderModeEnum.ButtonIcon)
                tag.Attributes.Add(Basics.CssAttrActionButton, "");

            bool hasText = false, hasImg = false;
            string innerHtml = "";
            if (mode != RenderModeEnum.LinksOnly && !string.IsNullOrWhiteSpace(ImageUrlFinal)) {
                TagBuilder tagImg = ImageHelper.BuildKnownImageTag(GetImageUrlFinal(), alt: mode == RenderModeEnum.NormalMenu ? MenuText : LinkText);
                innerHtml += tagImg.ToString(TagRenderMode.StartTag);
                hasImg = true;
            }
            if (mode != RenderModeEnum.IconsOnly && mode != RenderModeEnum.ButtonIcon) {
                string text = mode == RenderModeEnum.NormalMenu ? MenuText : LinkText;
                if (!string.IsNullOrWhiteSpace(text)) {
                    innerHtml += YetaWFManager.HtmlEncode(text);
                    hasText = true;
                }
            }
            if (hasText) {
                if (hasImg) {
                    tag.AddCssClass("y_act_textimg");
                } else {
                    tag.AddCssClass("y_act_text");
                }
            } else {
                if (hasImg) {
                    tag.AddCssClass("y_act_img");
                }
            }
            if (hasSubmenu && renderEngine == RenderEngineEnum.BootstrapSmartMenu) {
                innerHtml += " <span class='caret'></span>";
            }
            tag.AddCssClass(Globals.CssModuleNoPrint);
            tag.InnerHtml = innerHtml;

            return tag;
        }

        public string GetCompleteUrl(bool OnPage = false) {
            string qs = "";
            string url = Url;
            if (!string.IsNullOrWhiteSpace(url)) {
                // handle all args
                // add human readable args as URL segments
                RouteValueDictionary rvdhr = YetaWFManager.AnonymousObjectToRVD(QueryArgsHR);
                foreach (var entry in rvdhr) {
                    if (entry.Value != null) {
                        if (!url.EndsWith("/"))
                            url += "/";
                        url += string.Format("{0}/{1}", YetaWFManager.UrlEncodeSegment(entry.Key), YetaWFManager.UrlEncodeSegment(entry.Value.ToString()));
                    }
                }

                RouteValueDictionary rvd = YetaWFManager.AnonymousObjectToRVD(QueryArgs);
                if (NeedsModuleContext) //TODO: Url may already contain Basics.ModuleGuid
                    rvd.Add(Basics.ModuleGuid, GetOwningModuleGuid());
                foreach (var entry in rvd)
                    qs += string.Format("&{0}={1}", YetaWFManager.UrlEncodeArgs(entry.Key), entry.Value != null ? YetaWFManager.UrlEncodeArgs(entry.Value.ToString()) : "");
                if (QueryArgsRvd != null)
                    foreach (var entry in QueryArgsRvd)
                        qs += string.Format("&{0}={1}", YetaWFManager.UrlEncodeArgs(entry.Key), entry.Value != null ? YetaWFManager.UrlEncodeArgs(entry.Value.ToString()) : "");

                if (!string.IsNullOrWhiteSpace(qs) && !url.Contains('?'))
                    qs = '?' + qs.Substring(1);

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

        [ScriptIgnoreAttribute]
        public bool IsAuthorized {
            get {
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
                    ModuleDefinition mod  = ModuleDefinition.Load((Guid)SubModule, AllowNone: true);
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
                        url = url.Substring(Manager.CurrentSite.SiteUrlHttp.Length-1);
                    else if (url.StartsWith(Manager.CurrentSite.SiteUrlHttps, System.StringComparison.OrdinalIgnoreCase))
                        url = url.Substring(Manager.CurrentSite.SiteUrlHttps.Length-1);
                    if (url.StartsWith("/")) {
                        PageDefinition page = PageDefinition.LoadFromUrl(url);
                        if (page != null) {
                            PageSecurity = page.PageSecurity;
                            if (Resource.ResourceAccess.IsBackDoorWideOpen()) return true;
                            if (Manager.EditMode)
                                return page.IsAuthorized_Edit();
                            else
                                return page.IsAuthorized_View();
                        }
                        ModuleDefinition module = ModuleDefinition.FindDesignedModule(url);
                        if (module == null)
                            module = ModuleDefinition.LoadByUrl(url);
                        if (module != null) {
                            PageSecurity = module.ModuleSecurity;
                            if (Resource.ResourceAccess.IsBackDoorWideOpen()) return true;
                            if (Manager.EditMode)
                                return module.IsAuthorized(ModuleDefinition.RoleDefinition.Edit);
                            else
                                return module.IsAuthorized(ModuleDefinition.RoleDefinition.View);
                        }
                    }
                    // not a url for us, so we'll allow it
                    return true;
                }
                // no url
                return true;
            }
        }
    }
}
