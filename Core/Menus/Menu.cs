/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;

namespace YetaWF.Core.Menus {

    [Serializable]
    public class MenuList : SerializableList<ModuleAction> {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        // MENU CACHE
        public class SavedCacheInfo {
            public MenuList Menu { get; set; }
            public bool EditMode { get; set; }
            public int UserId { get; set; }
        }
        private static string GetCacheName(Guid moduleGuid) {
            Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            return string.Format("{0}_MenuCache_{1}_{2}", package.AreaName, Manager.CurrentSite.Identity, moduleGuid);
        }
        public static void SetCache(Guid moduleGuid, SavedCacheInfo cacheInfo) {
            Manager.SessionSettings.SiteSettings.SetValue<MenuList.SavedCacheInfo>(GetCacheName(moduleGuid), cacheInfo);
            Manager.SessionSettings.SiteSettings.Save();
        }
        public static SavedCacheInfo GetCache(Guid moduleGuid) {
            return Manager.SessionSettings.SiteSettings.GetValue<MenuList.SavedCacheInfo>(GetCacheName(moduleGuid));
        }
        public static void ClearCachedMenus() {
            Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            string prefix = string.Format("{0}_MenuCache_{1}_", package.AreaName, Manager.CurrentSite.Identity);
            Manager.SessionSettings.SiteSettings.ClearAllStartingWith(prefix);
            Manager.SessionSettings.SiteSettings.Save();
        }

        // MENU

        public MenuList() { }
        public MenuList(SerializableList<ModuleAction> val) : base(val) { }
        public MenuList(List<ModuleAction> val) : base(val) { }

        public Guid Version { get; set; }
        public void NewVersion() { Version = Guid.NewGuid(); }

        public void New(ModuleAction action, ModuleAction.ActionLocationEnum location = ModuleAction.ActionLocationEnum.Explicit) {
            if (action != null) {
                if ((location & ModuleAction.ActionLocationEnum.Explicit) != 0) // grid links are always explicit calls
                    Add(action);
                else if ((action.Location & location) != 0)
                    Add(action);
            }
        }

        public ModuleAction.RenderModeEnum RenderMode { get; set; }

        public MvcHtmlString Render(HtmlHelper htmlHelper = null, string id = null, string cssClass = null, ModuleAction.RenderEngineEnum RenderEngine = ModuleAction.RenderEngineEnum.JqueryMenu) {

            HtmlBuilder hb = new HtmlBuilder();
            int level = 0;

            if (this.Count == 0)
                return MvcHtmlString.Empty;
            string menuContents = RenderLI(htmlHelper, this, null, RenderMode, RenderEngine, level);
            if (string.IsNullOrWhiteSpace(menuContents))
                return MvcHtmlString.Empty;

            // <ul class= style= >
            TagBuilder ulTag = new TagBuilder("ul");
            if (!string.IsNullOrWhiteSpace(cssClass))
                ulTag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));
            ulTag.AddCssClass(string.Format("t_lvl{0}", level));
            if (RenderEngine == ModuleAction.RenderEngineEnum.BootstrapSmartMenu) {
                ulTag.AddCssClass("nav");
                ulTag.AddCssClass("navbar-nav");
            }
            if (!string.IsNullOrWhiteSpace(id))
                ulTag.Attributes.Add("id", id);
            hb.Append(ulTag.ToString(TagRenderMode.StartTag));

            // <li>....</li>
            hb.Append(menuContents);

            // </ul>
            hb.Append(ulTag.ToString(TagRenderMode.EndTag));

