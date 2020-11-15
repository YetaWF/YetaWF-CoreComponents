/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Modules {

    public partial class ModuleDefinition {

        /* private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(ModuleDefinition), name, defaultValue, parms); } */

        public virtual async Task<MenuList> GetModuleMenuListAsync(ModuleAction.RenderModeEnum renderMode, ModuleAction.ActionLocationEnum location) {

            MenuList moduleMenu = new MenuList() { RenderMode = renderMode };

            if (Manager.EditMode && !Manager.IsInPopup) {

                // module editing services
                ModuleDefinition? modServices = await ModuleDefinition.LoadAsync(Manager.CurrentSite.ModuleControlServices, AllowNone: true);
                if (modServices == null)
                    throw new InternalError("No module control services available - no module has been defined");

                // module settings services
                ModuleDefinition? modSettings = await ModuleDefinition.LoadAsync(Manager.CurrentSite.ModuleEditingServices, AllowNone: true);
                if (modSettings == null)
                    throw new InternalError("No module edit settings services available - no module has been defined");

                // package localization services
                ModuleDefinition? modLocalize = await ModuleDefinition.LoadAsync(Manager.CurrentSite.PackageLocalizationServices, AllowNone: true);
                if (modLocalize == null)
                    throw new InternalError("No localization services available - no module has been defined");

                PageDefinition page = Manager.CurrentPage;
                if (!this.Temporary && page != null && !page.Temporary) {

                    if ((location & ModuleAction.ActionLocationEnum.AnyMenu) != 0) {
                        // move to other panes
                        MenuList subMenu = await GetMoveToOtherPanesAsync(page, modServices);
                        if (subMenu.Count > 0) {
                            ModuleAction action = new ModuleAction(null) { MenuText = __ResStr("mmMoveTo", "Move To"), SubMenu = subMenu };
                            bool allDisabled = (from s in subMenu where s.Enabled == false select s).Count() == subMenu.Count;
                            if (!allDisabled)
                                moduleMenu.New(action, location);
                        }

                        // move within pane
                        subMenu = await GetMoveWithinPaneAsync(page, modServices);
                        if (subMenu.Count > 0) {
                            ModuleAction action = new ModuleAction(null) { MenuText = __ResStr("mmMove", "Move"), SubMenu = subMenu };
                            bool allDisabled = (from s in subMenu where s.Enabled == false select s).Count() == subMenu.Count;
                            if (!allDisabled)
                                moduleMenu.New(action, location);
                        }
                    }
                }
                if (!this.Temporary) {
                    // module settings
                    ModuleAction? action = await modSettings.GetModuleActionAsync("Settings", this.ModuleGuid);
                    moduleMenu.New(action, location);

                    // export module
                    action = await modServices.GetModuleActionAsync("ExportModule", this);
                    moduleMenu.New(action, location);

                    // localize
                    action = await modLocalize.GetModuleActionAsync("Browse", null, Package.GetCurrentPackage(this));
                    if (action != null) {
                        if (action.QueryArgsDict == null)
                            action.QueryArgsDict = new QueryHelper();
                        action.QueryArgsDict.Add(Globals.Link_NoEditMode, "y"); // force no edit mode
                        action.QueryArgsDict.Add(Globals.Link_PageControl, "y"); // force no control panel
                        moduleMenu.New(action, location);
                    }
                }

                if (!this.Temporary && page != null && !page.Temporary) {
                    // remove module
                    if (!page.Temporary && !this.Temporary) {
                        ModuleAction? action;
                        action = await modServices.GetModuleActionAsync("Remove", page, this, Guid.Empty, Manager.PaneRendered);
                        moduleMenu.New(action, location);
                        action = await modServices.GetModuleActionAsync("RemovePermanent", page, this, Guid.Empty, Manager.PaneRendered);
                        moduleMenu.New(action, location);
                    }
                }
            }

            if (!this.Temporary) {
                if (Manager.EditMode || this.ShowHelp) {
                    // module editing services
                    ModuleDefinition? modServices = await ModuleDefinition.LoadAsync(Manager.CurrentSite.ModuleControlServices, AllowNone: true);
                    //if (modServices == null)
                    //    throw new InternalError("No module control services available - no module has been defined");
                    if (modServices != null) {
                        ModuleAction? action = await modServices.GetModuleActionAsync("Help", this);
                        moduleMenu.New(action, location);
                    }
                }
            }

            // Add the module's actions
            foreach (var action in await RetrieveModuleActionsAsync()) {
                if ((action.Location & ModuleAction.ActionLocationEnum.NoAuto) == 0)
                    if (!Manager.IsInPopup || (action.Location & ModuleAction.ActionLocationEnum.InPopup) != 0)
                        moduleMenu.New(action, location);
            }
            return moduleMenu;
        }

        private async Task<MenuList> GetMoveToOtherPanesAsync(PageDefinition page, ModuleDefinition modServices) {

            MenuList menu = new MenuList();
            foreach (var pane in page.GetPanes()) {
                ModuleAction? action = await modServices.GetModuleActionAsync("MoveToPane", page, this, Manager.PaneRendered, pane);
                if (action != null)
                    menu.Add(action);
            }
            return menu;
        }

        private async Task<MenuList> GetMoveWithinPaneAsync(PageDefinition page, ModuleDefinition modServices) {

            MenuList menu = new MenuList();

            ModuleAction? action;
            string? pane = Manager.PaneRendered;
            action = await modServices.GetModuleActionAsync("MoveUp", page, this, pane);
            if (action != null)
                menu.Add(action);

            action = await modServices.GetModuleActionAsync("MoveDown", page, this, pane);
            if (action != null)
                menu.Add(action);

            action = await modServices.GetModuleActionAsync("MoveTop", page, this, pane);
            if (action != null)
                menu.Add(action);

            action = await modServices.GetModuleActionAsync("MoveBottom", page, this, pane);
            if (action != null)
                menu.Add(action);

            return menu;
        }
    }
}
