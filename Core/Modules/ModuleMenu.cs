/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Linq;
using System.Web.Mvc;
using YetaWF.Core.Menus;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Skins;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.Modules {

    public partial class ModuleDefinition {

        public virtual MenuList GetModuleMenuList(ModuleAction.RenderModeEnum renderMode, ModuleAction.ActionLocationEnum location) {

            MenuList moduleMenu = new MenuList() { RenderMode = renderMode };

            if (Manager.EditMode && !Manager.IsInPopup) {

                // module editing services
                ModuleDefinition modServices = ModuleDefinition.Load(Manager.CurrentSite.ModuleControlServices, AllowNone: true);
                if (modServices == null)
                    throw new InternalError("No module control services available - no module has been defined");

                // module settings services
                ModuleDefinition modSettings = ModuleDefinition.Load(Manager.CurrentSite.ModuleEditingServices, AllowNone: true);
                if (modSettings == null)
                    throw new InternalError("No module edit settings services available - no module has been defined");

                // package localization services
                ModuleDefinition modLocalize = ModuleDefinition.Load(Manager.CurrentSite.PackageLocalizationServices, AllowNone: true);
                if (modLocalize == null)
                    throw new InternalError("No localization services available - no module has been defined");

                PageDefinition page = Manager.CurrentPage;
                if (!this.Temporary && page != null && !page.Temporary) {

                    if ((location & ModuleAction.ActionLocationEnum.AnyMenu) != 0) {
                        // move to other panes
                        MenuList subMenu = GetMoveToOtherPanes(page, modServices);
                        if (subMenu.Count > 0) {
                            ModuleAction action = new ModuleAction(null) { MenuText = __ResStr("mmMoveTo", "Move To"), SubMenu = subMenu };
                            bool allDisabled = (from s in subMenu where s.Enabled == false select s).Count() == subMenu.Count;
                            if (!allDisabled)
                                moduleMenu.New(action, location);
                        }

                        // move within pane
                        subMenu = GetMoveWithinPane(page, modServices);
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
                    ModuleAction action = modSettings.GetModuleAction("Settings", this.ModuleGuid);
                    moduleMenu.New(action, location);

                    // export module
                    action = modServices.GetModuleAction("ExportModule", this);
                    moduleMenu.New(action, location);

                    // localize
                    action = modLocalize.GetModuleAction("Browse", null, Package.GetCurrentPackage(this));
                    if (action.QueryArgsRvd == null)
                        action.QueryArgsRvd = new System.Web.Routing.RouteValueDictionary();
                    action.QueryArgsRvd.Add(Globals.Link_TempNoEditMode, "y"); // force no edit mode
                    moduleMenu.New(action, location);
                }

                if (!this.Temporary && page != null && !page.Temporary) {
                    // remove module
                    if (!page.Temporary && !this.Temporary) {
                        ModuleAction action = modServices.GetModuleAction("Remove", page, this, Guid.Empty, Manager.PaneRendered);
                        moduleMenu.New(action, location);
                    }
                }

            }

            if (!this.Temporary) {
                if (Manager.EditMode || this.ShowHelp) {
                    // module editing services
                    ModuleDefinition modServices = ModuleDefinition.Load(Manager.CurrentSite.ModuleControlServices, AllowNone: true);
                    if (modServices == null)
                        throw new InternalError("No module control services available - no module has been defined");

                    ModuleAction action = modServices.GetModuleAction("Help", this);
                    moduleMenu.New(action, location);
                }
            }

            // Add the module's actions
            foreach (var action in ModuleActions) {
                if ((action.Location & ModuleAction.ActionLocationEnum.NoAuto) == 0)
                    if (!Manager.IsInPopup || (action.Location & ModuleAction.ActionLocationEnum.InPopup) != 0)
                        moduleMenu.New(action, location);
            }
            return moduleMenu;
        }

        public MvcHtmlString RenderModuleMenu() {

            HtmlBuilder hb = new HtmlBuilder();

            MenuList moduleMenu = GetModuleMenuList(ModuleAction.RenderModeEnum.NormalMenu, ModuleAction.ActionLocationEnum.ModuleMenu);

            string menuContents = moduleMenu.Render(null, null, Globals.CssModuleMenu).ToString();
            if (string.IsNullOrWhiteSpace(menuContents))
                return MvcHtmlString.Empty;// we don't have a menu

            // <div class= >
            TagBuilder divTag = new TagBuilder("div");
            divTag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModuleMenuEditIcon));
            divTag.Attributes.Add("style", "display:none");
            hb.Append(divTag.ToString(TagRenderMode.StartTag));

            SkinImages skinImages = new SkinImages();
            string imageUrl = skinImages.FindIcon_Package("#ModuleMenu", Package.GetCurrentPackage(this));
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

            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.popup.min.js");
            Manager.ScriptManager.AddKendoUICoreJsFile("kendo.menu.min.js");

            Manager.AddOnManager.AddAddOn("YetaWF", "Core", "kendoMenu");
            Manager.AddOnManager.AddAddOn("YetaWF", "Core", "Modules");// various module support
            Manager.AddOnManager.AddAddOnGlobal("jquery.com", "jquery-color");// for color change when entering module edit menu

            return MvcHtmlString.Create(hb.ToString());
        }

        public MvcHtmlString RenderModuleLinks() {

            HtmlBuilder hb = new HtmlBuilder();

            MenuList moduleMenu = GetModuleMenuList(ModuleAction.RenderModeEnum.NormalLinks, ModuleAction.ActionLocationEnum.ModuleLinks);

            string menuContents = moduleMenu.Render(null, null, Globals.CssModuleLinks).ToString();
            if (string.IsNullOrWhiteSpace(menuContents))
                return MvcHtmlString.Empty;// we don't have a menu

            // <div>
            TagBuilder div2Tag = new TagBuilder("div");
            div2Tag.AddCssClass(Manager.AddOnManager.CheckInvokedCssModule(Globals.CssModuleLinksContainer));
            hb.Append(div2Tag.ToString(TagRenderMode.StartTag));

            // <ul><li> menu
            hb.Append(menuContents);

            // </div>
            hb.Append(div2Tag.ToString(TagRenderMode.EndTag));

            return MvcHtmlString.Create(hb.ToString());
        }

        private MenuList GetMoveToOtherPanes(PageDefinition page, ModuleDefinition modServices) {

            MenuList menu = new MenuList();
            foreach (var pane in page.Panes) {
                ModuleAction action = modServices.GetModuleAction("MoveToPane", page, this, Manager.PaneRendered, pane);
                if (action != null)
                    menu.Add(action);
            }
            return menu;
        }

        private MenuList GetMoveWithinPane(PageDefinition page, ModuleDefinition modServices) {

            MenuList menu = new MenuList();

            ModuleAction action;
            string pane = Manager.PaneRendered;
            action = modServices.GetModuleAction("MoveUp", page, this, pane);
            if (action != null)
                menu.Add(action);

            action = modServices.GetModuleAction("MoveDown", page, this, pane);
            if (action != null)
                menu.Add(action);

            action = modServices.GetModuleAction("MoveTop", page, this, pane);
            if (action != null)
                menu.Add(action);

            action = modServices.GetModuleAction("MoveBottom", page, this, pane);
            if (action != null)
                menu.Add(action);

            return menu;
        }
    }
}
