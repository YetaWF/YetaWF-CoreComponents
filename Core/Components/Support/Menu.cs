/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.IO;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Components {

    public interface IModuleMenuAsync {
        Task<MenuList> GetMenuAsync();
        Task SaveMenuAsync(MenuList newMenu);
    }

    [Serializable]
    public class MenuList : SerializableList<ModuleAction> {

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        // MENU CACHE
        public class SavedCacheInfo {
            public MenuList Menu { get; set; }
            public bool EditMode { get; set; }
            public int UserId { get; set; }
            public long MenuVersion { get; set; }
        }
        private static string GetCacheName(Guid moduleGuid) {
            Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            return string.Format("{0}_MenuCache_{1}_{2}", package.AreaName, Manager.CurrentSite.Identity, moduleGuid);
        }
        public static SavedCacheInfo GetCache(Guid moduleGuid) {
            SessionStateIO<SavedCacheInfo> session = new SessionStateIO<SavedCacheInfo> {
                Key = GetCacheName(moduleGuid)
            };
            return session.Load();
        }
        public static void SetCache(Guid moduleGuid, SavedCacheInfo cacheInfo) {
            SessionStateIO<SavedCacheInfo> session = new SessionStateIO<SavedCacheInfo> {
                Key = GetCacheName(moduleGuid),
                Data = cacheInfo,
            };
            session.Save();
        }
        public static void ClearCachedMenus() {
            Package package = YetaWF.Core.Controllers.AreaRegistration.CurrentPackage;
            string prefix = string.Format("{0}_MenuCache_{1}_", package.AreaName, Manager.CurrentSite.Identity);
            List<string> keys = new List<string>();
            foreach (string name in Manager.CurrentSession.Keys) {
                if (name.StartsWith(prefix))
                    keys.Add(name);
            }
            foreach (string name in keys) {
                try {
                    Manager.CurrentSession.Remove(name);
                } catch (Exception) { }
            }
        }

        // MENU

        public MenuList() { }
        public MenuList(SerializableList<ModuleAction> val) : base(val) { }
        public MenuList(List<ModuleAction> val) : base(val) { }

        [Data_DontSave]
        public string LICssClass { get; set; }

        public void New(ModuleAction action, ModuleAction.ActionLocationEnum location = ModuleAction.ActionLocationEnum.Explicit) {
            if (action != null) {
                if ((location & ModuleAction.ActionLocationEnum.Explicit) != 0) // grid links are always explicit calls
                    Add(action);
                else if ((action.Location & location) != 0)
                    Add(action);
            }
        }
        public void NewIf(ModuleAction action, ModuleAction.ActionLocationEnum desiredLocation, ModuleAction.ActionLocationEnum location = ModuleAction.ActionLocationEnum.Explicit) {
            if (action != null) {
                if ((location & ModuleAction.ActionLocationEnum.Explicit) != 0) // grid links are always explicit calls
                    Add(action);
                else if ((desiredLocation & location) != 0)
                    Add(action);
            }
        }

        public ModuleAction.RenderModeEnum RenderMode { get; set; }

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
            if (string.IsNullOrWhiteSpace(newAction.Url) && newAction.SubModule == null) {
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
                action.DontFollow = false;
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
                action.DontFollow = false;
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
                action.DontFollow = newAction.DontFollow;
            }
            action.CookieAsDoneSignal = false;
            action.Location = ModuleAction.ActionLocationEnum.AnyMenu;
            //action.SubMenu = keep as-is
            action.QueryArgs = null;
            action.QueryArgsDict = null;
            //action.Id = will be updated during save
            action._AuthorizationEvaluated = false;

            UpdateIds();
        }

        public static MenuList DeserializeFromJSON(string menuJSON, MenuList Original) {
            List<ModuleAction> actions = (List<ModuleAction>) YetaWFManager.JsonDeserialize(menuJSON, typeof(List<ModuleAction>));
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
                if (string.IsNullOrWhiteSpace(action.Url) && action.SubModule == null) {
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
                    action.QueryArgsDict = null;
                    action.DontFollow = false;
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
                    action.QueryArgsDict = null;
                    action.DontFollow = false;
                }
                if (action.SubMenu != null && action.SubMenu.Count > 0)
                    FixMenuEntries(action.SubMenu);
            }
        }

        // Evaluate the menu and return an updated (copied) list based on the user's permissions
        public async Task<MenuList> GetUserMenuAsync() {
            MenuList menu = new MenuList();
            menu.RenderMode = RenderMode;

            foreach (ModuleAction m in this) {
                ModuleAction newAction = await EvaluateActionAsync(m);
                if (newAction != null)
                    menu.Add(newAction);
            }
            menu = new MenuList(DropEmptySubmenus(menu));
            menu.UpdateIds();
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
        private async Task<ModuleAction> EvaluateActionAsync(ModuleAction origAction) {
            if (!await origAction.IsAuthorizedAsync())
                return null;

            // make a copy of the original entry
            ModuleAction newAction = new ModuleAction();
            ObjectSupport.CopyData(origAction, newAction);
            newAction._AuthorizationEvaluated = true;
            newAction.SubMenu = null;

            await EvaluateSubMenu(origAction, newAction);
            //if ((newAction.SubMenu == null || newAction.SubMenu.Count == 0) && string.IsNullOrWhiteSpace(newAction.Url))
            //    return null;
            return newAction;
        }
        private async Task EvaluateSubMenu(ModuleAction origAction, ModuleAction newAction) {
            // now evaluate all submenus
            if (origAction.SubMenu != null && origAction.SubMenu.Count > 0) {
                foreach (ModuleAction m in origAction.SubMenu) {
                    ModuleAction subAction = await EvaluateActionAsync(m);
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
