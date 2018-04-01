/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Menus;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;
#if MVC6
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Modules {

    public partial class ModuleDefinition {

        public virtual async Task<MenuList> GetModuleMenuListAsync(ModuleAction.RenderModeEnum renderMode, ModuleAction.ActionLocationEnum location) {

            MenuList moduleMenu = new MenuList() { RenderMode = renderMode };

            if (Manager.EditMode && !Manager.IsInPopup) {

                // module editing services
                ModuleDefinition modServices = await ModuleDefinition.LoadAsync(Manager.CurrentSite.ModuleControlServices, AllowNone: true);
                if (modServices == null)
                    throw new InternalError("No module control services available - no module has been defined");

                // module settings services
                ModuleDefinition modSettings = await ModuleDefinition.LoadAsync(Manager.CurrentSite.ModuleEditingServices, AllowNone: true);
                if (modSettings == null)
                    throw new InternalError("No module edit settings services available - no module has been defined");

                // package localization services
                ModuleDefinition modLocalize = await ModuleDefinition.LoadAsync(Manager.CurrentSite.PackageLocalizationServices, AllowNone: true);
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
                    ModuleAction action = await modSettings.GetModuleActionAsync("Settings", this.ModuleGuid);
                    moduleMenu.New(action, location);

                    // export module
                    action = await modServices.GetModuleActionAsync("ExportModule", this);
                    moduleMenu.New(action, location);

                    // localize
                    action = await modLocalize.GetModuleActionAsync("Browse", null, Package.GetCurrentPackage(this));
                    if (action.QueryArgsDict == null)
                        action.QueryArgsDict = new QueryHelper();
                    action.QueryArgsDict.Add(Globals.Link_NoEditMode, "y"); // force no edit mode
                    action.QueryArgsDict.Add(Globals.Link_PageControl, "y"); // force no control panel
                    moduleMenu.New(action, location);
                }

                if (!this.Temporary && page != null && !page.Temporary) {
                    // remove module
                    if (!page.Temporary && !this.Temporary) {
                        ModuleAction action = await modServices.GetModuleActionAsync("Remove", page, this, Guid.Empty, Manager.PaneRendered);
                        moduleMenu.New(action, location);
                    }
                }
            }

            if (!this.Temporary) {
                if (Manager.EditMode || this.ShowHelp) {
                    // module editing services
                    ModuleDefinition modServices = await ModuleDefinition.LoadAsync(Manager.CurrentSite.ModuleControlServices, AllowNone: true);
                    //if (modServices == null)
                    //    throw new InternalError("No module control services available - no module has been defined");
                    if (modServices != null) {
                        ModuleAction action = await modServices.GetModuleActionAsync("Help", this);
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

        public async Task<HtmlString> RenderModuleMenuAsync() {

            HtmlBuilder hb = new HtmlBuilder();

            MenuList moduleMenu = await GetModuleMenuListAsync(ModuleAction.RenderModeEnum.NormalMenu, ModuleAction.ActionLocationEnum.ModuleMenu);

            string menuContents = (await moduleMenu.RenderAsync(null, null, Globals.CssModuleMenu)).ToString();
            if (string.IsNullOrWhiteSpace(menuContents))
                return HtmlStringExtender.Empty;// we don't have a menu

            // <div class= >
            TagBuilder divTag = new TagBuilder("div");
            divTag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModuleMenuEditIcon));
            divTag.Attributes.Add("style", "display:none");
            hb.Append(divTag.ToString(TagRenderMode.StartTag));

            SkinImages skinImages = new SkinImages();
            string imageUrl = await skinImages.FindIcon_PackageAsync("#ModuleMenu", Package.GetCurrentPackage(this));
            TagBuilder tagImg = ImageHelper.BuildKnownImageTag(imageUrl, alt: __ResStr("mmAlt", "Menu"));
            hb.Append(tagImg.ToString(TagRenderMode.StartTag));

            // <div>
            TagBuilder div2Tag = new TagBuilder("div");
            div2Tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModuleMenuContainer));
            hb.Append(div2Tag.ToString(TagRenderMode.StartTag));

            // <ul><li> menu
            hb.Append(menuContents);

            // </div>
            hb.Append(div2Tag.ToString(TagRenderMode.EndTag));

            // </div>
            hb.Append(divTag.ToString(TagRenderMode.EndTag));

            //Manager.ScriptManager.AddKendoUICoreJsFile("kendo.popup.min.js"); // is now a prereq of kendo.window (2017.2.621)
            await Manager.ScriptManager.AddKendoUICoreJsFileAsync("kendo.menu.min.js");

            await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF", "Core", "ModuleMenu");
            await Manager.AddOnManager.AddAddOnNamedAsync("YetaWF", "Core", "Modules");// various module support
            await Manager.AddOnManager.AddAddOnGlobalAsync("jquery.com", "jquery-color");// for color change when entering module edit menu

            return hb.ToHtmlString();
        }

        public async Task<HtmlString> RenderModuleLinksAsync(ModuleAction.RenderModeEnum renderMode, string cssClass) {

            HtmlBuilder hb = new HtmlBuilder();

            MenuList moduleMenu = await GetModuleMenuListAsync(renderMode, ModuleAction.ActionLocationEnum.ModuleLinks);

            string menuContents = (await moduleMenu.RenderAsync(null, null, Globals.CssModuleLinks)).ToString();
            if (string.IsNullOrWhiteSpace(menuContents))
                return HtmlStringExtender.Empty;// we don't have a menu

            // <div>
            TagBuilder div2Tag = new TagBuilder("div");
            div2Tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(cssClass));
            hb.Append(div2Tag.ToString(TagRenderMode.StartTag));

            // <ul><li> menu
            hb.Append(menuContents);

            // </div>
            hb.Append(div2Tag.ToString(TagRenderMode.EndTag));

            return hb.ToHtmlString();
        }

        private async Task<MenuList> GetMoveToOtherPanesAsync(PageDefinition page, ModuleDefinition modServices) {

            MenuList menu = new MenuList();
            foreach (var pane in await page.GetPanesAsync()) {
                ModuleAction action = await modServices.GetModuleActionAsync("MoveToPane", page, this, Manager.PaneRendered, pane);
                if (action != null)
                    menu.Add(action);
            }
            return menu;
        }

        private async Task<MenuList> GetMoveWithinPaneAsync(PageDefinition page, ModuleDefinition modServices) {

            MenuList menu = new MenuList();

            ModuleAction action;
            string pane = Manager.PaneRendered;
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
