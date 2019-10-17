/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.IO;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
#if MVC6
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

        /// <summary>
        /// Evaluates the menu and returns an updated (copied) list based on the user's actual permissions.
        /// </summary>
        public async Task<MenuList> GetUserMenuAsync() {
            MenuList menu = new MenuList();
            menu.RenderMode = this.RenderMode;
            foreach (ModuleAction m in this) {
                ModuleAction newAction = await EvaluateActionAsync(m);
                if (newAction != null)
                    menu.Add(newAction);
            }
            menu = new MenuList(DropEmptySubmenus(menu));
            return menu;
        }
        private SerializableList<ModuleAction> DropEmptySubmenus(SerializableList<ModuleAction> menu) {
            if (menu == null) return new MenuList();
            foreach (ModuleAction m in menu) {
                m.SubMenu = DropEmptySubmenus(m.SubMenu);
            }
            return new SerializableList<ModuleAction>(
                (from m in menu
                 where m.EntryType != ModuleAction.MenuEntryType.Parent || (m.SubMenu != null && m.SubMenu.Count() > 0) select m).ToList()
            );
        }
        private  async Task<ModuleAction> EvaluateActionAsync(ModuleAction origAction) {
            if (!await origAction.IsAuthorizedAsync())
                return null;
            // make a copy of the original entry
            ModuleAction newAction = new ModuleAction();
            ObjectSupport.CopyData(origAction, newAction);
            newAction._AuthorizationEvaluated = true;
            newAction.SubMenu = null;
            await EvaluateSubMenu(origAction, newAction);
            return newAction;
        }
        private async Task EvaluateSubMenu(ModuleAction origAction, ModuleAction newAction) {
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