            return MvcHtmlString.Create(hb.ToString());
        }
        private static string Render(HtmlHelper htmlHelper, List<ModuleAction> subMenu, Guid? subGuid, string cssClass, ModuleAction.RenderModeEnum renderMode, ModuleAction.RenderEngineEnum renderEngine, int level) {
            HtmlBuilder hb = new HtmlBuilder();

            string menuContents = RenderLI(htmlHelper, subMenu, subGuid, renderMode, renderEngine, level);
            if (string.IsNullOrWhiteSpace(menuContents)) return "";

            // <ul>
            TagBuilder ulTag = new TagBuilder("ul");
            ulTag.AddCssClass(string.Format("t_lvl{0}", level));
            if (renderEngine == ModuleAction.RenderEngineEnum.BootstrapSmartMenu)
                ulTag.AddCssClass("dropdown-menu");
            if (subGuid != null) {
                ulTag.AddCssClass("t_megamenu_content");
                ulTag.AddCssClass("mega-menu"); // used by smartmenus
            }
            if (!string.IsNullOrWhiteSpace(cssClass))
                ulTag.AddCssClass(cssClass);
            hb.Append(ulTag.ToString(TagRenderMode.StartTag));

            // <li>....</li>
            hb.Append(menuContents);

            // </ul>
            hb.Append(ulTag.ToString(TagRenderMode.EndTag));

            return hb.ToString();
        }

        private static string RenderLI(HtmlHelper htmlHelper, List<ModuleAction> subMenu, Guid? subGuid, ModuleAction.RenderModeEnum renderMode, ModuleAction.RenderEngineEnum renderEngine, int level) {
            HtmlBuilder hb = new HtmlBuilder();

            ++level;

            if (subGuid != null) {
                // megamenu content
                // <li>
                TagBuilder tag = new TagBuilder("li");
                tag.AddCssClass("t_megamenu_content");
                hb.Append(tag.ToString(TagRenderMode.StartTag));

                ModuleDefinition subMod = ModuleDefinition.Load((Guid)subGuid, AllowNone: true);
                if (subMod != null)
                    hb.Append(subMod.RenderModule(htmlHelper));

                hb.Append("</li>\n");
            } else {
                foreach (var menuEntry in subMenu) {

                    if (menuEntry.Enabled && menuEntry.RendersSomething) {

                        bool rendered = false;
                        string subMenuContents = null;

                        Guid? subModGuid = null;
                        if (!Manager.EditMode) {
                            // don't show submodule in edit mode
                            if ((menuEntry.SubModule != null && menuEntry.SubModule != Guid.Empty))
                                subModGuid = menuEntry.SubModule;
                        }

                        if (subModGuid != null || (menuEntry.SubMenu != null && menuEntry.SubMenu.Count > 0)) {

                            subMenuContents = Render(htmlHelper, menuEntry.SubMenu, subModGuid, menuEntry.CssClass, renderMode, renderEngine, level);
                            if (!string.IsNullOrWhiteSpace(subMenuContents)) {
                                // <li>
                                TagBuilder tag = new TagBuilder("li");
                                if (renderEngine == ModuleAction.RenderEngineEnum.BootstrapSmartMenu)
                                    tag.AddCssClass("dropdown");
                                if (subModGuid != null)
                                    tag.AddCssClass("t_megamenu_hassub");
                                hb.Append(tag.ToString(TagRenderMode.StartTag));

                                MvcHtmlString menuContents =  menuEntry.Render(renderMode, RenderEngine: renderEngine, HasSubmenu: true);
                                hb.Append(menuContents);

                                hb.Append("\n");
                                hb.Append(subMenuContents);

                                hb.Append("</li>\n");
                                rendered = true;
                            }
                        }

                        if (!rendered) {
                            // <li>
                            TagBuilder tag = new TagBuilder("li");
                            //if (!menuEntry.Enabled)
                            //    tag.MergeAttribute("disabled", "disabled");
                            hb.Append(tag.ToString(TagRenderMode.StartTag));

                            MvcHtmlString menuContents = menuEntry.Render(renderMode, RenderEngine: renderEngine);
                            hb.Append(menuContents);

                            hb.Append("</li>\n");
                        }
                    }
                }
            }

            --level;

            return hb.ToString();
        }

        public string SerializeToJSON() {
            return YetaWFManager.Jser.Serialize(this);
        }

        internal void UpdateIds() {
            // ids are used for editing purposes to match up old and new menu entries
            int counter = FindHighestId();// get the highest id used so we don't introduce conflicts
            UpdateIds(this, ref counter);
        }
        private void UpdateIds(SerializableList<ModuleAction> menuList, ref int counter) {
            foreach (ModuleAction action in menuList) {
                if (action.Id != 0) {
                    if (action.Id >= counter) throw new InternalError("Encountered out of range id");
                } else {
                    action.Id = counter++;
                }
                action.Location = ModuleAction.ActionLocationEnum.MainMenu; // mark it as a main menu (really any standalone menu)
                if (action.SubMenu != null && action.SubMenu.Count > 0)
                    UpdateIds(action.SubMenu, ref counter);
            }
        }
        internal int FindHighestId() {
            int max = 100;
            FindHighestId(this, ref max);
            return max;
        }
        private void FindHighestId(SerializableList<ModuleAction> menuList, ref int max) {
            foreach (ModuleAction action in menuList) {
                if (action.Id >= max)
                    max = action.Id+1;
                if (action.SubMenu != null && action.SubMenu.Count > 0)
                    FindHighestId(action.SubMenu, ref max);
            }
        }
        // merge the entry back into the menu
        public void MergeNewAction(int activeEntry, int newAfter, ModuleAction newAction) {
            ModuleAction action = null;
            if (activeEntry > 0) {
                action = FindId(activeEntry);
                if (action == null) throw new InternalError("Can't find entry {0} to update", activeEntry);
            } else if (newAfter > 0) { // adding as child item
                ModuleAction parentAction = FindId(newAfter);
                if (parentAction == null) throw new InternalError("Can't find parent entry {0}", activeEntry);
                if (parentAction.SubMenu == null)
                    parentAction.SubMenu = new SerializableList<ModuleAction>();
                action = new ModuleAction();
                parentAction.SubMenu.Insert(0, action);
            } else {
                // insert at top
                action = new ModuleAction();
                Insert(0, action);
            }
            if (string.IsNullOrWhiteSpace(newAction.Url)) {
                // parent item without real action
                action.Separator = false;
                action.Url = null;
                action.SubModule = null;
                action.MenuText = newAction.MenuText;
                action.LinkText = newAction.LinkText;
                action.ImageUrlFinal = newAction.ImageUrlFinal;
                action.Tooltip = newAction.Tooltip;
                action.Legend = newAction.Legend;
                action.Enabled = newAction.Enabled;
                action.CssClass = newAction.CssClass;
                action.Style = ModuleAction.ActionStyleEnum.Normal;
                action.Mode = ModuleAction.ActionModeEnum.Any;
                action.Category = ModuleAction.ActionCategoryEnum.Read;
                action.LimitToRole = newAction.LimitToRole;
                action.AuthorizationIgnore = false;
                action.ConfirmationText = new MultiString();
                action.PleaseWaitText = new MultiString();
                action.SaveReturnUrl = false;
                action.AddToOriginList = false;
                action.NeedsModuleContext = false;
            } else if (newAction.Separator) {
                // separator without real action
                action.Separator = newAction.Separator;
                action.Url = null;
                action.SubModule = null;
                action.MenuText = new MultiString();
                action.LinkText = new MultiString();
                action.ImageUrlFinal = null;
                action.Tooltip = new MultiString();
                action.Legend = new MultiString();
                action.CssClass = null;
                action.Style = ModuleAction.ActionStyleEnum.Normal;
                action.Mode = ModuleAction.ActionModeEnum.Any;
                action.Category = ModuleAction.ActionCategoryEnum.Read;
                action.LimitToRole = 0;
                action.AuthorizationIgnore = false;
                action.ConfirmationText = new MultiString();
                action.PleaseWaitText = new MultiString();
                action.SaveReturnUrl = false;
                action.AddToOriginList = false;
                action.NeedsModuleContext = false;
            } else {
                action.Separator = false;
                action.Url = newAction.Url;
                action.SubModule = newAction.SubModule != Guid.Empty ? newAction.SubModule : null;
                action.MenuText = newAction.MenuText;
                action.LinkText = newAction.LinkText;
                action.ImageUrlFinal = newAction.ImageUrlFinal;
                action.Tooltip = newAction.Tooltip;
                action.Legend = newAction.Legend;
                action.Enabled = newAction.Enabled;
                action.CssClass = newAction.CssClass;
                action.Style = newAction.Style;
                action.Mode = newAction.Mode;
                action.Category = newAction.Category;
                action.LimitToRole = newAction.LimitToRole;
                action.AuthorizationIgnore = newAction.AuthorizationIgnore;
                action.ConfirmationText = newAction.ConfirmationText;
                action.PleaseWaitText = newAction.PleaseWaitText;
                action.SaveReturnUrl = newAction.SaveReturnUrl;
                action.AddToOriginList = newAction.AddToOriginList;
                action.NeedsModuleContext = newAction.NeedsModuleContext;
            }
            action.CookieAsDoneSignal = false;
            action.Location = ModuleAction.ActionLocationEnum.AnyMenu;
            //action.SubMenu = keep as-is
            action.QueryArgs = null;
            action.QueryArgsRvd = null;
            //action.Id = will be updated during save
            action._AuthorizationEvaluated = false;

            UpdateIds();
        }

        public static MenuList DeserializeFromJSON(string menuJSON, MenuList Original) {
            List<ModuleAction> actions = (List<ModuleAction>) YetaWFManager.Jser.Deserialize(menuJSON, typeof(List<ModuleAction>));
            // fix some settings that aren't updated on the browser side
            FixMenuEntries(actions);
            MenuList menu = new MenuList(actions);
            return menu;
        }
        public ModuleAction FindId(int activeEntry) {
            if (activeEntry != 0) {
                ModuleAction entry = FindId(this, activeEntry);
                if (entry != null)
                    return entry;
            }
            return new ModuleAction();
        }
        private ModuleAction FindId(SerializableList<ModuleAction> menuList, int activeEntry) {
            foreach (ModuleAction action in menuList) {
                if (action.Id == activeEntry)
                    return action;
                if (action.SubMenu != null && action.SubMenu.Count > 0) {
                    ModuleAction entry = FindId(action.SubMenu, activeEntry);
                    if (entry != null)
                        return entry;
                }
            }
            return null;
        }

        private static void FixMenuEntries(List<ModuleAction> actions) {
            foreach (ModuleAction action in actions) {
                if (action.SubModule == Guid.Empty)
                    action.SubModule = null;
                if (string.IsNullOrWhiteSpace(action.Url)) {
                    // parent item without real action
                    action.SubModule = null;
                    action.Separator = false;
                    action.Style = ModuleAction.ActionStyleEnum.Normal;
                    action.Mode = ModuleAction.ActionModeEnum.Any;
                    action.Category = ModuleAction.ActionCategoryEnum.Read;
                    action.ConfirmationText = new MultiString();
                    action.PleaseWaitText = new MultiString();
                    action.SaveReturnUrl = false;
                    action.AddToOriginList = false;
                    action.NeedsModuleContext = false;
                    action.CookieAsDoneSignal = false;
                    action.Location = ModuleAction.ActionLocationEnum.AnyMenu;
                    action.QueryArgs = null;
                    action.QueryArgsRvd = null;
                } else if (action.Separator) {
                    // separator without real action
                    action.Url = null;
                    action.SubModule = null;
                    action.MenuText = new MultiString();
                    action.LinkText = new MultiString();
                    action.ImageUrlFinal = null;
                    action.Tooltip = new MultiString();
                    action.Legend = new MultiString();
                    action.Style = ModuleAction.ActionStyleEnum.Normal;
                    action.Mode = ModuleAction.ActionModeEnum.Any;
                    action.Category = ModuleAction.ActionCategoryEnum.Read;
                    action.LimitToRole = 0;
                    action.AuthorizationIgnore = false;
                    action.ConfirmationText = new MultiString();
                    action.PleaseWaitText = new MultiString();
                    action.SaveReturnUrl = false;
                    action.AddToOriginList = false;
                    action.NeedsModuleContext = false;
                    action.CookieAsDoneSignal = false;
                    action.Location = ModuleAction.ActionLocationEnum.AnyMenu;
                    action.QueryArgs = null;
                    action.QueryArgsRvd = null;
                }
                if (action.SubMenu != null && action.SubMenu.Count > 0)
                    FixMenuEntries(action.SubMenu);
            }
        }

        // Evaluate the menu and return an updated (copied) list based on the user's permissions
        public MenuList GetUserMenu() {
            MenuList menu = new MenuList();
            menu.RenderMode = RenderMode;

            foreach (ModuleAction m in this) {
                ModuleAction newAction = EvaluateAction(m);
                if (newAction != null)
                    menu.Add(newAction);
            }
            menu = new MenuList(DropEmptySubmenus(menu));
            menu.UpdateIds();
            menu.Version = this.Version;
            return menu;
        }
        private static SerializableList<ModuleAction> DropEmptySubmenus(SerializableList<ModuleAction> menu) {
            if (menu == null) return new MenuList();
            foreach (ModuleAction m in menu) {
                m.SubMenu = DropEmptySubmenus(m.SubMenu);
            }
            return new SerializableList<ModuleAction>(
                (from m in menu
                 where m.EntryType != ModuleAction.MenuEntryType.Parent || (m.SubMenu != null && m.SubMenu.Count() > 0) select m).ToList()
            );
        }
        private ModuleAction EvaluateAction(ModuleAction origAction) {
            if (!origAction.IsAuthorized)
                return null;

            // make a copy of the original entry
            ModuleAction newAction = new ModuleAction();
            ObjectSupport.CopyData(origAction, newAction);
            newAction._AuthorizationEvaluated = true;
            newAction.SubMenu = null;

            EvaluateSubMenu(origAction, newAction);
            //if ((newAction.SubMenu == null || newAction.SubMenu.Count == 0) && string.IsNullOrWhiteSpace(newAction.Url))
            //    return null;
            return newAction;
        }
        private void EvaluateSubMenu(ModuleAction origAction, ModuleAction newAction) {
            // now evaluate all submenus
            if (origAction.SubMenu != null && origAction.SubMenu.Count > 0) {
                foreach (ModuleAction m in origAction.SubMenu) {
                    ModuleAction subAction = EvaluateAction(m);
                    if (subAction != null) {
                        if (newAction.SubMenu == null)
                            newAction.SubMenu = new SerializableList<ModuleAction>();
                        newAction.SubMenu.Add(subAction);
                    }
                }
            }
        }
    }
}
